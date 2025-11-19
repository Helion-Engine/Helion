using FluentAssertions;
using Helion.Resources.IWad;
using Helion.Tests.Unit.GameAction.Util;
using Helion.Util;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Dehacked;

[Collection("GameActions")]
public class FastSwitch2 : IDisposable
{
    private const int SwitchTicks = 16;
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public FastSwitch2()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, WorldInit, IWadType.Doom2, dehackedPatch: Dehacked);
        World.Player.TickCommand = new TestTickCommand();
    }

    public void Dispose()
    {
        InventoryUtil.Reset(World, Player);
    }

    private void WorldInit(SinglePlayerWorld world)
    {
        world.Player.WeaponOffset.Y.Should().Be(Constants.WeaponBottom - Constants.WeaponRaiseSpeed);
        InventoryUtil.AssertWeapon(world.Player.Weapon, "Pistol");
        GameActions.TickWorld(world, 1);
        InventoryUtil.AssertWeapon(world.Player.AnimationWeapon, "Pistol");

        InventoryUtil.RunWeaponSwitch(world, world.Player, "Pistol");
        InventoryUtil.AssertWeapon(world.Player.Weapon, "Pistol");
    }

    [Fact(DisplayName = "Fast switch weapon")]
    public void FastSwitchWeapon()
    {
        int startTick = World.Gametick;
        Player.ChangeWeapon(InventoryUtil.GetWeapon(Player, "Fist"));
        InventoryUtil.RunWeaponSwitch(World, Player, "Fist");
        InventoryUtil.AssertWeapon(Player.Weapon, "Fist");
        int ticks = World.Gametick - startTick;
        ticks.Should().Be(SwitchTicks);

        Player.PendingWeapon.Should().BeNull();

        for (int i = 0; i < 10; i++)
        {
            GameActions.TickWorld(World, 1);
            Player.WeaponOffset.Y.Should().Be(Constants.WeaponTop);
        }
    }

    [Fact(DisplayName = "Fast switch weapon and fire")]
    public void FastSwitchWeaponAndFire()
    {
        int startTick = World.Gametick;
        Player.GiveItem(GameActions.GetEntityDefinition(World, "Shotgun"), null);
        InventoryUtil.RunWeaponSwitch(World, Player, "Shotgun");
        InventoryUtil.AssertWeapon(Player.Weapon, "Shotgun");
        int ticks = World.Gametick - startTick;
        ticks.Should().Be(SwitchTicks);

        for (int i = 0; i < 2; i++)
        {
            Player.FireWeapon().Should().BeTrue();
            InventoryUtil.RunWeaponFire(World, Player);
        }

        startTick = World.Gametick;
        Player.ChangeWeapon(InventoryUtil.GetWeapon(Player, "Pistol"));
        InventoryUtil.RunWeaponSwitch(World, Player, "Pistol");
        InventoryUtil.AssertWeapon(Player.Weapon, "Pistol");
        ticks = World.Gametick - startTick;
        ticks.Should().Be(SwitchTicks);
    }

    [Fact(DisplayName = "Fast switch weapon during fire")]
    public void FastSwitchWeaponDuringFire()
    {
        Player.GiveItem(GameActions.GetEntityDefinition(World, "Clip"), null);

        Player.FireWeapon().Should().BeTrue();
        Player.ChangeWeapon(InventoryUtil.GetWeapon(Player, "Fist"));
        InventoryUtil.RunWeaponFire(World, Player);

        int startTick = World.Gametick;
        InventoryUtil.RunWeaponSwitch(World, Player, "Fist");
        InventoryUtil.AssertWeapon(Player.Weapon, "Fist");
        int ticks = World.Gametick - startTick;
        ticks.Should().Be(1);
    }

    [Fact(DisplayName = "Fast switch weapon back and forth")]
    public void FastSwitchWeaponMultiple()
    {
        int startTick = World.Gametick;
        Player.ChangeWeapon(InventoryUtil.GetWeapon(Player, "Fist"));
        InventoryUtil.RunWeaponSwitch(World, Player, "Fist");
        InventoryUtil.AssertWeapon(Player.Weapon, "Fist");
        int ticks = World.Gametick - startTick;
        ticks.Should().Be(SwitchTicks);

        startTick = World.Gametick;
        Player.ChangeWeapon(InventoryUtil.GetWeapon(Player, "Pistol"));
        InventoryUtil.RunWeaponSwitch(World, Player, "Pistol");
        InventoryUtil.AssertWeapon(Player.Weapon, "Pistol");
        ticks = World.Gametick - startTick;
        ticks.Should().Be(SwitchTicks);
    }

    [Fact(DisplayName = "Fast switch weapon pickup")]
    public void FastSwitchWeaponPickup()
    {
        int startTick = World.Gametick;
        Player.GiveItem(GameActions.GetEntityDefinition(World, "Shotgun"), null);
        InventoryUtil.RunWeaponSwitch(World, Player, "Shotgun");
        InventoryUtil.AssertWeapon(Player.Weapon, "Shotgun");
        int ticks = World.Gametick - startTick;
        ticks.Should().Be(SwitchTicks);
    }

    private static readonly string Dehacked = @"Patch File for DeHackEd v3.0
