using System.Collections.Generic;

namespace Helion.World.Entities.Players;

public class TickCommand
{
    private static readonly HashSet<TickCommands> InstantCommands = new()
    {
        TickCommands.Forward,
        TickCommands.Backward,
        TickCommands.Left,
        TickCommands.Right,
        TickCommands.TurnLeft,
        TickCommands.TurnRight,
        TickCommands.LookDown,
        TickCommands.LookUp,
        TickCommands.Jump,
        TickCommands.Crouch,
        TickCommands.Attack,
        TickCommands.Speed,
        TickCommands.Strafe,
        TickCommands.CenterView
    };


    private readonly HashSet<TickCommands> m_commands = new();
    private readonly List<TickCommands> m_tickCommands = new();
    private readonly List<TickCommands> m_instantCommands = new();

    public double AngleTurn { get; set; }
    public double PitchTurn { get; set; }
    public double MouseAngle { get; set; }
    public double MousePitch { get; set; }
    public double ForwardMoveSpeed { get; set; }
    public double SideMoveSpeed { get; set; }

    public IEnumerable<TickCommands> Commands => m_commands;

    public void Clear()
    {
        for (int i = 0; i < m_instantCommands.Count; i++)
            m_commands.Remove(m_instantCommands[i]);
        m_instantCommands.Clear();
    }

    public void TickHandled()
    {
        AngleTurn = 0;
        PitchTurn = 0;
        MouseAngle = 0;
        MousePitch = 0;
        ForwardMoveSpeed = 0;
        SideMoveSpeed = 0;

        for (int i = 0; i < m_tickCommands.Count; i++)
            m_commands.Remove(m_tickCommands[i]);
        m_tickCommands.Clear();
    }

    public void Add(TickCommands command)
    {
        if (!m_commands.Add(command))
            return;

        if (InstantCommands.Contains(command))
        {
            m_instantCommands.Add(command);
            return;
        }

        m_tickCommands.Add(command);
    }

    public bool Has(TickCommands command) => m_commands.Contains(command);

    public bool HasTurnKey() =>
        Has(TickCommands.TurnLeft) || Has(TickCommands.TurnRight);

    public bool HasLookKey() =>
        Has(TickCommands.LookUp) || Has(TickCommands.LookDown);

    public bool IsFastSpeed(bool alwaysRun) =>
        (alwaysRun && !Has(TickCommands.Speed)) || (!alwaysRun && Has(TickCommands.Speed));
}
