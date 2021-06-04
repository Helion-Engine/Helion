using System;
using System.Collections.Generic;
using Helion.Render.OpenGL.Shaders;
using Helion.Render.OpenGL.Textures;
using Helion.Util.Container;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Pipeline
{
    public class RenderTexturePipeline<TShader, TVertex> : RenderPipeline<TShader, TVertex>
        where TVertex : struct
        where TShader : ShaderProgram, new()
    {
        private readonly Dictionary<GLTexture, DynamicArray<TVertex>> m_textureNameToData = new();
        private readonly DynamicArray<BufferRenderInfo> m_bufferRenderInfos = new();

        public RenderTexturePipeline(string name) : 
            base(name, BufferUsageHint.StreamDraw, PrimitiveType.Triangles)
        {
        }

        public void Quad(GLTexture texture, TVertex topLeft, TVertex topRight, TVertex bottomLeft, TVertex bottomRight)
        {
            if (!m_textureNameToData.TryGetValue(texture, out DynamicArray<TVertex>? array))
            {
                array = new DynamicArray<TVertex>();
                m_textureNameToData[texture] = array;
            }
            
            array.Add(topLeft);
            array.Add(bottomLeft);
            array.Add(topRight);
            
            array.Add(topRight);
            array.Add(bottomLeft);
            array.Add(bottomRight);
        }
        
        private void PopulateBufferRenderInfo()
        {
            m_bufferRenderInfos.Clear();

            foreach ((GLTexture texture, DynamicArray<TVertex> array) in m_textureNameToData)
            {
                BufferRenderInfo info = new(texture, Vbo.Count, array.Length);
                m_bufferRenderInfos.Add(info);
                
                Vbo.AddRange(array);
            }
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
            PopulateBufferRenderInfo();
            Vbo.BindAndUploadIfNeeded();

            if (Vbo.Count > 0)
            {
                Shader.Bind();
                Attributes.Bind();
            
                action(Shader);

                for (int i = 0; i < m_bufferRenderInfos.Length; i++)
                {
                    BufferRenderInfo info = m_bufferRenderInfos[i];
                
                    GL.BindTexture(info.Target, info.GLTextureName);
                    GL.DrawArrays(PrimitiveType.Triangles, info.Start, info.Count);
                    GL.BindTexture(info.Target, 0);
                }
            
                Attributes.Unbind();
                Shader.Unbind();
            }

            Vbo.Clear();
            foreach (DynamicArray<TVertex> array in m_textureNameToData.Values)
                array.Clear();
        }
    }

    internal readonly struct BufferRenderInfo
    {
        internal readonly TextureTarget Target;
        internal readonly int GLTextureName;
        internal readonly int Start;
        internal readonly int Count;

        public BufferRenderInfo(GLTexture texture, int start, int count)
        {
            Target = texture.Target;
            GLTextureName = texture.TextureName;
            Start = start;
            Count = count;
        }
    }
}
