using Helion.Models;
using Helion.Util.Container;
namespace Helion.World.Special;

public class SpecialModelData
{
    public readonly DynamicArray<ISpecialModel> Specials = [];
    public readonly DynamicArray<SectorMoveSpecialModel> MoveSpecials = [];
    public readonly DynamicArray<ScrollSpecialModel> ScrollSpecials = [];
    public readonly DynamicArray<LightChangeSpecialModel> LightChangeSpecials = [];
    public readonly DynamicArray<LightFireFlickerDoomModel> LightFireFlickerDoomSpecials = [];
    public readonly DynamicArray<LightFlickerDoomSpecialModel> LightFlickerDoomSpecials = [];
    public readonly DynamicArray<LightPulsateSpecialModel> LightPulsateSpecials = [];
    public readonly DynamicArray<LightStrobeSpecialModel> LightStrobeSpecials = [];
    public readonly DynamicArray<PushSpecialModel> PushSpecials = [];
    public readonly DynamicArray<StairSpecialModel> StairSpecials = [];
    public readonly DynamicArray<ElevatorSpecialModel> ElevatorSpecials = [];
    public readonly DynamicArray<SwitchChangeSpecialModel> SwitchSpecials = [];
    public readonly DynamicArray<SectorDamageSpecialModel> SectorDamageSpecials = [];

    public void Clear()
    {
        Specials.Clear();
        MoveSpecials.Clear();
        ScrollSpecials.Clear();
        LightChangeSpecials.Clear();
        LightFireFlickerDoomSpecials.Clear();
        LightFlickerDoomSpecials.Clear();
        LightPulsateSpecials.Clear();
        LightStrobeSpecials.Clear();
        PushSpecials.Clear();
        StairSpecials.Clear();
        ElevatorSpecials.Clear();
        SwitchSpecials.Clear();
        SectorDamageSpecials.Clear();
    }
}
