using System;
using Helion.Geometry.Vectors;
using Helion.Window.Input;

namespace Helion.Window;

/// <summary>
/// Input that can be 'consumed' which means once read, it will always return
/// as missing if queried again. The intent is to make it so that between layers,
/// if something else reads it, then it does not leak into another layer.
/// </summary>
public interface IConsumableInput
{
    IInputManager Manager { get; }

    void ConsumeAll();
    bool ConsumeKeyDown(Key key);
    bool ConsumeKeyPrevDown(Key key);
    bool ConsumeKeyHeldDown(Key key);
    bool ConsumeKeyUp(Key key);
    bool ConsumeKeyPrevUp(Key key);
    bool ConsumeKeyPressed(Key key);
    bool ConsumeKeyReleased(Key key);
    bool ConsumePressOrContinuousHold(Key key);
    ReadOnlySpan<char> ConsumeTypedCharacters();
    Vec2I ConsumeMouseMove();
    int ConsumeScroll();
}
