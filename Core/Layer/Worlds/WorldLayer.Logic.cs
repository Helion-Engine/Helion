using Helion.Demo;
using Helion.World;
using Helion.World.Cheats;

namespace Helion.Layer.Worlds;

public partial class WorldLayer
{
    private IDemoPlayer? m_demoPlayer;
    private IDemoRecorder? m_demoRecorder;
    private bool m_recording;
    private int m_demoSkipTicks;

    private bool AnyLayerObscuring => m_parent.ConsoleLayer != null ||
                                      m_parent.MenuLayer != null ||
                                      m_parent.TitlepicLayer != null ||
                                      m_parent.IntermissionLayer != null;
    public void RunLogic()
    {
        TickWorld();
        HandlePauseOrResume();
    }

    public bool StartRecording(IDemoRecorder recorder)
    {
        if (m_recording)
            return false;

        m_recording = true;
        m_demoRecorder = recorder;
        return false;
    }

    public bool StopRecording()
    {
        if (!m_recording)
            return false;

        m_recording = false;
        m_demoRecorder = null;
        return false;
    }

    public bool StartPlaying(IDemoPlayer player)
    {
        if (World.PlayingDemo)
            return false;

        World.PlayingDemo = true;
        m_demoPlayer = player;
        return false;
    }

    public bool StopPlaying()
    {
        if (!World.PlayingDemo)
            return false;

        World.PlayingDemo = false;
        m_demoPlayer = null;
        return true;
    }

    private void TickWorld()
    {
        m_lastTickInfo = m_ticker.GetTickerInfo();
        int ticksToRun = m_lastTickInfo.Ticks;

        double demoPlaybackSpeed = m_config.Demo.PlaybackSpeed;
        if (m_demoPlayer != null && demoPlaybackSpeed != 1 && demoPlaybackSpeed != 0)
        {
            if (demoPlaybackSpeed > 1)
            {
                ticksToRun = (int)(ticksToRun * demoPlaybackSpeed);
            }
            else if (m_demoSkipTicks <= 0)
            {
                m_demoSkipTicks = (int)(1 / demoPlaybackSpeed);
                return;
            }
        }

        m_demoSkipTicks--;
        if (m_demoSkipTicks > 0)
        {
            World.ResetInterpolation();
            return;
        }

        if (ticksToRun <= 0)
            return;

        if (ticksToRun > TickOverflowThreshold)
        {
            Log.Warn("Large tick overflow detected (likely due to delays/lag), reducing ticking amount");
            ticksToRun = 1;
        }

        // Need to process the same command for each tick that needs be run.
        if (m_demoPlayer == null)
            World.SetTickCommand(m_tickCommand);

        bool nextCommand = false;
        while (ticksToRun > 0)
        {
            nextCommand = NextTickCommand();
            World.Tick();
            RecordTickCommand();
            ticksToRun--;
        }

        if (nextCommand)
            m_tickCommand.Clear();
    }

    public void RunTicks(int ticks)
    {
        while (ticks > 0)
        {
            NextTickCommand();
            World.Tick();
            ticks--;
        }
    }

    private void RecordTickCommand()
    {
        if (m_demoRecorder != null)
            m_demoRecorder.AddTickCommand(World.Player);
    }

    private bool NextTickCommand()
    {
        if (World.Paused || World.WorldState != WorldState.Normal)
            return false;

        if (m_demoPlayer == null)
            return true;

        DemoTickResult result = m_demoPlayer.SetNextTickCommand(m_tickCommand, out _, out var activatedCheats);
        if (result == DemoTickResult.DemoEnded)
        {
            World.DisplayMessage(Player, null, "The demo has ended.");
            World.DemoEnded = true;
            World.Pause();
        }

        foreach (var cheat in activatedCheats)
        {
            // This can cause issues... skipping for now
            if ((CheatType)cheat.CheatType == CheatType.ChangeMusic)
                continue;

            World.CheatManager.ActivateCheat(World.Player, (CheatType)cheat.CheatType, cheat.LevelNumber);
        }

        World.SetTickCommand(m_tickCommand);
        return true;
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
