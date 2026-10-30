using System.Collections.Generic;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Welcome", "Uncle Moe", "1.4.0")]
    [Description("Greets players, shows a welcome popup, reminds about kits and posts the online count")]
    class Welcome : RustPlugin
    {
        const string Message =
            "<color=#e0b030>Welcome to Rust Lebanon x10!</color> x10 gather, fast craft, fast smelt, boosted loot. <color=#ffcc00>Wipe every Thursday 21:00 Lebanon time.</color>\n" +
            "<color=#9fd0ff>/kit</color> starter kit  <color=#9fd0ff>/sethome</color> <color=#9fd0ff>/home</color> <color=#9fd0ff>/tpr</color> teleport  <color=#9fd0ff>/backpack</color>  <color=#9fd0ff>/remove</color>  <color=#9fd0ff>/grade hqm</color>\n" +
            "Storm every hour. Have fun and invite your friends!";

        const string KitReminder =
            "<color=#ffcc00>FREE KITS!</color> Type <color=#9fd0ff>/kit</color> to open the kit menu. The starter kit can be claimed every 20 minutes.";

        const string PopupText =
            "<color=#e0b030>SERVER</color>\n" +
            "x10 gather  •  fast craft (3s max)  •  fast smelt and recycle  •  boosted loot  •  stacks x20\n" +
            "No NPCs or animals, pure PvP  •  storm every hour\n" +
            "<color=#ffcc00>Weekly wipe: every Thursday 21:00 Lebanon time</color>\n\n" +
            "<color=#e0b030>COMMANDS</color>\n" +
            "<color=#9fd0ff>/kit</color>  free kits (starter kit every 20 minutes)\n" +
            "<color=#9fd0ff>/sethome name</color>  then  <color=#9fd0ff>/home name</color>  teleport home (3 homes)\n" +
            "<color=#9fd0ff>/tpr player</color>  teleport to a friend (they accept with <color=#9fd0ff>/tpa</color>)\n" +
            "<color=#9fd0ff>/outpost</color>  <color=#9fd0ff>/bandit</color>  teleport to the safe zones\n" +
            "<color=#9fd0ff>/backpack</color>  extra storage    <color=#9fd0ff>/remove</color>  remove your own building parts\n" +
            "<color=#9fd0ff>/grade hqm</color>  auto upgrade while you build\n\n" +
            "<color=#e0b030>RULES</color>\n" +
            "No cheating, no exploiting, no racism. Have fun and invite your friends!";

        // Seconds between online count announcements
        const float OnlineInterval = 600f;
        // Seconds between kit reminders (first one is offset so it doesn't land on the online count)
        const float KitInterval = 900f;
        // The owner doesn't get the popup on join (set to your SteamID64)
        const ulong OwnerId = 0;
        const string PopupName = "WelcomePopup";
        // Popup closes by itself after this long
        const float PopupSeconds = 60f;

        readonly HashSet<ulong> greeted = new HashSet<ulong>();
        // Every player that has ever joined, so first-timers can be announced
        HashSet<ulong> seen;

        void OnServerInitialized()
        {
            seen = Interface.Oxide.DataFileSystem.ReadObject<HashSet<ulong>>("Welcome_Seen") ?? new HashSet<ulong>();
            // Anyone already on the map (awake or sleeping) has played before
            foreach (var p in BasePlayer.allPlayerList)
                if (p != null && !p.IsNpc && p.userID.IsSteamId()) seen.Add(p.userID);
            SaveSeen();
            timer.Every(OnlineInterval, AnnounceOnline);
            timer.Once(KitInterval / 2f, () =>
            {
                AnnounceKits();
                timer.Every(KitInterval, AnnounceKits);
            });
        }

        void Unload()
        {
            foreach (var p in BasePlayer.activePlayerList) CuiHelper.DestroyUi(p, PopupName);
        }

        void SaveSeen() => Interface.Oxide.DataFileSystem.WriteObject("Welcome_Seen", seen);

        void OnPlayerConnected(BasePlayer player)
        {
            if (player == null || !seen.Add(player.userID)) return;
            SaveSeen();
            PrintToChat($"<color=#e0b030>Everyone welcome</color> <color=#9fd0ff>{player.displayName}</color> <color=#e0b030>to Rust Lebanon x10!</color> First time on the server.");
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (player == null || player.IsNpc) return;
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot)) return;
            if (!greeted.Add(player.userID)) return;
            player.ChatMessage(Message);
            if (player.userID != OwnerId) ShowPopup(player);
        }

        void OnPlayerDisconnected(BasePlayer player) => greeted.Remove(player.userID);

        void AnnounceOnline()
        {
            var online = BasePlayer.activePlayerList.Count;
            if (online == 0) return;
            PrintToChat($"<color=#e0b030>Players online:</color> {online}/{ConVar.Server.maxplayers}. Invite your friends!");
        }

        void AnnounceKits()
        {
            if (BasePlayer.activePlayerList.Count == 0) return;
            PrintToChat(KitReminder);
        }

        void ShowPopup(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PopupName);
            string name = player.displayName.Replace("<", "").Replace(">", "");
            var c = new CuiElementContainer();
            c.Add(new CuiPanel
            {
                Image = { Color = "0.05 0.05 0.05 0.92" },
                RectTransform = { AnchorMin = "0.26 0.16", AnchorMax = "0.74 0.84" },
                CursorEnabled = true
            }, "Overlay", PopupName);
            c.Add(new CuiPanel
            {
                Image = { Color = "0.88 0.69 0.19 1" },
                RectTransform = { AnchorMin = "0 0.988", AnchorMax = "1 1" }
            }, PopupName);
            c.Add(new CuiLabel
            {
                Text = { Text = "WELCOME TO RUST LEBANON x10", FontSize = 26, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, Color = "0.88 0.69 0.19 1" },
                RectTransform = { AnchorMin = "0.03 0.87", AnchorMax = "0.97 0.97" }
            }, PopupName);
            c.Add(new CuiLabel
            {
                Text = { Text = $"Hey {name}, glad to have you here!", FontSize = 15, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.8" },
                RectTransform = { AnchorMin = "0.03 0.81", AnchorMax = "0.97 0.87" }
            }, PopupName);
            c.Add(new CuiLabel
            {
                Text = { Text = PopupText, FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 0.95" },
                RectTransform = { AnchorMin = "0.06 0.15", AnchorMax = "0.94 0.79" }
            }, PopupName);
            c.Add(new CuiButton
            {
                Button = { Command = "welcome.close", Color = "0.30 0.55 0.18 1" },
                RectTransform = { AnchorMin = "0.38 0.04", AnchorMax = "0.62 0.11" },
                Text = { Text = "LET'S PLAY", FontSize = 18, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
            }, PopupName);
            CuiHelper.AddUi(player, c);
            timer.Once(PopupSeconds, () =>
            {
                if (player != null && player.IsConnected) CuiHelper.DestroyUi(player, PopupName);
            });
        }

        [ConsoleCommand("welcome.close")]
        void CmdClose(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null) CuiHelper.DestroyUi(player, PopupName);
        }

        // Lets an admin see the popup that new players get
        [ChatCommand("welcomepreview")]
        void CmdPreview(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            ShowPopup(player);
        }
    }
}
