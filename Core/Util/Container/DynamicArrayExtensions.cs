using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helion.Util.Container;

public static class DynamicArrayExtensions
{
    public static void FlushReferences<T>(this DynamicArray<T> array) where T : class
    {
        for (int i = 0; i < array.Capacity; i++)
            array[i] = null;
    }
}
