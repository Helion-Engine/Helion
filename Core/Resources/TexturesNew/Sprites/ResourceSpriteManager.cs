using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.TexturesNew.Sprites;

public class ResourceSpriteManager : IResourceSpriteManager
{
    private const int Rotations = 8;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    private readonly ResourceTextureManager m_textureManager;
    // We will use 'null' here to mean we tried looking it up, and it was not found.
    private readonly Dictionary<string, ResourceSpriteDefinition?> m_spriteDefinitions = new(StringComparer.OrdinalIgnoreCase);

    public ResourceSpriteManager(ResourceTextureManager textureManager)
    {
        m_textureManager = textureManager;
    }

    public bool TryGet(string name, [NotNullWhen(true)] out ResourceSpriteDefinition? sprite)
    {
        if (name.Length != 5)
        {
            Fail($"Invalid call to {nameof(ResourceSpriteManager)}.{nameof(TryGet)}, {name} was not 5 characters (ex: PLAYB)");
            sprite = null;
            return false;
        }
        
        if (m_spriteDefinitions.TryGetValue(name, out sprite))
            return sprite != null;

        if (TryLoadAndTrack(name, out sprite))
            return true;
        
        // Since we didn't find it, we store null so we don't keep trying to
        // look up something that doesn't exist.
        m_spriteDefinitions[name] = null;
        
        sprite = null;
        return false;
    }

    private bool TryLoadAndTrack(string name, [NotNullWhen(true)] out ResourceSpriteDefinition? sprite)
    {
        char frame = name[4];
        ResourceSpriteRotation?[] frames = new ResourceSpriteRotation?[Rotations];

        // Successively overwrite whatever we find.
        FindFrameZero();
        FindFrameMirror(2, 8);
        FindFrameMirror(3, 7);
        FindFrameMirror(4, 6);
        FindIndividualFrames();

        if (!PopulateMissingFrames(name, frames))
        {
            // Since we had all null, then there is no sprite that exists for this.
            Log.Warn($"Cannot find any sprite textures for {name.ToUpper()}");
            sprite = null;
            return false;
        }

        Invariant(frames.All(f => f != null), "Should not have a null sprite at this point");
        sprite = new ResourceSpriteDefinition(frames[0]!, frames[1]!, frames[2]!, frames[3]!, frames[4]!, frames[5]!, frames[6]!, frames[7]!);
        m_spriteDefinitions[name] = sprite;
        return true;

        void FindFrameZero()
        {
            if (m_textureManager.TryGet($"{name}0", ResourceNamespace.Sprites, out ResourceTexture? frame0))
                for (int i = 0; i < Rotations; i++)
                    frames[i] = new ResourceSpriteRotation(frame0);
        }

        void FindIndividualFrames()
        {
            for (int i = 1; i <= Rotations; i++)
                FindFrame(i);
        }

        // Note: This deals with [1..Rotations], not [0..Rotations)!
        void FindFrame(int frameIndex)
        {
            if (m_textureManager.TryGet($"{name}{frameIndex}", ResourceNamespace.Sprites, out ResourceTexture? nonMirrorFrame))
                frames[frameIndex - 1] = new ResourceSpriteRotation(nonMirrorFrame);
        }
        
        void FindFrameMirror(int nonMirrorIndex, int mirrorIndex)
        {
            Invariant(nonMirrorIndex < mirrorIndex, "Non mirror sprite index should be ordered first");
            
            string spriteName = $"{name}{nonMirrorIndex}{frame}{mirrorIndex}";
            
            if (m_textureManager.TryGet(spriteName, ResourceNamespace.Sprites, out ResourceTexture? mirrorFrame))
            {
                frames[nonMirrorIndex - 1] = new ResourceSpriteRotation(mirrorFrame);
                frames[mirrorIndex - 1] = new ResourceSpriteRotation(mirrorFrame, true);
            }
        }
    }
    
    private static bool PopulateMissingFrames(string sprite, ResourceSpriteRotation?[] frames)
    {
        ResourceSpriteRotation? rotation = frames.FirstOrDefault(f => f != null);
        
        // If everything is null, then this sprite cannot possibly exist.
        if (rotation == null)
            return false;

        // Whatever was the first non-null frame, write it everywhere there is
        // a missing frame.
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
                continue;
            
            Log.Warn($"Missing sprite {sprite.ToUpper()}{i} (and/or mirror as well)");
            frames[i] = rotation;
        }

        return true;
    }
}