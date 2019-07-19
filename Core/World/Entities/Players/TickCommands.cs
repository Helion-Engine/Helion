using System.Collections.Generic;

namespace Helion.World.Entities.Players
{
    public enum TickCommands
    {
        Forward,
        Backward,
        Left,
        Right,
        Attack,
        Jump,
        Crouch,
        Use
    }

    public class TickCommand
    {
        private HashSet<TickCommands> m_commands = new HashSet<TickCommands>();

        public void Add(TickCommands command)
        {
            m_commands.Add(command);
        }

        public bool Has(TickCommands command) => m_commands.Contains(command);
    }
}