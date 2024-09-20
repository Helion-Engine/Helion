namespace Helion.Window.Input;

public interface IAnalogAdapter
{
    bool TryGetAnalogValueForAxis(Key key, out float axisAnalogValue);
}
