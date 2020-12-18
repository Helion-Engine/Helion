using System.Collections.Generic;

namespace Helion.Worlds.Entities.Players
{
    public class TickCommand
    {
        private readonly HashSet<TickCommands> m_commands = new();

        public void Add(TickCommands command)
        {
            m_commands.Add(command);
        }

        public bool Has(TickCommands command) => m_commands.Contains(command);
    }
}