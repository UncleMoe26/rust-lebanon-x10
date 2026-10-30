using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Auto Grade", "Uncle Moe", "1.0.0")]
    [Description("/grade wood|stone|metal|hqm|off - new building blocks are placed at that grade, paid from the player's inventory")]
    public class AutoGrade : RustPlugin
    {
        private const string PermUse = "autograde.use";

        private static readonly Dictionary<string, BuildingGrade.Enum> Grades = new Dictionary<string, BuildingGrade.Enum>
        {
            ["wood"] = BuildingGrade.Enum.Wood,
            ["stone"] = BuildingGrade.Enum.Stone,
            ["metal"] = BuildingGrade.Enum.Metal,
            ["hqm"] = BuildingGrade.Enum.TopTier,
        };

        private readonly Dictionary<ulong, BuildingGrade.Enum> chosenGrade = new Dictionary<ulong, BuildingGrade.Enum>();
        private readonly Dictionary<ulong, float> lastWarning = new Dictionary<ulong, float>();

        private void Init()
        {
            permission.RegisterPermission(PermUse, this);
        }

        [ChatCommand("grade")]
        private void CmdGrade(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermUse))
            {
                SendReply(player, "You don't have permission to use this command.");
                return;
            }

            BuildingGrade.Enum current;
            if (args.Length == 0)
            {
                string now = chosenGrade.TryGetValue(player.userID, out current) ? current.ToString() : "off (twig)";
                SendReply(player, $"Auto grade: {now}. Use /grade wood, /grade stone, /grade metal, /grade hqm or /grade off.");
                return;
            }

            string choice = args[0].ToLower();
            if (choice == "off" || choice == "twig")
            {
                chosenGrade.Remove(player.userID);
                SendReply(player, "Auto grade off. New blocks are placed as twig.");
                return;
            }

            BuildingGrade.Enum grade;
            if (!Grades.TryGetValue(choice, out grade))
            {
                SendReply(player, "Unknown grade. Use wood, stone, metal, hqm or off.");
                return;
            }

            chosenGrade[player.userID] = grade;
            SendReply(player, $"Auto grade set to {choice}. New blocks are upgraded right away, paid from your inventory.");
        }

        private void OnEntityBuilt(Planner planner, GameObject go)
        {
            BasePlayer player = planner?.GetOwnerPlayer();
            BuildingBlock block = go?.ToBaseEntity() as BuildingBlock;
            if (player == null || block == null) return;

            BuildingGrade.Enum grade;
            if (!chosenGrade.TryGetValue(player.userID, out grade)) return;
            if (!permission.UserHasPermission(player.UserIDString, PermUse)) return;

            NextTick(() => Upgrade(player, block, grade));
        }

        private void Upgrade(BasePlayer player, BuildingBlock block, BuildingGrade.Enum grade)
        {
            if (block == null || block.IsDestroyed || player == null) return;

            if (!block.CanChangeToGrade(grade, 0, player) || !block.CanAffordUpgrade(grade, 0, player))
            {
                // Don't spam the player on every block they place
                float last;
                if (!lastWarning.TryGetValue(player.userID, out last) || Time.realtimeSinceStartup - last > 10f)
                {
                    SendReply(player, $"Not enough resources to place this as {grade}, it stays twig.");
                    lastWarning[player.userID] = Time.realtimeSinceStartup;
                }
                return;
            }

            block.ChangeGrade(grade, true, true);
            block.PayForUpgrade(block.currentGrade, player);
            block.SetHealthToMax();
            block.SendNetworkUpdate();
        }
    }
}