Doom version = 21
Patch format = 6

Frame 3
Next frame = 2000

Frame 4
Next frame = 2001

Frame 11
Next frame = 2002

Frame 12
Next frame = 2003

Frame 19
Next frame = 2004

Frame 20
Next frame = 2005

Frame 33
Next frame = 2006

Frame 34
Next frame = 2007

Frame 50
Next frame = 2008

Frame 51
Next frame = 2009

Frame 58
Next frame = 2010

Frame 59
Next frame = 2011

Frame 69
Next frame = 2016

Frame 70
Next frame = 2017

Frame 75
Next frame = 2012

Frame 76
Next frame = 2013

Frame 82
Next frame = 2014

Frame 83
Next frame = 2015

Frame 685
Sprite subnumber = 32773

Frame 687
Sprite subnumber = 32773

Frame 689
Sprite subnumber = 32773

Frame 692
Sprite subnumber = 32776

Frame 693
Sprite subnumber = 32777

Frame 694
Sprite subnumber = 32778

Frame 695
Sprite subnumber = 32779

Frame 696
Sprite subnumber = 32780

Frame 697
Sprite subnumber = 32781

Frame 698
Sprite subnumber = 32782

Frame 1089
Next frame = 1107

Frame 1090
Next frame = 1108

Frame 1091
Next frame = 1109

Frame 1092
Next frame = 1110

Frame 1093
Next frame = 1111

Frame 1094
Next frame = 1112

Frame 1095
Next frame = 1113

Frame 1096
Next frame = 1114

Frame 1097
Next frame = 1115

Frame 1098
Next frame = 1116

Frame 1099
Next frame = 1117

Frame 1100
Next frame = 1118

Frame 1101
Next frame = 1119

Frame 1102
Next frame = 1120

Frame 1103
Next frame = 1121

Frame 1104
Next frame = 1122

Frame 1105
Next frame = 1123

Frame 1106
Next frame = 1124

Frame 2000
Sprite number = 2
Duration = 0
Next frame = 3

Frame 2001
Sprite number = 2
Duration = 0
Next frame = 4

Frame 2002
Sprite number = 3
Duration = 0
Next frame = 11

Frame 2003
Sprite number = 3
Duration = 0
Next frame = 12

Frame 2004
Sprite number = 1
Duration = 0
Next frame = 19

Frame 2005
Sprite number = 1
Duration = 0
Next frame = 20

Frame 2006
Sprite number = 6
Duration = 0
Next frame = 33

Frame 2007
Sprite number = 6
Duration = 0
Next frame = 34

Frame 2008
Sprite number = 7
Duration = 0
Next frame = 50

Frame 2009
Sprite number = 7
Duration = 0
Next frame = 51

Frame 2010
Sprite number = 9
Duration = 0
Next frame = 58

Frame 2011
Sprite number = 9
Duration = 0
Next frame = 59

Frame 2012
Sprite number = 12
Duration = 0
Next frame = 75

Frame 2013
Sprite number = 12
Duration = 0
Next frame = 76

Frame 2014
Sprite number = 14
Duration = 0
Next frame = 82

