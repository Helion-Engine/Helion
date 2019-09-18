using System.Collections.Generic;
using System.Linq;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Images;

namespace Helion.Resources
{
    public class SpriteDefinition
    {
        public string Name;
        private const int MaxFrames = 29;
        private const int MaxRotations = 8;
        private SpriteRotation[,] m_spriteRotations = new SpriteRotation[MaxFrames, MaxRotations];

        public SpriteDefinition(string name, List<Entry> entries, ArchiveImageRetriever imageRetriever)
        {
            Name = name;

            int frame;
            int rotation;

            foreach (var entry in entries)
            {
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

        public SpriteRotation GetSpriteRotation(int frame, int rotation)
        {
            return m_spriteRotations[frame, rotation];
        }

        private void CreateRotations(Entry entry, ArchiveImageRetriever imageRetriever, int frame, int rotation, bool mirror)
        {
            if (frame >= MaxFrames)
                return;

            Texture texture = new Texture(string.Empty, ResourceNamespace.Sprites, 0);
            texture.Image = imageRetriever.GetOnly(entry.Path.Name, ResourceNamespace.Sprites);

            // Does not have any rotations, just fill all 8 with the same texture for easier lookups
            if (rotation == 0)
            {
                for (int i = 0; i < 8; i++)
                    m_spriteRotations[frame, i] = new SpriteRotation(texture, mirror);
            }
            else
            {
                rotation--;
                if (rotation < 0 || rotation >= MaxRotations)
                    return;

                m_spriteRotations[frame, rotation] = new SpriteRotation(texture, mirror);
            }
        }
    }
}
