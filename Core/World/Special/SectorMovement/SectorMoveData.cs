using Helion.World.Special.Specials;

namespace Helion.World.Special.SectorMovement
{
    public class SectorMoveData
    {
        public SectorPlaneType SectorMoveType { get; set; }
        public readonly MoveRepetition MoveRepetition;
        public readonly double Speed;
        public readonly double ReturnSpeed;
        public readonly int Delay;
        public readonly CrushData? Crush;
        public readonly int? FloorChangeTextureHandle;
        public readonly int? CeilingChangeTextureHandle;
        public readonly SectorDamageSpecial? DamageSpecial;
        public readonly MoveDirection StartDirection;
        // If an entity will block movement then do not calculate the difference, movement is blocked entirely.
        public readonly bool CompatibilityBlockMovement;

        public const int InstantToggleSpeed = int.MaxValue;

        public SectorMoveData(SectorPlaneType moveType, MoveDirection startDirection, MoveRepetition repetition, 
            double speed, int delay, CrushData? crush = null,
            int? floorChangeTextureHandle = null,
            int? ceilingChangeTextureHandle = null,
            SectorDamageSpecial? damageSpecial = null,
            double? returnSpeed = null,
            bool compatibilityBlockMovement = false)
        {
            SectorMoveType = moveType;
            StartDirection = startDirection;
            MoveRepetition = repetition;
            Speed = speed;
            Delay = delay;
            Crush = crush;
            FloorChangeTextureHandle = floorChangeTextureHandle;
            CeilingChangeTextureHandle = ceilingChangeTextureHandle;
            DamageSpecial = damageSpecial;
            ReturnSpeed = returnSpeed ?? speed;
            CompatibilityBlockMovement = compatibilityBlockMovement;
        }
    }
}
