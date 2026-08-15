using System.Linq;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.Animdefs.Textures;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;

namespace Helion.World.Special.Switches;

public enum SwitchTextureType
{
    Current,
    Flip,
    Off
}

public readonly record struct SwitchTexture(int TextureHandle, WallLocation Location);

public static class SwitchManager
{
    public static bool IsLineSwitch(ArchiveCollection archiveCollection, Line line) => 
        GetLineLineSwitchTexture(archiveCollection, line, SwitchTextureType.Current).TextureHandle != Constants.NoTextureIndex;

    public static void SetLineSwitch(IWorld world, Line line, SwitchTextureType type)
    {
        var switchSet = GetLineLineSwitchTexture(world.ArchiveCollection, line, type);
        if (switchSet.TextureHandle != Constants.NoTextureIndex)
        {
            if (line.Back != null)
                world.SetSideTexture(line.Front, switchSet.Location, switchSet.TextureHandle);
            else
                world.SetSideTexture(line.Front, WallLocation.Middle, switchSet.TextureHandle);
        }
    }

    public static void SetLineSwitch(IWorld world, Line line, int textureHandle)
    {
        var switchSet = GetLineLineSwitchTexture(world.ArchiveCollection, line, SwitchTextureType.Current);
        if (switchSet.TextureHandle != Constants.NoTextureIndex)
        {
            if (line.Back != null)
                world.SetSideTexture(line.Front, switchSet.Location, textureHandle);
            else
                world.SetSideTexture(line.Front, WallLocation.Middle, textureHandle);
        }
    }

    public static SwitchTexture GetLineLineSwitchTexture(ArchiveCollection archiveCollection, Line line, SwitchTextureType type)
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
                    return GetSwitchTexture(animSwitch, side.Upper.TextureHandle, WallLocation.Upper, type);

                if (side.Middle.TextureHandle != Constants.NoTextureIndex && animSwitch.IsMatch(side.Middle.TextureHandle))
                    return GetSwitchTexture(animSwitch, side.Middle.TextureHandle, WallLocation.Middle, type);

                if (side.Lower.TextureHandle != Constants.NoTextureIndex && animSwitch.IsMatch(side.Lower.TextureHandle))
                    return GetSwitchTexture(animSwitch, side.Lower.TextureHandle, WallLocation.Lower, type);
            }
        }
        else
        {
            var switchList = archiveCollection.Definitions.Animdefs.AnimatedSwitches;
            var animSwitch = switchList.FirstOrDefault(sw => 
                (sw.IWad == IWadBaseType.None || sw.IWad == archiveCollection.IWadType) &&
                sw.IsMatch(line.Front.Middle.TextureHandle));
            if (animSwitch != null)
                return GetSwitchTexture(animSwitch, line.Front.Middle.TextureHandle, WallLocation.Middle, type);
        }

        return new(Constants.NoTextureIndex, WallLocation.None);
    }

    private static SwitchTexture GetSwitchTexture(AnimatedSwitch animSwitch, int textureHandle, WallLocation location, SwitchTextureType type)
    {
        return type switch
        {
            SwitchTextureType.Current => new(textureHandle, location),
            SwitchTextureType.Off => new(animSwitch.GetOffTexture(), location),
            _ => new(animSwitch.GetOpposingTexture(textureHandle), location),
        };
    }
}
