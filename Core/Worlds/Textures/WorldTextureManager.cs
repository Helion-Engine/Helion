using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Helion.Resource;
using Helion.Resource.Definitions.Animations.Switches;
using Helion.Resource.Definitions.Animations.Textures;
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
        private readonly Resources m_resources;
        private readonly TextureManager m_textureManager;
        private readonly NamespaceTracker<IWorldTexture> m_textures = new();
        private readonly HashSet<ITickable> m_tickableTextures = new();

        public WorldTextureManager(Resources resources)
        {
            m_resources = resources;
            m_textureManager = resources.Textures;
            MissingTexture = new("", m_textureManager.MissingTexture, true);
            SkyTexture = new("", m_textureManager.MissingTexture, isSky: true);
        }

        /// <summary>
        /// Gets a texture. Returns the texture for the name and namespace.
        /// </summary>
        /// <remarks>
        /// Also handles creating tickable textures and registers them.
        /// </remarks>
        /// <param name="name">The texture name.</param>
        /// <param name="resourceNamespace">The priority namespace.</param>
        /// <returns>The texture for the name/namespace combination.</returns>
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
            IAnimatedTexture? animatedDefinition = m_resources.Animations.Get(name, resourceNamespace);
            if (animatedDefinition != null)
            {
                IWorldTexture animatedTexture = CreateFromAnimated(animatedDefinition, resourceNamespace);
                TrackNewTexture(name, resourceNamespace, animatedTexture);
                worldTexture = animatedTexture;
                return true;
            }

            Texture texture = m_resources.Textures.GetOnly(name, resourceNamespace);
            if (texture.IsMissing)
            {
                worldTexture = null;
                return false;
            }

            worldTexture = new StaticWorldTexture(name, texture);
            TrackNewTexture(name, resourceNamespace, worldTexture);
            return true;
        }

        private IWorldTexture CreateFromAnimated(IAnimatedTexture animatedDefinition, Namespace resourceNamespace)
        {
            return animatedDefinition switch
            {
                AnimatedTexture animatedTexture => CreateFromAnimatedTexture(animatedTexture),
                AnimatedSwitch switchTexture => CreateFromAnimatedSwitch(switchTexture, resourceNamespace),
                _ => throw new Exception($"Texture {animatedDefinition.Name} (definition type {animatedDefinition.GetType().FullName}) not supported")
            };
        }

        private IWorldTexture CreateFromAnimatedTexture(AnimatedTexture animatedTexture)
        {
            List<AnimatedTextureFrame> frames = new();

            foreach (AnimatedTextureComponent component in animatedTexture.Components)
            {
                // TODO: We don't support randomization right now.
                int duration = component.MaxTicks;
                Texture texture = m_textureManager.Get(component.Texture, animatedTexture.Namespace);

                AnimatedTextureFrame frame = new(texture, duration);
                frames.Add(frame);
            }

            return new AnimatedWorldTexture(animatedTexture.Name, frames);
        }

        private IWorldTexture CreateFromAnimatedSwitch(AnimatedSwitch switchTexture, Namespace resourceNamespace)
        {
            // TODO: Will fix shortly (watch me forget...)
            int duration = Constants.TicksPerSecond * 2;

            // TODO: Only support basic 2-frame switches right now.
            Texture inactive = m_textureManager.Get(switchTexture.Components[0].Texture, resourceNamespace);
            Texture active = m_textureManager.Get(switchTexture.Components[1].Texture, resourceNamespace);

            return new SwitchWorldTexture(switchTexture.Name, inactive, active, duration);
        }

        private void TrackNewTexture(CIString name, Namespace resourceNamespace, IWorldTexture animatedTexture)
        {
            m_textures.Insert(name, resourceNamespace, animatedTexture);
        }

        private IWorldTexture ProcessTextureIfTickable(IWorldTexture texture)
        {
            // Certain types cannot return a shared instance. For example, a
            // switch texture contains state that is specific to the wall it
            // is on. Everything else though does not have state, so we can
            // return the (what effectively is a) singleton to the caller.
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
