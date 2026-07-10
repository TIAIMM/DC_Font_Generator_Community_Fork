using SkiaSharp;

namespace DC_Font_Generator
{
    // Kept as a source-compatible bridge for existing call sites. The implementation
    // is fully Skia-based; no DirectWrite COM objects or dwrite.dll entry points remain.
    internal static class DirectWriteGlyphPathService
    {
        public static bool TryGetGlyphPath(
            FontDescriptor font,
            FontStyleDescriptor descriptor,
            char c,
            float originX,
            float baseline,
            out SKPath path)
        {
            return SkiaGlyphPathService.TryGetGlyphPath(
                font,
                descriptor,
                c,
                originX,
                baseline,
                out path);
        }
    }
}
