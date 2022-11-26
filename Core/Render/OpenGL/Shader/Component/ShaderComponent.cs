using System;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Shader.Component;

public abstract class ShaderComponent : IDisposable
{
    protected readonly int ShaderId;

    protected ShaderComponent(string shaderText)
    {
        ShaderId = GL.CreateShader(GetShaderComponentType());
        GL.ShaderSource(ShaderId, shaderText);
        GL.CompileShader(ShaderId);

        CleanupAndThrowIfCompilationError();
    }

    ~ShaderComponent()
    {
        FailedToDispose(this);
        ReleaseUnmanagedResources();
    }

    public void AttachAnd(int programId, Action action)
    {
        GL.AttachShader(programId, ShaderId);
        action.Invoke();
        GL.DetachShader(programId, ShaderId);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    protected abstract ShaderType GetShaderComponentType();

    private void CleanupAndThrowIfCompilationError()
    {
        GL.GetShader(ShaderId, ShaderParameter.CompileStatus, out int status);
        if (status == GLHelper.GLTrue)
            return;

        string errorMsg = GL.GetShaderInfoLog(ShaderId);

        Dispose();

        throw new($"Error compiling shader {GetShaderComponentType()}: {errorMsg}");
    }

    private void ReleaseUnmanagedResources()
    {
        GL.DeleteShader(ShaderId);
    }
}
