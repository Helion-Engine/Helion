using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;

namespace Helion.Render.OpenGL.Queries;

public class GLElapsedTimeQuery : GLQuery
{
    public GLElapsedTimeQuery(string label) : base(label)
    {
    }

    // Times the function and returns how many milliseconds it takes.
    public static double Time(Action func, string label = "Anonymous elapsed query timer")
    {
        using GLElapsedTimeQuery query = new(label);

        query.Start();
        func();
        query.Stop();

        while (query.IsDone())
        {
            // Spin until it's done.
        }

        return query.DurationInMilliseconds();
    }

    public void Start()
    {
        GL.BeginQuery(QueryTarget.TimeElapsed, Name);
    }

    public void Stop()
    {
        GL.EndQuery(QueryTarget.TimeElapsed);
    }

    public bool IsDone()
    {
        GL.GetQueryObject(Name, GetQueryObjectParam.QueryResultAvailable, out int result);
        return result != 0;
    }

    public long DurationInNanoseconds()
    {
        Debug.Assert(IsDone(), $"Trying to get query value but the query is not available yet");

        GL.GetQueryObject(Name, GetQueryObjectParam.QueryResult, out long duration);
        return duration;
    }

    public double DurationInMilliseconds()
    {
        const double NanoToMilliseconds = 1000000.0;

        return DurationInNanoseconds() / NanoToMilliseconds;
    }
}
