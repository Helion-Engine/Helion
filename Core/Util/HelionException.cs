using System;

namespace Helion.Util;

/// <summary>
/// The base class that all exceptions for this project derive from. These
/// are to be reserved for critical errors that should never happen.
/// </summary>
[Serializable]
public class HelionException : Exception
{
    /// <summary>
    /// Creates an exception that is unique to the Helion project.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public HelionException(string message) : base(message)
    {
    }
}
