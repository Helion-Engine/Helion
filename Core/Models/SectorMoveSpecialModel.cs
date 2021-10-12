using Helion.World;
using Helion.World.Special;
using Helion.World.Special.Specials;
using Newtonsoft.Json;

namespace Helion.Models;

public class SectorMoveSpecialModel : ISpecialModel
{
    public int SectorId { get; set; }
    public int MoveType { get; set; }
    public int Repetion { get; set; }
    public double Speed { get; set; }
    public double ReturnSpeed { get; set; }
    public int Delay { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int? FloorChange { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int? CeilingChange { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public SectorDamageSpecialModel? DamageSpecial { get; set; }
    public int StartDirection { get; set; }
    public bool CompatibilityBlockMovement { get; set; }

    public CrushDataModel? Crush { get; set; }

    public string? StartSound { get; set; }
    public string? ReturnSound { get; set; }
    public string? StopSound { get; set; }
    public string? MovementSound { get; set; }

    public double DestZ { get; set; }
    public double StartZ { get; set; }
    public double MinZ { get; set; }
    public double MaxZ { get; set; }
    public double CurrentSpeed { get; set; }
    public int DelayTics { get; set; }
    public int Direction { get; set; }
    public bool Crushing { get; set; }
    public bool PlayedReturnSound { get; set; }
    public bool PlayedStartSound { get; set; }
    public bool Paused { get; set; }

    public virtual ISpecial? ToWorldSpecial(IWorld world)
    {
        if (SectorId < 0 || SectorId >= world.Sectors.Count)
            return null;

        return new SectorMoveSpecial(world, world.Sectors[SectorId], this);
    }
}
