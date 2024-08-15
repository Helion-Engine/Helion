using System;
using System.Collections.Generic;
using Helion.Geometry.Vectors;
using Helion.Util.Configs.Impl;
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
    public bool HandleKeyInput { get; set; }
    public int Scroll { get; }

    void ConsumeAll();
    bool ConsumeKeyDown(Key key);
    bool ConsumeKeyPressed(Key key);
    bool ConsumePressOrContinuousHold(Key key);
    bool HasAnyKeyPressed();
    ReadOnlySpan<char> ConsumeTypedCharacters();
    Vec2I ConsumeMouseMove();
    Vec2I GetMouseMove();
    int ConsumeScroll();
    void IterateCommands(IList<KeyCommandItem> commands, Action<IConsumableInput, KeyCommandItem> onCommand, bool consumeKeyPressed);
}
