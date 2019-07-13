using System;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util
{
    /// <summary>
    /// Represents a value, or a missing value with an error message.
    /// </summary>
    /// <typeparam name="T">The type to hold.</typeparam>
    public class Expected<T> where T : class
    {
        /// <summary>
        /// The value that is expected. This should not be accessed until
        /// HasValue is checked for being true.
        /// </summary>
        public T? Value { get; }

        /// <summary>
        /// The error result if the value is not present. This should not be
        /// accessed until HasValue is checked for being false.
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// Tells us whether a value is present (true) or an error is present
        /// (false).
        /// </summary>
        public bool HasValue => Value != null;

        private Expected(T? value, string? error = null)
        {
            Precondition(value != null ^ error != null, "Trying to make an expected with both value/error being both null or both empty");

            Value = value;
            Error = error ?? "";
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
        public static implicit operator bool(Expected<T> expected) => expected.HasValue;

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
        public static implicit operator Expected<T>(T t) => new Expected<T>(t);

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
        public static implicit operator Expected<T>(string e) => new Expected<T>(default, e);

        /// <summary>
        /// Creates an error result from the provided argument. It should not
        /// be null.
        /// </summary>
        /// <param name="e">The error message.</param>
        /// <returns>An empty expected.</returns>
        public static Expected<T> MakeError(string e) => new Expected<T>(default, e);

        /// <summary>
        /// Maps an expected to another expected type.
        /// </summary>
        /// <typeparam name="U">The new type.</typeparam>
        /// <param name="mapFunc">A function that maps the expected to another
        /// type.</param>
        /// <returns>A new expected value from the mapping.</returns>
        public Expected<U> Map<U>(Func<T, Expected<U>> mapFunc) where U : class
        {
            if (Value != null)
                return mapFunc(Value);
            if (Error != null)
                return Expected<U>.MakeError(Error);
            throw new HelionException("Malformed expected, has neither value nor error");
        }

        /// <summary>
        /// Performs a function on the value if it is present. Otherwise the
        /// second function is called (or nothing is done if it is null).
        /// </summary>
        /// <param name="func">The function to apply if the value is present.
        /// </param>
        /// <param name="errorFunc">A function to call if the error is present.
        /// </param>
        public void Then(Action<T> func, Action<string>? errorFunc = null)
        {
            if (Value != null)
                func(Value);
            else if (Error != null)
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
        public T ValueOr(Func<T> func) => Value ?? func();
    }
}