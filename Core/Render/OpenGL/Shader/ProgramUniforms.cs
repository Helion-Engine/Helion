using GlmSharp;
using Helion;
using Helion.Geometry.Vectors;
using Helion.Render;
using Helion.Render.OpenGL;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Helion.Render.OpenGL.Util;

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
    public void Set(bool value, string name) => Set(value, GetLocation(name));
    public void Set(int value, int location) => GL.Uniform1(location, value);
    public void Set(int value, string name) => Set(value, GetLocation(name));
    public void Set(float value, int location) => GL.Uniform1(location, value);
    public void Set(float value, string name) => Set(value, GetLocation(name));
    public void Set(Vec2F value, int location) => GL.Uniform2(location, value.X, value.Y);
    public void Set(Vec2F value, string name) => Set(value, GetLocation(name));
    public void Set(Vec3F value, int location) => GL.Uniform3(location, value.X, value.Y, value.Z);
    public void Set(Vec3F value, string name) => Set(value, GetLocation(name));
    public void Set(Vec4F value, int location) => GL.Uniform4(location, value.X, value.Y, value.Z, value.W);
    public void Set(Vec4F value, string name) => Set(value, GetLocation(name));
    public void Set(mat4 value, int location) => GL.UniformMatrix4(location, 1, false, value.ToUniformArray());
    public void Set(mat4 value, string name) => Set(value, GetLocation(name));
    public void Set(TextureUnit value, int location) => GL.Uniform1(location, (int)value - (int)TextureUnit.Texture0);
    public void Set(TextureUnit value, string name) => Set(value, GetLocation(name));

    public int GetLocation(string name) => m_nameToLocation[name];
}
