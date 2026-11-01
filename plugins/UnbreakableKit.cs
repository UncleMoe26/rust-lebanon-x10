using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Unbreakable Kit", "Uncle Moe", "1.0.0")]
    [Description("Items from the chosen kits never lose condition while their owner has that kit's permission")]
    class UnbreakableKit : RustPlugin
    {
        class PluginConfig
        {
            [JsonProperty("Kits whose items never break")]
            public List<string> Kits = new List<string> { "admin" };
        }

        PluginConfig config;
        // "shortname:skin" -> permission the owner needs
        readonly Dictionary<string, string> unbreakable = new Dictionary<string, string>();

        protected override void LoadDefaultConfig() => config = new PluginConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>() ?? new PluginConfig();
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        void OnServerInitialized() => LoadKits();

        // Read the item list straight from the Kits plugin's data, so editing the kit updates this too
        void LoadKits()
        {
            unbreakable.Clear();
            var data = Interface.Oxide.DataFileSystem.ReadObject<JObject>("Kits/kits_data");
            var kits = data?["_kits"] as JObject;
            if (kits == null) { PrintWarning("Kits data not found"); return; }
            foreach (string name in config.Kits)
            {
                var kit = kits[name] as JObject;
                if (kit == null) { PrintWarning($"Kit '{name}' not found"); continue; }
                string perm = (string)kit["RequiredPermission"] ?? "";
                foreach (string section in new[] { "MainItems", "WearItems", "BeltItems" })
                    foreach (var item in kit[section] as JArray ?? new JArray())
                    {
                        // Ammo, explosives and meds are used up anyway; only gear with condition matters
                        var def = ItemManager.FindItemDefinition((string)item["Shortname"]);
                        if (def == null || !def.condition.enabled) continue;
                        unbreakable[$"{def.shortname}:{(ulong)item["Skin"]}"] = perm;
                    }
            }
            Puts($"{unbreakable.Count} unbreakable items: {string.Join(", ", unbreakable.Keys)}");
        }

        bool IsUnbreakable(Item item)
        {
            if (item?.info == null) return false;
            string perm;
            if (!unbreakable.TryGetValue($"{item.info.shortname}:{item.skin}", out perm)) return false;
            BasePlayer owner = item.GetOwnerPlayer();
            return owner != null && (perm == "" || permission.UserHasPermission(owner.UserIDString, perm));
        }

        void OnLoseCondition(Item item, ref float amount)
        {
            if (!IsUnbreakable(item)) return;
            amount = 0f;
            // Also heal gear that was damaged before this plugin was loaded
            if (item.condition < item.maxCondition) item.condition = item.maxCondition;
        }

        [ConsoleCommand("unbreakablekit.reload")]
        void CmdReload(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) return;
            LoadKits();
        }
    }
}
