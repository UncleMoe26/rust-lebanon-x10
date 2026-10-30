using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Fast Smelt", "Uncle Moe", "1.0.0")]
    [Description("Smelts ores faster by shortening their cook time; uses Rust's own furnace logic, so no extra load")]
    public class FastSmelt : RustPlugin
    {
        private const float SpeedMultiplier = 5f;
        private static readonly string[] Ores = { "metal.ore", "sulfur.ore", "hq.metal.ore" };
        private readonly Dictionary<ItemModCookable, float> originalTimes = new Dictionary<ItemModCookable, float>();

        private void OnServerInitialized()
        {
            foreach (string shortname in Ores)
            {
                ItemDefinition definition = ItemManager.FindItemDefinition(shortname);
                ItemModCookable cookable = definition?.GetComponent<ItemModCookable>();
                if (cookable == null)
                {
                    PrintWarning($"{shortname} is not smeltable, skipped");
                    continue;
                }
                originalTimes[cookable] = cookable.cookTime;
                cookable.cookTime /= SpeedMultiplier;
                Puts($"{shortname}: {originalTimes[cookable]}s -> {cookable.cookTime}s per item");
            }
        }

        private void Unload()
        {
            foreach (KeyValuePair<ItemModCookable, float> pair in originalTimes)
                pair.Key.cookTime = pair.Value;
        }
    }
}
