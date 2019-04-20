using System;
using System.Runtime.Serialization;

namespace Helion.Util
{
    /// <summary>
    /// The base class that all exceptions for this project derive from.
    /// </summary>
    [Serializable]
    internal class HelionException : Exception
    {
        public HelionException()
        {
        }

        public HelionException(string message) : base(message)
        {
        }

        public HelionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected HelionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
