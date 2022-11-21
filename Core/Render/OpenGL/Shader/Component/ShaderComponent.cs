using System;
using Helion;
using Helion.Render;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Shader.Component;
using Helion.Render.OpenGL.Util;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Shader.Component;

public abstract class ShaderComponent : IDisposable
{
    protected readonly int ShaderId;
    protected readonly IGLFunctions gl;

    protected ShaderComponent(IGLFunctions functions, string shaderText)
    {
        gl = functions;
        ShaderId = gl.CreateShader(GetShaderComponentType());
        gl.ShaderSource(ShaderId, shaderText);
        gl.CompileShader(ShaderId);

        CleanupAndThrowIfCompilationError();
    }

    ~ShaderComponent()
    {
        FailedToDispose(this);
        ReleaseUnmanagedResources();
    }

    public void AttachAnd(int programId, Action action)
    {
        gl.AttachShader(programId, ShaderId);
        action.Invoke();
        gl.DetachShader(programId, ShaderId);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    protected abstract ShaderComponentType GetShaderComponentType();

    private void CleanupAndThrowIfCompilationError()
    {
        gl.GetShader(ShaderId, ShaderParameterType.CompileStatus, out int status);
        if (status == GLHelper.GLTrue)
            return;

        string errorMsg = gl.GetShaderInfoLog(ShaderId);

        Dispose();

        throw new ShaderException($"Error compiling shader {GetShaderComponentType()}: {errorMsg}");
    }

    private void ReleaseUnmanagedResources()
    {
        gl.DeleteShader(ShaderId);
    }
}
