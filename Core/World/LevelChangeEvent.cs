using static Helion.Util.Assertion.Assert;

namespace Helion.World
{
    public class LevelChangeEvent
    {
        public readonly LevelChangeType ChangeType;
        public readonly int LevelNumber = 1;

        public LevelChangeEvent(LevelChangeType levelChangeType)
        {
            Precondition(levelChangeType != LevelChangeType.SpecificLevel, "Wrong level change type constructor");

            ChangeType = levelChangeType;
        }

        public LevelChangeEvent(int levelNumber)
        {
            Precondition(levelNumber >= 0, "Cannot have a negative level number");

            ChangeType = LevelChangeType.SpecificLevel;
            LevelNumber = levelNumber;
        }
    }

    public enum LevelChangeType
    {
        Next,
        SecretNext,
        SpecificLevel,
        Reset
    }
}