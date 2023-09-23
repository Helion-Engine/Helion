using NLog;
using System.Diagnostics;

namespace Helion.Util.Assertion;

/// <summary>
/// A collection of assertion methods that only work in debug mode.
/// </summary>
public static class Assert
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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
    /// Should be called when a state should never be reached. Will not be
    /// called in release mode.
    /// </summary>
    /// <param name="obj">The object that should have been disposed but was
    /// not.</param>
    [Conditional("DEBUG")]
    public static void FailedToDispose(object obj)
    {
#if ENABLE_FAILED_TO_DISPOSE
        Fail($"Forgot to dispose {obj.GetType().FullName}, finalizer run when it should not have");
#endif
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

    [Conditional("DEBUG")]
    public static void InvariantWarning(bool invariant, string reason)
    {
        if (!invariant)
            Log.Warn(reason);
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
