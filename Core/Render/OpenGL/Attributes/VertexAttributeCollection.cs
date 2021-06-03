using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Helion.Render.OpenGL.Buffers;
using Helion.Render.OpenGL.Shaders;
using Helion.Render.OpenGL.Shaders.Attributes;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Attributes
{
    public class VertexAttributeCollection
    {
        private readonly List<VertexShaderAttribute> m_attributes;
        
        public VertexAttributeCollection(ShaderProgram program)
        {
            m_attributes = program.Attributes.ToList();
        }

        public void EnableAttributesOnVboOrThrow<TVertex>(VertexBufferObject<TVertex> vbo, int offset = 0) 
            where TVertex : struct
        {
            List<VertexAttributeArrayElement> attributes = VertexStructAttributes.GetAttributes<TVertex>();
            if (attributes.Count != m_attributes.Count)
                throw new Exception($"Mismatch in shader attributes to struct ({typeof(TVertex).FullName}) attributes ");

            vbo.Bind();

            IntPtr ptrOffset = new IntPtr(offset);
            int vertexByteSize = Marshal.SizeOf<TVertex>();
            foreach (VertexAttributeArrayElement attr in attributes)
            {
                int location = LookupLocationOrThrow<TVertex>(attr);
                
                GL.EnableVertexAttribArray(location);
                GL.VertexAttribPointer(location, attr.Size, attr.Type, attr.Normalized, vertexByteSize, ptrOffset);
                GL.DisableVertexAttribArray(location);
            }
            
            vbo.Unbind();
        }

        private int LookupLocationOrThrow<TVertex>(VertexAttributeArrayElement attr)
        {
            foreach (var attribute in m_attributes)
                if (attribute.Name.Equals(attr.Name))
                    return attribute.Location;

            throw new Exception($"Unable to locate attribute named {attr.Name} in class {typeof(TVertex).FullName}");
        }
        
        public void Bind()
        {
            foreach (VertexShaderAttribute attr in m_attributes)
                GL.EnableVertexAttribArray(attr.Location);
        }
        
        public void Unbind()
        {
            foreach (VertexShaderAttribute attr in m_attributes)
                GL.DisableVertexAttribArray(attr.Location);
        }

        public void BindAnd(Action action)
        {
            Bind();
            action();
            Unbind();
        }
    }
}
