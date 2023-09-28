using System.Diagnostics;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGLNew.Queries;

public class GLElapsedTimeQuery(string label) : GLQuery(label)
{
    public void Start()
    {
        GL.BeginQuery(QueryTarget.TimeElapsed, m_objectId);
    }

    public void Stop()
    {
        GL.EndQuery(QueryTarget.TimeElapsed);
    }

    public bool IsDone()
    {
        GL.GetQueryObject(m_objectId, GetQueryObjectParam.QueryResultAvailable, out int result);
        return result != 0;
    }

    public long DurationInNanoseconds()
    {
        Debug.Assert(IsDone(), "Trying to get query value but the query is not available yet");

        GL.GetQueryObject(m_objectId, GetQueryObjectParam.QueryResult, out long duration);
        return duration;
    }

    public double DurationInMilliseconds()
    {
        const double NanoToMilliseconds = 1000000.0;

        return DurationInNanoseconds() / NanoToMilliseconds;
    }
}
