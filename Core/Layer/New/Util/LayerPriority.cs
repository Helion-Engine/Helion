namespace Helion.Layer.New.Util;

// This is primarily to make the relative priorities visible relative to each other.
// A lower priority means it has input and logic run first, but is drawn on top of
// every other layer before it.
public enum LayerPriority
{
    Console = 0,
    ReadThis = 1,
    Menu = 2,
    EndGame = 3,
    Titlepic = 4,
    World = 5
}