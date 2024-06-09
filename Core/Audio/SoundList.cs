using Helion.Util;
using System.Collections.Generic;

namespace Helion.Audio;

public class SoundList
{
    public IAudioSource? Head;
    public int Count;

    public void Add(IAudioSource audio)
    {
        Count++;
        if (Head == null)
        {
            Head = audio;
            return;
        }

        audio.Next = Head;
        Head.Previous = audio;
        Head = audio;
    }

    public void Remove(IAudioSource node) => RemoveAndFree(node, null);

    public void RemoveAndFree(IAudioSource node, DataCache? dataCache)
    {
        Count--;
        if (node == Head)
        {
            Head = node.Next;
            if (Head != null)
                Head.Previous = null;
            node.Next = null;
            node.Previous = null;
            dataCache?.FreeAudioSource(node);
            return;
        }

        if (node.Next != null)
            node.Next.Previous = node.Previous;
        if (node.Previous != null)
            node.Previous.Next = node.Next;

        node.Next = null;
        node.Previous = null;
        dataCache?.FreeAudioSource(node);
    }

    public LinkedList<IAudioSource> ToLinkedList()
    {
        var list = new LinkedList<IAudioSource>();
        var node = Head;
        while (node != null)
        {
            list.AddFirst(node);
            node = node.Next;
        }
        return list;
    }
}
