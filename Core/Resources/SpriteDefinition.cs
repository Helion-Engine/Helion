using System.Collections.Generic;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Images;

namespace Helion.Resources;

public class SpriteDefinition
{
    private const int MaxFrames = 29;
    private const int MaxRotations = 8;

    public readonly string Name;
    public bool HasRotations { get; private set; }

    private readonly SpriteRotation?[,] m_spriteRotations = new SpriteRotation[MaxFrames, MaxRotations];

    public SpriteDefinition(string name, List<Entry> entries, IImageRetriever imageRetriever)
    {
        Name = name;

        int frame;
        int rotation;

        foreach (var entry in entries)
        {
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
        m_spriteRotations[frame, rotation];

    private void CreateRotations(Entry entry, IImageRetriever imageRetriever, int frame, int rotation, bool mirror)
    {
        if (frame < 0 || frame >= MaxFrames)
            return;

        Texture texture = new(entry.Path.Name, ResourceNamespace.Sprites, 0);
        texture.Image = imageRetriever.GetOnly(entry.Path.Name, ResourceNamespace.Sprites);

        // Does not have any rotations, just fill all 8 with the same texture for easier lookups
        if (rotation == 0)
        {
            SpriteRotation sr = new(texture, mirror);
            for (int i = 0; i < 8; i++)
                m_spriteRotations[frame, i] = sr;
        }
        else
        {
            HasRotations = true;
            rotation--;
            if (rotation < 0 || rotation >= MaxRotations)
                return;

            m_spriteRotations[frame, rotation] = new SpriteRotation(texture, mirror);
        }
    }
}
