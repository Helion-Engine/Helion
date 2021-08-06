using System;
using Helion.Render.OpenGL.Shaders;
using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Pipeline
{
    /// <summary>
    /// Often we want to put data into a buffer with some VAO, and render it
    /// with a shader specific to it. This is designed for those simple cases.
    /// If we want something advanced, we would not use this. However, it is
    /// likely the case this covers 95%+ of the use cases.
    /// </summary>
    public class RenderPipeline<TShader, TVertex> : RenderPipelineBase<TShader, TVertex>
        where TVertex : struct
        where TShader : ShaderProgram, new()
    {
        public RenderPipeline(string name, BufferUsageHint hint, PrimitiveType drawType) :
            base(name, hint, drawType)
        {
        }

        /// <summary>
        /// Will upload any data that hasn't been uploaded, bind, pass you the
        /// shader so you can set uniforms, and draw. If it's a stream buffer,
        /// the buffer will be cleared afterwards.
        /// </summary>
        /// <param name="action">Instructions to be called after the shader is
        /// bound.</param>
        public override void Draw(Action<TShader> action)
        {
            Vbo.BindAndUploadIfNeeded();

            if (Vbo.Count > 0)
            {
                Shader.Bind();
                Vbo.Bind();
                Attributes.Bind();
            
                action(Shader);
                GL.DrawArrays(DrawType, 0, Vbo.Count);
            
                Attributes.Unbind();
                Vbo.Unbind();
                Shader.Unbind();
            }

            if (Hint.IsStream())
                Vbo.Clear();
        }
    }
}
