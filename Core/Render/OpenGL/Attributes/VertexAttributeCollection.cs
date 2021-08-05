using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Buffers;
using Helion.Render.OpenGL.Primitives;
using Helion.Render.OpenGL.Shaders;
using Helion.Render.OpenGL.Shaders.Attributes;
using Helion.Render.OpenGL.Util;
using NLog;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Attributes
{
    public class VertexAttributeCollection
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly List<VertexAttributeElement> m_attributes;
        
        private VertexAttributeCollection(List<VertexAttributeElement> attributes)
        {
            m_attributes = attributes;
        }

        public static VertexAttributeCollection CreateOrThrow<T>(ShaderProgram program) where T : struct
        {
            List<VertexAttributeElement> attributes = new();

            List<AttributeFieldInfo> mapping = GetAttributeFieldMapping<T>();
            foreach (AttributeFieldInfo info in mapping)
            {
                int? location = FindLocation(info.Name, program.Attributes);
                if (location == null)
                {
                    Log.Error($"Missing GL attribute {info.Name} on {typeof(T).FullName}");
                    continue;
                }
                
                VertexAttributeElement element = new(location.Value, info.Name, info.Size, info.Normalized, info.Offset, 
                    VertexBufferObject<T>.BytesPerElement, info.AttribType);
                attributes.Add(element);
            }

            if (mapping.Count > attributes.Count)
                throw new Exception($"{typeof(T).FullName} is missing required GL attribute mappings");
            
            return new VertexAttributeCollection(attributes);
        }

        private static int? FindLocation(string name, IReadOnlyList<VertexShaderAttribute> shaderAttribs)
        {
            foreach (VertexShaderAttribute attrib in shaderAttribs)
                if (name.Equals(attrib.Name, StringComparison.OrdinalIgnoreCase))
                    return attrib.Location;
            return null;
        }

        private static List<AttributeFieldInfo> GetAttributeFieldMapping<T>() where T : struct
        {
            Type type = typeof(T);
            List<AttributeFieldInfo> infos = new();
            
            int offset = 0;
            foreach (FieldInfo fieldInfo in type.GetFields())
            {
                string name = fieldInfo.Name;
                bool normalized = HasNormalizedAttribute(fieldInfo);
                (int size, int bytesPerSize, VertexAttribPointerType attrib) = GetSizeAndTypeOrThrow(fieldInfo);

                AttributeFieldInfo info = new(name, size, bytesPerSize, normalized, offset, attrib);
                infos.Add(info);

                offset += size * bytesPerSize;
            }
            
            return infos;
        }

        private static (int size, int bytesPerSize, VertexAttribPointerType attrib) GetSizeAndTypeOrThrow(
            FieldInfo fieldInfo)
        {
            Type type = fieldInfo.FieldType;
            if (type == typeof(float))
                return (1, sizeof(float), VertexAttribPointerType.Float);
            if (type == typeof(vec2) || type == typeof(Vec2F))
                return (2, sizeof(float), VertexAttribPointerType.Float);
            if (type == typeof(vec3) || type == typeof(Vec3F))
                return (3, sizeof(float), VertexAttribPointerType.Float);
            if (type == typeof(vec4) || type == typeof(Vec4F))
                return (4, sizeof(float), VertexAttribPointerType.Float);
            if (type == typeof(ByteColor))
                return (4, sizeof(byte), VertexAttribPointerType.UnsignedByte);

            throw new Exception($"Unexpected vertex attribute type in struct: {fieldInfo.Name} ({type.FullName})");
        }

        private static bool HasNormalizedAttribute(FieldInfo fieldInfo)
        {
            return fieldInfo.GetCustomAttributes().OfType<NormalizedAttribute>().Any();
        }

        public void BindAttributesToVbo<T>(VertexBufferObject<T> vbo) where T : struct
        {
            vbo.Bind();

            foreach (VertexAttributeElement attr in m_attributes)
            {
                GL.EnableVertexAttribArray(attr.Location);
                GL.VertexAttribPointer(attr.Location, attr.Size, attr.Type, attr.Normalized, attr.Stride, attr.Offset);
                GL.DisableVertexAttribArray(attr.Location);
            }
            
            vbo.Unbind();
        }
        
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
