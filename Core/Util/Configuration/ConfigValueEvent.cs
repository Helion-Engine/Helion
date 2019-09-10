using System;

namespace Helion.Util.Configuration
{
    public class ConfigValueEvent<T> where T : IConvertible
    {
        public readonly T NewValue;

        public ConfigValueEvent(T newValue)
        {
            NewValue = newValue;
        }
    }
}