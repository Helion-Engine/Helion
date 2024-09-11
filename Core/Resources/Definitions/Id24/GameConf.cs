namespace Helion.Resources.Definitions.Id24;

public class GameConf
{
    public string Type { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    // TODO: id1.wad's GAMECONF has an empty "metadata": {} block here, but it doesn't seem to be in the spec
    public GameConfData Data { get; set; } = new();
}

public class GameConfData
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Description { get; set; }
    public string? Version { get; set; }
    public string? Iwad { get; set; }
    public string[]? Pwads { get; set; }
    public string[]? PlayerTranslations { get; set; }
    public string? WadTranslation { get; set; }
    public string? Executable { get; set; }
    public string? Mode { get; set; }
    public string? Options { get; set; }
}

public static class GameConfConstants
{
    public static class Header
    {
        public const string Type = "gameconf";
        public const string Version = "1.0.0";
    }

    public static class Version
    {
        public const string Doom1_9 = "doom1.9";
        public const string LimitRemoving = "limitremoving";
        public const string BugFixed = "bugfixed";
        public const string Boom2_02 = "boom2.02";
        public const string Complevel9 = "complevel9";
        public const string Mbf = "mbf";
        public const string Mbf21 = "mbf21";
        public const string Mbf21Ex = "mbf21ex";
        public const string Id24 = "id24";
    }

    public static readonly string[] ValidVersions = [
        Version.Doom1_9,
        Version.LimitRemoving,
        Version.BugFixed,
        Version.Boom2_02,
        Version.Complevel9,
        Version.Mbf,
        Version.Mbf21,
        Version.Mbf21Ex,
        Version.Id24,
    ];

    public static class Mode
    {
        public const string Registered = "registered";
        public const string Retail = "retail";
        public const string Commercial = "commercial";
    }

    public static readonly string[] ValidModes = [
        Mode.Registered,
        Mode.Retail,
        Mode.Commercial,
    ];
}