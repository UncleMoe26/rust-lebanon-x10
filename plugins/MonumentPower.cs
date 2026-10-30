using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Monument Power", "Uncle Moe", "1.0.0")]
    [Description("Keeps monument lights on and fuse boxes filled, so puzzles need no fuses")]
    public class MonumentPower : RustPlugin
    {
        private const float CheckSeconds = 30f;
        private readonly List<ItemBasedFlowRestrictor> fuseBoxes = new List<ItemBasedFlowRestrictor>();
        private readonly List<ElectricSwitch> lightSwitches = new List<ElectricSwitch>();

        private void OnServerInitialized()
        {
            // Monument entities are static, so collect them once
            foreach (IOEntity entity in BaseNetworkable.serverEntities.OfType<IOEntity>().Where(e => e.OwnerID == 0))
            {
                if (entity is ItemBasedFlowRestrictor) fuseBoxes.Add((ItemBasedFlowRestrictor)entity);
                else if (entity is ElectricSwitch && entity.ShortPrefabName == "simpleswitch_lightswitch") lightSwitches.Add((ElectricSwitch)entity);
            }
            Puts($"Managing {fuseBoxes.Count} fuse boxes and {lightSwitches.Count} light switches");
            Apply();
            timer.Every(CheckSeconds, Apply);
        }

        private void Unload()
        {
            foreach (ItemBasedFlowRestrictor box in fuseBoxes)
                if (box != null && !box.IsDestroyed) box.inventory?.SetLocked(false);
        }

        private void Apply()
        {
            int fusesAdded = 0, lightsOn = 0;

            foreach (ItemBasedFlowRestrictor box in fuseBoxes)
                if (box != null && !box.IsDestroyed && FillFuseBox(box)) fusesAdded++;

            foreach (ElectricSwitch lightSwitch in lightSwitches)
            {
                if (lightSwitch == null || lightSwitch.IsDestroyed || lightSwitch.IsOn()) continue;
                lightSwitch.SetSwitch(true);
                lightsOn++;
            }

            if (fusesAdded > 0 || lightsOn > 0)
                Puts($"Added {fusesAdded} fuses, switched on {lightsOn} lights");
        }

        private static bool FillFuseBox(ItemBasedFlowRestrictor box)
        {
            if (box.inventory == null || box.validPassthroughItems == null || box.validPassthroughItems.Length == 0) return false;

            Item fuse = box.inventory.GetSlot(0);
            if (fuse != null)
            {
                // Fuses wear out while powered; keep them at full condition
                if (fuse.hasCondition && fuse.condition < fuse.maxCondition) fuse.condition = fuse.maxCondition;
                return false;
            }

            box.inventory.SetLocked(false);
            Item newFuse = ItemManager.Create(box.validPassthroughItems[0], 1);
            bool added = newFuse.MoveToContainer(box.inventory);
            if (!added) newFuse.Remove();
            // Locked so players can't pull the fuse out
            box.inventory.SetLocked(true);
            return added;
        }
    }
}
