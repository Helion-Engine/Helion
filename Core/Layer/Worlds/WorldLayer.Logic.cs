namespace Helion.Layer.Worlds;

public partial class WorldLayer
{
    private bool AnyLayerObscuring => m_parent.ConsoleLayer != null ||
                                      m_parent.MenuLayer != null ||
                                      m_parent.TitlepicLayer != null ||
                                      m_parent.IntermissionLayer != null;
    public void RunLogic()
    {
        TickWorld();
        HandlePauseOrResume();
    }

    private void TickWorld()
    {
        m_lastTickInfo = m_ticker.GetTickerInfo();
        int ticksToRun = m_lastTickInfo.Ticks;

        if (ticksToRun <= 0)
            return;

        if (ticksToRun > TickOverflowThreshold)
        {
            Log.Warn("Large tick overflow detected (likely due to delays/lag), reducing ticking amount");
            ticksToRun = 1;
        }

        while (ticksToRun > 0)
        {
            World.SetTickCommand(m_tickCommand);
            World.Tick();
            ticksToRun--;
        }

        System.Threading.Thread.Sleep(100);

        m_tickCommand.Clear();
    }

    private void HandlePauseOrResume()
    {
        // If something is on top of our world (such as a menu, or a
        // console) then we should pause it. Likewise, if we're at the
        // top layer, then we should make sure we're not paused (like
        // if the user just removed the menu or console).
        if (AnyLayerObscuring)
        {
            World.Pause();
            return;
        }

        if (!m_paused && World.Paused)
            World.Resume();
    }
}
