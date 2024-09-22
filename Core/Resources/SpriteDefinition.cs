using System.Collections.Generic;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Images;

namespace Helion.Resources;

public class SpriteDefinition
{
    public const int MaxFrames = 29;
    public const int MaxRotations = 8;
    public SpriteRotation?[,] Rotations = new SpriteRotation[MaxFrames, MaxRotations];
    public bool HasRotations;

    private static readonly Dictionary<string, Texture> SpriteTextureLookup = [];

    public SpriteDefinition(IList<Entry> entries, IImageRetriever imageRetriever)
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

            CreateRotations(entry, imageRetriever, frame, rotation, false);

            if (entry.Path.Name.Length > 7)
            {
                frame = entry.Path.Name[6] - 'A';
                rotation = entry.Path.Name[7] - '0';
                CreateRotations(entry, imageRetriever, frame, rotation, true);
            }
        }
    }

    public SpriteRotation? GetSpriteRotation(int frame, uint rotation) =>
        Rotations[frame, rotation];

    private void CreateRotations(Entry entry, IImageRetriever imageRetriever, int frame, int rotation, bool mirror)
    {
        if (frame < 0 || frame >= MaxFrames)
            return;

        if (!SpriteTextureLookup.TryGetValue(entry.Path.Name, out var texture))
        {
            texture = new(entry.Path.Name, ResourceNamespace.Sprites, 0);
            texture.Image = imageRetriever.GetOnly(entry.Path.Name, ResourceNamespace.Sprites);
            SpriteTextureLookup[entry.Path.Name] = texture;
        }

        // Does not have any rotations, just fill all 8 with the same texture for easier lookups
        if (rotation == 0)
        {
            SpriteRotation sr = new(texture, mirror);
            for (int i = 0; i < 8; i++)
                Rotations[frame, i] = sr;
        }
        else
        {
            HasRotations = true;
            rotation--;
            if (rotation < 0 || rotation >= MaxRotations)
                return;

            Rotations[frame, rotation] = new SpriteRotation(texture, mirror);
        }
    }
}
