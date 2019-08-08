using Helion.Render.OpenGL.Context.Enums;

namespace Helion.Render.OpenGL.Context
{
    public abstract class GLFunctions
    {
        public abstract void Clear(ClearType type);
        
        public abstract void ClearColor(float r, float g, float b, float a);
        
        public abstract ErrorType GetError();

        public abstract int GetInteger(GetIntegerType type);

        public abstract string GetString(GetStringType type);

        public abstract string GetString(GetStringType type, int index);

        public abstract void Viewport(int x, int y, int width, int height);
    }
}