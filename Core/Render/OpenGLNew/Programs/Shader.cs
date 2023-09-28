using System;
using Helion.Render.OpenGL.Util;
using Helion.Render.OpenGLNew.Util;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGLNew.Programs;

public class Shader : IDisposable
{
    public readonly ShaderType ShaderType;
    private int m_shaderId;
    private bool m_disposed;

    public Shader(string label, ShaderType shaderType, string source)
    {
        ShaderType = shaderType;
        m_shaderId = GL.CreateShader(shaderType);
        CompileShaderOrThrow(source);
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Shader, m_shaderId, $"[Shader ({ShaderType})] {label}");
    }

    ~Shader()
    {
        ReleaseUnmanagedResources();
    }

    private void CompileShaderOrThrow(string source)
    {
        GL.ShaderSource(m_shaderId, source);
        GL.CompileShader(m_shaderId);
        ThrowIfShaderCompileFailure(m_shaderId, ShaderType);
    }

    private static void ThrowIfShaderCompileFailure(int shaderHandle, ShaderType type)
    {
        GL.GetShader(shaderHandle, ShaderParameter.CompileStatus, out int status);
        if (status == GLUtil.GLTrue)
            return;

        string errorMsg = GL.GetShaderInfoLog(shaderHandle);
        throw new($"Error compiling render shader {type}: {errorMsg}");
    }

    public void Attach(int programId)
    {
        GL.AttachShader(programId, m_shaderId);
    }

    public void Detach(int programId)
    {
        GL.DetachShader(programId, m_shaderId);
    }

    private void ReleaseUnmanagedResources()
    {
        if (m_disposed)
            return;

        GL.DeleteShader(m_shaderId);
        m_shaderId = GLUtil.NoObject;

        m_disposed = true;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
}