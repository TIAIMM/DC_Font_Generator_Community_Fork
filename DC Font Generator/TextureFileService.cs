using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

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

            Bitmap source = bitmap;
            bool disposeSource = false;
            if (source.PixelFormat != PixelFormat.Format32bppArgb)
            {
                source = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(source))
                {
                    g.DrawImageUnscaled(bitmap, 0, 0);
                }
                disposeSource = true;
            }

            Rectangle rect = new Rectangle(0, 0, source.Width, source.Height);
            BitmapData bmpData = source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int rowBytes = source.Width * 4;
                byte[] sourceRow = new byte[rowBytes];
                byte[] outputRow = new byte[rowBytes];

                for (int y = 0; y < source.Height; y++)
                {
                    IntPtr sourcePtr = IntPtr.Add(bmpData.Scan0, y * bmpData.Stride);
                    Marshal.Copy(sourcePtr, sourceRow, 0, rowBytes);

                    for (int i = 0; i < rowBytes; i += 4)
                    {
                        outputRow[i] = sourceRow[i + 2];
                        outputRow[i + 1] = sourceRow[i + 1];
                        outputRow[i + 2] = sourceRow[i];
                        outputRow[i + 3] = sourceRow[i + 3];
                    }

                    output.Write(outputRow, 0, rowBytes);
                    if ((y & 0x0F) == 0 || y == source.Height - 1)
                    {
                        ReportProgress(progress, "SavingTex", y + 1, bitmap.Height);
                    }
                }
            }
            finally
            {
                source.UnlockBits(bmpData);
                if (disposeSource)
                {
                    source.Dispose();
                }
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
                    Marshal.Copy(pixelData, 0, bmpData.Scan0, totalBytes);
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
