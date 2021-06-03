using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Helion.Render.OpenGL.Buffers;
using Helion.Render.OpenGL.Shaders;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Attributes
{
    public class VertexAttributeCollection<TVertex> where TVertex : struct
    {
        private readonly List<VertexAttributeElement> m_attributes;
        
        private VertexAttributeCollection(List<VertexAttributeElement> attributes)
        {
            m_attributes = attributes;
        }

        public static VertexAttributeCollection<T> CreateOrThrow<T>(ShaderProgram program, VertexBufferObject<T> vbo)
            where T : struct
        {
            List<VertexAttributeElement> attributes = new();
            
            // TODO
            
            return new VertexAttributeCollection<T>(attributes);
        }
        
        private static bool HasNormalizedAttribute(FieldInfo fieldInfo)
        {
            return fieldInfo.GetCustomAttributes().OfType<NormalizedAttribute>().Any();
        }

        // public void EnableAttributesOnVboOrThrow(VertexBufferObject<TVertex> vbo, int offset = 0)
        // {
        //     // if (attributes.Count != m_attributes.Count)
        //         // throw new Exception($"Mismatch in shader attributes to struct ({typeof(TVertex).FullName}) attributes ");
        //
        //     vbo.Bind();
        //
        //     IntPtr ptrOffset = new IntPtr(offset);
        //     int vertexByteSize = Marshal.SizeOf<TVertex>();
        //     foreach (VertexAttributeElement attr in attributes)
        //     {
        //         int location = LookupLocationOrThrow(attr);
        //         
        //         GL.EnableVertexAttribArray(location);
        //         GL.VertexAttribPointer(location, attr.Size, attr.Type, attr.Normalized, vertexByteSize, ptrOffset);
        //         GL.DisableVertexAttribArray(location);
        //     }
        //     
        //     vbo.Unbind();
        // }

        // private int LookupLocationOrThrow(VertexAttributeElement attr)
        // {
        //     foreach (var attribute in m_attributes)
        //         if (attribute.Name.Equals(attr.Name))
        //             return attribute.Location;
        //
        //     throw new Exception($"Unable to locate attribute named {attr.Name} in class {typeof(TVertex).FullName}");
        // }
        
        public void Bind()
        {
            foreach (VertexAttributeElement attr in m_attributes)
                GL.EnableVertexAttribArray(attr.Location);
        }
        
        public void Unbind()
        {
            foreach (VertexAttributeElement attr in m_attributes)
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
