using FluentAssertions;
using Helion.Maps.Specials;
using Helion.Resources.IWad;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;
using Helion.Util;
using Helion.Maps.Specials.ZDoom;
using Helion.World.Geometry.Sectors;
using Helion.World.Special.Specials;
using Helion.Util.Extensions;

namespace Helion.Tests.Unit.GameAction.Udmf;

[Collection("GameActions")]
public class UdmfMap
{
    private static readonly string ResourceZip = "Resources/udmfmap.zip";
    private static readonly string MapName = "MAP01";

    private readonly SinglePlayerWorld World;

    public UdmfMap()
    {
        World = WorldAllocator.LoadMap(ResourceZip, "udmfmap.wad", MapName, GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "UDMF side offsets")]
    public void SideOffsets()
    {
        var line = GameActions.GetLine(World, 0);
        line.Front.Offset.X.Should().Be(1);
        line.Front.Offset.Y.Should().Be(2);
        line.Front.Upper.Offset.X.Should().Be(3);
        line.Front.Upper.Offset.Y.Should().Be(4);
        line.Front.Middle.Offset.X.Should().Be(5);
        line.Front.Middle.Offset.Y.Should().Be(6);
        line.Front.Lower.Offset.X.Should().Be(7);
        line.Front.Lower.Offset.Y.Should().Be(8);
    }

    [Fact(DisplayName = "UDMF side scale")]
    public void SideScale()
    {
        var line = GameActions.GetLine(World, 4);
        line.Front.Upper.Scale.X.Should().Be(1.1f);
        line.Front.Upper.Scale.Y.Should().Be(1.2f);
        line.Front.Middle.Scale.X.Should().Be(1.3f);
        line.Front.Middle.Scale.Y.Should().Be(1.4f);
        line.Front.Lower.Scale.X.Should().Be(1.5f);
        line.Front.Lower.Scale.Y.Should().Be(1.6f);
    }

    [Fact(DisplayName = "UDMF side light level")]
    public void SideLightLevel()
    {
        var line = GameActions.GetLine(World, 11);
        line.Front.Upper.LightLevel.Should().Be(128);
        line.Front.Upper.LightLevelAbsolute.Should().BeTrue();
        line.Front.Middle.LightLevel.Should().Be(96);
        line.Front.Middle.LightLevelAbsolute.Should().BeTrue();
        line.Front.Lower.LightLevel.Should().Be(64);
        line.Front.Lower.LightLevelAbsolute.Should().BeTrue();
    }

    [Fact(DisplayName = "UDMF side flags")]
    public void SideFlags()
    {
        var line = GameActions.GetLine(World, 3);
        line.Front.Flags.WrapMidTex.Should().BeFalse();
        line.Front.Flags.SmoothLighting.Should().BeFalse();
        line.Front.Flags.NoFakeContrast.Should().BeFalse();

        line = GameActions.GetLine(World, 8);
        line.Front.Flags.WrapMidTex.Should().BeTrue();
        line.Front.Flags.SmoothLighting.Should().BeTrue();
        line.Front.Flags.NoFakeContrast.Should().BeTrue();
    }

    [Fact(DisplayName = "UDMF side textures")]
    public void SideTextures()
    {
        var line = GameActions.GetLine(World, 13);
        line.Front.Upper.TextureHandle.Should().Be(Constants.NoTextureIndex);
        line.Front.Middle.TextureHandle.Should().Be(Constants.NoTextureIndex);
        line.Front.Lower.TextureHandle.Should().Be(Constants.NoTextureIndex);

        line.Back.Should().NotBeNull();
        line.Back!.Upper.TextureHandle.Should().Be(Constants.NoTextureIndex);
        line.Back!.Middle.TextureHandle.Should().Be(Constants.NoTextureIndex);
        line.Back!.Lower.TextureHandle.Should().Be(Constants.NoTextureIndex);

        line = GameActions.GetLine(World, 14);
        AssertTexture(line.Front.Upper.TextureHandle, "ASHWALL2");
        AssertTexture(line.Front.Middle.TextureHandle, "BROWN1");
        AssertTexture(line.Front.Lower.TextureHandle, "BROWNGRN");

        line.Back.Should().NotBeNull();
        AssertTexture(line.Back!.Upper.TextureHandle, "BIGDOOR2");
        AssertTexture(line.Back!.Middle.TextureHandle, "COMPSTA1");
        AssertTexture(line.Back!.Lower.TextureHandle, "GSTONE1");
    }

