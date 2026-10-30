using System.Collections.Generic;
using System.Linq;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Loot Boost", "Uncle Moe", "1.1.0")]
    [Description("Removes junk from barrels and crates, triples scrap and adds useful loot")]
    public class LootBoost : RustPlugin
    {
        private class Drop
        {
            public string Shortname;
            public int Min, Max, Weight;
            public Drop(string shortname, int min, int max, int weight) { Shortname = shortname; Min = min; Max = max; Weight = weight; }
        }

        private class LootTable
        {
            public int Rolls;
            public int ScrapMultiplier;
            // Scrap added on top of whatever vanilla rolled
            public int BonusScrapMin, BonusScrapMax;
            public Drop[] Drops;
        }

        // Containers already boosted, kept across restarts so saved loot isn't boosted twice
        private HashSet<ulong> boostedIds = new HashSet<ulong>();

        private static readonly HashSet<string> Junk = new HashSet<string>
        {
            "rope", "tarp", "sewingkit", "bleach", "ducttape", "glue", "sticks", "propanetank"
        };

        private static readonly LootTable Barrel = new LootTable
        {
            Rolls = 2, ScrapMultiplier = 3, BonusScrapMin = 10, BonusScrapMax = 25,
            Drops = new[]
            {
                new Drop("ammo.pistol", 20, 40, 10), new Drop("ammo.rifle", 15, 30, 8), new Drop("ammo.shotgun", 8, 16, 6),
                new Drop("syringe.medical", 1, 2, 8), new Drop("bandage", 2, 4, 6),
                new Drop("metal.fragments", 150, 400, 10), new Drop("gunpowder", 40, 100, 8), new Drop("lowgradefuel", 20, 50, 8),
                new Drop("scrap", 15, 40, 10), new Drop("techparts", 1, 2, 5), new Drop("metalpipe", 1, 3, 5),
                new Drop("metalspring", 1, 2, 5), new Drop("gears", 1, 3, 5),
                new Drop("semibody", 1, 1, 3), new Drop("smgbody", 1, 1, 2), new Drop("riflebody", 1, 1, 1),
            }
        };

        // Brown crates, tool boxes and basic underwater crates
        private static readonly LootTable Crate = new LootTable
        {
            Rolls = 2, ScrapMultiplier = 3, BonusScrapMin = 25, BonusScrapMax = 60,
            Drops = new[]
            {
                new Drop("techparts", 2, 4, 8), new Drop("gears", 2, 4, 8), new Drop("metalpipe", 2, 4, 7),
                new Drop("metalspring", 1, 3, 7), new Drop("sheetmetal", 1, 3, 6), new Drop("roadsigns", 1, 3, 6),
                new Drop("metalblade", 2, 4, 5), new Drop("semibody", 1, 1, 5), new Drop("smgbody", 1, 1, 4), new Drop("riflebody", 1, 1, 2),
                new Drop("ammo.pistol", 30, 60, 8), new Drop("ammo.rifle", 20, 40, 6),
                new Drop("weapon.mod.holosight", 1, 1, 2), new Drop("weapon.mod.flashlight", 1, 1, 2),
                new Drop("syringe.medical", 1, 3, 6), new Drop("metal.fragments", 200, 500, 8),
            }
        };

        private static readonly LootTable Military = new LootTable
        {
            Rolls = 3, ScrapMultiplier = 3, BonusScrapMin = 50, BonusScrapMax = 100,
            Drops = new[]
            {
                new Drop("rifle.ak", 1, 1, 3), new Drop("rifle.lr300", 1, 1, 2), new Drop("rifle.bolt", 1, 1, 2),
                new Drop("rifle.semiauto", 1, 1, 6), new Drop("smg.mp5", 1, 1, 5), new Drop("smg.thompson", 1, 1, 6),
                new Drop("pistol.python", 1, 1, 5), new Drop("shotgun.pump", 1, 1, 5),
                new Drop("ammo.rifle", 60, 128, 10), new Drop("ammo.pistol", 60, 128, 8), new Drop("ammo.shotgun", 20, 40, 6),
                new Drop("syringe.medical", 3, 5, 8), new Drop("largemedkit", 1, 2, 5),
                new Drop("explosive.satchel", 1, 2, 4), new Drop("grenade.f1", 2, 4, 5), new Drop("explosives", 10, 20, 4),
                new Drop("weapon.mod.holosight", 1, 1, 4), new Drop("weapon.mod.silencer", 1, 1, 4), new Drop("weapon.mod.flashlight", 1, 1, 3),
                new Drop("metal.facemask", 1, 1, 3), new Drop("metal.plate.torso", 1, 1, 3),
                new Drop("roadsign.jacket", 1, 1, 5), new Drop("roadsign.kilt", 1, 1, 5), new Drop("coffeecan.helmet", 1, 1, 5),
            }
        };

        private static readonly LootTable Elite = new LootTable
        {
            Rolls = 3, ScrapMultiplier = 3, BonusScrapMin = 100, BonusScrapMax = 200,
            Drops = new[]
            {
                new Drop("rifle.ak", 1, 1, 6), new Drop("rifle.lr300", 1, 1, 5), new Drop("rifle.bolt", 1, 1, 4),
                new Drop("explosive.timed", 1, 1, 3), new Drop("ammo.rocket.basic", 1, 2, 3), new Drop("explosives", 20, 40, 5),
                new Drop("ammo.rifle", 100, 128, 10), new Drop("largemedkit", 1, 2, 5),
                new Drop("metal.facemask", 1, 1, 5), new Drop("metal.plate.torso", 1, 1, 5),
            }
        };

        private void OnServerInitialized()
        {
            bool firstRun = !Interface.Oxide.DataFileSystem.ExistsDatafile("LootBoost_Boosted");
            boostedIds = Interface.Oxide.DataFileSystem.ReadObject<HashSet<ulong>>("LootBoost_Boosted") ?? new HashSet<ulong>();
            var containers = BaseNetworkable.serverEntities.OfType<LootContainer>().Where(c => GetTable(c) != null).ToList();
            // v1.0 already boosted barrels and military/elite crates without tracking them
            if (firstRun)
                foreach (LootContainer c in containers)
                    if (GetTable(c) != Crate && c.ShortPrefabName != "crate_underwater_advanced") boostedIds.Add(c.net.ID.Value);
            // Forget containers that no longer exist
            var alive = new HashSet<ulong>(containers.Select(c => c.net.ID.Value));
            boostedIds.RemoveWhere(id => !alive.Contains(id));
            // Containers that exist but were never boosted get boosted once; new ones are handled in OnLootSpawn
            int boosted = 0;
            foreach (LootContainer container in containers)
            {
                if (boostedIds.Contains(container.net.ID.Value)) continue;
                Boost(container);
                boosted++;
            }
            SaveBoosted();
            Puts($"Boosted {boosted} existing barrels and crates ({boostedIds.Count} tracked)");
        }

        private void OnServerSave() => SaveBoosted();

        private void Unload() => SaveBoosted();

        private void SaveBoosted() => Interface.Oxide.DataFileSystem.WriteObject("LootBoost_Boosted", boostedIds);

        private void OnEntityKill(LootContainer container)
        {
            if (container?.net != null) boostedIds.Remove(container.net.ID.Value);
        }

        private void OnLootSpawn(LootContainer container)
        {
            if (GetTable(container) == null) return;
            // Let vanilla fill the container first, then fix it up
            NextTick(() => Boost(container));
        }

        private static LootTable GetTable(LootContainer container)
        {
            string name = container.ShortPrefabName;
            if (name.Contains("barrel") && !name.Contains("oil")) return Barrel;
            if (name == "crate_normal" || name == "crate_normal_2" || name == "crate_tools" || name == "crate_underwater_basic") return Crate;
            if (name == "crate_military" || name == "crate_underwater_advanced") return Military;
            if (name == "crate_elite") return Elite;
            return null;
        }

        private void Boost(LootContainer container)
        {
            if (container == null || container.IsDestroyed || container.inventory == null || container.net == null) return;
            LootTable table = GetTable(container);
            if (table == null) return;
            boostedIds.Add(container.net.ID.Value);

            foreach (Item item in container.inventory.itemList.ToList())
            {
                if (Junk.Contains(item.info.shortname))
                {
                    item.RemoveFromContainer();
                    item.Remove();
                }
                else if (item.info.shortname == "scrap")
                {
                    item.amount *= table.ScrapMultiplier;
                    item.MarkDirty();
                }
            }

            for (int i = 0; i < table.Rolls; i++)
            {
                Drop drop = Pick(table.Drops);
                AddItem(container, drop.Shortname, UnityEngine.Random.Range(drop.Min, drop.Max + 1));
            }
            AddItem(container, "scrap", UnityEngine.Random.Range(table.BonusScrapMin, table.BonusScrapMax + 1));
        }

        private static void AddItem(LootContainer container, string shortname, int amount)
        {
            Item item = ItemManager.CreateByName(shortname, amount);
            if (item == null) return;
            if (!item.MoveToContainer(container.inventory))
            {
                container.inventory.capacity++;
                if (!item.MoveToContainer(container.inventory)) item.Remove();
            }
        }

        private static Drop Pick(Drop[] drops)
        {
            int roll = UnityEngine.Random.Range(0, drops.Sum(d => d.Weight));
            foreach (Drop drop in drops)
            {
                roll -= drop.Weight;
                if (roll < 0) return drop;
            }
            return drops[drops.Length - 1];
        }
    }
}
