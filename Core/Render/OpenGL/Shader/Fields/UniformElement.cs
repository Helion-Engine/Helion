using Helion.Render.OpenGL.Context;

namespace Helion.Render.OpenGL.Shader.Fields
{
    [Uniform]
    public abstract class UniformElement<T> where T : struct
    {
        internal const int NoLocation = -1;

        protected readonly IGLFunctions gl;
        protected internal int Location = NoLocation;

        protected UniformElement(IGLFunctions functions)
        {
            gl = functions;
        }

        public abstract void Set(T value);
    }
}