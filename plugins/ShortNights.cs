using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Short Nights", "Uncle Moe", "1.0.0")]
    [Description("Keeps the night but makes it pass faster; the day keeps its normal length")]
    class ShortNights : RustPlugin
    {
        class PluginConfig
        {
            [JsonProperty("Night length in real minutes (sunset to sunrise)")]
            public float NightMinutes = 8f;
            [JsonProperty("Day length in real minutes (0 = Rust's normal day)")]
            public float DayMinutes = 0f;
        }

        PluginConfig config;
        float originalCycle;
        Timer ticker;

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
            if (TOD_Sky.Instance == null) { PrintWarning("No sky found"); return; }
            originalCycle = TOD_Sky.Instance.Components.Time.DayLengthInMinutes;
            Puts($"Rust cycle {originalCycle:0.#} min: day {DayHours():0.#} h of game time = {originalCycle * DayHours() / 24f:0.#} min, night {24f - DayHours():0.#} h = {originalCycle * (24f - DayHours()) / 24f:0.#} min. Night now {config.NightMinutes:0.#} min.");
            Apply();
            ticker = timer.Every(5f, Apply);
        }

        void Unload()
        {
            if (TOD_Sky.Instance != null && originalCycle > 0)
                TOD_Sky.Instance.Components.Time.DayLengthInMinutes = originalCycle;
        }

        float DayHours() => TOD_Sky.Instance.SunsetTime - TOD_Sky.Instance.SunriseTime;

        bool IsNight()
        {
            float h = TOD_Sky.Instance.Cycle.Hour;
            return h >= TOD_Sky.Instance.SunsetTime || h < TOD_Sky.Instance.SunriseTime;
        }

        // DayLengthInMinutes is the real time one full 24 h cycle would take at the current speed,
        // so the night speed is: night minutes stretched over 24 h, divided by the night's share of the clock
        void Apply()
        {
            float dayHours = DayHours();
            float cycle = IsNight()
                ? config.NightMinutes * 24f / (24f - dayHours)
                : config.DayMinutes > 0 ? config.DayMinutes * 24f / dayHours : originalCycle;
            var time = TOD_Sky.Instance.Components.Time;
            if (cycle > 0 && System.Math.Abs(time.DayLengthInMinutes - cycle) > 0.01f) time.DayLengthInMinutes = cycle;
        }

        [ConsoleCommand("shortnights.status")]
        void CmdStatus(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) return;
            Puts($"Hour {TOD_Sky.Instance.Cycle.Hour:0.00}, {(IsNight() ? "night" : "day")}, sunrise {TOD_Sky.Instance.SunriseTime:0.00}, sunset {TOD_Sky.Instance.SunsetTime:0.00}, cycle speed {TOD_Sky.Instance.Components.Time.DayLengthInMinutes:0.#} min");
        }
    }
}
