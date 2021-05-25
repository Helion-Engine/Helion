using System.Collections.Generic;

namespace Helion.World.Entities.Players
{
    public class TickCommand
    {
        private readonly HashSet<TickCommands> m_commands = new();

        public double AngleTurn { get; set; }
        public double PitchTurn { get; set; }
        public double MouseAngle { get; set; }
        public double MousePitch { get; set; }
        public double ForwardMoveSpeed { get; set; }
        public double SideMoveSpeed { get; set; }

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

        public void Add(TickCommands command)
        {
            m_commands.Add(command);
        }

        public bool Has(TickCommands command) => m_commands.Contains(command);

        public bool HasTurnKey() =>
            Has(TickCommands.TurnLeft) || Has(TickCommands.TurnRight);

        public bool HasLookKey() =>
            Has(TickCommands.LookUp) || Has(TickCommands.LookDown);

        public bool IsFastSpeed(bool alwaysRun) =>
            (alwaysRun && !Has(TickCommands.Speed)) || (!alwaysRun && Has(TickCommands.Speed));
    }
}