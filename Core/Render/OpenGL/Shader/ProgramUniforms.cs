using GlmSharp;
using Helion;
using Helion.Geometry.Vectors;
using Helion.Render;
using Helion.Render.OpenGL;
using Helion.Render.OpenGL.Shader;
using OneOf;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Helion.Render.OpenGL.Shader;

public class ProgramUniforms
{
    private readonly Dictionary<string, int> m_nameToLocation = new(StringComparer.OrdinalIgnoreCase);

    public void Populate(int program)
    {
        GL.GetProgram(program, GetProgramParameterName.ActiveUniforms, out int uniformCount);

        for (int i = 0; i < uniformCount; i++)
        {
            string name = GL.GetActiveUniform(program, i, out _, out _);
            int location = GL.GetUniformLocation(program, name);
            Debug.Assert(location != -1, $"Unable to index shader uniform (index {i}): {name}");
            Debug.Assert(!m_nameToLocation.ContainsKey(name), $"Trying to add duplicate uniform name: {name}");

            m_nameToLocation[name] = location;
        }
    }

    public void Set(bool value, int location) => GL.Uniform1(location, value ? 1 : 0);
    public void Set(int value, int location) => GL.Uniform1(location, value);
    public void Set(float value, int location) => GL.Uniform1(location, value);
    public void Set(Vec2F value, int location) => GL.Uniform2(location, value.X, value.Y);
    public void Set(Vec3F value, int location) => GL.Uniform3(location, value.X, value.Y, value.Z);
    public void Set(Vec4F value, int location) => GL.Uniform4(location, value.X, value.Y, value.Z, value.W);
    public void Set(mat4 value, int location) => GL.UniformMatrix4(location, 1, false, value.Values1D);
    public void Set(TextureUnit value, int location) => GL.Uniform1(location, (int)value - (int)TextureUnit.Texture0);

    public int GetLocation(string name) => m_nameToLocation[name];

    public OneOf<bool, int, float, Vec2F, Vec3F, Vec4F, mat4, TextureUnit> this[string name]
    {
        set
        {
            if (!m_nameToLocation.TryGetValue(name, out int location))
            {
                Debug.Assert(false, $"Cannot find uniform: {name}");
                return;
            }

            value.Switch(
                b => GL.Uniform1(location, b ? 1 : 0),
                i => GL.Uniform1(location, i),
                f => GL.Uniform1(location, f),
                v => GL.Uniform2(location, v.X, v.Y),
                v => GL.Uniform3(location, v.X, v.Y, v.Z),
                v => GL.Uniform4(location, v.X, v.Y, v.Z, v.W),
                mat => GL.UniformMatrix4(location, 1, false, mat.Values1D),
                texUnit => GL.Uniform1(location, (int)texUnit - (int)TextureUnit.Texture0)
            );
        }
    }
}
