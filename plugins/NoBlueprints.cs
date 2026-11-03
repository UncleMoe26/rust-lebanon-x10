namespace Oxide.Plugins
{
    [Info("No Blueprints", "Uncle Moe", "1.0.0")]
    [Description("Every player knows every blueprint from the start, so nobody has to research anything (\"No BPs\")")]
    class NoBlueprints : RustPlugin
    {
        void OnServerInitialized()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList) Unlock(player);
        }

        // Blueprints are loaded a moment after connecting, so unlock a second later
        void OnPlayerConnected(BasePlayer player) => timer.Once(1f, () => Unlock(player));

        void OnPlayerRespawned(BasePlayer player) => Unlock(player);

        void Unlock(BasePlayer player)
        {
            if (player == null || !player.IsConnected || player.blueprints == null) return;
            player.blueprints.UnlockAll();
        }
    }
}
