namespace Helion.World.Special.SectorMovement;

public readonly struct SectorSoundData
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

    public override bool Equals(object? obj)
    {
        if (obj is not SectorSoundData sound)
            return false;

        return sound.StartSound == StartSound && 
            sound.ReturnSound == ReturnSound && 
            sound.StopSound == StopSound && 
            sound.MovementSound == MovementSound;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
