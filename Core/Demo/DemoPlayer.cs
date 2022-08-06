using Helion.Util;
using Helion.World.Cheats;
using Helion.World.Entities.Players;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Helion.Demo;

public class DemoPlayer : IDemoPlayer
{
    public event EventHandler? PlaybackEnded;

    private readonly FileStream m_fileStream;
    private readonly byte[] m_buffer;
    private readonly IList<DemoCheat> m_cheats;
    private readonly List<DemoCheat> m_activatedCheats = new();
    private bool m_playing;
    private int m_cheatIndex;

    public int CommandIndex { get; private set; }

    public DemoPlayer(string file, IList<DemoCheat> cheats)
    {
        m_fileStream = File.OpenRead(file);
        m_buffer = new byte[Marshal.SizeOf(typeof(DemoCommand))];
        m_cheats = cheats;
    }

    public DemoTickResult SetNextTickCommand(TickCommand command, out int playerNumber, out IList<DemoCheat> activatedCheats)
    {
        playerNumber = 0;
        m_activatedCheats.Clear();
        activatedCheats = m_activatedCheats;
        if (!m_playing)
            return DemoTickResult.None;

        if (m_fileStream.Position >= m_fileStream.Length)
        {
            PlaybackEnded?.Invoke(this, EventArgs.Empty);
            return DemoTickResult.DemoEnded;
        }

        AddActivatedCheats();

        command.Clear();
        DemoCommand demoCommand = m_fileStream.ReadStructure<DemoCommand>(m_buffer);
        for (int i = 0; i < 32; i++)
        {
            if (((1 << i) & demoCommand.Buttons) == 0)
                continue;

            command.Add(((DemoTickCommands)i).ToTickCommand());
        }

        command.AngleTurn = demoCommand.AngleTurn;
        command.PitchTurn = demoCommand.PitchTurn;
        command.MouseAngle = demoCommand.MouseAngle;
        command.MousePitch = demoCommand.MousePitch;
        command.ForwardMoveSpeed = demoCommand.ForwardMoveSpeed;
        command.SideMoveSpeed = demoCommand.SideMoveSpeed;
        CommandIndex++;
        return DemoTickResult.SuccessStopReading;
    }

    private void AddActivatedCheats()
    {
        while (m_cheatIndex < m_cheats.Count && m_cheats[m_cheatIndex].CommandIndex == CommandIndex)
        {
            m_activatedCheats.Add(m_cheats[m_cheatIndex]);
            m_cheatIndex++;
        }
    }

    public bool SetCommandIndex(int index)
    {
        long offset = Marshal.SizeOf(typeof(DemoCommand)) * index;
        if (offset >= m_fileStream.Length)
            return false;

        try
        {
            m_fileStream.Seek(offset, SeekOrigin.Begin);
            CommandIndex = index;

            m_cheatIndex = 0;
            while (m_cheatIndex < m_cheats.Count && m_cheats[m_cheatIndex].CommandIndex <= CommandIndex)
                m_cheatIndex++;

            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Start() => m_playing = true;

    public void Stop() => m_playing = false;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        m_fileStream.Dispose();
    }
}
