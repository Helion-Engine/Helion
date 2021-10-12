namespace Helion.World.Special.SectorMovement;

public class SectorSoundData
{
    public readonly string? StartSound;
    public readonly string? ReturnSound;
    public readonly string? StopSound;
    public readonly string? MovementSound;

    public SectorSoundData()
    {
    }

    public SectorSoundData(string? startSound, string? returnSound, string? stopSound, string? movementSound = null)
    {
        StartSound = startSound;
        ReturnSound = returnSound;
        StopSound = stopSound;
        MovementSound = movementSound;
    }
}

