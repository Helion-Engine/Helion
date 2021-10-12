using Helion.Bsp.Node;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helion.Bsp;

public interface IBspBuilder
{
    BspNode? Build();
}
