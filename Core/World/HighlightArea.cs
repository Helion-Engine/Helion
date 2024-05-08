using Helion.Geometry.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helion.World;

public readonly record struct HighlightArea(Vec3D Position, double Area);
