using System;
using Helion.Geometry.Vectors;
using Helion.Util.Container;
using Helion.Window.Input;

namespace Helion.Window;

/// <summary>
/// Manages input from some source.
/// </summary>
public interface IInputManager
{
    Vec2I MouseMove { get; }
    Vec2I MousePosition { get; set; }
    public int Scroll { get; }
    public ReadOnlySpan<char> TypedCharacters { get; }
    public IAnalogAdapter? AnalogAdapter { get; }
    bool IsKeyDown(Key key);
    bool IsKeyPrevDown(Key key);
    bool IsKeyHeldDown(Key key);
    bool IsKeyPressed(Key key);
    bool HasAnyKeyPressed();
    bool HasAnyKeyDown();
    void Clear();
    void ClearMouse();
    void ProcessedKeys();
    void ProcessedMouseMovement();
    bool IsKeyContinuousHold(Key key);
    void GetPressedKeys(DynamicArray<Key> pressedKeys);
    IConsumableInput Poll(bool pollKeys);
}
