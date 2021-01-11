using System.IO;

namespace Helion.Resources.IWad
{
    public class IWadInfo
    {
        public static readonly IWadInfo DefaultIWadInfo = new IWadInfo(string.Empty, IWadType.None, string.Empty);

        public readonly string Title;
        public readonly IWadType IWadType;
        public readonly string MapInfoResource;

        public IWadInfo(string title, IWadType type, string mapInfo)
        {
            Title = title;
            IWadType = type;
            MapInfoResource = mapInfo;
        }

        public static IWadInfo? GetIWadInfo(string fileName)
        {
            string name = Path.GetFileNameWithoutExtension(fileName).ToUpper();

            return name switch
            {
                "DOOM1" => new IWadInfo("Doom Shareware", IWadType.DoomShareware, GetMapInfoResource("doom1")),
                "DOOM" => new IWadInfo("The Ultimate Doom", IWadType.UltimateDoom, GetMapInfoResource("doom1")),
                "DOOM2" => new IWadInfo("Doom II: Hell on Earth", IWadType.Doom2, GetMapInfoResource(name)),
                "PLUTONIA" => new IWadInfo("Final Doom: The Plutonia Experiment", IWadType.Plutonia, GetMapInfoResource(name)),
                "TNT" => new IWadInfo("Final Doom: TNT: Evilution", IWadType.TNT, GetMapInfoResource(name)),
                "CHEX" => new IWadInfo("Chex Quest", IWadType.ChexQuest, GetMapInfoResource(name)),
                _ => null,
            };
        }

        private static string GetMapInfoResource(string name) => $"MapInfo/{name}.txt";
    }
}
