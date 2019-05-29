using System;

namespace Helion.Maps
{
    /// <summary>
    /// An enumeration of all the linedef flags.
    /// </summary>
    [Flags]
    public enum LineFlag : uint
    {
        PhysicsBlockPlayersAndMonsters = 0x0001U,
        PhysicsBlockMonsters = 0x0002U,
        TwoSidedLine = 0x0004U,
        UpperUnpegged = 0x0008U,
        LowerUnpegged = 0x0010U,
        RenderAsOneSided = 0x0020U,
        SoundBlock = 0x0040U,
        RenderInvisible = 0x0080U,
        RenderAlways = 0x0100U,
        PhysicsStrifeRailing = 0x0200U,
        PassUse = 0x0200U,
        RepeatSpecial = 0x0200U,
        PhysicsBlockFloatingMonsters = 0x0400U,
        PhysicsWalkOn3DMidTexture = 0x0400U,
        ActivateOnPlayerUse = 0x0400U,
        ActivateOnProjectileHit = 0x0800U,
        ActivateOnMonsterCross = 0x0C00U,
        ActivateOnPlayerBump = 0x1000U,
        ActivateOnPlayerCross = 0x1400U,
        ActivateWhenUsedByPlayerPassThrough = 0x1800U,
        AlphaThreeQuartersTransparent = 0x1000U,
        AlphaQuarterTransparent = 0x2000U,
        MonsterCanActivate = 0x2000U,
        PhysicsBlockPlayer = 0x4000U,
        PhysicsBlockEverything = 0x8000U
    };
}
