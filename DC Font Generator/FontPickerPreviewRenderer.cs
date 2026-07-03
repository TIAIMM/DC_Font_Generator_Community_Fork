using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace DC_Font_Generator
{
    internal sealed class FontPickerPreviewRequest
    {
        public Font PreviewFont { get; set; }
        public Font SingleByteFont { get; set; }
        public Font DoubleByteFont { get; set; }
        public bool EditingDoubleByteFont { get; set; }
        public bool AsciiOnly { get; set; }
        public int EncodingCodePage { get; set; }
        public int Glow { get; set; }
        public Color GlowColor { get; set; }
        public int Outline { get; set; }
        public Color OutlineColor { get; set; }
        public Color FontColor { get; set; }
        public Color BackColor { get; set; }
    }

    internal static class FontPickerPreviewRenderer
    {
        public static void Draw(Graphics graphics, FontPickerPreviewRequest request)
        {
            graphics.Clear(request.BackColor);
            ConfigureGraphics(graphics);

            if (request.PreviewFont == null)
            {
                return;
            }

            PreviewText previewText = PreviewText.ForEncoding(request.EncodingCodePage, request.AsciiOnly);
            Font singleFont = request.EditingDoubleByteFont ? request.SingleByteFont : request.PreviewFont;
            Font doubleFont = request.EditingDoubleByteFont ? request.PreviewFont : request.DoubleByteFont;

            using (StringFormat format = new StringFormat())
            using (SolidBrush fillBrush = new SolidBrush(request.FontColor))
            {
                float y = 10f;
                float lineHeight = previewText.HasDoubleByteText
                    ? Math.Max(GetLineHeight(singleFont), GetLineHeight(doubleFont))
                    : GetLineHeight(singleFont);
                format.FormatFlags = StringFormatFlags.NoClip;
                format.Trimming = StringTrimming.None;

                DrawPreviewLine(
                    graphics,
                    request,
                    format,
                    fillBrush,
                    y,
                    new PreviewRun(previewText.SingleByteText, singleFont),
                    previewText.HasDoubleByteText ? new PreviewRun(previewText.DoubleByteText, doubleFont) : null);

                y += lineHeight;

                if (previewText.HasDoubleByteText)
                {
                    DrawPreviewLine(
                        graphics,
                        request,
                        format,
                        fillBrush,
                        y,
                        new PreviewRun("SBCS: " + previewText.SingleByteOnlyText, singleFont));
                    y += lineHeight;

                    DrawPreviewLine(
                        graphics,
                        request,
                        format,
                        fillBrush,
                        y,
                        new PreviewRun("DBCS: " + previewText.DoubleByteOnlyText, doubleFont));
                }
                else
                {
                    DrawPreviewLine(
                        graphics,
                        request,
                        format,
                        fillBrush,
                        y,
                        new PreviewRun(previewText.SingleByteOnlyText, singleFont));
                }
            }
        }

        private static void DrawPreviewLine(
            Graphics graphics,
            FontPickerPreviewRequest request,
            StringFormat format,
            Brush fillBrush,
            float y,
            params PreviewRun[] runs)
        {
            float x = 10f;
            int effectShift = request.Glow + request.Outline;
            for (int i = 0; i < runs.Length; i++)
            {
                PreviewRun run = runs[i];
                if (run == null || string.IsNullOrEmpty(run.Text) || run.Font == null)
                {
                    continue;
                }

                using (GraphicsPath path = new GraphicsPath())
                {
                    PointF point = new PointF(x + effectShift + 0.5f, y + effectShift + 0.5f);
                    path.AddString(
                        run.Text,
                        run.Font.FontFamily,
                        (int)run.Font.Style,
                        run.Font.Size,
                        point,
                        format);

                    DrawGlow(graphics, request, path);
                    DrawOutline(graphics, request, path);
                    graphics.FillPath(fillBrush, path);
                }

                x += MeasurePathWidth(run.Font, run.Text, format) + 8f;
            }
        }

        private static float MeasurePathWidth(Font font, string text, StringFormat format)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddString(
                    text,
                    font.FontFamily,
                    (int)font.Style,
                    font.Size,
                    new PointF(0.5f, 0.5f),
                    format);
                RectangleF bounds = path.GetBounds();
                return bounds.Width;
            }
        }

        private static void DrawGlow(Graphics graphics, FontPickerPreviewRequest request, GraphicsPath path)
        {
            if (request.Glow <= 0)
            {
                return;
            }

            int size = request.Outline + request.Glow;
            int glowStep = 0x80 / (request.Glow + 1);
            int alpha = glowStep;
            for (int i = 0; i < request.Glow; i++)
            {
                using (Pen pen = new Pen(Color.FromArgb(alpha, request.GlowColor.R, request.GlowColor.G, request.GlowColor.B), Math.Max(1, size - i)))
                {
                    pen.LineJoin = LineJoin.Round;
                    graphics.DrawPath(pen, path);
                }

                if (i >= request.Outline)
                {
                    alpha += glowStep;
                    if (alpha > 0x80)
                    {
                        alpha = 0x80;
                    }
                }
            }
        }

        private static void DrawOutline(Graphics graphics, FontPickerPreviewRequest request, GraphicsPath path)
        {
            if (request.Outline <= 0)
            {
                return;
            }

            using (Pen pen = new Pen(request.OutlineColor, request.Outline))
            {
                pen.LineJoin = LineJoin.Round;
                graphics.DrawPath(pen, path);
            }
        }

        private static float GetLineHeight(Font font)
        {
            FontFamily family = font.FontFamily;
            int em = family.GetEmHeight(font.Style);
            return font.Size * family.GetLineSpacing(font.Style) / em;
        }

        private static void ConfigureGraphics(Graphics graphics)
        {
            graphics.PageUnit = GraphicsUnit.Pixel;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        }

        private sealed class PreviewRun
        {
            public PreviewRun(string text, Font font)
            {
                Text = text;
                Font = font;
            }

            public string Text { get; }
            public Font Font { get; }
        }

        private sealed class PreviewText
        {
            private PreviewText(string singleByteText, string singleByteOnlyText, string doubleByteText, string doubleByteOnlyText)
            {
                SingleByteText = singleByteText;
                SingleByteOnlyText = singleByteOnlyText;
                DoubleByteText = doubleByteText;
                DoubleByteOnlyText = doubleByteOnlyText;
            }

            public string SingleByteText { get; }
            public string SingleByteOnlyText { get; }
            public string DoubleByteText { get; }
            public string DoubleByteOnlyText { get; }
            public bool HasDoubleByteText => !string.IsNullOrEmpty(DoubleByteText);

            public static PreviewText ForEncoding(int codePage, bool asciiOnly)
            {
                if (asciiOnly)
                {
                    return new PreviewText("Here is example ! HHHHHH", "0123456789 ABC xyz", "", "");
                }

                switch (codePage)
                {
                    case 932:
                        return new PreviewText("Here is example ! ", "ABC 123 HHHHHH", "\u65E5\u672C\u8A9E\u30AB\u30CA", "\u30C6\u30B9\u30C8\u65E5\u672C\u8A9E");
                    case 949:
                        return new PreviewText("Here is example ! ", "ABC 123 HHHHHH", "\uD55C\uAE00\uD14C\uC2A4\uD2B8", "\uAC00\uB098\uB2E4\uB77C\uD55C\uAE00");
                    case 950:
                        return new PreviewText("Here is example ! ", "ABC 123 HHHHHH", "\u6E2C\u8A66\u6E2C\u8A66\u7E41\u9AD4", "\u6B63\u9AD4\u4E2D\u6587\u6E2C\u8A66");
                    case 936:
                    default:
                        return new PreviewText("Here is example ! ", "ABC 123 HHHHHH", "\u6D4B\u8BD5\u6D4B\u8BD5\u7B80\u4F53", "\u7B80\u4F53\u4E2D\u6587\u6D4B\u8BD5");
                }
            }
        }
    }
}
