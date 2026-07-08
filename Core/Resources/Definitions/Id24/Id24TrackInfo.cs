namespace Helion.Resources.Definitions.Id24;

public class Id24TrackInfo
{
    public string? Midi { get; set; }
    public string? Remixed { get; set; }

    public bool IsValid() => !string.IsNullOrEmpty(Midi) || !string.IsNullOrEmpty(Remixed);
}
