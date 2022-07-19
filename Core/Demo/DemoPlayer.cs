using Helion.World.Entities.Players;
using System;
using System.IO;

namespace Helion.Demo;

public class DemoPlayer : IDemoPlayer, IDisposable
{
    public event EventHandler? PlaybackEnded;

    private readonly FileStream m_fileStream;
    private readonly BinaryReader m_reader;
    private bool m_playing;

    public DemoPlayer(string file)
    {
        m_fileStream = File.OpenRead(file);
        m_reader = new BinaryReader(m_fileStream);
    }

    public DemoTickResult SetNextTickCommand(TickCommand command, out int playerNumber)
    {
        playerNumber = 0;
        if (!m_playing)
            return DemoTickResult.None;

        if (m_reader.BaseStream.Position >= m_reader.BaseStream.Length)
        {
            PlaybackEnded?.Invoke(this, EventArgs.Empty);
            return DemoTickResult.DemoEnded;
        }

        command.Clear();
        command.RandomIndex = m_reader.ReadInt32();
        int commands = m_reader.ReadInt32();
        for (int i = 0; i < 32; i++)
        {
            if (((1 << i) & commands) == 0)
                continue;

            command.Add((TickCommands)i);
        }

        command.AngleTurn = m_reader.ReadDouble();
        command.PitchTurn = m_reader.ReadDouble();
        command.MouseAngle = m_reader.ReadDouble();
        command.MousePitch = m_reader.ReadDouble();
        command.ForwardMoveSpeed = m_reader.ReadDouble();
        command.SideMoveSpeed = m_reader.ReadDouble();
        return DemoTickResult.SuccessStopReading;
    }

    public void Start() => m_playing = true;

    public void Stop() => m_playing = false;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        m_reader.Dispose();
        m_fileStream.Dispose();
    }
}
