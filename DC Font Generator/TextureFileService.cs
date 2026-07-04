using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace DC_Font_Generator
{
    public static class TextureFileService
    {
        public static void SaveTex(string path, Bitmap bitmap, IProgress<FontProgress> progress = null)
        {
            using FileStream output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024);
            using BinaryWriter writer = new BinaryWriter(output);
            writer.Write(bitmap.Width);
            writer.Write(bitmap.Height);
            ReportProgress(progress, "SavingTex", 0, bitmap.Height);
            writer.Flush();

            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                TexturePixelCodec.SaveTexPixels(output, bmpData, bitmap.Width, bitmap.Height, progress);
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }
        }

        public static Bitmap LoadTex(string path)
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

                Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                BitmapData bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppArgb);

                try
                {
                    TexturePixelCodec.LoadTexPixels(pixelData, bmpData, width, height);
                }
                finally
                {
                    bitmap.UnlockBits(bmpData);
                }

                return bitmap;
            }
        }

        public static void SaveBmp(string path, Bitmap bitmap)
        {
            bitmap.Save(path, ImageFormat.Png);
        }

        public static Bitmap LoadBmp(string path)
        {
            return (Bitmap)Bitmap.FromFile(path, true);
        }

        private static void ReportProgress(IProgress<FontProgress> progress, string stage, int value, int maximum)
        {
            progress?.Report(new FontProgress(stage, value, maximum));
        }
    }
}
