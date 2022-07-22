using System.Collections.Generic;

namespace Helion.World.Entities.Players;

public class TickCommand
{
    // These commands are only processed once even if held down.
    private static readonly HashSet<TickCommands> SinglePressCommands = new()
    {
        TickCommands.Use,
        TickCommands.NextWeapon,
        TickCommands.PreviousWeapon,
        TickCommands.WeaponSlot1,
        TickCommands.WeaponSlot2,
        TickCommands.WeaponSlot3,
        TickCommands.WeaponSlot4,
        TickCommands.WeaponSlot5,
        TickCommands.WeaponSlot6,
        TickCommands.WeaponSlot7,
    };

    private readonly HashSet<TickCommands> m_commands = new();
    private readonly HashSet<TickCommands> m_previousCommands = new();

    public double AngleTurn { get; set; }
    public double PitchTurn { get; set; }
    public double MouseAngle { get; set; }
    public double MousePitch { get; set; }
    public double ForwardMoveSpeed { get; set; }
    public double SideMoveSpeed { get; set; }

    public IEnumerable<TickCommands> Commands => m_commands;

    public void Clear()
    {
        AngleTurn = 0;
        PitchTurn = 0;
        MouseAngle = 0;
        MousePitch = 0;
        ForwardMoveSpeed = 0;
        SideMoveSpeed = 0;
        m_commands.Clear();
    }

    // When the command is handled set the previous commands.
    // This way commands like TickCommands.Use are only processed once until released and pressed again.
    public virtual void TickHandled()
    {
        m_previousCommands.Clear();
        foreach (var command in m_commands)
            m_previousCommands.Add(command);
    }

    public bool Add(TickCommands command) => m_commands.Add(command);

    public bool Has(TickCommands command)
    {
        if (!m_commands.Contains(command))
            return false;

        if (m_previousCommands.Contains(command) && SinglePressCommands.Contains(command))
            return false;

        return true;
    }

    public bool HasTurnKey() =>
        Has(TickCommands.TurnLeft) || Has(TickCommands.TurnRight);

    public bool HasLookKey() =>
        Has(TickCommands.LookUp) || Has(TickCommands.LookDown);

    public bool IsFastSpeed(bool alwaysRun) =>
        (alwaysRun && !Has(TickCommands.Speed)) || (!alwaysRun && Has(TickCommands.Speed));
}
