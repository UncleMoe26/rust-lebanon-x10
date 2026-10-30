namespace Oxide.Plugins
{
    [Info("FastRecycler", "Uncle Moe", "1.0.0")]
    [Description("Recyclers process an item every second instead of every 5")]
    class FastRecycler : RustPlugin
    {
        // Seconds per recycle tick (vanilla is 5)
        const float TickSeconds = 1f;

        void OnRecyclerToggle(Recycler recycler, BasePlayer player)
        {
            NextTick(() =>
            {
                if (recycler == null || recycler.IsDestroyed || !recycler.IsOn()) return;
                recycler.CancelInvoke(recycler.actionRecycleThink);
                recycler.InvokeRepeating(recycler.actionRecycleThink, TickSeconds, TickSeconds);
            });
        }
    }
}
