using Helion.World;
using Helion.World.Special;
using Helion.World.Special.Specials;
using Newtonsoft.Json;

namespace Helion.Models
{
    public class SectorMoveSpecialModel : ISpecialModel
    {
        public int SectorId;
        public int MoveType;
        public int Repetion;
        public double Speed;
        public int Delay;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? FloorChange;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SectorDamageSpecialModel? DamageSpecial;
        public int StartDirection;

        public CrushDataModel? Crush;

        public string? StartSound;
        public string? ReturnSound;
        public string? StopSound;
        public string? MovementSound;

        public double DestZ;
        public double StartZ;
        public double MinZ;
        public double MaxZ;
        public double CurrentSpeed;
        public int DelayTics;
        public int Direction;
        public bool Crushing;
        public bool PlayedReturnSound;
        public bool PlayedStartSound;
        public bool Paused;

        public ISpecial? ToWorldSpecial(IWorld world)
        {
            if (SectorId < 0 || SectorId >= world.Sectors.Count)
                return null;

            return new SectorMoveSpecial(world, world.Sectors[SectorId], this);
        }
    }
}
