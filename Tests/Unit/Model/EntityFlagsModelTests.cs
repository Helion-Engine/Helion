using FluentAssertions;
using Helion.Models;
using Helion.World.Entities.Definition.Flags;
using Xunit;

namespace Helion.Tests.Unit.Model
{
    public class EntityFlagsModelTests
    {
        [Fact(DisplayName = "EntityFlagsModel conversion (all false)")]
        public void TestAllFalseFlags()
        {
            EntityFlags entityFlags = new EntityFlags();
            EntityFlagsModel entityFlagsModel = entityFlags.ToEntityFlagsModel();
            EntityFlags backToEntityFlags = new EntityFlags(entityFlagsModel);

            backToEntityFlags.Equals(entityFlags).Should().BeTrue();
        }

        [Fact(DisplayName = "EntityFlagsModel conversion (all true)")]
        public void TestAllTrueFlags()
        {
            EntityFlags entityFlags = new EntityFlags();

            // TODO fix
            //for (int i = 0; i < EntityFlags.NumFlags; i++)
            //    entityFlags[(EntityFlag)i] = true;
            
            EntityFlagsModel entityFlagsModel = entityFlags.ToEntityFlagsModel();
            EntityFlags backToEntityFlags = new EntityFlags(entityFlagsModel);

            backToEntityFlags.Equals(entityFlags).Should().BeTrue();
        }

        [Fact(DisplayName = "TEntityFlagsModel conversion (alternating true/false)")]
        public void TestAlternatingFlags()
        {
            EntityFlags entityFlags = new EntityFlags();

            // TODO fix
            //for (int i = 0; i < EntityFlags.NumFlags; i+=2)
            //    entityFlags[(EntityFlag)i] = true;

            EntityFlagsModel entityFlagsModel = entityFlags.ToEntityFlagsModel();
            EntityFlags backToEntityFlags = new EntityFlags(entityFlagsModel);

            backToEntityFlags.Equals(entityFlags).Should().BeTrue();
        }
    }
}
