using FluentAssertions;
using Helion.Maps.Specials.ZDoom;
using Helion.Resources.Definitions.MapInfo;
using Xunit;

namespace Helion.Tests.Unit.ParseMapInfo;

public class ZMapInfo
{
    [Fact(DisplayName ="Special Action")]
    public void SpecialAction()
    {
		string data = @"MAP MAP30
{
	SpecialAction = ""Deh_Actor_163"", ""Floor_LowerToHighest"", 176, 8, 420, 69, 70
	SpecialAction = ""Deh_Actor_161"", ""Door_Open"", 183, 16, 100, 101, 102
}";
        MapInfoDefinition def = new();
        def.Parse(new MockPathResolver(), data, false);

		def.MapInfo.Maps.Count.Should().Be(1);
		var map = def.MapInfo.Maps[0];
		map.BossActions.Count.Should().Be(2);

		AssertBossAction(map.BossActions[0], "Deh_Actor_163", ZDoomLineSpecialType.FloorLowerToHighest, 176, 8, 420, 69, 70);
        AssertBossAction(map.BossActions[1], "Deh_Actor_161", ZDoomLineSpecialType.DoorOpenStay, 183, 16, 100, 101, 102);
    }

	private static void AssertBossAction(BossAction action, string actor, ZDoomLineSpecialType type, params int[] args)
	{
		action.ActorName.Should().Be(actor);
		action.ZDoomAction.Should().Be(type);

		for (int i = 0; i < args.Length; i++)
		{
			switch (i)
			{
				case 0:
					action.ZDoomSpecialArgs.Arg0.Should().Be(args[i]);
					break;
                case 1:
                    action.ZDoomSpecialArgs.Arg1.Should().Be(args[i]);
                    break;
                case 2:
                    action.ZDoomSpecialArgs.Arg2.Should().Be(args[i]);
                    break;
                case 3:
                    action.ZDoomSpecialArgs.Arg3.Should().Be(args[i]);
                    break;
                case 4:
                    action.ZDoomSpecialArgs.Arg4.Should().Be(args[i]);
                    break;
            }
		}
	}
}
