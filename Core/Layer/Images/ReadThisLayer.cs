using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Helion.Audio.Sounds;
using Helion.Resources.Archives.Collection;
using Helion.Util.Extensions;

namespace Helion.Layer.Images
{
    public class ReadThisLayer : CycleImageLayer
    {
        private ReadThisLayer(IGameLayerParent parent, SoundManager soundManager, IList<string> images) : 
            base(parent, soundManager, images)
        {
        }

        public static bool TryCreate(IGameLayerParent parent, SoundManager soundManager,
            ArchiveCollection archiveCollection, [NotNullWhen(true)] out ReadThisLayer? layer)
        {
            IList<string> infoPages = archiveCollection.Definitions.MapInfoDefinition.GameDefinition.InfoPages;
            if (infoPages.Empty())
            {
                layer = null;
                return false;
            }

            layer = new ReadThisLayer(parent, soundManager, infoPages);
            return true;
        }
    }
}
