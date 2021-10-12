using System;
using System.Diagnostics;

namespace Helion.Layer.EndGame;

public partial class EndGameLayer
{
    private readonly TimeSpan m_scrollTimespan = TimeSpan.FromMilliseconds(40);
    private readonly Stopwatch m_scroller = new();
    private readonly Stopwatch m_stopwatch = new();

    public void RunLogic()
    {
        UpdateScroller();

        if (!m_forceState && (m_drawState == EndGameDrawState.Complete || !m_stopwatch.IsRunning || m_stopwatch.Elapsed < m_timespan))
            return;

        if (m_drawState == EndGameDrawState.TextComplete && NextMapInfo != null)
            return;

        if (m_drawState == EndGameDrawState.TextComplete)
        {
            if (m_shouldScroll)
            {
                m_drawState++;
                m_timespan += TimeSpan.FromSeconds(2);
                PlayMusic("D_BUNNY");
            }
            else
            {
                m_drawState = EndGameDrawState.Complete;
            }
        }
        else if (m_drawState == EndGameDrawState.ImageScroll)
        {
            if (m_forceState)
            {
                m_scroller.Stop();
                m_xOffset = m_xOffsetStop;
                m_drawState = EndGameDrawState.TheEnd;
                m_theEndImageIndex = TheEndImages.Count - 1;
                return;
            }

            if (m_scroller.IsRunning)
                return;

            m_drawState++;
            m_timespan = GetPageTime();
        }
        else if (m_drawState == EndGameDrawState.TheEnd)
        {
            m_timespan = TimeSpan.FromMilliseconds(150);
            if (m_theEndImageIndex < TheEndImages.Count - 1)
            {
                m_theEndImageIndex++;
                m_soundManager.PlayStaticSound("weapons/pistol");
            }
        }
        else if (m_drawState < EndGameDrawState.Complete)
        {
            // This is cluster text and does not proceed further
            if (NextMapInfo != null)
                m_drawState = EndGameDrawState.TextComplete;
            else
                m_drawState++;
        }

        if (m_drawState == EndGameDrawState.ImageScroll)
            m_scroller.Start();

        m_forceState = false;
        m_stopwatch.Restart();
    }

    private void UpdateScroller()
    {
        if (m_scroller.IsRunning && m_scroller.Elapsed >= m_scrollTimespan)
        {
            m_scroller.Restart();
            m_xOffset += 1;

            if (m_xOffset >= m_xOffsetStop)
            {
                m_xOffset = m_xOffsetStop;
                m_scroller.Stop();
            }
        }
    }
}

