using Helion.Resource.Textures;
using Helion.Util;

namespace Helion.Resource.Sprites
{
    /// <summary>
    /// A collection of texture for the sprite.
    /// </summary>
    public class Sprite
    {
        public const int MaxRotations = 8;

        /// <summary>
        /// The name of the sprite. This is equal to `BaseName + BaseFrame`.
        /// </summary>
        public readonly CIString Name;

        /// <summary>
        /// The base name of the sprite (ex: POSSA means this is "POSS").
        /// This is always four letters.
        /// </summary>
        public readonly CIString BaseName;

        /// <summary>
        /// The letter from the frame (ex: POSSA means this is 'A').
        /// </summary>
        public readonly char BaseFrame;

        /// <summary>
        /// The rotation images.
        /// </summary>
        public readonly SpriteRotation[] Rotations;

        /// <summary>
        /// True if it has rotations, false if not (as in a 0 frame, like
        /// CNDLA0).
        /// </summary>
        public readonly bool HasRotations;

        /// <summary>
        /// Creates a sprite from a single frame. Intended for sprites that
        /// have no rotations (as in, ending with xxxxx0).
        /// </summary>
        /// <param name="name">The sprite name.</param>
        /// <param name="tex0">The single texture for each rotation.</param>
        public Sprite(CIString name, Texture tex0)
        {
            SpriteRotation rotation = new(tex0, false);

            Name = name;
            BaseName = name.ToString().Substring(0, 4);
            BaseFrame = name[4];
            HasRotations = false;
            Rotations = new[] { rotation, rotation, rotation, rotation, rotation, rotation, rotation, rotation };
        }

        /// <summary>
        /// A sprite that has mirroring textures.
        /// </summary>
        /// <param name="name">The sprite name.</param>
        /// <param name="tex1">The front texture.</param>
        /// <param name="tex28">The 45 degree rotation (and mirror).</param>
        /// <param name="tex37">The 90 degree rotation (and mirror).</param>
        /// <param name="tex46">The 135 degree rotation (and mirror).</param>
        /// <param name="tex5">The back texture.</param>
        public Sprite(CIString name, Texture tex1, Texture tex28, Texture tex37, Texture tex46, Texture tex5)
        {
            Name = name;
            BaseName = name.ToString().Substring(0, 4);
            BaseFrame = name[4];
            HasRotations = true;
            Rotations = new[]
            {
                new SpriteRotation(tex1, false),
                new SpriteRotation(tex28, false),
                new SpriteRotation(tex37, false),
                new SpriteRotation(tex46, false),
                new SpriteRotation(tex5, false),
                new SpriteRotation(tex46, true),
                new SpriteRotation(tex37, true),
                new SpriteRotation(tex28, true),
            };
        }

        /// <summary>
        /// A sprite from all 8 frames.
        /// </summary>
        /// <param name="name">The sprite name.</param>
        /// <param name="tex1">The front texture.</param>
        /// <param name="tex2">The 45 degree rotation.</param>
        /// <param name="tex3">The 90 degree rotation.</param>
        /// <param name="tex4">The 135 degree rotation.</param>
        /// <param name="tex5">The back texture.</param>
        /// <param name="tex6">The other 45 degree rotation.</param>
        /// <param name="tex7">The other 90 degree rotation.</param>
        /// <param name="tex8">The other 135 degree rotation.</param>
        public Sprite(CIString name, Texture tex1, Texture tex2, Texture tex3, Texture tex4, Texture tex5,
            Texture tex6,Texture tex7, Texture tex8)
        {
            Name = name;
            BaseName = name.ToString().Substring(0, 4);
            BaseFrame = name[4];
            HasRotations = true;
            Rotations = new[]
            {
                new SpriteRotation(tex1, false),
                new SpriteRotation(tex2, false),
                new SpriteRotation(tex3, false),
                new SpriteRotation(tex4, false),
                new SpriteRotation(tex5, false),
                new SpriteRotation(tex6, false),
                new SpriteRotation(tex7, false),
                new SpriteRotation(tex8, false),
            };
        }

        public override string ToString() => Name.ToString();
    }
}
