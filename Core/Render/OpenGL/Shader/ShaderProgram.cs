using System;
using System.Diagnostics;
using System.Reflection;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Shader.Fields;
using Helion.Render.OpenGL.Util;
using Helion.Render.OpenGL.Vertex;
using MoreLinq.Extensions;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Shader;

public class ShaderProgram : IDisposable
{
    private readonly int m_program;

    public ShaderProgram(ShaderBuilder builder, VertexArrayAttributes attributes)
    {
        m_program = GL.CreateProgram();

        builder.Vertex.AttachAnd(m_program, () =>
        {
            builder.Fragment.AttachAnd(m_program, () =>
            {
                // TODO: This will cause us to not detach if any throw.
                for (int i = 0; i < attributes.AttributesArray.Length; i++)
                {
                    var attr = attributes.AttributesArray[i];
                    GL.BindAttribLocation(m_program, attr.Index, attr.Name);
                }
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
        GL.UseProgram(m_program);
    }

    public void Unbind()
    {
        GL.UseProgram(0);
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
        GL.GetProgram(m_program, GetProgramParameterName.ActiveAttributes, out int numAttributes);
        Invariant(numAttributes == attributes.AttributesArray.Length, "Attribute mismatch, shader attributes do not match VAO attribute size (did you forget some? or not remove some?)");
    }

    private void IndexUniformsOrThrow()
    {
        GL.GetProgram(m_program, GetProgramParameterName.ActiveUniforms, out int numUniforms);

        for (int uniformIndex = 0; uniformIndex < numUniforms; uniformIndex++)
        {
            string name = GL.GetActiveUniform(m_program, uniformIndex, out _, out _);
            int location = GL.GetUniformLocation(m_program, name);
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
                throw new($"Unexpected uniform type for uniform '{name}' in class '{GetType().Name}' with field '{field.Name}'");
            }
        }

        throw new($"Encountered uniform '{name}' which has no backing field in the class: {GetType().Name}");
    }

    private void LinkProgramOrThrow()
    {
        GL.LinkProgram(m_program);

        GL.GetProgram(m_program, GetProgramParameterName.LinkStatus, out var status);
        if (status == GLHelper.GLTrue)
            return;

        string errorMsg = GL.GetProgramInfoLog(m_program);
        throw new($"Error linking shader: {errorMsg}");
    }

    private void ReleaseUnmanagedResources()
    {
        GL.DeleteProgram(m_program);
    }
}
