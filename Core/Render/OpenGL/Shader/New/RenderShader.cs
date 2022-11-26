using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Helion.Render.OpenGL.Shader.New;

public abstract class RenderShader : IDisposable
{
    public readonly UniformManager Uniforms = new();
    private int m_program;
    private bool m_disposed;

    protected RenderShader(string label)
    {
        m_program = GL.CreateProgram();
        CreateAndCompileShaderOrThrow();

        Bind();
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Program, m_program, label);
        Uniforms.Populate(m_program);
        Unbind();
    }

    ~RenderShader()
    {
        PerformDispose();
    }

    protected abstract string VertexShader();
    protected abstract string FragmentShader();

    private void CreateAndCompileShaderOrThrow()
    {
        IEnumerable<int> shaderHandles = CompileShadersOrThrow();

        GL.LinkProgram(m_program);
        ThrowIfLinkFailure();

        foreach (int shaderHandle in shaderHandles)
        {
            GL.DetachShader(m_program, shaderHandle);
            GL.DeleteShader(shaderHandle);
        }
    }

    private IEnumerable<int> CompileShadersOrThrow()
    {
        int? vertexShader = null;
        int? fragmentShader = null;

        try
        {
            vertexShader = CompileShaderOrThrow(VertexShader(), ShaderType.VertexShader);
            fragmentShader = CompileShaderOrThrow(FragmentShader(), ShaderType.FragmentShader);
            return new[] { vertexShader.Value, fragmentShader.Value };
        }
        catch
        {
            if (vertexShader != null)
            {
                GL.DetachShader(m_program, vertexShader.Value);
                GL.DeleteShader(vertexShader.Value);
            }

            if (fragmentShader != null)
            {
                GL.DetachShader(m_program, fragmentShader.Value);
                GL.DeleteShader(fragmentShader.Value);
            }

            throw;
        }
    }

    private void ThrowIfLinkFailure()
    {
        GL.GetProgram(m_program, GetProgramParameterName.LinkStatus, out int status);
        if (status == GLHelper.GLTrue)
            return;

        string errorMsg = GL.GetProgramInfoLog(m_program);
        throw new($"Error linking render shader: {errorMsg}");
    }

    private int CompileShaderOrThrow(string source, ShaderType type)
    {
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
        throw new Exception($"Error compiling render shader {type}: {errorMsg}");
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
