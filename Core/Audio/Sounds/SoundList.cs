using Helion.Util;
using System.Collections.Generic;

namespace Helion.Audio.Sounds;

public class SoundList : LinkedList<IAudioSource>
{
    public void Free(LinkedListNode<IAudioSource> node, DataCache dataCache)
    {
        dataCache.FreeAudioSource(node.Value);
        Remove(node);
        dataCache.FreeAudioNode(node);
    }
}
