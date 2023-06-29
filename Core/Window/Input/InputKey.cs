namespace Helion.Window.Input;

internal readonly struct InputKey
{
    public readonly Key Key;
    public readonly bool Pressed;

    public InputKey(Key key, bool pressed)
    {
        Key = key;
        Pressed = pressed;
    }
}
