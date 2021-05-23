using System;
using Helion.Render.OpenGL.Arrays;
using Helion.Render.OpenGL.Buffers;
using Helion.Render.OpenGL.Shaders;
using OpenTK.Graphics.OpenGL4;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Pipeline
{
    /// <summary>
    /// Often we want to put data into a buffer with some VAO, and render it
    /// with a shader specific to it. This is designed for those simple cases.
    /// If we want something advanced, we would not use this. However, it is
    /// likely the case this covers 95%+ of the use cases.
    /// </summary>
    public abstract class RenderPipelineBase<TShader, TVertex> : IDisposable
        where TVertex : struct
        where TShader : ShaderProgram, new()
    {
        public readonly TShader Shader;
        public readonly VertexBufferObject<TVertex> Vbo;
        public readonly VertexArrayObject<TVertex> Vao;
        protected readonly BufferUsageHint Hint;
        protected readonly PrimitiveType DrawType;
        private bool m_disposed;

        public RenderPipelineBase(string name, BufferUsageHint hint, PrimitiveType drawType)
        {
            Hint = hint;
            DrawType = drawType;
            Shader = new TShader();
            Vbo = new VertexBufferObject<TVertex>(hint);
            Vao = new VertexArrayObject<TVertex>();
            
            Vao.BindAttributes(Vbo);
            Shader.CheckVertexAttributesOrThrow<TVertex>();
            
            Vbo.SetDebugLabel(name);
            Vao.SetDebugLabel(name);
            Shader.SetDebugLabel(name);
        }

        ~RenderPipelineBase()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        /// <summary>
        /// Will upload any data that hasn't been uploaded, bind, pass you the
        /// shader so you can set uniforms, and draw. If it's a stream buffer,
        /// the buffer will be cleared afterwards.
        /// </summary>
        /// <param name="action">Instructions to be called after the shader is
        /// bound.</param>
        public abstract void Draw(Action<TShader> action);

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        private void PerformDispose()
        {
            if (m_disposed)
                return;

            Vao.Dispose();
            Vbo.Dispose();
            Shader.Dispose();

            m_disposed = true;
        }
    }
}
