using System.Diagnostics;

namespace Oxide.Plugins
{
    [Info("MemoryRestart", "Uncle Moe", "1.1.0")]
    [Description("Restarts the server with a chat countdown daily and when memory use gets too high")]
    class MemoryRestart : RustPlugin
    {
        // RustDedicated memory (MB) that triggers a restart; VPS has 12 GB
        const long LimitMb = 10500;
        // Countdown before quitting; systemd starts the server again
        const int CountdownSeconds = 600;

        // Daily restart: countdown starts at 03:50 UTC so the server restarts at 04:00 UTC (06:00 Lebanon winter time)
        const int DailyHourUtc = 3;
        const int DailyMinuteUtc = 50;

        bool restarting;

        void OnServerInitialized()
        {
            timer.Every(60f, Check);
        }

        void Restart(string reason)
        {
            restarting = true;
            Puts($"{reason}, restarting in {CountdownSeconds}s");
            ConsoleSystem.Run(ConsoleSystem.Option.Server, "restart", CountdownSeconds, "Server restart to keep things smooth");
        }

        long MemoryMb()
        {
            var p = Process.GetCurrentProcess();
            p.Refresh();
            return p.WorkingSet64 / (1024 * 1024);
        }

        void Check()
        {
            if (restarting) return;
            var now = System.DateTime.UtcNow;
            // Skip the daily restart if the server only just started
            if (now.Hour == DailyHourUtc && now.Minute == DailyMinuteUtc && UnityEngine.Time.realtimeSinceStartup > 3600f)
            {
                Restart("Daily restart");
                return;
            }
            var mb = MemoryMb();
            if (mb >= LimitMb) Restart($"Memory {mb} MB over {LimitMb} MB");
        }

        [ConsoleCommand("memrestart.status")]
        void CmdStatus(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) return;
            arg.ReplyWith($"Memory {MemoryMb()} MB / limit {LimitMb} MB, restarting: {restarting}");
        }
    }
}
