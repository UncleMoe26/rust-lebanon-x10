using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Stack Sizes", "Uncle Moe", "1.0.2")]
    [Description("Resources stack to 10000, weapons to 1, everything else that stacks x20")]
    public class StackSizes : RustPlugin
    {
        private const int ResourceStack = 10000;
        private const int OtherMultiplier = 20;
        private readonly Dictionary<ItemDefinition, int> originalStacks = new Dictionary<ItemDefinition, int>();

        private void OnServerInitialized()
        {
            int resources = 0, weapons = 0, others = 0;
            foreach (ItemDefinition definition in ItemManager.itemList)
            {
                int newStack;
                if (definition.category == ItemCategory.Resources)
                {
                    // Never shrink a stack: water and other liquids stack far above 10000 in vanilla,
                    // and tanks and catchers can only fill up to the stack size
                    newStack = System.Math.Max(ResourceStack, definition.stackable);
                    resources++;
                }
                else if (definition.category == ItemCategory.Weapon)
                {
                    newStack = 1;
                    weapons++;
                }
                else if (definition.stackable > 1)
                {
                    newStack = definition.stackable * OtherMultiplier;
                    others++;
                }
                else continue; // armor, clothing, tools etc. don't stack in vanilla and stay that way

                originalStacks[definition] = definition.stackable;
                definition.stackable = newStack;
            }
            Puts($"Stack sizes set: {resources} resources -> {ResourceStack}, {weapons} weapons -> 1, {others} other items x{OtherMultiplier}");
            ItemDefinition water = ItemManager.FindItemDefinition("water");
            if (water != null) Puts($"Water stacks to {water.stackable}");
        }

        private void Unload()
        {
            foreach (KeyValuePair<ItemDefinition, int> pair in originalStacks)
                pair.Key.stackable = pair.Value;
        }
    }
}
