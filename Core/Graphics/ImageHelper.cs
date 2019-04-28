using System.Drawing;

namespace Helion.Graphics
{
    public static class ImageHelper
    {
        /// <summary>
        /// Creates a new checked 8x8 black/white null image.
        /// </summary>
        /// <returns>A null image.</returns>
        public static Image CreateNullImage()
        {
            int dimension = 8;
            int halfDimension = dimension / 2;
            Image image = new Image(dimension, dimension, Color.Black);

            for (int y = 0; y < halfDimension; y++)
                for (int x = 0; x < halfDimension; x++)
                    image.Bitmap.SetPixel(x, y, Color.Red);

            for (int y = halfDimension; y < dimension; y++)
                for (int x = halfDimension; x < dimension; x++)
                    image.Bitmap.SetPixel(x, y, Color.Red);

            return image;
        }
    }
}
