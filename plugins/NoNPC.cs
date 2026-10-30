using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("NoNPC", "Claude", "1.0.0")]
    [Description("Removes hostile NPCs and wild animals to save server CPU. Keeps safe-zone guards, shopkeepers, turrets and horses.")]
    class NoNPC : RustPlugin
    {
        // Animal populations set to 0 so they stop spawning (horses kept)
        static readonly string[] PopulationVars = {
            "bear.population", "polarbear.population", "boar.population", "stag.population",
            "wolf.population", "wolf2.population", "chicken.population", "halloween.scarecrowpopulation",
        };

        void OnServerInitialized()
        {
            foreach (var v in PopulationVars)
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), v, 0);

            var removed = new Dictionary<string, int>();
            foreach (var e in BaseNetworkable.serverEntities.OfType<BaseCombatEntity>().ToList())
            {
                if (!ShouldRemove(e)) continue;
                var name = e.GetType().Name;
                removed[name] = removed.TryGetValue(name, out var c) ? c + 1 : 1;
                e.Kill();
            }
            foreach (var kv in removed.OrderByDescending(x => x.Value))
                Puts($"Removed {kv.Value} {kv.Key}");
        }

        void OnEntitySpawned(BaseCombatEntity entity)
        {
            if (!ShouldRemove(entity)) return;
            NextTick(() => { if (entity != null && !entity.IsDestroyed) entity.Kill(); });
        }

        static bool ShouldRemove(BaseCombatEntity e)
        {
            if (e == null || e is BanditGuard || e is NPCShopKeeper || e is BaseVehicle) return false; // horses are vehicles
            if (e is HumanNPC || e is ScarecrowNPC || e is BaseNpc) return true;
            // BaseNPC2 (new AI scientists/animals) isn't reachable from plugins, so match by type name
            for (var t = e.GetType(); t != null; t = t.BaseType)
                if (t.Name == "BaseNPC2" || t.Name.Contains("Animal")) return true;
            return false;
        }
    }
}
