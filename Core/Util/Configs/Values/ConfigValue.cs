using System;

namespace Helion.Util.Configs.Values
{
    public abstract class ConfigValue<T>
    {
        /// <summary>
        /// The backing value.
        /// </summary>
        public T Value { get; protected set; }

        /// <summary>
        /// If this value changed since loading.
        /// </summary>
        public bool Changed { get; protected set; }

        /// <summary>
        /// The event to be fired upon the element changing.
        /// </summary>
        public event EventHandler<T>? OnChanged;

        protected ConfigValue(T value = default)
        {
            Value = value;
        }

        /// <summary>
        /// Sets the value to the object provided. This allows anything to be
        /// passed in and be configured as needed. If the value is different
        /// to the current one, then its status will be considered changed.
        /// </summary>
        /// <param name="obj">The object to try to set.</param>
        /// <returns>True on success, false if the object could not be turned
        /// into something that could be set. This will end up with a default
        /// value being set if false is returned.</returns>
        public abstract bool Set(object obj);

        /// <summary>
        /// Fires an event if the old value was changed. Assumes the new value
        /// was already set in <see cref="Value"/>.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        protected void EmitEventIfChanged(T oldValue)
        {
            if (oldValue != null && oldValue.Equals(Value))
                return;

            Changed = true;
            OnChanged?.Invoke(this, Value);
        }

        public override string ToString() => Value!.ToString() ?? "null";
    }
}
