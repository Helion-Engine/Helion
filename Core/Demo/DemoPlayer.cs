using Helion.Util;
using Helion.World.Entities.Players;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Helion.Demo;

public class DemoPlayer : IDemoPlayer
{
    public event EventHandler? PlaybackEnded;

    private readonly FileStream m_fileStream;
    private readonly byte[] m_buffer;
    private bool m_playing;

    public int CommandIndex { get; private set; }

    public DemoPlayer(string file)
    {
        m_fileStream = File.OpenRead(file);
        m_buffer = new byte[Marshal.SizeOf(typeof(DemoCommand))];
    }

    public DemoTickResult SetNextTickCommand(TickCommand command, out int playerNumber)
    {
        playerNumber = 0;
        if (!m_playing)
            return DemoTickResult.None;

        if (m_fileStream.Position >= m_fileStream.Length)
        {
            PlaybackEnded?.Invoke(this, EventArgs.Empty);
            return DemoTickResult.DemoEnded;
        }

        command.Clear();
        DemoCommand demoCommand = m_fileStream.ReadStructure<DemoCommand>(m_buffer);
        for (int i = 0; i < 32; i++)
        {
            if (((1 << i) & demoCommand.Buttons) == 0)
                continue;

            command.Add((TickCommands)i);
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

    public bool SetCommandIndex(int index)
    {
        long offset = Marshal.SizeOf(typeof(DemoCommand)) * index;
        if (offset >= m_fileStream.Length)
            return false;

        try
        {
            m_fileStream.Seek(offset, SeekOrigin.Begin);
            CommandIndex = index;
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
