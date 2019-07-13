using System;
using System.Diagnostics;
using System.Reflection;
using Helion.Render.OpenGL.Buffers;
using MoreLinq;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Shader
{
    public class ShaderProgram : IDisposable
    {
        private readonly int m_programId;
        
        protected ShaderProgram(ShaderBuilder builder, VertexArrayAttributes attributes)
        {
            Precondition(builder.IsValid, "Must be a valid shader builder");

            // TODO: We should throw only after we've deallocated whatever has
            //       been created. It might be best to follow through to the
            //       end and then call Dispose() on any failures, and then have
            //       an exception be thrown so that way there's no leaking of
            //       resources.
            
            int vertexShader = CreateAndCompileShaderOrThrow(ShaderType.VertexShader, builder.VertexShaderText);
            int fragmentShader = CreateAndCompileShaderOrThrow(ShaderType.FragmentShader, builder.FragmentShaderText);

            m_programId = GL.CreateProgram();
            GL.AttachShader(m_programId, vertexShader);
            GL.AttachShader(m_programId, fragmentShader);

            attributes.ForEach(attr => GL.BindAttribLocation(m_programId, attr.Index, attr.Name));
            LinkProgramOrThrow();
            AssertAttributesMatch(attributes);

            GL.DetachShader(m_programId, vertexShader);
            GL.DetachShader(m_programId, fragmentShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteShader(vertexShader);

            IndexUniformsOrThrow();
        }
        
        ~ShaderProgram()
        {
            ReleaseUnmanagedResources();
        }
        
        public void Bind()
        {
            GL.UseProgram(m_programId);            
        }

        public void Unbind()
        {
            GL.UseProgram(0);
        }

        public void BindAnd(Action action)
        {
            Bind();
            action.Invoke();
            Unbind();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
        
        private int CreateAndCompileShaderOrThrow(ShaderType shaderType, string vertexShaderText)
        {
            int shaderId = GL.CreateShader(shaderType);
            GL.ShaderSource(shaderId, vertexShaderText);
            GL.CompileShader(shaderId);

            GL.GetShader(shaderId, ShaderParameter.CompileStatus, out int status);
            if (status != (int)All.True)
            {
                string errorMsg = GL.GetShaderInfoLog(shaderId);
                throw new ShaderException($"Error compiling shader {shaderType.ToString()}: {errorMsg}");
            }

            return shaderId;
        }
        
        private void LinkProgramOrThrow()
        {
            GL.LinkProgram(m_programId);

            GL.GetProgram(m_programId, GetProgramParameterName.LinkStatus, out var status);
            if (status != (int)All.True)
            {
                string errorMsg = GL.GetProgramInfoLog(m_programId);
                throw new ShaderException($"Error linking shader: {errorMsg}");
            }
        }

        [Conditional("DEBUG")]
        private void AssertAttributesMatch(VertexArrayAttributes attributes)
        {
            GL.GetProgram(m_programId, GetProgramParameterName.ActiveAttributes, out int numAttributes);
            Invariant(numAttributes == attributes.Count, "Attribute mismatch, shader attributes do not match VAO attribute size (did you forget some? or not remove some?)");
        }
        
        private static bool HasUniformAttribute(FieldInfo fieldInfo)
        {
            return fieldInfo.FieldType.IsDefined(typeof(ShaderUniformAttribute), true);
        }

        private void IndexUniformsOrThrow()
        {
            GL.GetProgram(m_programId, GetProgramParameterName.ActiveUniforms, out int numUniforms);

            for (int uniformIndex = 0; uniformIndex < numUniforms; uniformIndex++)
            {
                string name = GL.GetActiveUniform(m_programId, uniformIndex, out _, out _);
                int location = GL.GetUniformLocation(m_programId, name);
                Invariant(location != -1, $"Unable to index shader uniform {name}");

                FindAndSetUniformFieldIndexOrThrow(name, location);
            }
        }

        private void FindAndSetUniformFieldIndexOrThrow(string name, int location)
        {
            string lowerName = name.ToLower();
            
            foreach (FieldInfo field in GetType().GetFields())
            {
                if (!HasUniformAttribute(field) || field.Name.ToLower() != lowerName) 
                    continue;
                
                switch (field.GetValue(this))
                {
                case UniformInt uniformInt:
                    uniformInt.Location = location;
                    return;
                case UniformFloat uniformFloat:
                    uniformFloat.Location = location;
                    return;
                case UniformVec3 uniformVec3:
                    uniformVec3.Location = location;
                    return;
                case UniformVec4 uniformVec4:
                    uniformVec4.Location = location;
                    return;
                case UniformMatrix4 uniformMatrix4:
                    uniformMatrix4.Location = location;
                    return;
                default:
                    throw new ShaderException($"Unexpected uniform type for uniform '{name}' in class '{GetType().Name}' with field '{field.Name}'");                        
                }
            }

            throw new ShaderException($"Encountered uniform '{name}' which has no backing field in the class: {GetType().Name}");
        }

        private void ReleaseUnmanagedResources()
        {
            GL.DeleteProgram(m_programId);
        }
    }
}