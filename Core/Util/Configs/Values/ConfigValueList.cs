using System;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Util.Configs.Values
{
    public class ConfigValueList<T> : IConfigValue<List<T>>
    {
        public List<T> Value { get; private set; }
        public bool Changed { get; private set; }
        public event EventHandler<List<T>>? OnChanged;

        public ConfigValueList(List<T>? list = null)
        {
            Value = list?.ToList() ?? new List<T>();
        }

        public ConfigValueList(T item) : this(new List<T> { item })
        {
        }

        public static implicit operator List<T>(ConfigValueList<T> configValue) => configValue.Value;

        public object Get() => Value;

        public bool Set(object obj)
        {
            List<T> oldValue = Value;

            switch (obj)
            {
            case IEnumerable<T> items:
                Value = items.ToList();
                EmitEventIfChanged(oldValue);
                return true;
            case T t:
                Value = new List<T> { t };
                EmitEventIfChanged(oldValue);
                return true;
            default:
                return false;
            }
        }

        private void EmitEventIfChanged(List<T> oldValue)
        {
            // If the size changed, their has to be a change. This also avoids
            // iterating over everything in the case that an item is added.
            if (oldValue.Count != Value.Count)
                OnChanged?.Invoke(this, Value);

            // Emit an event if any of the elements are different.
            for (int i = 0; i < Value.Count; i++)
            {
                if (Equals(oldValue[i], Value[i]))
                    continue;

                Changed = true;
                OnChanged?.Invoke(this, Value);
                return;
            }
        }

        public override string ToString() => "[" + string.Join(", ", Value) + "]";
    }
}
