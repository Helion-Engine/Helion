using System;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.ConfigsNew.Values
{
    public class ConfigValue<T> : IConfigValue where T : notnull
    {
        public object Value => m_value;
        public bool Changed { get; private set; }
        public ConfigSetFlags SetFlags { get; }
        private T m_value;
        
        // Given a <previousValue, newValue>, returns true if the value should
        // be accepted, or false if it is an unacceptable value and should not
        // be set. If not null, this is always run first before the transformer
        // function.
        private readonly Func<T, T, bool>? m_filter;
        
        // Transforms a new incoming value into something usable if it is not
        // acceptable. If not null, this is always run after the filter function.
        private readonly Func<T, T>? m_transformer;

        // This is for a change when the config set flags want it to be delayed.
        // For example, if we want this to only update on a world change, then
        // we stash away the change until then.
        private T? m_queuedChange;
        
        public event EventHandler<(T Before, T After)>? OnChanged;

        public ConfigValue(T initialValue, Func<T, T, bool> filter) : 
            this(initialValue, ConfigSetFlags.Normal, filter)
        {
        }
        
        public ConfigValue(T initialValue, Func<T, T> transformer) : 
            this(initialValue, ConfigSetFlags.Normal, null, transformer)
        {
        }
        
        public ConfigValue(T initialValue, Func<T, T, bool> filter, Func<T, T> transformer) : 
            this(initialValue, ConfigSetFlags.Normal, filter, transformer)
        {
        }
        
        public ConfigValue(T initialValue, ConfigSetFlags setFlags = ConfigSetFlags.Normal, Func<T, T, bool>? filter = null,
            Func<T, T>? transformer = null)
        {
            m_value = transformer != null ? transformer(initialValue) : initialValue;
            m_filter = filter;
            m_transformer = transformer;
            SetFlags = setFlags;
            
            Postcondition(filter == null || filter(initialValue, initialValue), $"Initial config value of '{initialValue}' is filtered");
        }
        
        public static implicit operator T(ConfigValue<T> val) => val.m_value;

        public ConfigSetResult Set(object newValue)
        {
            return ConfigConverter.TryConvert(newValue, out T? converted) ? 
                Set(converted) : 
                ConfigSetResult.NotSetByBadConversion;
        }
        
        public ConfigSetResult Set(T newValue)
        {
            if (Equals(newValue, m_value))
                return ConfigSetResult.Unchanged;

            if (m_filter != null && !m_filter(m_value, newValue))
                return ConfigSetResult.NotSetByFilter;

            if (m_transformer != null)
                newValue = m_transformer(newValue);

            if (SetFlags != ConfigSetFlags.Normal)
            {
                m_queuedChange = newValue;
                return ConfigSetResult.Queued;
            }
            
            T oldValue = m_value;
            m_value = newValue;
            Changed = true;
            OnChanged?.Invoke(this, (oldValue, newValue));
            
            return ConfigSetResult.Set;
        }

        public void ApplyQueuedChange(ConfigSetFlags flagType)
        {
            if (m_queuedChange == null)
                return;
            
            if ((SetFlags & flagType) == flagType)
                Set(m_queuedChange);
        }

        public override string ToString() => Value.ToString() ?? "";
    }
}
