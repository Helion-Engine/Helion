using Helion.Resource.Archives;
using Helion.Resource.Definitions.Animations.Switches;
using Helion.Resource.Definitions.Animations.Textures;
using Helion.Util;
using NLog;

namespace Helion.Resource.Definitions.Animations
{
    /// <summary>
    /// Manages all of the animated textures that are parsed.
    /// </summary>
    public class AnimationManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly NamespaceTracker<IAnimatedTexture> m_animatedTextures = new();

        /// <summary>
        /// Gets any animated texture, with focus on the provided namespace.
        /// </summary>
        /// <param name="name">The texture name.</param>
        /// <param name="priorityNamespace">The namespace to search first.
        /// </param>
        /// <returns>The animated texture, or null if none exist.</returns>
        public IAnimatedTexture? Get(CIString name, Namespace priorityNamespace)
        {
            return m_animatedTextures.Get(name, priorityNamespace);
        }

        /// <summary>
        /// Gets any animated texture, only looking in the namespace provided.
        /// </summary>
        /// <param name="name">The texture name.</param>
        /// <param name="resourceNamespace">The namespace to search.</param>
        /// <returns>The animated texture, or null if none exist.</returns>
        public IAnimatedTexture? GetOnly(CIString name, Namespace resourceNamespace)
        {
            return m_animatedTextures.GetOnly(name, resourceNamespace);
        }

        /// <summary>
        /// Adds an entry to be processed.
        /// </summary>
        /// <param name="entry">The entry to process.</param>
        public void AddDefinitions(Entry entry)
        {
            AnimdefsParser parser = new();
            if (!parser.Parse(entry))
            {
                Log.Error("Unable to parse animdefs file, animations will be missing");
                return;
            }

            foreach (AnimatedTexture animatedTexture in parser.AnimatedTextures)
                m_animatedTextures.Insert(animatedTexture.Name, animatedTexture.Namespace, animatedTexture);
            foreach (AnimatedWarpTexture animatedWarp in parser.WarpTextures)
                m_animatedTextures.Insert(animatedWarp.Name, animatedWarp.Namespace, animatedWarp);
            foreach (AnimatedCameraTexture animatedCamera in parser.CameraTextures)
                m_animatedTextures.Insert(animatedCamera.Name, animatedCamera.Namespace, animatedCamera);
        }
    }
}