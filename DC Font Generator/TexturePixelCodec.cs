using System;
using System.Buffers;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace DC_Font_Generator
{
    internal static unsafe class TexturePixelCodec
    {
        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        private static extern void CopyMemory(IntPtr dest, IntPtr src, uint length);

        public static void Clear(BitmapData data, int width, int height, Color color)
        {
            byte b = color.B;
            byte g = color.G;
            byte r = color.R;
            byte a = color.A;
            for (int y = 0; y < height; y++)
            {
                byte* row = (byte*)data.Scan0 + (y * data.Stride);
                for (int x = 0; x < width; x++)
                {
                    int offset = x * 4;
                    row[offset] = b;
                    row[offset + 1] = g;
                    row[offset + 2] = r;
                    row[offset + 3] = a;
                }
            }
        }

        public static void CopyBitmapToLocked(Bitmap source, BitmapData targetData, int targetX, int targetY)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source.Width <= 0 || source.Height <= 0) return;

            BitmapData sourceData = source.LockBits(
                new Rectangle(0, 0, source.Width, source.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                int copyBytes = source.Width * 4;
                for (int y = 0; y < source.Height; y++)
                {
                    IntPtr sourceRow = IntPtr.Add(sourceData.Scan0, y * sourceData.Stride);
                    IntPtr targetRow = IntPtr.Add(targetData.Scan0, ((targetY + y) * targetData.Stride) + (targetX * 4));
                    CopyMemory(targetRow, sourceRow, (uint)copyBytes);
                }
            }
            finally
            {
                source.UnlockBits(sourceData);
            }
        }

        public static bool HasNonZeroPixel(BitmapData data, Rectangle rect)
        {
            int maxY = rect.Y + rect.Height;
            int maxX = rect.X + rect.Width;
            for (int y = rect.Y; y < maxY; y++)
            {
                byte* row = (byte*)data.Scan0 + (y * data.Stride);
                for (int x = rect.X; x < maxX; x++)
                {
                    int offset = x * 4;
                    if (row[offset] != 0
                        || row[offset + 1] != 0
                        || row[offset + 2] != 0
                        || row[offset + 3] != 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void SaveTexPixels(Stream output, BitmapData data, int width, int height, IProgress<FontProgress> progress)
        {
            int rowBytes = width * 4;
            byte[] rented = ArrayPool<byte>.Shared.Rent(rowBytes);
            try
            {
                for (int y = 0; y < height; y++)
                {
                    byte* sourceRow = (byte*)data.Scan0 + (y * data.Stride);
                    for (int x = 0; x < width; x++)
                    {
                        int offset = x * 4;
                        rented[offset] = sourceRow[offset + 2];
                        rented[offset + 1] = sourceRow[offset + 1];
                        rented[offset + 2] = sourceRow[offset];
                        rented[offset + 3] = sourceRow[offset + 3];
                    }

                    output.Write(rented.AsSpan(0, rowBytes));
                    if ((y & 0x0F) == 0 || y == height - 1)
                    {
                        progress?.Report(new FontProgress("SavingTex", y + 1, height));
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        public static void LoadTexPixels(ReadOnlySpan<byte> pixelData, BitmapData data, int width, int height)
        {
            int rowBytes = width * 4;
            for (int y = 0; y < height; y++)
            {
                ReadOnlySpan<byte> source = pixelData.Slice(y * rowBytes, rowBytes);
                byte* targetRow = (byte*)data.Scan0 + (y * data.Stride);
                fixed (byte* sourcePtr = source)
                {
                    Buffer.MemoryCopy(sourcePtr, targetRow, rowBytes, rowBytes);
                }
            }
        }
    }
}
