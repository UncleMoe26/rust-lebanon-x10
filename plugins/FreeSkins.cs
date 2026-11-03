using System.Collections.Generic;
using System.Linq;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Free Skins", "Uncle Moe", "1.0.0")]
    [Description("/skin: pick any approved workshop skin for the item in your hand, free, from a click window")]
    class FreeSkins : RustPlugin
    {
        const string Ui = "FreeSkinsUI";
        const int PerPage = 24;
        // shortname -> approved workshop skin ids
        readonly Dictionary<string, List<ulong>> skins = new Dictionary<string, List<ulong>>();

        void OnServerInitialized()
        {
            foreach (var skin in Rust.Workshop.Approved.All.Values)
            {
                if (skin?.Skinnable == null || string.IsNullOrEmpty(skin.Skinnable.ItemName)) continue;
                List<ulong> list;
                if (!skins.TryGetValue(skin.Skinnable.ItemName, out list)) skins[skin.Skinnable.ItemName] = list = new List<ulong>();
                list.Add(skin.WorkshopdId);
            }
            Puts($"{skins.Values.Sum(l => l.Count)} approved skins for {skins.Count} items");
        }

        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, Ui);
        }

        [ChatCommand("skin")]
        void CmdSkin(BasePlayer player, string command, string[] args) => Open(player, 0);

        [ConsoleCommand("freeskins.page")]
        void CcPage(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player != null) Open(player, arg.GetInt(0));
        }

        [ConsoleCommand("freeskins.close")]
        void CcClose(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player != null) CuiHelper.DestroyUi(player, Ui);
        }

        [ConsoleCommand("freeskins.apply")]
        void CcApply(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            Item item = player.GetActiveItem();
            ulong skin = arg.GetULong(0);
            List<ulong> list;
            if (item == null || (skin != 0 && (!skins.TryGetValue(item.info.shortname, out list) || !list.Contains(skin)))) return;
            item.skin = skin;
            BaseEntity held = item.GetHeldEntity();
            if (held != null)
            {
                held.skinID = skin;
                held.SendNetworkUpdate();
            }
            item.MarkDirty();
            Open(player, arg.GetInt(1));
        }

        void Open(BasePlayer player, int page)
        {
            CuiHelper.DestroyUi(player, Ui);
            Item item = player.GetActiveItem();
            List<ulong> list;
            if (item == null || !skins.TryGetValue(item.info.shortname, out list) || list.Count == 0)
            {
                SendReply(player, item == null ? "Hold the item you want to skin (a weapon, tool or deployable in your hotbar), then type <color=#ffcc00>/skin</color>." : $"There are no approved skins for {item.info.displayName.english}.");
                return;
            }
            int pages = (list.Count + PerPage - 1) / PerPage;
            page = Mathf.Clamp(page, 0, pages - 1);

            var ui = new CuiElementContainer();
            ui.Add(new CuiPanel { Image = { Color = "0.08 0.07 0.06 0.96" }, RectTransform = { AnchorMin = "0.2 0.15", AnchorMax = "0.8 0.87" }, CursorEnabled = true }, "Overlay", Ui);
            ui.Add(new CuiLabel
            {
                Text = { Text = $"<color=#e0b030>FREE SKINS</color>  {item.info.displayName.english}  <color=#a39886>({list.Count} skins, page {page + 1}/{pages})</color>", FontSize = 16, Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = "0.02 0.92", AnchorMax = "0.75 0.99" },
            }, Ui);
            ui.Add(new CuiButton { Button = { Command = "freeskins.close", Color = "0.7 0.2 0.15 1" }, Text = { Text = "CLOSE", FontSize = 13, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0.88 0.93", AnchorMax = "0.98 0.985" } }, Ui);
            ui.Add(new CuiButton { Button = { Command = $"freeskins.apply 0 {page}", Color = "0.25 0.25 0.25 1" }, Text = { Text = "DEFAULT", FontSize = 12, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0.76 0.93", AnchorMax = "0.87 0.985" } }, Ui);

            const int cols = 6;
            var shown = list.Skip(page * PerPage).Take(PerPage).ToList();
            for (int n = 0; n < shown.Count; n++)
            {
                int row = n / cols, col = n % cols;
                float x0 = 0.02f + col * 0.16f, x1 = x0 + 0.15f, y1 = 0.9f - row * 0.205f, y0 = y1 - 0.19f;
                ui.Add(new CuiButton
                {
                    Button = { Command = $"freeskins.apply {shown[n]} {page}", Color = item.skin == shown[n] ? "0.8 0.38 0.17 1" : "0.18 0.16 0.14 1" },
                    Text = { Text = "" },
                    RectTransform = { AnchorMin = $"{x0} {y0}", AnchorMax = $"{x1} {y1}" },
                }, Ui, $"{Ui}.s{n}");
                ui.Add(new CuiElement
                {
                    Parent = $"{Ui}.s{n}",
                    Components = { new CuiImageComponent { ItemId = item.info.itemid, SkinId = shown[n] }, new CuiRectTransformComponent { AnchorMin = "0.12 0.08", AnchorMax = "0.88 0.92" } },
                });
            }
            if (page > 0) ui.Add(new CuiButton { Button = { Command = $"freeskins.page {page - 1}", Color = "0.3 0.3 0.3 1" }, Text = { Text = "< PREV", FontSize = 12, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0.02 0.015", AnchorMax = "0.15 0.065" } }, Ui);
            if (page < pages - 1) ui.Add(new CuiButton { Button = { Command = $"freeskins.page {page + 1}", Color = "0.3 0.3 0.3 1" }, Text = { Text = "NEXT >", FontSize = 12, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0.85 0.015", AnchorMax = "0.98 0.065" } }, Ui);
            CuiHelper.AddUi(player, ui);
        }
    }
}
