using System;
using System.Collections.Generic;
using static Helion.Util.Assert;

namespace Helion.Util.Geometry
{
    public class QuantizedGrid<T>
    {
        private readonly double epsilon;
        private readonly int quantizationMultiplier;
        private readonly Dictionary<int, Dictionary<int, T>> grid = new Dictionary<int, Dictionary<int, T>>();

        public QuantizedGrid(double epsilonRadius)
        {
            Precondition(epsilonRadius > 0, "Cannot quantize to an epsilon that is not positive");

            epsilon = epsilonRadius;
            quantizationMultiplier = (int)(2.0 / epsilonRadius);
        }

        /// <summary>
        /// Quantizes a double to an integral grid.
        /// </summary>
        /// <param name="value">The value to quantize.</param>
        /// <returns>The quantized value.</returns>
        private int Quantize(double value) => (int)Math.Floor((value + epsilon) * quantizationMultiplier);

        public bool Contains(double x, double y)
        {
            if (grid.TryGetValue(Quantize(x), out Dictionary<int, T> yValues))
                return yValues.ContainsKey(Quantize(y));
            return false;
        }

        public T GetExistingOrAdd(double x, double y, T newValue)
        {
            int xQuantized = Quantize(x);
            int yQuantized = Quantize(y);

            if (grid.TryGetValue(xQuantized, out Dictionary<int, T> yValues))
            {
                if (yValues.TryGetValue(yQuantized, out T element))
                {
                    return element;
                }
                else
                {
                    yValues[yQuantized] = newValue;
                    return newValue;
                }
            }
            else
            {
                Dictionary<int, T> newYValues = new Dictionary<int, T>();
                grid[xQuantized] = newYValues;

                newYValues[yQuantized] = newValue;
                return newValue;
            }
        }

        public bool TryGetValue(double x, double y, ref T value)
        {
            if (grid.TryGetValue(Quantize(x), out Dictionary<int, T> yValues))
                if (yValues.TryGetValue(Quantize(y), out value))
                    return true;
            return false;
        }
    }
}
