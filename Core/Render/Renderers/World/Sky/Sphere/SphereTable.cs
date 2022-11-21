using System;
using System.Collections.Generic;
using Helion;
using Helion.Render;
using Helion.Render.Legacy;
using Helion.Render.Renderers.World.Sky.Sphere;
using Helion.Util;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Renderers.World.Sky.Sphere;

/// <summary>
/// A collection of all the spherical coordinates which map onto a mercator
/// projection that can be used in texturing a sphere and finding point
/// locations.
/// </summary>
public class SphereTable
{
    /// <summary>
    /// A row-major set of 3D vertices mapped onto a 2D grid. Access should
    /// be done like [row, col].
    /// </summary>
    /// <remarks>
    /// <para>This will be 1 element larger than the constructor values on
    /// each dimension because it will simplify the iteration over the
    /// endpoints easier without having to worry about an end case. This
    /// means that anything in the column [0, X] is equal to the column
    /// [Len - 1, X].</para>
    /// <para>The UV coordinate system has [0.0, 0.0] being the bottom
    /// right and [1.0, 1.0] being the top left.</para>
    /// <para>The rectangle grows from the bottom upwards as if it was
    /// cartesian coordinate based.</para>
    /// <para>Finally, this uses OpenGL's Y-axis-is-up system, not the
    /// coordinate system everything else uses where Z is up.</para>
    /// </remarks>
    public readonly SkySphereVertex[,] MercatorRectangle;

    /// <summary>
    /// Creates a new table from the number of points (+ 1) provided. See
    /// <see cref="MercatorRectangle"/> remarks for more details.
    /// </summary>
    /// <param name="horizontalPoints">The number of points along a sphere
    /// on the horizontal plane.</param>
    /// <param name="verticalPoints">The number of points along a sphere on
    /// the vertical plane.</param>
    public SphereTable(int horizontalPoints, int verticalPoints)
    {
        Precondition(horizontalPoints >= 2, "Insufficient amount of horizontal points for sphere angle table");
        Precondition(verticalPoints >= 2, "Insufficient amount of vertical points for sphere angle table");

        List<float> sinYaw = new List<float>();
        List<float> cosYaw = new List<float>();
        List<float> sinPitch = new List<float>();
        List<float> cosPitch = new List<float>();

        float inverseHorizontal = (float)MathHelper.TwoPi / horizontalPoints;
        for (int i = 0; i <= horizontalPoints; i++)
        {
            sinYaw.Add((float)Math.Sin(i * inverseHorizontal));
            cosYaw.Add((float)Math.Cos(i * inverseHorizontal));
        }

        // The vertical axis goes from [0, pi] as per spherical coordinates
        // require, but due to how we write coordinates, it should start at
        // the bottom. Since iteration begins at 0 and goes to Pi, then the
        // system becomes cos(0) -> cos(pi) which is 1 -> -1... opposite
        // direction of what we want. We'll just do Pi - angle instead to
        // flip this.
        float inverseVertical = (float)MathHelper.Pi / verticalPoints;
        for (int i = 0; i <= verticalPoints; i++)
        {
            sinPitch.Add((float)Math.Sin(MathHelper.Pi - i * inverseVertical));
            cosPitch.Add((float)Math.Cos(MathHelper.Pi - i * inverseVertical));
        }

        MercatorRectangle = new SkySphereVertex[verticalPoints + 1, horizontalPoints + 1];

        float verticalPointsInverse = 1.0f / verticalPoints;
        float horizontalPointsInverse = 1.0f / horizontalPoints;
        for (int row = 0; row <= verticalPoints; row++)
        {
            for (int col = 0; col <= horizontalPoints; col++)
            {
                float x = cosYaw[col] * sinPitch[row];
                float y = cosPitch[row];
                float z = sinYaw[col] * sinPitch[row];

                // We do `1 - v` below because we upload the image upside
                // down (due to OpenGL's image system being cartesian).
                float u = col * horizontalPointsInverse;
                float v = 1.0f - row * verticalPointsInverse;

                MercatorRectangle[row, col] = new SkySphereVertex(x, y, z, u, v);
            }
        }
    }
}
