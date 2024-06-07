using Helion.Audio.Sounds;
using Helion.Util;
using System.Collections.Generic;

namespace Helion.Audio;

public class WaitingSoundList : LinkedList<WaitingSound>
{
    public void Free(LinkedListNode<WaitingSound> node, DataCache dataCache)
    {
        Remove(node);
        dataCache.FreeWaitingSoundNode(node);
    }
}
