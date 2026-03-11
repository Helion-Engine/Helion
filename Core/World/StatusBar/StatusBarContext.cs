using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.Definitions.StatusBar;
using Helion.World.Entities.Players;

namespace Helion.World.StatusBar;

public readonly record struct StatusBarContext(
    IWorld World,
    Player Player,
    MapInfoDef MapInfo,
    StatusBarLayoutDef? ActiveLayout,
    bool AutomapVisible, 
    bool Widescreen,
    int Fps,
    string? ConsoleMessage,
    bool IsMessageCentered,
    bool HasBackPack,
    bool HasTicks
);