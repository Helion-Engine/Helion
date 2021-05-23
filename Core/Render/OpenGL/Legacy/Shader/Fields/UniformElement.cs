using Helion.Render.OpenGL.Legacy.Context;

namespace Helion.Render.OpenGL.Legacy.Shader.Fields
{
    [Uniform]
    public abstract class UniformElement<T> where T : struct
    {
        internal const int NoLocation = -1;

        protected internal int Location = NoLocation;

        public abstract void Set(IGLFunctions gl, T value);
    }
}