namespace Helion.Window.Input;

public interface IAnalogAdapter
{
    void Poll();
    bool TryGetAnalogValueForAxis(Key key, out float axisAnalogValue);
    bool KeyIsAnalogAxis(Key key);
}
