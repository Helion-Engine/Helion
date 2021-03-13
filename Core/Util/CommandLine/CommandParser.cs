using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Util.CommandLine;

namespace Helion.Util.CommandLine
{
    public class CommandParser
    {
        private readonly string[] m_argStart;

        public CommandParser(string[] argStart)
        {
            m_argStart = argStart;
        }

        public List<CommandArg> Parse(string[] stringArgs)
        {
            List<CommandArg> args = new();
            CommandArg? current = null;

            foreach (string stringArg in stringArgs)
            {
                string? argKey = m_argStart.FirstOrDefault(x => ArgEquals(x, stringArg));

                if (argKey != null)
                {
                    current = FindOrCreate(stringArg, args);
                    continue;
                }

                current?.Values.Add(stringArg.Replace("\"", string.Empty));
            }

            return args;
        }

        private CommandArg FindOrCreate(string argKey, List<CommandArg> args)
        {
            CommandArg? arg = args.FirstOrDefault(x => x.Key == argKey);
            if (arg != null)
                return arg;

            arg = new CommandArg(argKey);
            args.Add(arg);
            return arg;
        }

        private bool ArgEquals(string arg, string cmp)
        {
            if (cmp.Length < arg.Length)
                return false;

            return cmp.Substring(0, arg.Length).Equals(arg, StringComparison.OrdinalIgnoreCase);
        }
    }
}
