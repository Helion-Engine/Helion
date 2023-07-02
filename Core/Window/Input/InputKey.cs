namespace Helion.Window.Input;

public readonly struct InputKey
{
    public readonly Key Key;
    public readonly bool Pressed;

    public InputKey(Key key, bool pressed)
    {
        Key = key;
        Pressed = pressed;
    }
}
