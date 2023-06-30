using Helion.Util.Container;
using System.Collections.Generic;

namespace Helion.World.Entities.Players;

public class TickCommand
{
    // These commands are only processed once even if held down.
    private static readonly TickCommands[] SinglePressCommands = new[]
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
        TickCommands.CenterView
    };

    private readonly DynamicArray<TickCommands> m_commands = new();
    private readonly DynamicArray<TickCommands> m_previousCommands = new();
    private readonly List<TickCommands> m_returnCommands = new();

    public double AngleTurn { get; set; }
    public double PitchTurn { get; set; }
    public double MouseAngle { get; set; }
    public double MousePitch { get; set; }
    public double ForwardMoveSpeed { get; set; }
    public double SideMoveSpeed { get; set; }
    public int WeaponScroll { get; set; }

    public DynamicArray<TickCommands> GetCommands() => m_commands;

    public void Clear()
    {
        AngleTurn = 0;
        PitchTurn = 0;
        MouseAngle = 0;
        MousePitch = 0;
        ForwardMoveSpeed = 0;
        SideMoveSpeed = 0;
        WeaponScroll = 0;
        m_commands.Clear();
    }

    // When the command is handled set the previous commands.
    // This way commands like TickCommands.Use are only processed once until released and pressed again.
    public virtual void TickHandled()
    {
        m_previousCommands.Clear();
        for (int i = 0; i < m_commands.Length; i++)
            m_previousCommands.Add(m_commands[i]);
    }

    public bool Add(TickCommands command)
    {
        if (SearchCommand(m_commands.Data, m_commands.Length, command))
            return false;

        m_commands.Add(command);
        return true;
    }

    public bool Has(TickCommands command)
    {
        if (!SearchCommand(m_commands.Data, m_commands.Length, command))
            return false;

        if (SearchCommand(m_previousCommands.Data, m_previousCommands.Length, command) && 
            SearchCommand(SinglePressCommands, SinglePressCommands.Length, command))
            return false;

        return true;
    }

    public bool HasTurnKey() =>
        Has(TickCommands.TurnLeft) || Has(TickCommands.TurnRight);

    public bool HasLookKey() =>
        Has(TickCommands.LookUp) || Has(TickCommands.LookDown);

    public bool IsFastSpeed(bool alwaysRun) =>
        (alwaysRun && !Has(TickCommands.Speed)) || (!alwaysRun && Has(TickCommands.Speed));

    private static bool SearchCommand(TickCommands[] commands, int length, TickCommands command)
    {
        for (int i = 0; i < length; i++)
        {
            if (commands[i] == command)
                return true;
        }

        return false;
    }
}
