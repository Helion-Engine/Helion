using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Helion.Resource;
using Helion.Resource.Textures;
using Helion.Resource.Tracker;
using Helion.Util;
using Helion.Worlds.Textures.Types;
using MoreLinq;

namespace Helion.Worlds.Textures
{
    /// <summary>
    /// Manages textures and logic in a world. Is responsible for coordination
    /// of animated/switch textures as well.
    /// </summary>
    public class WorldTextureManager : ITickable
    {
        public readonly StaticWorldTexture MissingTexture;
        public readonly StaticWorldTexture SkyTexture;
        private readonly TextureManager m_textureManager;
        private readonly NamespaceTracker<IWorldTexture> m_textures = new();
        private readonly HashSet<ITickable> m_tickableTextures = new();

        public WorldTextureManager(TextureManager textureManager)
        {
            m_textureManager = textureManager;
            MissingTexture = new("", textureManager.MissingTexture, true);
            SkyTexture = new("", textureManager.MissingTexture, isSky: true);
        }

        /// <summary>
        /// Gets a texture. Returns the
        /// </summary>
        /// <param name="name"></param>
        /// <param name="resourceNamespace"></param>
        /// <returns></returns>
        public IWorldTexture Get(CIString name, Namespace resourceNamespace)
        {
            IWorldTexture? texture = m_textures.GetOnly(name, resourceNamespace);
            if (texture != null)
                return ProcessTextureIfTickable(texture);

            if (TryCreateTexture(name, resourceNamespace, out IWorldTexture? newTexture))
                return ProcessTextureIfTickable(newTexture);

            m_textures.Insert(name, resourceNamespace, MissingTexture);
            return MissingTexture;
        }

        private bool TryCreateTexture(CIString name, Namespace resourceNamespace,
            [NotNullWhen(true)] out IWorldTexture? worldTexture)
        {
            // TODO

            worldTexture = null;
            return false;
        }

        private IWorldTexture ProcessTextureIfTickable(IWorldTexture texture)
        {
            // Certain types cannot return a shared instance. For example, a
            // switch texture contains state specific to the wall it is on.
            switch (texture)
            {
            case SwitchWorldTexture switchTexture:
                SwitchWorldTexture switchCopy = new(switchTexture);
                m_tickableTextures.Add(switchCopy);
                return switchCopy;
            default:
                return texture;
            }
        }

        public void Tick()
        {
            m_tickableTextures.ForEach(tickableTexture => tickableTexture.Tick());
        }
    }
}
