using Helion.Util.Bytes;
using System.Collections.Generic;

namespace Helion.Resources.IWad
{
    public class IWadInfo
    {
        private class IWadData
        {
            public readonly string Title;
            public readonly string MapInfo;
            public readonly string Decorate;
            public IWadData(string title, string mapinfo, string decorate)
            {
                Title = title;
                MapInfo = mapinfo;
                Decorate = decorate;
            }
        }

        private const string DoomDecorate = "Decorate/DoomDecorate.txt";

        private static readonly Dictionary<IWadType, IWadData> IWadDataLookup = new()
        {
            { IWadType.None, new(string.Empty, string.Empty, string.Empty) },
            { IWadType.Doom2, new("Doom II: Hell on Earth", "MapInfo/Doom2.txt", DoomDecorate) },
            { IWadType.Plutonia, new("Final Doom: The Plutonia Experiment", "MapInfo/Plutonia.txt", DoomDecorate) },
            { IWadType.TNT, new("Final Doom: TNT: Evilution", "MapInfo/Tnt.txt", DoomDecorate) },
            { IWadType.UltimateDoom, new("The Ultimate Doom", "MapInfo/DoomRegistered.txt", DoomDecorate) },
            { IWadType.ChexQuest, new("Chex Quest", "MapInfo/Chex.txt", "Decorate/ChexDecorate.txt") },
            { IWadType.DoomShareware, new("Doom Shareware", "MapInfo/Doom1.txt", DoomDecorate) },
            { IWadType.DoomRegistered, new("Doom", "MapInfo/DoomRegistered.txt", DoomDecorate) },
            { IWadType.NoRestForTheLiving, new("No Rest for the Living", "MapInfo/Doom2.txt", DoomDecorate) },
        };

        private readonly static Dictionary<string, IWadType> MD5Lookup = new()
        {
            { "90facab21eede7981be10790e3f82da2", IWadType.DoomShareware },
            { "52cbc8882f445573ce421fa5453513c1", IWadType.DoomShareware },
            { "30aa5beb9e5ebfbbe1e1765561c08f38", IWadType.DoomShareware },
            { "a21ae40c388cb6f2c3cc1b95589ee693", IWadType.DoomShareware },
            { "e280233d533dcc28c1acd6ccdc7742d4", IWadType.DoomShareware },
            { "762fd6d4b960d4b759730f01387a50a1", IWadType.DoomShareware },
            { "c428ea394dc52835f2580d5bfd50d76f", IWadType.DoomShareware },
            { "5f4eb849b1af12887dec04a2a12e5e62", IWadType.DoomShareware },
            { "f0cefca49926d00903cf57551d901abe", IWadType.DoomShareware },

            { "981b03e6d1dc033301aa3095acc437ce", IWadType.DoomRegistered },
            { "792fd1fea023d61210857089a7c1e351", IWadType.DoomRegistered },
            { "54978d12de87f162b9bcc011676cb3c0", IWadType.DoomRegistered },
            { "11e1cd216801ea2657723abc86ecb01f", IWadType.DoomRegistered },
            { "1cd63c5ddff1bf8ce844237f580e9cf3", IWadType.DoomRegistered },

            { "c4fe9fd920207691a9f493668e0a2083", IWadType.UltimateDoom },
            { "0c8758f102ccafe26a3040bee8ba5021", IWadType.UltimateDoom },
            { "72286ddc680d47b9138053dd944b2a3d", IWadType.UltimateDoom },
            { "fb35c4a5a9fd49ec29ab6e900572c524", IWadType.UltimateDoom },

            { "30e3c2d0350b67bfbf47271970b74b2f", IWadType.Doom2 },
            { "d9153ced9fd5b898b36cc5844e35b520", IWadType.Doom2 },
            { "ea74a47a791fdef2e9f2ea8b8a9da13b", IWadType.Doom2 },
            { "d7a07e5d3f4625074312bc299d7ed33f", IWadType.Doom2 },
            { "c236745bb01d89bbb866c8fed81b6f8c", IWadType.Doom2 },
            { "25e1459ca71d321525f84628f45ca8cd", IWadType.Doom2 },
            { "a793ebcdd790afad4a1f39cc39a893bd", IWadType.Doom2 },
            { "43c2df32dc6c740cb11f34dc5ab693fa", IWadType.Doom2 },
            { "c3bea40570c23e511a7ed3ebcd9865f7", IWadType.Doom2 },

            { "75c8cf89566741fa9d22447604053bd7", IWadType.Plutonia },
            { "3493be7e1e2588bc9c8b31eab2587a04", IWadType.Plutonia },

            { "4e158d9953c79ccf97bd0663244cc6b6", IWadType.TNT },
            { "1d39e405bf6ee3df69a8d2646c8d5c49", IWadType.TNT },

            { "967d5ae23daf45196212ae1b605da3b0", IWadType.NoRestForTheLiving },

            { "25485721882b050afa96a56e5758dd52", IWadType.ChexQuest },
        };

        public static readonly IWadInfo DefaultIWadInfo = new IWadInfo(string.Empty, IWadBaseType.None, IWadType.None, "MapInfo/Doom2.txt", DoomDecorate);

        public readonly string Title;
        public readonly IWadBaseType IWadBaseType;
        public readonly IWadType IWadType; 
        public readonly string MapInfoResource;
        public readonly string DecorateResource;

        public IWadInfo(string title, IWadBaseType baseType, IWadType type, string mapInfo, string decorate)
        {
            Title = title;
            IWadBaseType = baseType;
            IWadType = type;
            MapInfoResource = mapInfo;
            DecorateResource = decorate;
        }

        public static IWadInfo GetIWadInfo(string fileName)
        {
            string? md5 = Files.CalculateMD5(fileName);
            if (md5 == null)
                return DefaultIWadInfo;

            if (!MD5Lookup.TryGetValue(md5, out IWadType iwadType))
                return DefaultIWadInfo;

            IWadBaseType baseType = (IWadBaseType)iwadType;
            if (iwadType == IWadType.DoomShareware || iwadType == IWadType.DoomRegistered)
                baseType = IWadBaseType.Doom1;
            else if (iwadType == IWadType.NoRestForTheLiving)
                baseType = IWadBaseType.Doom2;

            if (IWadDataLookup.TryGetValue(iwadType, out IWadData? data))
                return new IWadInfo(data.Title, baseType, iwadType, data.MapInfo, data.Decorate);

            return DefaultIWadInfo;
        }
    }
}
