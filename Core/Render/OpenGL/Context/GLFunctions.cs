using System;
using Helion.Render.OpenGL.Context.Types;

namespace Helion.Render.OpenGL.Context
{
    public abstract class GLFunctions
    {
        public abstract void BlendFunc(BlendingFactorType sourceFactor, BlendingFactorType destFactor);
        public abstract void Clear(ClearType type);
        public abstract void ClearColor(float r, float g, float b, float a);
        public abstract void CullFace(CullFaceType type);
        public abstract void DebugMessageCallback(Action<DebugLevel, string> callback);
        public abstract void Enable(EnableType type);
        public abstract void FrontFace(FrontFaceType type);
        public abstract ErrorType GetError();
        public abstract int GetInteger(GetIntegerType type);
        public abstract string GetString(GetStringType type);
        public abstract string GetString(GetStringType type, int index);
        public abstract void PolygonMode(PolygonFaceType faceType, PolygonModeType fillType);
        public abstract void Viewport(int x, int y, int width, int height);
    }
}