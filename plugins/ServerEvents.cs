using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Server Events", "Uncle Moe", "1.0.0")]
    [Description("Hourly PvP events: a locked crate at a monument (marked on the map) and a supply drop, taking turns; events.now [crate|drop] runs one from the console")]
    class ServerEvents : RustPlugin
    {
        const float IntervalMinutes = 60f;
        const float FirstEventMinutes = 20f;
        const float MarkerMinutes = 25f;
        const string CratePrefab = "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab";
        const string PlanePrefab = "assets/prefabs/npc/cargo plane/cargo_plane.prefab";
        const string MarkerPrefab = "assets/prefabs/tools/map/genericradiusmarker.prefab";

        // Big monuments worth fighting over (matched against the monument prefab name)
        static readonly string[] Monuments =
        {
            "airfield", "powerplant", "trainyard", "water_treatment", "military_tunnel", "launch_site", "harbor", "satellite_dish",
            "sphere_tank", "junkyard", "excavator", "radtown", "ferry_terminal", "arctic_research", "nuclear_missile_silo", "desert_military_base",
        };

        int turn;
        readonly List<BaseEntity> markers = new List<BaseEntity>();

        void OnServerInitialized()
        {
            timer.Once(FirstEventMinutes * 60f, () =>
            {
                RunEvent();
                timer.Every(IntervalMinutes * 60f, RunEvent);
            });
        }

        void Unload()
        {
            foreach (BaseEntity marker in markers) if (marker != null && !marker.IsDestroyed) marker.Kill();
        }

        [ConsoleCommand("events.now")]
        void CcNow(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && !arg.IsAdmin) return;
            // events.now crate | drop runs that event even with nobody online
            string kind = arg.GetString(0);
            if (kind == "crate") LockedCrate();
            else if (kind == "drop") SupplyDrop();
            else RunEvent();
        }

        void RunEvent()
        {
            if (BasePlayer.activePlayerList.Count == 0) { turn++; return; }
            if (turn++ % 2 == 0) LockedCrate();
            else SupplyDrop();
        }

        void LockedCrate()
        {
            List<MonumentInfo> spots = TerrainMeta.Path.Monuments.Where(m => m != null && Monuments.Any(n => m.name.ToLower().Contains(n))).ToList();
            if (spots.Count == 0) { SupplyDrop(); return; }
            MonumentInfo monument = spots[Random.Range(0, spots.Count)];
            Vector3 pos = monument.transform.position + new Vector3(Random.Range(-8f, 8f), 0f, Random.Range(-8f, 8f));
            RaycastHit hit;
            if (Physics.Raycast(pos + Vector3.up * 60f, Vector3.down, out hit, 120f, LayerMask.GetMask("Terrain", "World", "Construction")))
                pos = hit.point;
            else pos.y = TerrainMeta.HeightMap.GetHeight(pos);

            BaseEntity crate = GameManager.server.CreateEntity(CratePrefab, pos + Vector3.up * 0.2f, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            if (crate == null) return;
            crate.Spawn();
            Marker(pos, 0.9f, 0.3f, 0.1f);
            string where = monument.displayPhrase != null && !string.IsNullOrEmpty(monument.displayPhrase.english) ? monument.displayPhrase.english : "a monument";
            Broadcast($"<color=#ff7a2e>EVENT</color> A <color=#ffcc00>locked crate</color> just dropped at <color=#ffcc00>{where}</color> ({Grid(pos)}). Hack it (15 min) and hold it. It's marked on your map.");
            Puts($"Locked crate at {where} {Grid(pos)} {pos}");
        }

        void SupplyDrop()
        {
            float half = World.Size / 2f - 400f;
            Vector3 pos = new Vector3(Random.Range(-half, half), 0f, Random.Range(-half, half));
            for (int tries = 0; tries < 20 && TerrainMeta.HeightMap.GetHeight(pos) < 2f; tries++)
                pos = new Vector3(Random.Range(-half, half), 0f, Random.Range(-half, half));
            pos.y = TerrainMeta.HeightMap.GetHeight(pos);

            CargoPlane plane = GameManager.server.CreateEntity(PlanePrefab) as CargoPlane;
            if (plane == null) return;
            plane.InitDropPosition(pos);
            plane.Spawn();
            Marker(pos, 0.2f, 0.6f, 1f);
            Broadcast($"<color=#ff7a2e>EVENT</color> A <color=#9fd0ff>supply drop</color> is on its way to <color=#9fd0ff>{Grid(pos)}</color>. First one there takes it. It's marked on your map.");
            Puts($"Supply drop to {Grid(pos)} {pos}");
        }

        void Marker(Vector3 pos, float r, float g, float b)
        {
            MapMarkerGenericRadius marker = GameManager.server.CreateEntity(MarkerPrefab, pos) as MapMarkerGenericRadius;
            if (marker == null) return;
            marker.alpha = 0.6f;
            marker.color1 = new Color(r, g, b);
            marker.color2 = Color.black;
            marker.radius = 0.25f;
            marker.Spawn();
            marker.SendUpdate();
            markers.Add(marker);
            timer.Once(MarkerMinutes * 60f, () =>
            {
                markers.Remove(marker);
                if (marker != null && !marker.IsDestroyed) marker.Kill();
            });
        }

        void Broadcast(string message)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList) player.ChatMessage(message);
        }

        // Map grid like the in-game map: letters left to right, numbers top to bottom, 146.3 m squares
        static string Grid(Vector3 pos)
        {
            const float cell = 146.3f;
            float size = World.Size;
            int col = Mathf.Clamp(Mathf.FloorToInt((pos.x + size / 2f) / cell), 0, 99);
            int row = Mathf.Clamp(Mathf.FloorToInt((size / 2f - pos.z) / cell), 0, 99);
            string letters = "";
            for (int c = col; c >= 0; c = c / 26 - 1) letters = (char)('A' + c % 26) + letters;
            return letters + row;
        }
    }
}
