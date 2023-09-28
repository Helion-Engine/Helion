using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Helion.Render.OpenGLNew.Util;
using Helion.Util.Extensions;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGLNew.Programs;

public abstract class Program : IBindable, IDisposable
{
    private int m_programId;
    private bool m_disposed;

    protected Program(string label)
    {
        IEnumerable<Shader> shaders = CreateShaders();
        Debug.Assert(shaders.Any(), "Program needs at least one shader");
        Debug.Assert(HasValidShaderCombination(shaders), "Program should be either only compute, or only graphics with at least a vertex shader");
        
        m_programId = GL.CreateProgram();
        CompileProgramOrThrow(shaders);
        
        foreach (Shader shader in shaders)
            shader.Dispose();
        
        Bind();
        GLUtil.ObjectLabel(ObjectLabelIdentifier.Program, m_programId, $"Program: {label}");
        Unbind();
    }

    ~Program()
    {
        ReleaseUnmanagedResources();
    }
    
    protected abstract IEnumerable<Shader> CreateShaders();

    private bool HasValidShaderCombination(IEnumerable<Shader> shaders)
    {
        if (shaders.Empty())
            return false;

        int shaderCount = shaders.Count();
        
        // If it's compute, it can only be compute, and just one shader.
        bool isCompute = shaders.Any(s => s.ShaderType == ShaderType.ComputeShader);
        if (isCompute)
            return shaderCount == 1;

        // There should be at least a vertex shader since it's graphics.
        if (shaders.Count(s => s.ShaderType == ShaderType.VertexShader) != 1)
            return false;
        
        // There should only be one of each type of shader.
        return shaders.Select(s => s.ShaderType).Distinct().Count() == shaderCount;
    }

    private void CompileProgramOrThrow(IEnumerable<Shader> shaders)
    {
        foreach (Shader shader in shaders)
            shader.Attach(m_programId);
        
        GL.LinkProgram(m_programId);

        foreach (Shader shader in shaders)
            shader.Detach(m_programId);
    }

    private void RetrieveAndUpdateUniformVariables()
    {
        // TODO
    }
    
    private void RetrieveShaderAttributes()
    {
        // TODO
    }

    public void Bind()
    {
        GL.UseProgram(m_programId);
    }

    public void Unbind()
    {
        GL.UseProgram(GLUtil.NoObject);
    }

    private void ReleaseUnmanagedResources()
    {
        if (m_disposed)
            return;
    
        GL.DeleteProgram(m_programId);
        m_programId = GLUtil.NoObject;

        m_disposed = true;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
}