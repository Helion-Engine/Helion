using System;
using static Helion.Util.Assert;

namespace Helion.Util
{
    /// <summary>
    /// Holds either an expected value, or an error result. Sometimes we want
    /// to call a function that gives us an expected result but for some reason
    /// it cannot. Returning an expected object lets the caller know more about
    /// what went wrong rather than an optional.
    /// 
    /// This class also supports functional properties to write cleaner code.
    /// </summary>
    /// <typeparam name="T">The expected type to hold.</typeparam>
    /// <typeparam name="E">The error result.</typeparam>
    public class Expected<T, E>
    {
        /// <summary>
        /// The value that is expected. This should not be accessed until
        /// HasValue is checked for being true.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// The error result if the value is not present. This should not be
        /// accessed until HasValue is checked for being false.
        /// </summary>
        public E Error { get; }

        /// <summary>
        /// Tells us whether a value is present (true) or an error is present
        /// (false).
        /// </summary>
        public bool HasValue => Value != null;

        private Expected(T value, E error = default)
        {
            Precondition(value != null || error != null, "Trying to make an expected with both value/error being null");

            Value = value;
            Error = error;
        }

        /// <summary>
        /// A convenience method that allows us to treat it as a boolean value.
        /// This makes it convenient to use in the if conditional block so we
        /// don't have to keep writing out `if (expected.HasValue) { ... }`.
        /// 
        /// Returns true if the expected has a value, false if an error.
        /// </summary>
        /// <param name="expected">The expected to convert to true/false.
        /// </param>
        public static implicit operator bool(Expected<T, E> expected) => expected.HasValue;

        /// <summary>
        /// A convenience method that lets us return values from a function 
        /// without having to write `new Expected...()` every time. Instead, 
        /// values can be directly returned which makes the code a bit 
        /// cleaner.
        /// 
        /// Note that this will likely be problematic if T == E.
        /// 
        /// This is not intended to be used with null values.
        /// </summary>
        /// <param name="t">The value to create an expected from.</param>
        public static implicit operator Expected<T, E>(T t) => new Expected<T, E>(t);

        /// <summary>
        /// A convenience method that lets us return error values from a 
        /// function without having to write `new Expected...()` every time. 
        /// Instead, error values can be directly returned which makes the code
        /// a bit cleaner.
        /// 
        /// Note that this will likely be problematic if T == E.
        /// 
        /// This is not intended to be used with null values.
        /// </summary>
        /// <param name="e">The error to create an expected from.</param>
        public static implicit operator Expected<T, E>(E e) => new Expected<T, E>(default, e);

        /// <summary>
        /// Creates an error result from the provided argument. It should not
        /// be null.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static Expected<T, E> MakeError(E e) => new Expected<T, E>(default, e);

        /// <summary>
        /// Maps an expected to another expected type.
        /// </summary>
        /// <typeparam name="U">The new type.</typeparam>
        /// <param name="mapFunc">A function that maps the expected to another
        /// type.</param>
        /// <returns>A new expected value from the mapping.</returns>
        public Expected<U, E> Map<U>(Func<T, Expected<U, E>> mapFunc) => HasValue ? mapFunc(Value) : Expected<U, E>.MakeError(Error);

        /// <summary>
        /// Maps the value to another value, or the error to a value.
        /// </summary>
        /// <typeparam name="U">The type to map it to.</typeparam>
        /// <param name="mapFunc">The mapping function for the value if it is
        /// present.</param>
        /// <param name="errorFunc">The error function for mapping to a value
        /// if the error is present.</param>
        /// <returns>A value from the functions provided depending on whether a
        /// value is present or not.</returns>
        public U MapValue<U>(Func<T, U> mapFunc, Func<E, U> errorFunc)
        {
            return HasValue ? mapFunc(Value) : errorFunc(Error);
        }

        /// <summary>
        /// Performs a function on the value if it is present. Otherwise the
        /// second function is called (or nothing is done if it is null).
        /// </summary>
        /// <param name="func">The function to apply if the value is present.
        /// </param>
        /// <param name="errorFunc">A function to call if the error is present.
        /// </param>
        public void Then(Action<T> func, Action<E> errorFunc = null)
        {
            if (HasValue)
                func(Value);
            else
                errorFunc?.Invoke(Error);
        }

        /// <summary>
        /// Gets the value if present, or calls the function provided to return
        /// a value.
        /// </summary>
        /// <param name="func">The function to call to generate a value if this
        /// expected is holding an error.</param>
        /// <returns>The value if present, or returns the value from calling
        /// the function.</returns>
        public T ValueOr(Func<T> func) => HasValue ? Value : func();
    }
}
