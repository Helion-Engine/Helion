namespace Helion.Maps.Specials.ZDoom;

public enum ZDoomLineBlockFlags
{
    None,
    Creatures = 1,
    Monsters = 2,
    Players = 4,
    Floaters = 8,
    Projectiles = 16,
    Everything = 32,
    Railing = 64,
    Use = 128,
    Sight = 256,
    HitScan = 512,
    Sound = 1024,
    LandMonsters = 2048,
    All = 0xFFFFFF
}