    [Fact(DisplayName = "UDMF line flags")]
    public void LineFlags()
    {
        var line = GameActions.GetLine(World, 13);
        line.Flags.TwoSided.Should().BeFalse();
        line.Flags.Unpegged.Upper.Should().BeFalse();
        line.Flags.Unpegged.Lower.Should().BeFalse();
        line.Flags.Blocking.Everything.Should().BeFalse();
        line.Flags.Blocking.Monsters.Should().BeFalse();
        line.Flags.Blocking.Players.Should().BeFalse();
        line.Flags.Blocking.LandMonsters.Should().BeFalse();
        line.Flags.Blocking.FloatMonsters.Should().BeFalse();
        line.Flags.BlockSound.Should().BeFalse();
        line.Flags.Blocking.Projectiles.Should().BeFalse();
        line.Flags.Blocking.Hitscan.Should().BeFalse();
        line.Flags.Blocking.Use.Should().BeFalse();
        line.Flags.Blocking.Sight.Should().BeFalse();
        line.Front.Flags.WrapMidTex.Should().BeFalse();
        line.Flags.Automap.AlwaysDraw.Should().BeFalse();
        line.Flags.Automap.DrawAsOneSided.Should().BeFalse();
        line.Flags.Activations.HasFlag(LineActivations.Monster).Should().BeFalse();

        line = GameActions.GetLine(World, 14);
        line.Flags.TwoSided.Should().BeTrue();
        line.Flags.Unpegged.Upper.Should().BeTrue();
        line.Flags.Unpegged.Lower.Should().BeTrue();
        line.Flags.Blocking.Everything.Should().BeTrue();
        line.Flags.Blocking.Monsters.Should().BeTrue();
        line.Flags.Blocking.Players.Should().BeTrue();
        line.Flags.Blocking.LandMonsters.Should().BeTrue();
        line.Flags.Blocking.FloatMonsters.Should().BeTrue();
        line.Flags.BlockSound.Should().BeTrue();
        line.Flags.Blocking.Projectiles.Should().BeTrue();
        line.Flags.Blocking.Hitscan.Should().BeTrue();
        line.Flags.Blocking.Use.Should().BeTrue();
        line.Flags.Blocking.Sight.Should().BeTrue();
        line.Front.Flags.WrapMidTex.Should().BeTrue();
        line.Flags.Automap.AlwaysDraw.Should().BeTrue();
        line.Flags.Automap.DrawAsOneSided.Should().BeTrue();
        line.Flags.Activations.HasFlag(LineActivations.Monster).Should().BeTrue();
    }

    [Fact(DisplayName = "UDMF line properties")]
    public void LineProperties()
    {
        var line = GameActions.GetLine(World, 13);
        line.Alpha.Should().Be(1f);
        line.LockNumber.Should().Be(ZDoomKeyType.None);
        line.SectorTag.Should().Be(Sector.NoTag);

        line = GameActions.GetLine(World, 19);
        line.Alpha.Should().Be(0.5f);
        line.LockNumber.Should().Be(ZDoomKeyType.RedKeyCard);
        line.MapLineId.Should().Be(69);
    }

    [Fact(DisplayName = "UDMF line special")]
    public void LineSpecial()
    {
        var line = GameActions.GetLine(World, 20);
        line.Special.LineSpecialType.Should().Be(ZDoomLineSpecialType.DoorGeneric);
        line.Args.Arg0.Should().Be(420);
        line.Args.Arg1.Should().Be(16);
        line.Args.Arg2.Should().Be(1);
        line.Args.Arg3.Should().Be(34);
        line.Args.Arg4.Should().Be(1);
    }

    [Fact(DisplayName = "UDMF line activations")]
    public void LineActivation()
    {
        var line = GameActions.GetLine(World, 13);
        line.Flags.Activations.HasFlag(LineActivations.Player).Should().BeFalse();
        line.Flags.Activations.HasFlag(LineActivations.Monster).Should().BeFalse();
        line.Flags.Activations.HasFlag(LineActivations.Hitscan).Should().BeFalse();
        line.Flags.Activations.HasFlag(LineActivations.CrossLine).Should().BeFalse();
        line.Flags.Activations.HasFlag(LineActivations.UseLine).Should().BeFalse();
        line.Flags.Activations.HasFlag(LineActivations.ImpactLine).Should().BeFalse();
        line.Flags.Activations.HasFlag(LineActivations.UseLineBack).Should().BeFalse();
        line.Flags.Activations.HasFlag(LineActivations.CheckSwitchRange).Should().BeFalse();
        line.Flags.Activations.HasFlag(LineActivations.FrontSideOnly).Should().BeFalse();

        line = GameActions.GetLine(World, 20);
        line.Flags.Activations.HasFlag(LineActivations.Player).Should().BeTrue();
        line.Flags.Activations.HasFlag(LineActivations.Monster).Should().BeTrue();
        line.Flags.Activations.HasFlag(LineActivations.Hitscan).Should().BeTrue();
        line.Flags.Activations.HasFlag(LineActivations.CrossLine).Should().BeTrue();
        line.Flags.Activations.HasFlag(LineActivations.UseLine).Should().BeTrue();
        line.Flags.Activations.HasFlag(LineActivations.ImpactLine).Should().BeTrue();
        line.Flags.Activations.HasFlag(LineActivations.UseLineBack).Should().BeTrue();
        line.Flags.Activations.HasFlag(LineActivations.CheckSwitchRange).Should().BeTrue();
        line.Flags.Activations.HasFlag(LineActivations.FrontSideOnly).Should().BeTrue();
    }

