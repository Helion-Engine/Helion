using Helion.Demo;
using Helion.Util.Timing;
using Helion.World;
using Helion.World.Cheats;
using Helion.World.Entities.Players;

namespace Helion.Layer.Worlds;

public partial class WorldLayer
{
    private IDemoPlayer? m_demoPlayer;
    private IDemoRecorder? m_demoRecorder;
    private bool m_recording;
    private bool m_demoEnded;
    private int m_demoSkipTicks;

    private bool AnyLayerObscuring => m_parent.ConsoleLayer != null ||
                                      m_parent.MenuLayer != null ||
                                      m_parent.TitlepicLayer != null ||
                                      m_parent.IntermissionLayer != null ||
                                      m_parent.ReadThisLayer != null ||
                                      m_parent.LoadingLayer != null;
    public void RunLogic(TickerInfo tickerInfo)
    {
        TickWorld(tickerInfo);
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

        m_demoEnded = false;
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

    public int GetTicksToRun() => m_lastTickInfo.Ticks;

    private void TickWorld(TickerInfo tickerInfo)
    {
        m_lastTickInfo = tickerInfo;
        int ticksToRun = m_lastTickInfo.Ticks;
        World.AnyLayerObscuring = AnyLayerObscuring;
        TickCommand cmd = GetTickCommand();

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
            World.SetTickCommand(World.GetCameraPlayer(), cmd);

        bool nextCommand = false;
        while (ticksToRun > 0)
        {
            nextCommand = NextTickCommand();
            World.Tick();
            RecordTickCommand();
            ticksToRun--;
        }

        if (nextCommand)
            cmd.Clear();
    }

    private TickCommand GetTickCommand()
    {
        if (World.IsChaseCamMode)
            return m_chaseCamTickCommand;
        return m_tickCommand;
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
        if (m_demoRecorder != null && !World.Paused)
            m_demoRecorder.AddTickCommand(World.Player);
    }

    private bool NextTickCommand()
    {
        if (World.Paused || World.WorldState != WorldState.Normal || World.IsChaseCamMode)
        {
            if (AnyLayerObscuring)
                return false;

            World.SetTickCommand(World.GetCameraPlayer(), GetTickCommand());
            if (!World.IsChaseCamMode)
                return false;
        }

        if (m_demoPlayer == null)
            return true;

        if (World.Paused || World.WorldState != WorldState.Normal)
            return m_demoEnded;

        DemoTickResult result = m_demoPlayer.SetNextTickCommand(m_demoTickCommand, out _, out var activatedCheats);
        if (result == DemoTickResult.DemoEnded)
        {
            m_demoEnded = true;
            World.DisplayMessage("The demo has ended.");
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

        World.SetTickCommand(Player, m_demoTickCommand);
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
