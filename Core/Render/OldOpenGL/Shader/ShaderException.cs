using System;
using System.Runtime.Serialization;

namespace Helion.Render.OldOpenGL.Shader
{
    public class ShaderException : Exception
    {
        public ShaderException()
        {
        }

        public ShaderException(string message) : base(message)
        {
        }

        public ShaderException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ShaderException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}