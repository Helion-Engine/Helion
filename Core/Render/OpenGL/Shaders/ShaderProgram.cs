using System;
using System.Collections.Generic;
using System.Reflection;
using Helion.Render.OpenGL.Shaders.Attributes;
using Helion.Render.OpenGL.Shaders.Uniforms;
using Helion.Render.OpenGL.Util;
using Helion.Util.Extensions;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Shaders;

public abstract class ShaderProgram : IDisposable
{
    private int m_program;
    private bool m_disposed;
    private readonly List<VertexShaderAttribute> m_attributes = new();

    public IReadOnlyList<VertexShaderAttribute> Attributes => m_attributes;

    protected ShaderProgram()
    {
        m_program = GL.CreateProgram();

        CreateAndCompileShaderOrThrow();
        AssignIndicesToUniformsOrThrow();
        RetrieveAttributes();
    }

    ~ShaderProgram()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    public void SetDebugLabel(string name)
    {
        GLUtil.Label($"Shader: {name}", ObjectLabelIdentifier.Program, m_program);
    }

    protected abstract string VertexShader();

    protected abstract string FragmentShader();

    private void CreateAndCompileShaderOrThrow()
    {
        // TODO: Free properly on failure, if someone catches then we have a problem...

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
        int vertexShader = CompileShaderOrThrow(VertexShader(), ShaderType.VertexShader);
        int fragmentShader = CompileShaderOrThrow(FragmentShader(), ShaderType.FragmentShader);
        return new[] { vertexShader, fragmentShader };
    }

    private void ThrowIfLinkFailure()
    {
        GL.GetProgram(m_program, GetProgramParameterName.LinkStatus, out int status);
        if (status == GLUtil.GLTrue)
            return;

        string errorMsg = GL.GetProgramInfoLog(m_program);
        throw new ShaderException($"Error linking shader: {errorMsg}");
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
        if (status == GLUtil.GLTrue)
            return;

        string errorMsg = GL.GetShaderInfoLog(shaderHandle);
        throw new ShaderException($"Error compiling shader {type}: {errorMsg}");
    }

    private void AssignIndicesToUniformsOrThrow()
    {
        GL.GetProgram(m_program, GetProgramParameterName.ActiveUniforms, out int uniformCount);

        for (int i = 0; i < uniformCount; i++)
        {
            string name = GL.GetActiveUniform(m_program, i, out _, out _);
            int location = GL.GetUniformLocation(m_program, name);
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
            if (!HasUniformAttribute(field) || !field.Name.EqualsIgnoreCase(lowerName))
                continue;

            if (field.GetValue(this) is not Uniform uniform)
                throw new Exception($"Attribute on non-uniform class: {field.Name}");

            uniform.Location = location;
            return;
        }

        throw new ShaderException($"Encountered uniform '{name}' which has no backing field in the class: {GetType().Name}");
    }

    private void RetrieveAttributes()
    {
        GL.GetProgram(m_program, GetProgramParameterName.ActiveAttributes, out int attribCount);
        GL.GetProgram(m_program, GetProgramParameterName.ActiveAttributeMaxLength, out int maxNameLength);

        for (int i = 0; i < attribCount; i++)
        {
            GL.GetActiveAttrib((uint)m_program, (uint)i, maxNameLength, out _,
                out int size, out ActiveAttribType attribType, out string name);

            int location = GL.GetAttribLocation(m_program, name);
            if (location == VertexShaderAttribute.NoLocation)
                throw new Exception("Unable to get attribute location in shader (should never happen)");

            VertexShaderAttribute attribute = new(location, name, i, size, attribType);
            m_attributes.Add(attribute);
        }
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
