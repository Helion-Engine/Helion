namespace Helion.Util;

public struct ToggleState
{
    public bool Current;
    public bool Previous;

    public bool ConsumeChange()
    {
        bool changed = Current != Previous && Current == true;
        Previous = Current;
        return changed;
    }

    public void Set(bool newValue)
    {
        Current = newValue;
    }
}