    [Fact(DisplayName = "UDMF line health")]
    public void LineHealth()
    {
        var line = GameActions.GetLine(World, 20);
        line.ObjectHealth.Health.Should().Be(42069);
        line.ObjectHealth.HealthGroup.Should().Be(7);
        line.ObjectHealth.DamageSpecial.Should().BeTrue();
        line.ObjectHealth.DeathSpecial.Should().BeTrue();
    }

    [Fact(DisplayName = "UDMF sector properties")]
    public void SectorProperties()
    {
        var sector = GameActions.GetSector(World, 0);
        sector.Ceiling.RenderOffsets.Offset.X.Should().Be(0);
        sector.Ceiling.RenderOffsets.Offset.Y.Should().Be(0);
        sector.Ceiling.RenderOffsets.Scale.X.Should().Be(1);
        sector.Ceiling.RenderOffsets.Scale.Y.Should().Be(1);
        sector.Floor.RenderOffsets.Offset.X.Should().Be(0);
        sector.Floor.RenderOffsets.Offset.Y.Should().Be(0);
        sector.Floor.RenderOffsets.Scale.X.Should().Be(1);
        sector.Floor.RenderOffsets.Scale.Y.Should().Be(1);
        sector.Gravity.Should().Be(1);
        sector.LightLevel.Should().Be(255);
        sector.Tag.Should().Be(Sector.NoTag);
        sector.Silent.Should().BeFalse();
        sector.NoAttack.Should().BeFalse();

        sector = GameActions.GetSector(World, 2);
        sector.Ceiling.RenderOffsets.Offset.X.Should().Be(32);
        sector.Ceiling.RenderOffsets.Offset.Y.Should().Be(48);
        sector.Ceiling.RenderOffsets.Scale.X.Should().Be(3);
        sector.Ceiling.RenderOffsets.Scale.Y.Should().Be(2);
        sector.Ceiling.LightLevel.Should().Be(64);
        sector.Ceiling.LightLevelAbsolute.Should().BeFalse();
        sector.Ceiling.RenderOffsets.Rotate.Should().Be(MathHelper.ToRadians(160));

        sector.Floor.RenderOffsets.Offset.X.Should().Be(16);
        sector.Floor.RenderOffsets.Offset.Y.Should().Be(24);
        sector.Floor.RenderOffsets.Scale.X.Should().Be(0.5);
        sector.Floor.RenderOffsets.Scale.Y.Should().Be(0.6);
        sector.Floor.LightLevel.Should().Be(128);
        sector.Floor.LightLevelAbsolute.Should().BeTrue();
        sector.Floor.RenderOffsets.Rotate.Should().Be(MathHelper.ToRadians(45));

        sector.LightLevel.Should().Be(96);
        sector.Tag.Should().Be(777);
        sector.Silent.Should().BeTrue();
        sector.NoAttack.Should().BeTrue();
    }

    [Fact(DisplayName = "UDMF sector damage")]
    public void SectorDamage()
    {
        var sector = GameActions.GetSector(World, 0);
        sector.DamageAmount.Should().Be(0);
        sector.DamageInterval.Should().Be(SectorDamageSpecial.DefaultDamageInterval);
        sector.DamageLeakiness.Should().Be(0);

        sector = GameActions.GetSector(World, 3);
        sector.DamageAmount.Should().Be(1);
        sector.DamageInterval.Should().Be(5);
        sector.DamageLeakiness.Should().Be(64);
    }

    [Fact(DisplayName = "UDMF thing properties")]
    public void ThingProperties()
    {
        var thing = GameActions.GetEntity(World, 1);
        thing.Definition.Name.EqualsIgnoreCase("Column");
        thing.Position.X.Should().Be(-224);
        thing.Position.Y.Should().Be(-352);
        thing.Flags.Dormant.Should().BeTrue();
        thing.Flags.Friendly.Should().BeTrue();
        thing.Flags.Invisible.Should().BeTrue();
        thing.Flags.CountSecret.Should().BeTrue();
        thing.Alpha.Should().Be(0.69f);
        thing.Gravity.Should().Be(0.5f);
        thing.Health.Should().Be(1234);
    }

    private void AssertTexture(int textureHandle, string textureName)
    {
        World.TextureManager.GetTexture(textureHandle).Name.Equals(textureName, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
    }
}
