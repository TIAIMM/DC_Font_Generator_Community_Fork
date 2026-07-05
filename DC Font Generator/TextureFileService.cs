using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace DC_Font_Generator
{
    public static class TextureFileService
    {
        public static void SaveTex(string path, Bitmap bitmap, IProgress<FontProgress> progress = null)
        {
            SaveTexImage(path, Bgra32Image.FromBitmap(bitmap), progress);
        }

        public static void SaveTexImage(string path, Bgra32Image image, IProgress<FontProgress> progress = null)
        {
            using FileStream output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024);
            using BinaryWriter writer = new BinaryWriter(output);
            writer.Write(image.Width);
            writer.Write(image.Height);
            ReportProgress(progress, "SavingTex", 0, image.Height);
            writer.Flush();
            TexturePixelCodec.SaveTexPixels(output, image, progress);
        }

        public static Bitmap LoadTex(string path)
        {
            return LoadTexImage(path).ToBitmap();
        }

        public static Bgra32Image LoadTexImage(string path)
        {
            using (FileStream input = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(input))
            {
                int width = reader.ReadInt32();
                int height = reader.ReadInt32();
                int totalPixels = width * height;
                int totalBytes = totalPixels * 4;

                byte[] pixelData = reader.ReadBytes(totalBytes);
                if (pixelData.Length != totalBytes)
                {
                    throw new EndOfStreamException("Unexpected end of texture file");
                }

                Bgra32Image image = new Bgra32Image(width, height);
                TexturePixelCodec.LoadTexPixels(pixelData, image);
                return image;
            }
        }

        public static void SaveBmp(string path, Bitmap bitmap)
        {
            SavePngImage(path, Bgra32Image.FromBitmap(bitmap));
        }

        public static void SavePngImage(string path, Bgra32Image image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            GCHandle handle = GCHandle.Alloc(image.Pixels, GCHandleType.Pinned);
            try
            {
                SKImageInfo info = new SKImageInfo(image.Width, image.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
                using (SKPixmap pixmap = new SKPixmap(info, handle.AddrOfPinnedObject(), image.Stride))
                using (SKImage skImage = SKImage.FromPixels(pixmap))
                {
                    if (skImage == null)
                    {
                        throw new InvalidOperationException("Unable to create PNG image.");
                    }

                    using (SKData data = skImage.Encode(SKEncodedImageFormat.Png, 100))
                    using (FileStream output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024))
                    {
                        if (data == null)
                        {
                            throw new InvalidOperationException("Unable to encode PNG.");
                        }

                        data.SaveTo(output);
                    }
                }
            }
            finally
            {
                handle.Free();
            }
        }

        public static Bitmap LoadBmp(string path)
        {
            return LoadPngImage(path).ToBitmap();
        }

        public static Bgra32Image LoadPngImage(string path)
        {
            using (SKData data = SKData.Create(path))
            {
                if (data == null)
                {
                    throw new InvalidOperationException("Unable to decode PNG.");
                }

                using (SKImage skImage = SKImage.FromEncodedData(data))
                {
                    if (skImage == null)
                    {
                        throw new InvalidOperationException("Unable to decode PNG.");
                    }

                    Bgra32Image image = new Bgra32Image(skImage.Width, skImage.Height);
                    GCHandle handle = GCHandle.Alloc(image.Pixels, GCHandleType.Pinned);
                    try
                    {
                        SKImageInfo info = new SKImageInfo(image.Width, image.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
                        if (!skImage.ReadPixels(info, handle.AddrOfPinnedObject(), image.Stride, 0, 0))
                        {
                            throw new InvalidOperationException("Unable to decode PNG pixels.");
                        }
                    }
                    finally
                    {
                        handle.Free();
                    }

                    return image;
                }
            }
        }

        private static void ReportProgress(IProgress<FontProgress> progress, string stage, int value, int maximum)
        {
            progress?.Report(new FontProgress(stage, value, maximum));
        }
    }
}
