using System;
using System.Diagnostics;
using System.Reflection;
using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Context.Types;
using Helion.Render.Legacy.Shader.Fields;
using Helion.Render.Legacy.Util;
using Helion.Render.Legacy.Vertex;
using MoreLinq.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Legacy.Shader
{
    public class ShaderProgram : IDisposable
    {
        private readonly IGLFunctions gl;
        private readonly int m_programId;

        public ShaderProgram(IGLFunctions functions, ShaderBuilder builder, VertexArrayAttributes attributes)
        {
            gl = functions;
            m_programId = gl.CreateProgram();
            
            builder.Vertex.AttachAnd(m_programId, () =>
            {
                builder.Fragment.AttachAnd(m_programId, () =>
                {
                    // TODO: This will cause us to not detach if any throw.
                    attributes.ForEach(attr => gl.BindAttribLocation(m_programId, attr.Index, attr.Name));
                    LinkProgramOrThrow();
                    AssertAttributesMatch(attributes);
                });
            });
            
            IndexUniformsOrThrow();
        }
        
        ~ShaderProgram()
        {
            FailedToDispose(this);
            ReleaseUnmanagedResources();
        }
        
        public void Bind()
        {
            gl.UseProgram(m_programId);            
        }

        public void Unbind()
        {
            gl.UseProgram(0);
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
        
        private static bool HasUniformAttribute(FieldInfo fieldInfo)
        {
            return fieldInfo.FieldType.IsDefined(typeof(UniformAttribute), true);
        }
                
        [Conditional("DEBUG")]
        private void AssertAttributesMatch(VertexArrayAttributes attributes)
        {
            gl.GetProgram(m_programId, GetProgramParameterType.ActiveAttributes, out int numAttributes);
            Invariant(numAttributes == attributes.Count, "Attribute mismatch, shader attributes do not match VAO attribute size (did you forget some? or not remove some?)");
        }
        
        private void IndexUniformsOrThrow()
        {
            gl.GetProgram(m_programId, GetProgramParameterType.ActiveUniforms, out int numUniforms);

            for (int uniformIndex = 0; uniformIndex < numUniforms; uniformIndex++)
            {
                string name = gl.GetActiveUniform(m_programId, uniformIndex, out _, out _);
                int location = gl.GetUniformLocation(m_programId, name);
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
                case UniformMatrix4 uniformMatrix:
                    uniformMatrix.Location = location;
                    return;
                case UniformVec3 uniformVec3:
                    uniformVec3.Location = location;
                    return;
                default:
                    throw new ShaderException($"Unexpected uniform type for uniform '{name}' in class '{GetType().Name}' with field '{field.Name}'");                        
                }
            }

            throw new ShaderException($"Encountered uniform '{name}' which has no backing field in the class: {GetType().Name}");
        }
        
        private void LinkProgramOrThrow()
        {
            gl.LinkProgram(m_programId);

            gl.GetProgram(m_programId, GetProgramParameterType.LinkStatus, out var status);
            if (status == GLHelper.GLTrue)
                return;

            string errorMsg = gl.GetProgramInfoLog(m_programId);
            throw new ShaderException($"Error linking shader: {errorMsg}");
        }

        private void ReleaseUnmanagedResources()
        {
            gl.DeleteProgram(m_programId);
        }
    }
}