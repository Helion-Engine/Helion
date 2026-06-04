using Helion.World.Geometry.Lines;
using System;
using System.Collections.Generic;

namespace Helion.Tests.Unit.GameAction;

public static partial class GameActions
{
    public static void AssertLineBlockFlags(in LineBlockFlags actual, params string[] expectedTrue)
    {
        var expectedSet = new HashSet<string>(expectedTrue);

        foreach (var prop in typeof(LineBlockFlags).GetFields())
        {
            var value = (bool)prop.GetValue(actual)!;
            var expected = expectedSet.Contains(prop.Name);

            if (value != expected)
            {
                throw new Exception(
                    $"Flag mismatch: {prop.Name} was {(value ? "true" : "false")} but expected {(expected ? "true" : "false")}.");
            }
        }
    }

    public static void AssertAllLineBlockFlags(in LineBlockFlags actual, bool expected)
    {
        foreach (var prop in typeof(LineBlockFlags).GetFields())
        {
            if (prop.Name == nameof(LineBlockFlags.PlayersMbf21) || prop.Name == nameof(LineBlockFlags.LandMonstersMbf21) ||
                prop.Name == nameof(LineBlockFlags.MidTex3D) || prop.Name == nameof(LineBlockFlags.BlockMissileMidTex3D))
                continue;

            var value = (bool)prop.GetValue(actual)!;
            if (value != expected)
            {
                throw new Exception(
                    $"Flag mismatch: {prop.Name} was {(value ? "true" : "false")} but expected {(expected ? "true" : "false")}.");
            }
        }
    }
}