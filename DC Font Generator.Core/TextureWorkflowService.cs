using System;
using System.Drawing;
using System.IO;

namespace DC_Font_Generator
{
    internal enum TextureWorkflowFormat
    {
        Tex,
        Png
    }

    internal sealed class TextureImportResult
    {
        public Bitmap Image { get; set; }
        public Bgra32Image ImagePixels { get; set; }
        public Size ImageSize => Image == null ? Size.Empty : Image.Size;
    }

    internal static class TextureWorkflowService
    {
        public static string GetPngOutputPath(string texPath)
        {
            return Path.ChangeExtension(texPath, ".png");
        }

        public static string GetTexOutputPath(string pngPath)
        {
            return Path.ChangeExtension(pngPath, ".Tex");
        }

        public static TextureImportResult Import(string path, TextureWorkflowFormat format)
        {
            Bgra32Image image = LoadImage(path, format);
            return new TextureImportResult
            {
                ImagePixels = image,
                Image = image.ToBitmap()
            };
        }

        public static void ConvertTexToPng(string texPath, string pngPath)
        {
            Bgra32Image image = TextureFileService.LoadTexImage(texPath);
            TextureFileService.SavePngImage(pngPath, image);
        }

        public static void ConvertPngToTex(string pngPath, string texPath, IProgress<FontProgress> progress)
        {
            Bgra32Image image = TextureFileService.LoadPngImage(pngPath);
            TextureFileService.SaveTexImage(texPath, image, progress);
        }

        private static Bgra32Image LoadImage(string path, TextureWorkflowFormat format)
        {
            switch (format)
            {
                case TextureWorkflowFormat.Tex:
                    return TextureFileService.LoadTexImage(path);
                case TextureWorkflowFormat.Png:
                    return TextureFileService.LoadPngImage(path);
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }
    }
}
