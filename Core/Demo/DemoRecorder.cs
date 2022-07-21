using Helion.Util.RandomGenerators;
using Helion.World.Entities.Players;
using System;
using System.IO;

namespace Helion.Demo;

public class DemoRecorder : IDemoRecorder, IDisposable
{
    private readonly FileStream m_fileStream;
    private readonly BinaryWriter m_writer;
    private bool m_recording;

    public DemoRecorder(string file)
    {
        if (File.Exists(file))
            File.Delete(file);

        m_fileStream = File.OpenWrite(file);
        m_writer = new BinaryWriter(m_fileStream);
    }

    public void AddTickCommand(Player player)
    {
        if (!m_recording)
            return;

        TickCommand command = player.TickCommand;
        int commands = 0;
        foreach (var cmd in command.Commands)
            commands |= 1 << (int)cmd;

        m_writer.Write(commands);
        m_writer.Write(command.AngleTurn);
        m_writer.Write(command.PitchTurn);
        m_writer.Write(command.MouseAngle);
        m_writer.Write(command.MousePitch);
        m_writer.Write(command.ForwardMoveSpeed);
        m_writer.Write(command.SideMoveSpeed);
        m_writer.Flush();
    }

    public void Start() => m_recording = true;

    public void Stop() => m_recording = false;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        m_writer.Dispose();
        m_fileStream.Dispose();
    }
}
