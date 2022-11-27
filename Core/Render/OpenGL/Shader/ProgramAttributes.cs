using OpenTK.Graphics.OpenGL;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Helion.Render.OpenGL.Shader;

public record ProgramAttribute(string Name, int Index, int Size, ActiveAttribType Type);

public class ProgramAttributes : IReadOnlyList<ProgramAttribute>
{
    private readonly List<ProgramAttribute> m_attributes = new();

    public int Count => m_attributes.Count;
    public ProgramAttribute this[int index] => m_attributes[index];

    public void Populate(int program)
    {
        const int MaxNameLength = 128;

        GL.GetProgram(program, GetProgramParameterName.ActiveAttributes, out int attrCount);

        for (int i = 0; i < attrCount; i++)
        {
            GL.GetActiveAttrib(program, i, MaxNameLength, out int strLen, out int size, out ActiveAttribType type, out string name);
            Debug.Assert(strLen < MaxNameLength - 1, $"Attribute name is significantly longer than expected, something is likely wrong: {i} {strLen} {name}");

            ProgramAttribute attr = new(name, i, size, type);
            m_attributes.Add(attr);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => m_attributes.GetEnumerator();
    public IEnumerator<ProgramAttribute> GetEnumerator() => m_attributes.GetEnumerator();
}
