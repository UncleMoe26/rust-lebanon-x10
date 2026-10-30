using System.Linq;

namespace Oxide.Plugins
{
    [Info("TunnelDwellerCap", "Uncle Moe", "1.0.0")]
    [Description("Caps the number of tunnel dweller NPCs to save server CPU")]
    class TunnelDwellerCap : RustPlugin
    {
        const int MaxDwellers = 40;
        const string Prefab = "npc_tunneldweller";

        int Count() => BaseNetworkable.serverEntities.OfType<global::HumanNPC>().Count(n => n.ShortPrefabName == Prefab && !n.IsDestroyed);

        void OnServerInitialized()
        {
            var extra = BaseNetworkable.serverEntities.OfType<global::HumanNPC>().Where(n => n.ShortPrefabName == Prefab).Skip(MaxDwellers).ToList();
            foreach (var n in extra) n.Kill();
            Puts($"Removed {extra.Count} tunnel dwellers over the cap of {MaxDwellers}");
        }

        void OnEntitySpawned(global::HumanNPC npc)
        {
            if (npc == null || npc.ShortPrefabName != Prefab) return;
            NextTick(() =>
            {
                if (npc == null || npc.IsDestroyed) return;
                if (Count() > MaxDwellers) npc.Kill();
            });
        }
    }
}
