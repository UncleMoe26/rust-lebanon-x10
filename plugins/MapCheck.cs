using System.Collections.Generic;
using System.Linq;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("MapCheck", "Claude", "1.0.0")]
    [Description("Records which monuments the map has, so the wipe script can re-roll maps without a Launch Site")]
    class MapCheck : RustPlugin
    {
        class Report
        {
            public uint Seed;
            public uint Size;
            public bool LaunchSite;
            public List<string> Monuments;
        }

        void OnServerInitialized()
        {
            List<string> names = TerrainMeta.Path.Monuments.Select(m => m.name.Split('/').Last().Replace(".prefab", "")).OrderBy(n => n).ToList();
            var report = new Report { Seed = World.Seed, Size = World.Size, LaunchSite = names.Any(n => n.StartsWith("launch_site")), Monuments = names };
            Interface.Oxide.DataFileSystem.WriteObject("MapCheck", report);
            Puts($"Map {report.Size} seed {report.Seed}: Launch Site {(report.LaunchSite ? "yes" : "NO")}, {names.Count} monuments");
        }
    }
}
