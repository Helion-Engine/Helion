using System.Collections.Generic;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Geometry
{
    public class QuantizedGrid<T>
    {
        private readonly double quantizationMultiplier;
        private readonly Dictionary<int, Dictionary<int, T>> grid = new Dictionary<int, Dictionary<int, T>>();

        public QuantizedGrid(double epsilonRadius)
        {
            Precondition(epsilonRadius > 0, "Cannot quantize to an epsilon that is not positive");

            quantizationMultiplier = 1.0 / epsilonRadius;
        }

        /// <summary>
        /// Checks if there exists some value in range of the coordinates
        /// provided.
        /// </summary>
        /// <param name="x">The X value region to check.</param>
        /// <param name="y">The Y value region to check.</param>
        /// <returns>True if an element is close to the provided point based on
        /// the epsilon provided when constructing the object, false if not.
        /// </returns>
        public bool Contains(double x, double y)
        {
            int xMiddle = Quantize(x);
            int yMiddle = Quantize(y);
            int[] xComponents = new int[] { xMiddle, xMiddle - 1, xMiddle + 1 };
            int[] yComponents = new int[] { yMiddle, yMiddle - 1, yMiddle + 1 };

            foreach (int xQuantized in xComponents)
                if (grid.TryGetValue(xQuantized, out Dictionary<int, T>? yValues))
                    foreach (int yQuantized in yComponents)
                        if (yValues.ContainsKey(yQuantized))
                            return true;

            return false;
        }

        /// <summary>
        /// Either gets an existing value at the coordinates provided (within
        /// the epsilon range) or creates a new entry with the value provided.
        /// </summary>
        /// <remarks>
        /// If a value already exists, then newValue is not used. This is an
        /// unfortunate side effect of supporting struct types. A potential
        /// way around this in the future could be to provide a generator
        /// function that would be invoked if it can't be found.
        /// </remarks>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <param name="newValue">The value to add if and only if the provided
        /// coordinates do not have a value already near it.</param>
        /// <returns>Either the value that already existed, or if it did not
        /// then the value provided for the `newValue` argument.</returns>
        public T GetExistingOrAdd(double x, double y, T newValue)
        {
            int xMiddle = Quantize(x);
            int yMiddle = Quantize(y);
            int[] xComponents = new int[] { xMiddle, xMiddle - 1, xMiddle + 1 };
            int[] yComponents = new int[] { yMiddle, yMiddle - 1, yMiddle + 1 };

            foreach (int xQuantized in xComponents)
                if (grid.TryGetValue(xQuantized, out Dictionary<int, T>? yValues))
                    foreach (int yQuantized in yComponents)
                        if (yValues.TryGetValue(yQuantized, out var element))
                            return element;

            if (grid.TryGetValue(xMiddle, out Dictionary<int, T>? existingYValues))
                existingYValues[yMiddle] = newValue;
            else
                grid[xMiddle] = new Dictionary<int, T>() { [yMiddle] = newValue };

            return newValue;
        }

        /// <summary>
        /// Tries to get the value that is stored in the epsilon region of the
        /// (x, y) coordinate.
        /// </summary>
        /// <param name="x">The X value region to check.</param>
        /// <param name="y">The Y value region to check.</param>
        /// <param name="value">The value reference if it was found.</param>
        /// <returns>True if an element is close to the provided point based on
        /// the epsilon provided when constructing the object, false if not. A
        /// value of false means `value` should not be used.
        /// </returns>
        public bool TryGetValue(double x, double y, ref T value)
        {
            int xMiddle = Quantize(x);
            int yMiddle = Quantize(y);
            int[] xComponents = new int[] { xMiddle, xMiddle - 1, xMiddle + 1 };
            int[] yComponents = new int[] { yMiddle, yMiddle - 1, yMiddle + 1 };

            foreach (int xQuantized in xComponents)
            {
                if (grid.TryGetValue(xQuantized, out Dictionary<int, T>? yValues))
                {
                    foreach (int yQuantized in yComponents)
                    {
                        if (yValues.TryGetValue(yQuantized, out T val))
                        {
                            value = val;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Quantizes a double to an integral grid.
        /// </summary>
        /// <param name="value">The value to quantize.</param>
        /// <returns>The quantized value.</returns>
        private int Quantize(double value) => (int)(value * quantizationMultiplier);
    }
}
