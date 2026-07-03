using SkiaSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace DC_Font_Generator
{
    internal static class SkiaBitmapInterop
    {
        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        private static extern void CopyMemory(IntPtr dest, IntPtr src, uint length);

        public static SKColor ToSKColor(Color color)
        {
            return new SKColor(color.R, color.G, color.B, color.A);
        }

        public static byte[] ReadSurfacePixels(SKSurface surface, int width, int height)
        {
            byte[] pixels = new byte[width * height * 4];
            GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            try
            {
                SKImageInfo info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
                if (!surface.ReadPixels(info, handle.AddrOfPinnedObject(), width * 4, 0, 0))
                {
                    throw new InvalidOperationException("Unable to read Skia surface pixels.");
                }
            }
            finally
            {
                handle.Free();
            }

            return pixels;
        }

        public static Rectangle FindContentBounds(byte[] pixels, int width, int height, Color background)
        {
            int backgroundArgb = background.ToArgb();
            int top = int.MaxValue;
            int left = int.MaxValue;
            int bottom = int.MinValue;
            int right = int.MinValue;
            bool found = false;

            for (int y = 0; y < height; y++)
            {
                int row = y * width * 4;
                for (int x = 0; x < width; x++)
                {
                    int offset = row + (x * 4);
                    int argb = pixels[offset]
                        | (pixels[offset + 1] << 8)
                        | (pixels[offset + 2] << 16)
                        | (pixels[offset + 3] << 24);
                    if (argb == backgroundArgb)
                    {
                        continue;
                    }

                    found = true;
                    if (y < top) top = y;
                    if (y > bottom) bottom = y;
                    if (x < left) left = x;
                    if (x > right) right = x;
                }
            }

            if (!found)
            {
                return Rectangle.Empty;
            }

            return new Rectangle(left, top, right - left + 1, bottom - top + 1);
        }

        public static Bitmap CreateBitmapFromBgra(byte[] pixels, int sourceWidth, Rectangle sourceRect)
        {
            if (sourceRect.Width <= 0 || sourceRect.Height <= 0)
            {
                return new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            }

            Bitmap bitmap = new Bitmap(sourceRect.Width, sourceRect.Height, PixelFormat.Format32bppArgb);
            CopyBgraToBitmap(pixels, sourceWidth, sourceRect, bitmap);
            return bitmap;
        }

        public static void CopySurfaceToBitmap(SKSurface surface, Bitmap target)
        {
            byte[] pixels = ReadSurfacePixels(surface, target.Width, target.Height);
            CopyBgraToBitmap(pixels, target.Width, new Rectangle(0, 0, target.Width, target.Height), target);
        }

        public static void CopyBitmapRegion(Bitmap source, Bitmap target, Rectangle sourceRect)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            Rectangle bounds = Rectangle.Intersect(
                sourceRect,
                new Rectangle(0, 0, Math.Min(source.Width, target.Width), Math.Min(source.Height, target.Height)));
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            BitmapData sourceData = source.LockBits(
                bounds,
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                BitmapData targetData = target.LockBits(
                    bounds,
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppArgb);
                try
                {
                    int copyBytes = bounds.Width * 4;
                    for (int y = 0; y < bounds.Height; y++)
                    {
                        IntPtr sourceRow = IntPtr.Add(sourceData.Scan0, y * sourceData.Stride);
                        IntPtr targetRow = IntPtr.Add(targetData.Scan0, y * targetData.Stride);
                        CopyMemory(targetRow, sourceRow, (uint)copyBytes);
                    }
                }
                finally
                {
                    target.UnlockBits(targetData);
                }
            }
            finally
            {
                source.UnlockBits(sourceData);
            }
        }

        public static void DrawToBitmap(Bitmap target, Action<SKCanvas> draw)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (draw == null) throw new ArgumentNullException(nameof(draw));

            BitmapData data = target.LockBits(
                new Rectangle(0, 0, target.Width, target.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);
            try
            {
                SKImageInfo imageInfo = new SKImageInfo(target.Width, target.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
                using (SKSurface surface = SKSurface.Create(imageInfo, data.Scan0, data.Stride))
                {
                    draw(surface.Canvas);
                    surface.Canvas.Flush();
                }
            }
            finally
            {
                target.UnlockBits(data);
            }
        }

        public static void CopyBgraToBitmap(byte[] pixels, int sourceWidth, Rectangle sourceRect, Bitmap target)
        {
            if (target.Width != sourceRect.Width || target.Height != sourceRect.Height)
            {
                throw new ArgumentException("Target bitmap dimensions must match source rectangle.");
            }

            BitmapData data = target.LockBits(
                new Rectangle(0, 0, target.Width, target.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                int sourceStride = sourceWidth * 4;
                int copyBytes = sourceRect.Width * 4;
                for (int y = 0; y < sourceRect.Height; y++)
                {
                    int sourceOffset = ((sourceRect.Y + y) * sourceStride) + (sourceRect.X * 4);
                    IntPtr destination = IntPtr.Add(data.Scan0, y * data.Stride);
                    Marshal.Copy(pixels, sourceOffset, destination, copyBytes);
                }
            }
            finally
            {
                target.UnlockBits(data);
            }
        }

        public static Bitmap CreateBitmapFromSurface(SKSurface surface, int width, int height)
        {
            byte[] pixels = ReadSurfacePixels(surface, width, height);
            return CreateBitmapFromBgra(pixels, width, new Rectangle(0, 0, width, height));
        }

        public static SKBitmap CreateSKBitmap(Bitmap bitmap)
        {
            SKBitmap skBitmap = new SKBitmap(new SKImageInfo(
                bitmap.Width,
                bitmap.Height,
                SKColorType.Bgra8888,
                SKAlphaType.Unpremul));

            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                int copyBytes = bitmap.Width * 4;
                IntPtr destinationPixels = skBitmap.GetPixels();
                for (int y = 0; y < bitmap.Height; y++)
                {
                    IntPtr sourceRow = IntPtr.Add(data.Scan0, y * data.Stride);
                    IntPtr destinationRow = IntPtr.Add(destinationPixels, y * skBitmap.RowBytes);
                    CopyMemory(destinationRow, sourceRow, (uint)copyBytes);
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }

            return skBitmap;
        }
    }
}
