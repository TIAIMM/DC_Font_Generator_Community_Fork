using System.Drawing;

namespace DC_Font_Generator
{
    internal static class TextureService
    {
        public static Bitmap LoadBmp(string path)
        {
            return TextureFileService.LoadBmp(path);
        }
    }
}
