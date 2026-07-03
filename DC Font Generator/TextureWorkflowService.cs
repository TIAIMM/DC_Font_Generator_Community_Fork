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
            return new TextureImportResult
            {
                Image = Load(path, format)
            };
        }

        public static void ConvertTexToPng(string texPath, string pngPath)
        {
            using (Bitmap bitmap = TextureFileService.LoadTex(texPath))
            {
                TextureFileService.SaveBmp(pngPath, bitmap);
            }
        }

        public static void ConvertPngToTex(string pngPath, string texPath, IProgress<FontProgress> progress)
        {
            using (Bitmap bitmap = TextureFileService.LoadBmp(pngPath))
            {
                TextureFileService.SaveTex(texPath, bitmap, progress);
            }
        }

        private static Bitmap Load(string path, TextureWorkflowFormat format)
        {
            switch (format)
            {
                case TextureWorkflowFormat.Tex:
                    return TextureFileService.LoadTex(path);
                case TextureWorkflowFormat.Png:
                    return TextureFileService.LoadBmp(path);
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }
    }
}
