using System;
using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Shader;

public abstract class RenderProgram : IDisposable
{
    public readonly string Label;
    public readonly ProgramUniforms Uniforms = new();
    public readonly ProgramAttributes Attributes = new();
    private int m_program;
    private bool m_disposed;

    protected RenderProgram(string label)
    {
        Label = label;
        m_program = GL.CreateProgram();
        CreateAndCompileShaderOrThrow();

        Bind();
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Program, m_program, $"Program: {label}");
        Uniforms.Populate(m_program);
        Attributes.Populate(m_program);
        Unbind();
    }

    ~RenderProgram()
    {
        PerformDispose();
    }

    protected abstract string VertexShader();
    protected virtual string? GeometryShader() => null;
    protected virtual string? FragmentShader() => null;

    private void CreateAndCompileShaderOrThrow()
    {
        (int? vertex, int? geometry, int? fragment) = CompileShadersOrThrow();

        GL.LinkProgram(m_program);
        ThrowIfLinkFailure();

        DetachAndDelete(m_program, vertex);
        DetachAndDelete(m_program, geometry);
        DetachAndDelete(m_program, fragment);
    }

    private (int? vertex, int? geometry, int? fragment) CompileShadersOrThrow()
    {
        int? vertexShader = null;
        int? geometryShader = null;
        int? fragmentShader = null;

        try
        {
            vertexShader = CompileShaderOrThrow(VertexShader(), ShaderType.VertexShader);
            geometryShader = CompileShaderOrThrow(GeometryShader(), ShaderType.GeometryShader);
            fragmentShader = CompileShaderOrThrow(FragmentShader(), ShaderType.FragmentShader);
            return (vertexShader, geometryShader, fragmentShader);
        }
        catch
        {
            if (vertexShader != null)
            {
                GL.DetachShader(m_program, vertexShader.Value);
                GL.DeleteShader(vertexShader.Value);
            }

            if (geometryShader != null)
            {
                GL.DetachShader(m_program, geometryShader.Value);
                GL.DeleteShader(geometryShader.Value);

            }
            
            if (fragmentShader != null)
            {
                GL.DetachShader(m_program, fragmentShader.Value);
                GL.DeleteShader(fragmentShader.Value);
            }

            throw;
        }
    }

    private static void DetachAndDelete(int program, int? shader)
    {
        if (shader == null)
            return;

        GL.DetachShader(program, shader.Value);
        GL.DeleteShader(shader.Value);
    }

    private void ThrowIfLinkFailure()
    {
        GL.GetProgram(m_program, GetProgramParameterName.LinkStatus, out int status);
        if (status == GLHelper.GLTrue)
            return;

        string errorMsg = GL.GetProgramInfoLog(m_program);
        throw new($"Error linking render shader: {errorMsg}");
    }

    private int? CompileShaderOrThrow(string? source, ShaderType type)
    {
        if (source == null)
            return null;

        int shaderHandle = GL.CreateShader(type);
        GL.ShaderSource(shaderHandle, source);
        GL.CompileShader(shaderHandle);
        ThrowIfShaderCompileFailure(shaderHandle, type);

        GL.AttachShader(m_program, shaderHandle);
        return shaderHandle;
    }

    private static void ThrowIfShaderCompileFailure(int shaderHandle, ShaderType type)
    {
        GL.GetShader(shaderHandle, ShaderParameter.CompileStatus, out int status);
        if (status == GLHelper.GLTrue)
            return;

        string errorMsg = GL.GetShaderInfoLog(shaderHandle);
        throw new($"Error compiling render shader {type}: {errorMsg}");
    }

    public void Bind()
    {
        GL.UseProgram(m_program);
    }

    public void Unbind()
    {
        GL.UseProgram(0);
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

        GL.DeleteProgram(m_program);
        m_program = 0;

        m_disposed = true;
    }
}
