using System;

namespace Helion.Util
{
    /// <summary>
    /// Represents a possibly empty result that the user must check against. It
    /// also provides helpful methods that can be chained together to make any
    /// code operating on optionals a lot cleaner.
    /// </summary>
    /// <typeparam name="T">The type contained in the optional.</typeparam>
    public class Optional<T>
    {
        /// <summary>
        /// The value that may exist. This should not be used unless HasValue
        /// is checked first.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Is true if the value exists, false if it is not.
        /// </summary>
        public bool HasValue => Value != null;

        private Optional(T value) => Value = value;

        /// <summary>
        /// A convenience method that allows us to treat it as a boolean value.
        /// This makes it convenient to use in the if conditional block so we
        /// don't have to keep writing out `if (opt.HasValue) { ... }`.
        /// 
        /// Returns true if the optional has a value, false if empty.
        /// </summary>
        /// <param name="opt">The optional to convert to true/false.</param>
        public static implicit operator bool(Optional<T> opt) => opt.HasValue;

        /// <summary>
        /// A convenience method that lets us return values from a function
        /// without having to write `new Optional...()` every time. Instead,
        /// values can be directly returned which makes the code a bit cleaner.
        /// 
        /// Note that this also allows null to be returned and act as an empty
        /// optional, but it is recommended to still use Empty() for clarity
        /// reasons.
        /// </summary>
        /// <param name="t">The value to create an optional from.</param>
        public static implicit operator Optional<T>(T t) => new Optional<T>(t);

        /// <summary>
        /// Creates an empty optional.
        /// </summary>
        /// <returns>An empty optional.</returns>
        public static Optional<T> Empty() => new Optional<T>(default);

        /// <summary>
        /// Applies the function to the value if it exists. If the optional has
        /// no value then an empty optional is returned.
        /// </summary>
        /// <typeparam name="U">The type to map to.</typeparam>
        /// <param name="mapFunc">A function to apply to the value if it is
        /// present.</param>
        /// <returns>An optional with the mapped value, or an empty optional
        /// if the mapping function failed or this is empty.</returns>
        public Optional<U> Map<U>(Func<T, Optional<U>> mapFunc) => HasValue ? mapFunc(Value) : Optional<U>.Empty();

        /// <summary>
        /// Maps a value to another value.
        /// </summary>
        /// <typeparam name="U">The type to map to.</typeparam>
        /// <param name="mapFunc">The function to map the value if present.
        /// </param>
        /// <param name="defaultVal">The default value to return if the value
        /// is not present.</param>
        /// <returns>Either the result of the function, or the default value.
        /// </returns>
        public U MapValueOr<U>(Func<T, U> mapFunc, U defaultVal) => HasValue ? mapFunc(Value) : defaultVal;

        /// <summary>
        /// Performs a function on the value if it is present. Otherwise the
        /// second function is called (or nothing is done if it is null).
        /// </summary>
        /// <param name="func">The function to apply if the value is present.
        /// </param>
        /// <param name="elseFunc">A function to call if no value is present.
        /// </param>
        public void Then(Action<T> func, Action elseFunc = null)
        {
            if (HasValue)
                func(Value);
            else
                elseFunc?.Invoke();
        }

        /// <summary>
        /// Gets the value if present, or returns the default value provided.
        /// </summary>
        /// <param name="defaultValue">The value to return if this optional is
        /// empty.</param>
        /// <returns>The value if present, or returns the default value 
        /// provided.</returns>
        public T ValueOr(T defaultValue) => HasValue ? Value : defaultValue;

        /// <summary>
        /// Gets the value if present, or calls the function provided to return
        /// a value.
        /// </summary>
        /// <param name="func">The function to call to generate a value if this
        /// optional is empty.</param>
        /// <returns>The value if present, or returns the value from calling
        /// the function.</returns>
        public T ValueOr(Func<T> func) => HasValue ? Value : func();
    }
}
