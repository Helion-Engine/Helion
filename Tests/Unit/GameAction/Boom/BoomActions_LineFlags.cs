using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.World.Geometry.Lines;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

public partial class BoomActions
{
    [Fact(DisplayName = "Mbf21 line flags")]
    public void Mbf21LineFlags()
    {
        var lineBlockFlags = typeof(LineBlockFlags);
        GameActions.AssertFlags(GameActions.GetLine(World, 441).Flags.Blocking,
            lineBlockFlags.GetField(nameof(LineBlockFlags.LandMonstersMbf21)));
        GameActions.AssertFlags(GameActions.GetLine(World, 444).Flags.Blocking,
            lineBlockFlags.GetField(nameof(LineBlockFlags.PlayersMbf21)));
    }

    [Fact(DisplayName = "Block land monsters")]
    public void BlockLandMonsters()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", Vec3D.Zero);
        GameActions.EntityBlockedByLine(World, imp, 441).Should().BeTrue();

        var caco = GameActions.CreateEntity(World, "Cacodemon", Vec3D.Zero);
        GameActions.EntityBlockedByLine(World, caco, 441).Should().BeFalse();
    }

    [Fact(DisplayName = "Block players")]
    public void BlockPlayers()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", Vec3D.Zero);
        GameActions.EntityBlockedByLine(World, imp, 444).Should().BeFalse();

        GameActions.EntityBlockedByLine(World, Player, 444).Should().BeTrue();
    }
}
