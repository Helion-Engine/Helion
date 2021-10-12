using System;

namespace Helion.Util.Assertion;

/// <summary>
/// An exception triggered when an assertion fails.
/// </summary>
[Serializable]
public class AssertionException : HelionException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssertionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public AssertionException(string message) :  base(message)
    {
    }
}

