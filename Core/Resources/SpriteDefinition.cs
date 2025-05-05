using System.Collections.Generic;
using Helion.Resources.Archives.Entries;

namespace Helion.Resources;

public class SpriteDefinition
{
    public const int MaxFrames = 29;
    public const int MaxRotations = 8;
    public SpriteRotation?[,] Rotations = new SpriteRotation[MaxFrames, MaxRotations];
    public bool HasRotations;

    private static readonly Dictionary<string, Texture> SpriteTextureLookup = [];

    public SpriteDefinition(IList<Entry> entries, TextureManager textureManager)
    {
        int frame;
        int rotation;

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry.Path.Name.Length < 6)
                continue;

            frame = entry.Path.Name[4] - 'A';
            rotation = entry.Path.Name[5] - '0';

            CreateRotations(entry, textureManager, frame, rotation, false);

            if (entry.Path.Name.Length > 7)
            {
                frame = entry.Path.Name[6] - 'A';
                rotation = entry.Path.Name[7] - '0';
                CreateRotations(entry, textureManager, frame, rotation, true);
            }
        }
    }

    public SpriteRotation? GetSpriteRotation(int frame, uint rotation) =>
        Rotations[frame, rotation];

    private void CreateRotations(Entry entry, TextureManager textureManager, int frame, int rotation, bool mirror)
    {
        if (frame < 0 || frame >= MaxFrames)
            return;

        bool brightmapNoFullbright = false;
        if (!SpriteTextureLookup.TryGetValue(entry.Path.Name, out var texture))
        {
            texture = new(entry.Path.Name, ResourceNamespace.Sprites, 0);
            texture.Image = textureManager.ImageRetriever.GetOnly(entry.Path.Name, ResourceNamespace.Sprites);
            var brightmap = textureManager.GetBrightmapFor(texture.Name, ResourceNamespace.Sprites);
            texture.BrightmapImage = brightmap.Image;
            brightmapNoFullbright = brightmap.DisableFullbright;
            SpriteTextureLookup[entry.Path.Name] = texture;
        }

        // Does not have any rotations, just fill all 8 with the same texture for easier lookups
        if (rotation == 0)
        {
            SpriteRotation sr = new(texture, mirror, brightmapNoFullbright);
            for (int i = 0; i < 8; i++)
                Rotations[frame, i] = sr;
        }
        else
        {
            HasRotations = true;
            rotation--;
            if (rotation < 0 || rotation >= MaxRotations)
                return;

            Rotations[frame, rotation] = new SpriteRotation(texture, mirror, brightmapNoFullbright);
        }
    }
}
