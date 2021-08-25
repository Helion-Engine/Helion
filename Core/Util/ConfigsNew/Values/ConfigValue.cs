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
        
        // Parameters are <Before, After, Result?>. If the return value is null,
        // that means nothing should be set. Whatever is returned that is not null
        // will be set. This allows a function to transform the value to also be
        // transformed (ex: clamped), while also allowing for rejection.
        private readonly Func<T, T, T?>? m_filter;

        // This is for a change when the config set flags want it to be delayed.
        // For example, if we want this to only update on a world change, then
        // we stash away the change until then.
        private T? m_queuedChange;
        
        public event EventHandler<(T Before, T After)>? OnChanged;

        public ConfigValue(T initialValue, Func<T, T, T?> filter) : 
            this(initialValue, ConfigSetFlags.Normal, filter)
        {
        }
        
        public ConfigValue(T initialValue, ConfigSetFlags setFlags = ConfigSetFlags.Normal, Func<T, T, T?>? filter = null)
        {
            Invariant(filter == null || (filter(initialValue, initialValue) != null), $"Initial config value of '{initialValue}' is filtered");
            
            m_value = initialValue;
            m_filter = filter;
            SetFlags = setFlags;
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

            if (m_filter != null)
            {
                T? filtered = m_filter(m_value, newValue);
                if (filtered == null)
                    return ConfigSetResult.NotSetByFilter;
                
                newValue = filtered;
            }

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
