using System;
using System.Buffers;
using System.Drawing;
using System.IO;

namespace DC_Font_Generator
{
    internal static class TexturePixelCodec
    {
        public static void Clear(Bgra32Image image, Color color)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            image.Clear(color);
        }

        public static void CopyImageToImage(Bgra32Image source, Bgra32Image target, int targetX, int targetY)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            source.CopyTo(target, targetX, targetY);
        }

        public static bool HasNonZeroPixel(Bgra32Image image, Rectangle rect)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            return image.HasNonZeroPixel(rect);
        }

        public static void SaveTexPixels(Stream output, Bgra32Image image, IProgress<FontProgress> progress)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            int rowBytes = image.Width * 4;
            byte[] rented = ArrayPool<byte>.Shared.Rent(rowBytes);
            try
            {
                for (int y = 0; y < image.Height; y++)
                {
                    int sourceRow = y * image.Stride;
                    for (int x = 0; x < image.Width; x++)
                    {
                        int offset = x * 4;
                        int sourceOffset = sourceRow + offset;
                        rented[offset] = image.Pixels[sourceOffset + 2];
                        rented[offset + 1] = image.Pixels[sourceOffset + 1];
                        rented[offset + 2] = image.Pixels[sourceOffset];
                        rented[offset + 3] = image.Pixels[sourceOffset + 3];
                    }

                    output.Write(rented.AsSpan(0, rowBytes));
                    if ((y & 0x0F) == 0 || y == image.Height - 1)
                    {
                        progress?.Report(new FontProgress("SavingTex", y + 1, image.Height));
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        public static void LoadTexPixels(ReadOnlySpan<byte> pixelData, Bgra32Image image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            int rowBytes = image.Width * 4;
            for (int y = 0; y < image.Height; y++)
            {
                ReadOnlySpan<byte> source = pixelData.Slice(y * rowBytes, rowBytes);
                Span<byte> target = image.Pixels.AsSpan(y * image.Stride, rowBytes);
                for (int x = 0; x < image.Width; x++)
                {
                    int offset = x * 4;
                    target[offset] = source[offset + 2];
                    target[offset + 1] = source[offset + 1];
                    target[offset + 2] = source[offset];
                    target[offset + 3] = source[offset + 3];
                }
            }
        }
    }
}