Frame 2015
Sprite number = 14
Duration = 0
Next frame = 83

Frame 2016
Sprite number = 11
Sprite subnumber = 2
Duration = 0
Next frame = 69

Frame 2017
Sprite number = 11
Sprite subnumber = 2
Duration = 0
Next frame = 70

[CODEPTR]
FRAME 2000 = Lower
FRAME 2001 = Raise
FRAME 2002 = Lower
FRAME 2003 = Raise
FRAME 2004 = Lower
FRAME 2005 = Raise
FRAME 2006 = Lower
FRAME 2007 = Raise
FRAME 2008 = Lower
FRAME 2009 = Raise
FRAME 2010 = Lower
FRAME 2011 = Raise
FRAME 2012 = Lower
FRAME 2013 = Raise
FRAME 2014 = Lower
FRAME 2015 = Raise
FRAME 2016 = Lower
FRAME 2017 = Raise

Frame 1100
Sprite number = 38
Sprite subnumber = 13
Next frame = 0

Frame 1101
Sprite number = 31
Sprite subnumber = 25
Next frame = 0

Frame 1102
Sprite number = 46
Sprite subnumber = 15
Next frame = 0

Frame 1103
Sprite number = 43
Sprite subnumber = 14

Frame 1104
Sprite number = 35
Sprite subnumber = 16
Next frame = 0

Frame 1105
Sprite number = 45
Sprite subnumber = 18
Next frame = 0

Frame 1106
Sprite number = 49
Sprite subnumber = 15
Next frame = 0

Frame 1107
Sprite number = 39
Sprite subnumber = 13
Next frame = 0

Frame 1108
Sprite number = 245
Next frame = 0

Frame 1109
Sprite number = 112
Sprite subnumber = 32769
Next frame = 0

Frame 1110
Sprite number = 112
Sprite subnumber = 32773
Next frame = 0

Frame 1111
Sprite number = 246
Next frame = 0

Frame 1112
Sprite number = 247
Next frame = 0

Frame 1113
Sprite number = 248
Next frame = 0

Frame 1114
Sprite number = 249
Next frame = 0

Frame 1115
Sprite number = 250
Next frame = 0

Frame 1116
Sprite number = 251
Next frame = 0

Frame 1117
Sprite number = 252
Sprite subnumber = 32768
Next frame = 1118
Duration = 6

Frame 1118
Sprite number = 252
Sprite subnumber = 32769
Next frame = 1119
Duration = 6

Frame 1119
Sprite number = 252
Sprite subnumber = 32770
Next frame = 1117
Duration = 6

Frame 1120
Sprite number = 253
Next frame = 0

Frame 1121
Sprite number = 254
Next frame = 0

Frame 1122
Sprite number = 255
Next frame = 0

Frame 1123
Sprite number = 256
Next frame = 0

Frame 1124
Sprite number = 257
Next frame = 0

Frame 1125
Sprite number = 258
Next frame = 0

Frame 1126
Sprite number = 259
Sprite subnumber = 32768
Next frame = 0

Frame 1127
Sprite number = 260
Sprite subnumber = 32768
Next frame = 1128
Duration = 4

Frame 1128
Sprite number = 260
Sprite subnumber = 32769
Next frame = 1129
Duration = 4

Frame 1129
Sprite number = 260
Sprite subnumber = 32770
Next frame = 1130
Duration = 4

Frame 1130
Sprite number = 260
Sprite subnumber = 32771
Next frame = 1127
Duration = 4

Frame 1131
Sprite number = 260
Sprite subnumber = 32768
Next frame = 1132
Duration = 4

Frame 1132
Sprite number = 260
Sprite subnumber = 32769
Next frame = 1133
Duration = 4

Frame 1133
Sprite number = 260
Sprite subnumber = 32770
Next frame = 1131
Duration = 4

Frame 1134
Sprite number = 261
Next frame = 0

Frame 1135
Sprite number = 262
Next frame = 0

Frame 1136
Sprite number = 263
Next frame = 0

Frame 1137
Sprite number = 264
Next frame = 0

Frame 1138
Sprite number = 265
Next frame = 0
";
}
