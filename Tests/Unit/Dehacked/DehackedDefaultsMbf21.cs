using FluentAssertions;
using Helion.Dehacked;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Locator;
using Helion.Resources.Definitions;
using Helion.World.Entities.Definition.States;
using Xunit;

namespace Helion.Tests.Unit.Dehacked;

public class DehackedDefaultsMbf21
{
    [Fact(DisplayName = "A_MonsterBulletAttack defaults")]
    public void A_MonsterBulletAttackDefaults()
    {
        var definitionEntries = CreateAndApply(ParseDehacked("MonsterBulletAttack"));
        var frame = definitionEntries.EntityFrameTable.Frames[0];
        AssertArgs(frame, [0, 0, 1, 5, 3, 0, 0, 0]);
        frame.ActionFunction.Should().NotBeNull();
        frame.ActionFunction.Method.Name.Should().Be("A_MonsterBulletAttack");
    }

    [Fact(DisplayName = "A_MonsterBulletAttack")]
    public void A_MonsterBulletAttack()
    {
        var definitionEntries = CreateAndApply(ParseDehacked("MonsterBulletAttack", noArgs: false));
        var frame = definitionEntries.EntityFrameTable.Frames[0];
        AssertArgs(frame, [10, 20, 30, 40, 50, 60, 70, 80]);
        frame.ActionFunction.Should().NotBeNull();
        frame.ActionFunction.Method.Name.Should().Be("A_MonsterBulletAttack");
    }

    [Fact(DisplayName = "A_MonsterMeleeAttack defaults")]
    public void A_MonsterMeleeAttackDefaults()
    {
        var definitionEntries = CreateAndApply(ParseDehacked("MonsterMeleeAttack"));
        var frame = definitionEntries.EntityFrameTable.Frames[0];
        AssertArgs(frame, [3, 8, 0, 0, 0, 0, 0, 0]);
        frame.ActionFunction.Should().NotBeNull();
        frame.ActionFunction.Method.Name.Should().Be("A_MonsterMeleeAttack");
    }

    [Fact(DisplayName = "A_MonsterMeleeAttack")]
    public void A_MonsterMeleeAttack()
    {
        var definitionEntries = CreateAndApply(ParseDehacked("MonsterMeleeAttack", noArgs: false));
        var frame = definitionEntries.EntityFrameTable.Frames[0];
        AssertArgs(frame, [10, 20, 30, 40, 50, 60, 70, 80]);
        frame.ActionFunction.Should().NotBeNull();
        frame.ActionFunction.Method.Name.Should().Be("A_MonsterMeleeAttack");
    }

    [Fact(DisplayName = "A_WeaponBulletAttack defaults")]
    public void A_WeaponBulletAttackDefaults()
    {
        var definitionEntries = CreateAndApply(ParseDehacked("WeaponBulletAttack"));
        var frame = definitionEntries.EntityFrameTable.Frames[0];
        AssertArgs(frame, [0, 0, 1, 5, 3, 0, 0, 0]);
        frame.ActionFunction.Should().NotBeNull();
        frame.ActionFunction.Method.Name.Should().Be("A_WeaponBulletAttack");
    }

    [Fact(DisplayName = "A_WeaponBulletAttack")]
    public void A_WeaponBulletAttac()
    {
        var definitionEntries = CreateAndApply(ParseDehacked("WeaponBulletAttack", noArgs: false));
        var frame = definitionEntries.EntityFrameTable.Frames[0];
        AssertArgs(frame, [10, 20, 30, 40, 50, 60, 70, 80]);
        frame.ActionFunction.Should().NotBeNull();
        frame.ActionFunction.Method.Name.Should().Be("A_WeaponBulletAttack");
    }

    [Fact(DisplayName = "A_WeaponMeleeAttack defaults")]
    public void A_WeaponMeleeAttackDefaults()
    {
        var definitionEntries = CreateAndApply(ParseDehacked("WeaponMeleeAttack"));
        var frame = definitionEntries.EntityFrameTable.Frames[0];
        AssertArgs(frame, [2, 10, 65536, 0, 0, 0, 0, 0]);
        frame.ActionFunction.Should().NotBeNull();
        frame.ActionFunction.Method.Name.Should().Be("A_WeaponMeleeAttack");
    }

    [Fact(DisplayName = "A_WeaponMeleeAttack defaults")]
    public void A_WeaponMeleeAttack()
    {
        var definitionEntries = CreateAndApply(ParseDehacked("WeaponMeleeAttack", noArgs: false));
        var frame = definitionEntries.EntityFrameTable.Frames[0];
        AssertArgs(frame, [10, 20, 30, 40, 50, 60, 70, 80]);
        frame.ActionFunction.Should().NotBeNull();
        frame.ActionFunction.Method.Name.Should().Be("A_WeaponMeleeAttack");
    }

    private static void AssertArgs(EntityFrame frame, params int[] args)
    {
        frame.DehackedArgs1.Should().Be(args[0]);
        frame.DehackedArgs2.Should().Be(args[1]);
        frame.DehackedArgs3.Should().Be(args[2]);
        frame.DehackedArgs4.Should().Be(args[3]);
        frame.DehackedArgs5.Should().Be(args[4]);
        frame.DehackedArgs6.Should().Be(args[5]);
        frame.DehackedArgs7.Should().Be(args[6]);
        frame.DehackedArgs8.Should().Be(args[7]);
    }
    private static DefinitionEntries CreateAndApply(DehackedDefinition definition)
    {
        var archiveCollection = new ArchiveCollection(new FilesystemArchiveLocator(), new(), new());
        var definitionEntries = new DefinitionEntries(archiveCollection, new());
        var applier = new DehackedApplier(definitionEntries, definition);
        applier.Apply(definition, definitionEntries, new(archiveCollection));
        return definitionEntries;
    }

    private static DehackedDefinition ParseDehacked(string functionName, bool noArgs = true)
    {
        var dehacked = GetDehacked(functionName, noArgs);
        var def = new DehackedDefinition();
        def.Parse(dehacked);
        return def;
    }

    private static string GetDehacked(string functionName, bool noArgs = true)
    {
        return $@"
[CodePtr]
Frame 1234 = {functionName}

Frame 1234
Sprite number = 1
Sprite subnumber = 2
Duration = 3
Next frame = 4
" +
(noArgs ? "" :
@"Args1 = 10
Args2 = 20
Args3 = 30
Args4 = 40
Args5 = 50
Args6 = 60
Args7 = 70
Args8 = 80");
    }
}
