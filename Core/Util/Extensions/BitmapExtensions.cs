using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Helion.Util.Extensions;

public static class BitmapExtensions
{
    /// <summary>
    /// Allows for guaranteed locking and unlocking without needing to
    /// worry about doing it. This assumes it is unlocked, and that the
    /// user wants read and write.
    /// </summary>
    /// <param name="bitmap">The bitmap.</param>
    /// <param name="func">The action to take when locked.</param>
    /// <param name="lockMode">The locking mode (default read/write).</param>
    public static void WithLockedBits(this Bitmap bitmap, Action<IntPtr> func, ImageLockMode lockMode = ImageLockMode.ReadWrite)
    {
        Rectangle rect = new(0, 0, bitmap.Width, bitmap.Height);
        BitmapData metadata = bitmap.LockBits(rect, lockMode, bitmap.PixelFormat);
        func(metadata.Scan0);
        bitmap.UnlockBits(metadata);
    }
}
