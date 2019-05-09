using System.Collections.Generic;
using static Helion.Util.Assert;

namespace Helion.Util.Geometry
{
    public class QuantizedGrid<T>
    {
        private readonly int quantizationMultiplier;
        private readonly Dictionary<int, Dictionary<int, T>> grid = new Dictionary<int, Dictionary<int, T>>();

        public QuantizedGrid(double epsilon)
        {
            Precondition(epsilon > 0, "Cannot quantize to an epsilon that is not positive");

            quantizationMultiplier = (int)(1.0 / epsilon);
        }

        private int Quantize(double value) => (int)(value * quantizationMultiplier);

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

        public bool TryGetValue(double x, double y, out T value)
        {
            if (grid.TryGetValue(Quantize(x), out Dictionary<int, T> yValues))
                if (yValues.TryGetValue(Quantize(y), out value))
                    return true;

            value = default;
            return false;
        }
    }
}
