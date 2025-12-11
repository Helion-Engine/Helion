using Helion.World.Entities.Players;

namespace Helion.World.StatusBar;

public readonly record struct StatusBarContext(
    IWorld World,
    Player Player, 
    bool AutomapVisible, 
    bool Widescreen,
    int Fps,
    string? ConsoleMessage,
    bool IsMessageCentered,
    bool HasBackPack
);