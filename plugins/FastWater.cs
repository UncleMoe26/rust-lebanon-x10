using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Fast Water", "Uncle Moe", "1.1.0")]
    [Description("Water moves through pipes faster: player-built catchers, barrels, tanks and pumps push more per tick")]
    class FastWater : RustPlugin
    {
        class PluginConfig
        {
            [JsonProperty("Flow multiplier for water pushed through pipes")]
            public int FlowMultiplier = 10;
            [JsonProperty("Water pump output multiplier")]
            public int PumpMultiplier = 10;
        }

        PluginConfig config;
        // Original values, so unloading the plugin puts everything back to vanilla
        readonly Dictionary<LiquidContainer, int[]> originalFlow = new Dictionary<LiquidContainer, int[]>();
        readonly Dictionary<WaterPump, int> originalPump = new Dictionary<WaterPump, int>();

        protected override void LoadDefaultConfig() => config = new PluginConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>() ?? new PluginConfig();
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        void OnServerInitialized()
        {
            foreach (var entity in BaseNetworkable.serverEntities.OfType<BaseEntity>().ToList()) Boost(entity);
            Puts($"Faster water on {originalFlow.Count} containers and {originalPump.Count} pumps");
        }

        void OnEntitySpawned(BaseEntity entity) => NextTick(() => Boost(entity));

        void OnEntityKill(BaseEntity entity)
        {
            var liquid = entity as LiquidContainer;
            if (liquid != null) originalFlow.Remove(liquid);
            var pump = entity as WaterPump;
            if (pump != null) originalPump.Remove(pump);
        }

        void Boost(BaseEntity entity)
        {
            // Only things players build; monument water stays vanilla.
            // A powered purifier's storage tank is a child entity without an owner, so check the parent too
            if (entity == null || entity.IsDestroyed) return;
            if (entity.OwnerID == 0 && (entity.GetParentEntity()?.OwnerID ?? 0) == 0) return;
            var liquid = entity as LiquidContainer;
            if (liquid != null && !originalFlow.ContainsKey(liquid))
            {
                // autofillTickAmount is what a container actually pushes down the pipe every tick; maxOutputFlow caps it
                originalFlow[liquid] = new[] { liquid.maxOutputFlow, liquid.autofillTickAmount };
                liquid.maxOutputFlow *= config.FlowMultiplier;
                liquid.autofillTickAmount *= config.FlowMultiplier;
            }
            var pump = entity as WaterPump;
            if (pump != null && !originalPump.ContainsKey(pump))
            {
                originalPump[pump] = pump.AmountPerPump;
                pump.AmountPerPump *= config.PumpMultiplier;
            }
        }

        void Unload()
        {
            foreach (var pair in originalFlow)
                if (pair.Key != null && !pair.Key.IsDestroyed)
                {
                    pair.Key.maxOutputFlow = pair.Value[0];
                    pair.Key.autofillTickAmount = pair.Value[1];
                }
            foreach (var pair in originalPump) if (pair.Key != null && !pair.Key.IsDestroyed) pair.Key.AmountPerPump = pair.Value;
        }
    }
}
