namespace Helion.Util;

public struct ToggleState
{
    public bool Current;
    public bool Previous;
    private bool m_resetChange;

    public void Reset(bool value)
    {
        Current = value;
        m_resetChange = true;
    }

    public bool ConsumeChangeTrue()
    {
        var changed = false;
        if (m_resetChange && Current != Previous)
            changed = true;

        changed = changed || (Current != Previous && Current == true);
        Previous = Current;
        m_resetChange = false;
        return changed;
    }

    public void Set(bool newValue)
    {
        Current = newValue;
    }
}
