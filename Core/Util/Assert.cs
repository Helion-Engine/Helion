using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Helion.Util
{
    /// <summary>
    /// A collection of assertion methods that only work in debug mode.
    /// </summary>
    public static class Assert
    {
        /// <summary>
        /// Should be called when a state should never be reached. Will not be
        /// called in release mode.
        /// </summary>
        /// <param name="reason">The reason for the assertion failure.</param>
        [Conditional("DEBUG")]
        public static void Fail(string reason)
        {
            throw new AssertionException(reason);
        }

        /// <summary>
        /// Checks for a precondition, throws if the precondition is false. 
        /// This is intended to be used to assert incoming data is valid.
        /// </summary>
        /// <param name="precondition">An expression that should be true. If
        /// it is false, it throws.</param>
        /// <param name="reason">The reason for this precondition.</param>
        [Conditional("DEBUG")]
        public static void Precondition(bool precondition, string reason)
        {
            if (!precondition)
                Fail(reason);
        }


        /// <summary>
        /// Checks for an invariant, throws if the invariant is false. 
        /// This is intended to be used to track the internal state, indicating
        /// whether or not the logic that has been written holds.
        /// </summary>
        /// <param name="invariant">An expression that should be true. If it is
        /// is false, it throws.</param>
        /// <param name="reason">The reason for this invariant.</param>
        [Conditional("DEBUG")]
        public static void Invariant(bool invariant, string reason)
        {
            if (!invariant)
                Fail(reason);
        }

        /// <summary>
        /// Checks for a postcondition, throws if the postcondition is false. 
        /// This is intended to be used to assert that the data being returned
        /// is correct.
        /// </summary>
        /// <param name="postcondition">An expression that should be true. If
        /// it is false, it throws.</param>
        /// <param name="reason">The reason for this postcondition.</param>
        [Conditional("DEBUG")]
        public static void Postcondition(bool postcondition, string reason)
        {
            if (!postcondition)
                Fail(reason);
        }
    }

    [Serializable]
    internal class AssertionException : HelionException
    {
        public AssertionException() { }
        public AssertionException(string message) : base(message) { }
        public AssertionException(string message, Exception innerException) : base(message, innerException) { }
        protected AssertionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
