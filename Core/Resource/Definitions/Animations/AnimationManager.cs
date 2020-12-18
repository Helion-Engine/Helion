using Helion.Resource.Archives;
using Helion.Resource.Definitions.Animations.Switches;
using Helion.Resource.Definitions.Animations.Textures;
using Helion.Resource.Tracker;
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

        public IAnimatedTexture? Get(CIString name, Namespace priorityNamespace)
        {
            return m_animatedTextures.Get(name, priorityNamespace);
        }

        public IAnimatedTexture? GetOnly(CIString name, Namespace resourceNamespace)
        {
            return m_animatedTextures.GetOnly(name, resourceNamespace);
        }

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

            foreach (AnimatedSwitch animatedSwitch in parser.AnimatedSwitches)
            {
                if (animatedSwitch.Components.Count == 2)
                    m_animatedTextures.Insert(animatedSwitch.Name, animatedSwitch.Namespace, animatedSwitch);
                else
                    Log.Warn("Animated switch '{0}' only supports 2 frames, instead it has {1}", animatedSwitch.Name, animatedSwitch.Components.Count);
            }
        }
    }
}