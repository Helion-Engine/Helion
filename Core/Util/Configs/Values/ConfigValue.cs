using System;
using System.Collections.Generic;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;
using static Helion.Util.Configs.Values.ConfigConverters;

namespace Helion.Util.Configs.Values;

public class ConfigValue<T> : IConfigValue where T : notnull
{
    // This value could be passed any type. To avoid many creations, this
    // is cached between different generic types.
    private static readonly Func<object, T> ObjectToTypeConverterOrThrow;

    // We need these to run at the start. They won't, and this forces it.
    // This way any developer who adds something that would break due to
    // not having the proper conversions will be notified immediately. For
    // whatever reason, assigning these directly will delay the function
    // invocation, which means if the developer forgets, it will blow up
    // much later (possibly even on write!) which is very bad because configs
    // will never get written.
    static ConfigValue()
    {
        ObjectToTypeConverterOrThrow = MakeObjectToTypeConverterOrThrow<T>();
    }

    public object ObjectValue => Value;
    public object ObjectDefaultValue => DefaultValue;
    public T Value { get; private set; }
    public T UserValue { get; private set; }
    public T DefaultValue { get; private set; }
    public bool Changed { get; set; }
    public Type ValueType => typeof(T);
    public ConfigSetFlags SetFlags { get; }
    public bool WriteToConfig { get; set; } = true;
    public bool OptionDisabled { get; set; }

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
    // This is necessary because if T is a struct (int, double, enum) it won't actually be 'null'
    private bool m_hasQueuedChange;

    public event EventHandler<T>? OnChanged;
    public event EventHandler<(T Before, T After)>? OnChangedBeforeAfter;

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
        Value = transformer != null ? transformer(initialValue) : initialValue;
        DefaultValue = Value;
        m_filter = filter;
        m_transformer = transformer;
        SetFlags = setFlags;

        Postcondition(filter == null || filter(initialValue, initialValue), $"Initial config value of '{initialValue}' is filtered");
    }

    public static implicit operator T(ConfigValue<T> val) => val.Value;

    public ConfigSetResult Set(object newValue)
    {
        try
        {
            if (typeof(T) == typeof(bool) && newValue is string str && str.Length == 1 && str[0] == '*')
            {
                bool value = Convert.ToBoolean(Value);
                return Set(!value);
            }

            T converted = ObjectToTypeConverterOrThrow(newValue);
            return Set(converted);
        }
        catch
        {
            return ConfigSetResult.NotSetByBadConversion;
        }
    }

    public ConfigSetResult Set(T newValue)
    {
        WriteToConfig = true;
        var result = SetValue(newValue, false);
        UserValue = Value;
        return result;
    }

    public ConfigSetResult SetWithNoWriteConfig(T newValue)
    {
        WriteToConfig = false;
        return SetValue(newValue, false);
    }

    public void ResetToUserValue()
    {
        Value = UserValue;
    }

    private ConfigSetResult SetValue(T newValue, bool ignoreSetFlags)
    {
        if (Equals(newValue, Value))
            return ConfigSetResult.Unchanged;

        if (m_filter != null && !m_filter(Value, newValue))
            return ConfigSetResult.NotSetByFilter;

        if (m_transformer != null)
            newValue = m_transformer(newValue);

        if (SetFlags != ConfigSetFlags.Normal && !ignoreSetFlags)
        {
            m_queuedChange = newValue;
            m_hasQueuedChange = true;
            return ConfigSetResult.Queued;
        }

        T oldValue = Value;
        Value = newValue;
        Changed = true;
        OnChanged?.Invoke(this, newValue);
        OnChangedBeforeAfter?.Invoke(this, (oldValue, newValue));

        m_queuedChange = default(T);
        m_hasQueuedChange = false;

        return ConfigSetResult.Set;
    }

    public void ApplyQueuedChange(ConfigSetFlags flagType)
    {
        if (m_queuedChange == null || !m_hasQueuedChange)
            return;

        if ((SetFlags & flagType) == flagType)
            SetValue(m_queuedChange, true);
    }

    public override string ToString()
    {
        // For now, we only have a very few cases, so we'll handle them here.
        if (Value is List<string> stringList)
            return $"[\"{stringList.Join("\", \"")}\"]";
        if (Value is double d)
            return d.ToString("0.0######", Parsing.DecimalFormat);

        return Value.ToString() ?? "";
    }

    public IConfigValue Clone()
    {
        return new ConfigValue<T>(Value, m_filter);
    }
}
