using Helion.Render.OpenGL.Shared.Buffer.Vao;
using Helion.Util;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL.Shared.Shader
{
    public class ShaderProgram : IDisposable
    {
        // TODO: This class needs to intelligently clean up after itself. For
        // example, if the fragment shader throws, the vertex shader should be
        // deleted. Likewise, if linking a program fails, the previous shader 
        // creation ids should be deleted and the program deleted.

        // TODO: To avoid throwing, we should move the constructor logic into
        // the static create method, and return null/deallocate correctly on
        // any failure.

        protected bool disposed;
        private readonly int program;
        private readonly Dictionary<string, int> uniforms = new Dictionary<string, int>();

        private ShaderProgram(ShaderBuilder builder, VertexArrayObject? vao = null)
        {
            Precondition(builder.IsValid, "Must be a valid shader builder");

            int vertexShader = CreateAndCompileShaderOrThrow(ShaderType.VertexShader, builder.VertexShaderText);
            int fragmentShader = CreateAndCompileShaderOrThrow(ShaderType.FragmentShader, builder.FragmentShaderText);

            program = GL.CreateProgram();
            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);
            if (vao != null)
                SetAttributeLocations(vao);
            LinkProgramOrThrow();

            GL.DetachShader(program, vertexShader);
            GL.DetachShader(program, fragmentShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteShader(vertexShader);

            IndexUniformsOrThrow();
        }

        ~ShaderProgram() => Dispose(false);

        public static ShaderProgram? Create(ShaderBuilder builder, VertexArrayObject? vao = null)
        {
            return builder.IsValid ? new ShaderProgram(builder, vao) : null;
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
                throw new HelionException($"Error compiling shader {shaderType.ToString()}: {errorMsg}");
            }

            return shaderId;
        }

        private void SetAttributeLocations(VertexArrayObject vao) => vao.BindShaderLocations(program);

        private void LinkProgramOrThrow()
        {
            GL.LinkProgram(program);

            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var status);
            if (status != (int)All.True)
            {
                string errorMsg = GL.GetProgramInfoLog(program);
                throw new HelionException($"Error linking shader: {errorMsg}");
            }
        }

        private void IndexUniformsOrThrow()
        {
            GL.GetProgram(program, GetProgramParameterName.ActiveUniforms, out int numUniforms);

            for (int uniformIndex = 0; uniformIndex < numUniforms; uniformIndex++)
            {
                string name = GL.GetActiveUniform(program, uniformIndex, out _, out _);
                int location = GL.GetUniformLocation(program, name);
                Invariant(location != -1, $"Unable to index shader uniform {name}");

                uniforms[name] = location;
            }
        }

        protected void Bind() => GL.UseProgram(program);

        protected void Unbind() => GL.UseProgram(0);

        public void BindAnd(Action action)
        {
            Bind();
            action.Invoke();
            Unbind();
        }

        public void SetInt(string name, int data) => GL.Uniform1(uniforms[name], data);
        public void SetFloat(string name, float data) => GL.Uniform1(uniforms[name], data);
        public void SetVector3(string name, Vector3 data) => GL.Uniform3(uniforms[name], data);
        public void SetVector4(string name, Vector4 data) => GL.Uniform4(uniforms[name], data);
        public void SetMatrix(string name, Matrix4 data) => GL.UniformMatrix4(uniforms[name], true, ref data);

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                GL.DeleteProgram(program);

            disposed = true;
        }

        public void Dispose() => Dispose(true);
    }
}
