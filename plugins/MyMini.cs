using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("My Mini", "Uncle Moe", "1.0.0")]
    [Description("/mymini spawns your own fuelled minicopter in front of you, /nomini removes it. One each, with a cooldown")]
    class MyMini : RustPlugin
    {
        const string MinicopterPrefab = "assets/content/vehicles/minicopter/minicopter.entity.prefab";
        const float CooldownSeconds = 600f;
        const int Fuel = 200;

        class StoredData
        {
            public Dictionary<ulong, ulong> Minis = new Dictionary<ulong, ulong>();
        }

        StoredData data;
        readonly Dictionary<ulong, float> lastSpawn = new Dictionary<ulong, float>();

        void OnServerInitialized()
        {
            data = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name) ?? new StoredData();
        }

        void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, data);

        BaseEntity FindMini(ulong owner)
        {
            ulong id;
            if (!data.Minis.TryGetValue(owner, out id)) return null;
            BaseEntity mini = BaseNetworkable.serverEntities.Find(new NetworkableId(id)) as BaseEntity;
            return mini == null || mini.IsDestroyed ? null : mini;
        }

        [ChatCommand("mymini")]
        void CmdMyMini(BasePlayer player, string command, string[] args)
        {
            float last;
            float wait = lastSpawn.TryGetValue(player.userID, out last) ? CooldownSeconds - (Time.realtimeSinceStartup - last) : 0f;
            if (wait > 0f && !player.IsAdmin)
            {
                SendReply(player, $"Your next minicopter is ready in <color=#ffcc00>{Mathf.CeilToInt(wait / 60f)} min</color>.");
                return;
            }
            if (player.isMounted || !player.IsOnGround() || player.IsSwimming())
            {
                SendReply(player, "Stand on the ground to call your minicopter.");
                return;
            }

            // 4 m in front of you, on whatever is below, with room for the rotor
            Vector3 forward = player.eyes.HeadForward();
            forward.y = 0f;
            forward = forward.sqrMagnitude < 0.01f ? player.transform.forward : forward.normalized;
            Vector3 pos = player.transform.position + forward * 4f;
            RaycastHit hit;
            if (Physics.Raycast(pos + Vector3.up * 3f, Vector3.down, out hit, 8f, LayerMask.GetMask("Terrain", "World", "Construction", "Default")))
                pos = hit.point;
            if (Physics.CheckSphere(pos + Vector3.up * 1.6f, 1.4f, LayerMask.GetMask("Construction", "Deployed", "World", "Tree", "Vehicle_Large", "Vehicle_World")))
            {
                SendReply(player, "There's no room here. Go outside, to an open spot, and try again.");
                return;
            }

            BaseEntity old = FindMini(player.userID);
            if (old != null) old.Kill();

            BaseEntity mini = GameManager.server.CreateEntity(MinicopterPrefab, pos + Vector3.up * 0.3f, Quaternion.LookRotation(forward));
            if (mini == null) return;
            mini.OwnerID = player.userID;
            mini.Spawn();
            AddFuel(mini);

            data.Minis[player.userID] = mini.net.ID.Value;
            SaveData();
            lastSpawn[player.userID] = Time.realtimeSinceStartup;
            SendReply(player, $"Your minicopter is here with {Fuel} low grade fuel. Calling a new one removes this one. <color=#ffcc00>/nomini</color> puts it away.");
        }

        [ChatCommand("nomini")]
        void CmdNoMini(BasePlayer player, string command, string[] args)
        {
            BaseEntity mini = FindMini(player.userID);
            if (mini == null)
            {
                SendReply(player, "You don't have a minicopter out. Type <color=#ffcc00>/mymini</color> to call one.");
                return;
            }
            mini.Kill();
            data.Minis.Remove(player.userID);
            SaveData();
            SendReply(player, "Your minicopter is put away.");
        }

        void AddFuel(BaseEntity mini)
        {
            var vehicle = mini as BaseVehicle;
            var fuelSystem = vehicle?.GetFuelSystem() as EntityFuelSystem;
            StorageContainer tank = fuelSystem?.GetFuelContainer();
            if (tank == null) return;
            Item fuel = ItemManager.CreateByName("lowgradefuel", Fuel);
            if (fuel != null && !fuel.MoveToContainer(tank.inventory)) fuel.Remove();
        }
    }
}
