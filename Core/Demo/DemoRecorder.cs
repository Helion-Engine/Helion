using Helion.Util;
using Helion.World.Entities.Players;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Helion.Demo;

public class DemoRecorder : IDemoRecorder
{
    private readonly FileStream m_fileStream;
    private readonly byte[] m_buffer;
    private readonly string m_file;

    public int CommandIndex { get; private set; }
    public bool Recording { get; private set; }

    public DemoRecorder(string file)
    {
        if (File.Exists(file))
            File.Delete(file);

        m_fileStream = File.OpenWrite(file);
        m_buffer = new byte[Marshal.SizeOf<DemoCommand>()];
        m_file = file;
    }

    public void AddTickCommand(Player player)
    {
        if (!Recording)
            return;

        TickCommand command = player.TickCommand;
        DemoCommand demoCommand = new();
        var commands = command.GetCommands();
        for (int i = 0; i < commands.Length; i++)
            demoCommand.Buttons |= 1 << (int)commands[i].ToDemoTickCommand();

        demoCommand.AngleTurn = command.AngleTurn;
        demoCommand.PitchTurn = command.PitchTurn;
        demoCommand.MouseAngle = command.MouseAngle;
        demoCommand.MousePitch = command.MousePitch;
        demoCommand.ForwardMoveSpeed = command.ForwardMoveSpeed;
        demoCommand.SideMoveSpeed = command.SideMoveSpeed;

        WriteCommand(m_fileStream, demoCommand, m_buffer);
        m_fileStream.Flush();

        CommandIndex++;
    }

    private static void WriteCommand(Stream stream, DemoCommand obj, byte[]? buffer = null)
    {
        int size = Marshal.SizeOf<DemoCommand>();
        if (buffer == null)
            buffer = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(obj, ptr, true);
        Marshal.Copy(ptr, buffer, 0, size);
        stream.Write(buffer, 0, size);
        Marshal.FreeHGlobal(ptr);
    }

    public void Start() => Recording = true;

    public void Stop() => Recording = false;

    public string DemoFile => m_file;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        m_fileStream.Dispose();
    }
}
