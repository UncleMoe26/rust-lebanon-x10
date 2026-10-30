namespace Oxide.Plugins
{
    [Info("Hourly Storm", "Uncle Moe", "1.0.0")]
    [Description("Starts a storm (rain, thunder, wind) every hour and clears it after 10 minutes")]
    public class HourlyStorm : RustPlugin
    {
        private const float IntervalSeconds = 3600f;
        private const float DurationSeconds = 600f;

        private void OnServerInitialized()
        {
            timer.Every(IntervalSeconds, StartStorm);
        }

        private void Unload()
        {
            EndStorm();
        }

        private void StartStorm()
        {
            rust.RunServerCommand("weather.rain", "1");
            rust.RunServerCommand("weather.thunder", "1");
            rust.RunServerCommand("weather.wind", "1");
            PrintToChat("<color=#4da6ff>A storm is coming!</color> Heavy rain and thunder for the next 10 minutes.");
            timer.Once(DurationSeconds, EndStorm);
        }

        private void EndStorm()
        {
            // -1 hands the weather back to Rust's normal cycle
            rust.RunServerCommand("weather.rain", "-1");
            rust.RunServerCommand("weather.thunder", "-1");
            rust.RunServerCommand("weather.wind", "-1");
        }

        [ConsoleCommand("storm.now")]
        private void CmdStormNow(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && !arg.IsAdmin) return;
            StartStorm();
        }
    }
}
