using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Fast Crafting", "Uncle Moe", "1.0.0")]
    [Description("Caps crafting time at 3 seconds for every item")]
    public class FastCrafting : RustPlugin
    {
        private const float MaxCraftSeconds = 3f;
        private readonly Dictionary<ItemBlueprint, float> originalTimes = new Dictionary<ItemBlueprint, float>();

        private void OnServerInitialized()
        {
            foreach (ItemBlueprint blueprint in ItemManager.bpList)
            {
                // Items that are already faster stay as they are
                if (blueprint.time <= MaxCraftSeconds) continue;
                originalTimes[blueprint] = blueprint.time;
                blueprint.time = MaxCraftSeconds;
            }
            Puts($"Crafting time capped at {MaxCraftSeconds}s for {originalTimes.Count} items");
        }

        private void Unload()
        {
            foreach (KeyValuePair<ItemBlueprint, float> pair in originalTimes)
                pair.Key.time = pair.Value;
        }
    }
}
