using System;

namespace Helion.Render.OpenGL.Shader;

public interface IRenderProgramHandle
{
    RenderProgram ProgramBase { get; }
    int Version { get; }
}

public sealed class RenderProgramHandle<T>(T program) : IRenderProgramHandle
    where T : RenderProgram
{
    public T Program = program;
    public RenderProgram ProgramBase => Program;
    public int Version { get; private set; }

    public void Dispose()
    {
        Program.Dispose();
    }

    public void Recreate()
    {
        Version++;
        var program = Program.Recreate();
        Program = program as T ?? throw new InvalidOperationException("Failed to recreate program");
    }
}
