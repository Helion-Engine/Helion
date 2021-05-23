using System;
using System.Collections.Generic;
using System.Reflection;
using Helion.Render.OpenGL.Arrays;
using Helion.Render.OpenGL.Shaders.Uniforms;
using OpenTK.Graphics.OpenGL4;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Shaders
{
    public abstract class ShaderProgram : IDisposable
    {
        private int m_glName;
        private bool m_disposed;

        protected ShaderProgram()
        {
            m_glName = GL.CreateProgram();

            CreateAndCompileShaderOrThrow();
            AssignIndicesToUniformsOrThrow();
        }

        ~ShaderProgram()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public void SetDebugLabel(string name)
        {
            GLUtil.Label($"Shader: {name}", ObjectLabelIdentifier.Program, m_glName);
        }
        
        protected abstract string VertexShader();
        
        protected abstract string FragmentShader();
        
        private void CreateAndCompileShaderOrThrow()
        {
            // TODO: Free properly on failure, if someone catches then we have a problem...
            
            IEnumerable<int> shaderHandles = CompileShadersOrThrow();

            GL.LinkProgram(m_glName);
            ThrowIfLinkFailure();

            foreach (int shaderHandle in shaderHandles)
            {
                GL.DetachShader(m_glName, shaderHandle);
                GL.DeleteShader(shaderHandle);
            }
        }

        private IEnumerable<int> CompileShadersOrThrow()
        {
            int vertexShader = CompileShaderOrThrow(VertexShader(), ShaderType.VertexShader);
            int fragmentShader = CompileShaderOrThrow(FragmentShader(), ShaderType.FragmentShader);
            return new[] { vertexShader, fragmentShader };
        }

        private void ThrowIfLinkFailure()
        {
            GL.GetProgram(m_glName, GetProgramParameterName.LinkStatus, out int status);
            if (status == GLUtil.GLTrue) 
                return;
            
            string errorMsg = GL.GetProgramInfoLog(m_glName);
            throw new ShaderException($"Error linking shader: {errorMsg}");
        }

        private int CompileShaderOrThrow(string source, ShaderType type)
        {
            int shaderHandle = GL.CreateShader(type);
            GL.ShaderSource(shaderHandle, source);
            GL.CompileShader(shaderHandle);
            ThrowIfShaderCompileFailure(shaderHandle, type);

            GL.AttachShader(m_glName, shaderHandle);
            return shaderHandle;
        }

        private static void ThrowIfShaderCompileFailure(int shaderHandle, ShaderType type)
        {
            GL.GetShader(shaderHandle, ShaderParameter.CompileStatus, out int status);
            if (status == GLUtil.GLTrue) 
                return;
            
            string errorMsg = GL.GetShaderInfoLog(shaderHandle);
            throw new ShaderException($"Error compiling shader {type}: {errorMsg}");
        }

        private void AssignIndicesToUniformsOrThrow()
        {
            GL.GetProgram(m_glName, GetProgramParameterName.ActiveUniforms, out int uniformCount);
            
            for (int i = 0; i < uniformCount; i++)
            {
                string name = GL.GetActiveUniform(m_glName, i, out _, out _);
                int location = GL.GetUniformLocation(m_glName, name);
                Invariant(location != -1, $"Unable to index shader uniform (index {i}): {name}");

                FindAndSetUniformFieldIndexOrThrow(name, location);
            }
        }
        
        private static bool HasUniformAttribute(FieldInfo fieldInfo)
        {
            return fieldInfo.FieldType.IsDefined(typeof(UniformAttribute), true);
        }
        
        private void FindAndSetUniformFieldIndexOrThrow(string name, int location)
        {
            string lowerName = name.ToLower();
            
            foreach (FieldInfo field in GetType().GetFields())
            {
                if (!HasUniformAttribute(field) || field.Name.ToLower() != lowerName) 
                    continue;

                if (field.GetValue(this) is not Uniform uniform)
                    throw new Exception($"Attribute on non-uniform class: {field.Name}");
                
                uniform.Location = location;
                return;
            }

            throw new ShaderException($"Encountered uniform '{name}' which has no backing field in the class: {GetType().Name}");
        }

        public void CheckVertexAttributesOrThrow<TVertex>() where TVertex : struct
        {
            int attributeCount = VertexArrayAttribute.FindAttributes<TVertex>().Count;
            GL.GetProgram(m_glName, GetProgramParameterName.ActiveAttributes, out int attribCount);
            
            // For now, we only check if the attributes match.
            // TODO: Support checking types as well.
            if (attribCount != attributeCount)
                throw new Exception($"Shader has {attributeCount}, but vertices have {attribCount} attributes");
        }

        public void Bind()
        {
            GL.UseProgram(m_glName);            
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
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        private void PerformDispose()
        {
            if (m_disposed)
                return;
            
            GL.DeleteProgram(m_glName);
            m_glName = 0;

            m_disposed = true;
        }
    }
}
