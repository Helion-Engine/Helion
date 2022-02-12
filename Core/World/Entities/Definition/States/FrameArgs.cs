using System;
using System.Collections.Generic;

namespace Helion.World.Entities.Definition.States
{
    public class FrameArgs
    {
        private enum ArgType
        {
            Undefined,
            Int,
            Double,
            String
        }

        public static readonly FrameArgs Default = new();

        public readonly IList<object> Values;
        private readonly ArgType[] m_types;

        public FrameArgs()
        {
            Values = Array.Empty<object>();
            m_types = Array.Empty<ArgType>();
        }

        public FrameArgs(IList<object> args)
        {
            Values = args;
            m_types = new ArgType[args.Count];

            for (int i = 0; i < args.Count; i++)
                m_types[i] = GetType(args[i]);
        }

        private static ArgType GetType(object obj)
        {
            Type type = obj.GetType();
            if (type == typeof(int))
                return ArgType.Int;
            else if (type == typeof(double))
                return ArgType.Double;
            else if (type == typeof(string))
                return ArgType.String;

            return ArgType.Undefined;
        }

        public FrameArgs(object arg)
            : this(new object[] { arg })
        {
        }

        public int GetInt(int index)
        {
            if (m_types[index] == ArgType.Int || m_types[index] == ArgType.Double)
                return (int)Values[index];

            return 0;
        }

        public double GetDouble(int index)
        {
            if (m_types[index] == ArgType.Int || m_types[index] == ArgType.Double)
                return (double)Values[index];

            return 0;
        }

        public string GetString(int index)
        {
            if (m_types[index] == ArgType.String)
                return (string)Values[index];

            return string.Empty;
        }
    }
}
