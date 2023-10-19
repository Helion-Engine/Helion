using System.Collections.Generic;

namespace Helion.Layer.New.Levels.EndGame;

public record CastData(string DefinitionName, string DisplayName)
{
    public static readonly IReadOnlyList<CastData> Cast = new List<CastData>
    {
        new("ZombieMan", "$CC_ZOMBIE"),
        new("ShotgunGuy", "$CC_SHOTGUN"),
        new("ChaingunGuy", "$CC_HEAVY"),
        new("DoomImp", "$CC_IMP"),
        new("Demon", "$CC_DEMON"),
        new("LostSoul", "$CC_LOST"),
        new("Cacodemon", "$CC_CACO"),
        new("HellKnight", "$CC_HELL"),
        new("BaronOfHell", "$CC_BARON"),
        new("Arachnotron", "$CC_ARACH"),
        new("PainElemental", "$CC_PAIN"),
        new("Revenant", "$CC_REVEN"),
        new("Fatso", "$CC_MANCU"),
        new("Archvile", "$CC_ARCH"),
        new("SpiderMastermind", "$CC_SPIDER"),
        new("Cyberdemon", "$CC_CYBER"),
        new("DoomPlayer", "$CC_HERO"),
    };
}