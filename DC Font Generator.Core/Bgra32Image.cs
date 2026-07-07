using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace DC_Font_Generator
{
    public sealed class Bgra32Image
    {
        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        private static extern void CopyMemory(IntPtr dest, IntPtr src, uint length);

        public Bgra32Image(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            Width = width;
            Height = height;
            Stride = width * 4;
            Pixels = new byte[Stride * height];
        }

        public Bgra32Image(int width, int height, byte[] pixels)
            : this(width, height)
        {
            if (pixels == null) throw new ArgumentNullException(nameof(pixels));
            if (pixels.Length < Pixels.Length)
            {
                throw new ArgumentException("Pixel buffer is too small.", nameof(pixels));
            }

            Buffer.BlockCopy(pixels, 0, Pixels, 0, Pixels.Length);
        }

        public int Width { get; }
        public int Height { get; }
        public int Stride { get; }
        public byte[] Pixels { get; }
        public Size Size => new Size(Width, Height);

        public static Bgra32Image FromBitmap(Bitmap bitmap)
        {
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));

            Bgra32Image image = new Bgra32Image(bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                CopyFromLocked(data, image.Pixels, image.Width, image.Height, image.Stride);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }

            return image;
        }

        public static Bgra32Image FromBgra(byte[] pixels, int sourceWidth, Rectangle sourceRect)
        {
            if (pixels == null) throw new ArgumentNullException(nameof(pixels));
            if (sourceWidth <= 0) throw new ArgumentOutOfRangeException(nameof(sourceWidth));
            if (sourceRect.Width <= 0 || sourceRect.Height <= 0)
            {
                return new Bgra32Image(1, 1);
            }

            Bgra32Image image = new Bgra32Image(sourceRect.Width, sourceRect.Height);
            int sourceStride = sourceWidth * 4;
            int copyBytes = sourceRect.Width * 4;
            for (int y = 0; y < sourceRect.Height; y++)
            {
                int sourceOffset = ((sourceRect.Y + y) * sourceStride) + (sourceRect.X * 4);
                Buffer.BlockCopy(pixels, sourceOffset, image.Pixels, y * image.Stride, copyBytes);
            }

            return image;
        }

        public Bitmap ToBitmap()
        {
            Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            CopyToBitmap(bitmap);
            return bitmap;
        }

        public Bgra32Image Clone()
        {
            return new Bgra32Image(Width, Height, Pixels);
        }

        public Bgra32Image Crop(Rectangle sourceRect)
        {
            Rectangle bounds = Rectangle.Intersect(sourceRect, new Rectangle(0, 0, Width, Height));
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return new Bgra32Image(1, 1);
            }

            return FromBgra(Pixels, Width, bounds);
        }

        public void CopyTo(Bgra32Image target, int targetX, int targetY)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (targetX >= target.Width || targetY >= target.Height) return;

            int copyWidth = Math.Min(Width, target.Width - targetX);
            int copyHeight = Math.Min(Height, target.Height - targetY);
            if (copyWidth <= 0 || copyHeight <= 0) return;

            int copyBytes = copyWidth * 4;
            for (int y = 0; y < copyHeight; y++)
            {
                int sourceOffset = y * Stride;
                int targetOffset = ((targetY + y) * target.Stride) + (targetX * 4);
                Buffer.BlockCopy(Pixels, sourceOffset, target.Pixels, targetOffset, copyBytes);
            }
        }

        public void CopyToBitmap(Bitmap bitmap)
        {
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));
            if (bitmap.Width != Width || bitmap.Height != Height)
            {
                throw new ArgumentException("Target bitmap dimensions must match image dimensions.", nameof(bitmap));
            }

            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                CopyToLocked(data);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        public void CopyToLocked(BitmapData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            int copyBytes = Width * 4;
            GCHandle handle = GCHandle.Alloc(Pixels, GCHandleType.Pinned);
            try
            {
                IntPtr source = handle.AddrOfPinnedObject();
                for (int y = 0; y < Height; y++)
                {
                    IntPtr sourceRow = IntPtr.Add(source, y * Stride);
                    IntPtr targetRow = IntPtr.Add(data.Scan0, y * data.Stride);
                    CopyMemory(targetRow, sourceRow, (uint)copyBytes);
                }
            }
            finally
            {
                handle.Free();
            }
        }

        public void Clear(Color color)
        {
            byte b = color.B;
            byte g = color.G;
            byte r = color.R;
            byte a = color.A;
            for (int y = 0; y < Height; y++)
            {
                int row = y * Stride;
                for (int x = 0; x < Width; x++)
                {
                    int offset = row + (x * 4);
                    Pixels[offset] = b;
                    Pixels[offset + 1] = g;
                    Pixels[offset + 2] = r;
                    Pixels[offset + 3] = a;
                }
            }
        }

        public bool HasNonZeroPixel(Rectangle rect)
        {
            Rectangle bounds = Rectangle.Intersect(rect, new Rectangle(0, 0, Width, Height));
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return false;
            }

            int maxY = bounds.Y + bounds.Height;
            int maxX = bounds.X + bounds.Width;
            for (int y = bounds.Y; y < maxY; y++)
            {
                int row = y * Stride;
                for (int x = bounds.X; x < maxX; x++)
                {
                    int offset = row + (x * 4);
                    if (Pixels[offset] != 0
                        || Pixels[offset + 1] != 0
                        || Pixels[offset + 2] != 0
                        || Pixels[offset + 3] != 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void CopyFromLocked(BitmapData data, byte[] target, int width, int height, int targetStride)
        {
            int copyBytes = width * 4;
            GCHandle handle = GCHandle.Alloc(target, GCHandleType.Pinned);
            try
            {
                IntPtr targetBase = handle.AddrOfPinnedObject();
                for (int y = 0; y < height; y++)
                {
                    IntPtr sourceRow = IntPtr.Add(data.Scan0, y * data.Stride);
                    IntPtr targetRow = IntPtr.Add(targetBase, y * targetStride);
                    CopyMemory(targetRow, sourceRow, (uint)copyBytes);
                }
            }
            finally
            {
                handle.Free();
            }
        }
    }
}
