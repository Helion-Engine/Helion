using System.Linq;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions;
using Helion.Resources.Definitions.Animdefs.Textures;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;

namespace Helion.World.Special.Switches;

public static class SwitchManager
{
    public static bool IsLineSwitch(ArchiveCollection archiveCollection, Line line) => GetLineLineSwitchTexture(archiveCollection, line).Item1 != Constants.NoTextureIndex;

    public static void SetLineSwitch(ArchiveCollection archiveCollection, Line line)
    {
        (int, WallLocation) switchSet = GetLineLineSwitchTexture(archiveCollection, line);
        if (switchSet.Item1 != Constants.NoTextureIndex)
        {
            if (line.Back != null)
            {
                switch (switchSet.Item2)
                {
                    case WallLocation.Upper:
                        line.Front.Upper.SetTexture(switchSet.Item1, SideDataTypes.UpperTexture);
                        break;
                    case WallLocation.Middle:
                        line.Front.Middle.SetTexture(switchSet.Item1, SideDataTypes.MiddleTexture);
                        break;
                    case WallLocation.Lower:
                        line.Front.Lower.SetTexture(switchSet.Item1, SideDataTypes.LowerTexture);
                        break;
                }
            }
            else
            {
                line.Front.Middle.SetTexture(switchSet.Item1, SideDataTypes.MiddleTexture);
            }
        }
    }

    private static (int, WallLocation) GetLineLineSwitchTexture(ArchiveCollection archiveCollection, Line line)
    {
        if (line.Back != null)
        {
            Side side = line.Front;
            for (int i = 0; i < archiveCollection.Definitions.Animdefs.AnimatedSwitches.Count; i++)
            {
                var animSwitch = archiveCollection.Definitions.Animdefs.AnimatedSwitches[i];
                if (animSwitch.IWad != IWadBaseType.None && animSwitch.IWad != archiveCollection.IWadType)
                    continue;

                if (side.Upper.TextureHandle != Constants.NoTextureIndex && animSwitch.IsMatch(side.Upper.TextureHandle))
                    return (animSwitch.GetOpposingTexture(side.Upper.TextureHandle), WallLocation.Upper);

                if (side.Middle.TextureHandle != Constants.NoTextureIndex && animSwitch.IsMatch(side.Middle.TextureHandle))
                    return (animSwitch.GetOpposingTexture(side.Middle.TextureHandle), WallLocation.Middle);

                if (side.Lower.TextureHandle != Constants.NoTextureIndex && animSwitch.IsMatch(side.Lower.TextureHandle))
                    return (animSwitch.GetOpposingTexture(side.Lower.TextureHandle), WallLocation.Lower);
            }
        }
        else
        {
            var switchList = archiveCollection.Definitions.Animdefs.AnimatedSwitches;
            AnimatedSwitch? animSwitch = switchList.FirstOrDefault(sw => 
                (sw.IWad == IWadBaseType.None || sw.IWad == archiveCollection.IWadType) &&
                sw.IsMatch(line.Front.Middle.TextureHandle));
            if (animSwitch != null)
                return (animSwitch.GetOpposingTexture(line.Front.Middle.TextureHandle), WallLocation.Middle);
        }

        return (Constants.NoTextureIndex, WallLocation.None);
    }
}
