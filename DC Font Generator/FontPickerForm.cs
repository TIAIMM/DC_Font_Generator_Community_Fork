using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DC_Font_Generator
{
    public sealed class FontPickerForm : Form
    {
        private readonly ListBox fontList = new ListBox();
        private readonly TextBox fontSearch = new TextBox();
        private readonly ListBox styleList = new ListBox();
        private readonly NumericUpDown sizeInput = new NumericUpDown();
        private readonly Panel previewPanel = new BufferedPreviewPanel();
        private readonly Dictionary<string, FontEntry> fontEntries = new Dictionary<string, FontEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly bool editingDoubleByteFont;
        private readonly bool asciiOnly;
        private readonly int encodingCodePage;
        private readonly int glow;
        private readonly Color glowColor;
        private readonly int outline;
        private readonly Color outlineColor;
        private readonly Color fontColor;
        private readonly Font singleByteFont;
        private readonly Font doubleByteFont;
        private List<FontEntry> allFontEntries = new List<FontEntry>();
        private Font previewFont;
        private Button okButton;
        private static readonly object FontCacheLock = new object();
        private static Task<List<FontEntry>> fontLoadTask;

        public FontPickerForm(Font currentFont)
            : this(currentFont, 0, Color.FromArgb(0x80, 0x80, 0x80, 0x80), 0, Color.FromArgb(0xFF, 80, 80, 80), Color.Black)
        {
        }

        public FontPickerForm(Font currentFont, int glow, Color glowColor, int outline, Color outlineColor, Color fontColor)
            : this(currentFont, currentFont, currentFont, false, true, 0, glow, glowColor, outline, outlineColor, fontColor)
        {
        }

        public FontPickerForm(
            Font currentFont,
            Font singleByteFont,
            Font doubleByteFont,
            bool editingDoubleByteFont,
            bool asciiOnly,
            int encodingCodePage,
            int glow,
            Color glowColor,
            int outline,
            Color outlineColor,
            Color fontColor)
        {
            if (currentFont == null)
            {
                throw new ArgumentNullException(nameof(currentFont));
            }
            if (singleByteFont == null)
            {
                throw new ArgumentNullException(nameof(singleByteFont));
            }
            if (doubleByteFont == null)
            {
                throw new ArgumentNullException(nameof(doubleByteFont));
            }

            this.singleByteFont = (Font)singleByteFont.Clone();
            this.doubleByteFont = (Font)doubleByteFont.Clone();
            this.editingDoubleByteFont = editingDoubleByteFont;
            this.asciiOnly = asciiOnly;
            this.encodingCodePage = encodingCodePage;
            this.glow = Math.Max(0, glow);
            this.glowColor = glowColor;
            this.outline = Math.Max(0, outline);
            this.outlineColor = outlineColor;
            this.fontColor = fontColor;
            SelectedFont = (Font)currentFont.Clone();
            InitializeComponent();
            LoadFonts(currentFont);
            SelectCurrentFont(currentFont);
            UpdateStyleList(currentFont.Style);
            UpdatePreview();
        }

        public Font SelectedFont { get; private set; }

        public static void BeginWarmup()
        {
            EnsureFontLoadTask();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SelectedFont?.Dispose();
                previewFont?.Dispose();
                singleByteFont?.Dispose();
                doubleByteFont?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            Text = "Select Font";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ClientSize = new Size(760, 500);
            AcceptButton = CreateOkButton();
            CancelButton = CreateCancelButton();

            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3,
                Padding = new Padding(10)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));

            root.Controls.Add(CreateFontPanel(), 0, 0);
            root.Controls.Add(CreateStylePanel(), 1, 0);
            root.Controls.Add(CreateSizePanel(), 2, 0);
            Control preview = CreatePreviewPanel();
            root.Controls.Add(preview, 0, 1);
            root.SetColumnSpan(preview, 3);

            FlowLayoutPanel buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };
            buttons.Controls.Add((Control)CancelButton);
            buttons.Controls.Add((Control)AcceptButton);
            root.Controls.Add(buttons, 0, 2);
            root.SetColumnSpan(buttons, 3);

            Controls.Add(root);
        }

        private Control CreateFontPanel()
        {
            TableLayoutPanel panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            panel.Controls.Add(CreateHeaderLabel("Font"), 0, 0);

            fontSearch.Dock = DockStyle.Fill;
            fontSearch.PlaceholderText = "Search by name";
            fontSearch.TextChanged += delegate { ApplyFontFilter(GetSelectedFontName()); };
            panel.Controls.Add(fontSearch, 0, 1);

            fontList.Dock = DockStyle.Fill;
            fontList.IntegralHeight = false;
            fontList.SelectedIndexChanged += delegate
            {
                UpdateStyleList(GetSelectedStyle());
                UpdatePreview();
            };
            panel.Controls.Add(fontList, 0, 2);
            return panel;
        }

        private Control CreateStylePanel()
        {
            TableLayoutPanel panel = CreateLabeledPanel("Style");
            styleList.Dock = DockStyle.Fill;
            styleList.IntegralHeight = false;
            styleList.SelectedIndexChanged += delegate { UpdatePreview(); };
            panel.Controls.Add(styleList, 0, 1);
            return panel;
        }

        private Control CreateSizePanel()
        {
            TableLayoutPanel panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));

            panel.Controls.Add(CreateHeaderLabel("Size (px)"), 0, 0);

            sizeInput.DecimalPlaces = 0;
            sizeInput.Minimum = 1;
            sizeInput.Maximum = 512;
            sizeInput.Increment = 1;
            sizeInput.Dock = DockStyle.Top;
            sizeInput.ValueChanged += delegate { UpdatePreview(); };
            panel.Controls.Add(sizeInput, 0, 1);

            return panel;
        }

        private Control CreatePreviewPanel()
        {
            TableLayoutPanel panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            panel.Controls.Add(CreateHeaderLabel("Preview"), 0, 0);
            previewPanel.BorderStyle = BorderStyle.FixedSingle;
            previewPanel.Dock = DockStyle.Fill;
            previewPanel.BackColor = Color.Lime;
            previewPanel.Paint += PreviewPanelPaint;
            previewPanel.Resize += delegate { previewPanel.Invalidate(); };
            panel.Controls.Add(previewPanel, 0, 1);

            return panel;
        }

        private static TableLayoutPanel CreateLabeledPanel(string text)
        {
            TableLayoutPanel panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            panel.Controls.Add(CreateHeaderLabel(text), 0, 0);
            return panel;
        }

        private static Label CreateHeaderLabel(string text)
        {
            return new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private Button CreateOkButton()
        {
            okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Width = 88
            };
            okButton.Click += delegate { ApplySelectedFont(); };
            return okButton;
        }

        private static Button CreateCancelButton()
        {
            return new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Width = 88
            };
        }

        private void LoadFonts(Font currentFont)
        {
            Task<List<FontEntry>> task = EnsureFontLoadTask();
            if (task.IsCompletedSuccessfully)
            {
                PopulateFontList(task.Result, currentFont.FontFamily.Name);
                return;
            }

            PopulateFontList(new List<FontEntry> { FontEntry.FromFontFamily(currentFont.FontFamily.Name) }, currentFont.FontFamily.Name);
            task.ContinueWith(delegate(Task<List<FontEntry>> completedTask)
            {
                if (completedTask.Status != TaskStatus.RanToCompletion || IsDisposed)
                {
                    return;
                }

                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        if (!IsDisposed)
                        {
                            PopulateFontList(completedTask.Result, GetSelectedFontName());
                            UpdateStyleList(GetSelectedStyle());
                            UpdatePreview();
                        }
                    });
                }
                catch
                {
                }
            });
        }

        private void PopulateFontList(List<FontEntry> entries, string selectedFontName)
        {
            allFontEntries = new List<FontEntry>(entries);
            if (!ContainsFontEntry(allFontEntries, selectedFontName))
            {
                allFontEntries.Insert(0, FontEntry.FromFontFamily(selectedFontName));
            }

            ApplyFontFilter(selectedFontName);
        }

        private void ApplyFontFilter(string selectedFontName)
        {
            string filter = fontSearch.Text.Trim();
            fontList.BeginUpdate();
            fontList.Items.Clear();
            fontEntries.Clear();

            foreach (FontEntry entry in allFontEntries)
            {
                if (filter.Length > 0
                    && !entry.Name.StartsWith(filter, StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                fontEntries[entry.Name] = entry;
                fontList.Items.Add(entry.Name);
            }

            if (filter.Length > 0)
            {
                foreach (FontEntry entry in allFontEntries)
                {
                    if (entry.Name.StartsWith(filter, StringComparison.CurrentCultureIgnoreCase)
                        || entry.Name.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) < 0)
                    {
                        continue;
                    }

                    fontEntries[entry.Name] = entry;
                    fontList.Items.Add(entry.Name);
                }
            }

            if (fontList.Items.Count == 0)
            {
                fontList.EndUpdate();
                styleList.Items.Clear();
                previewFont?.Dispose();
                previewFont = null;
                previewPanel.Invalidate();
                SetOkEnabled(false);
                return;
            }

            int index = fontList.FindStringExact(selectedFontName);
            if (index < 0)
            {
                index = 0;
            }

            fontList.SelectedIndex = index;
            fontList.EndUpdate();
            SetOkEnabled(true);
        }

        private static bool ContainsFontEntry(List<FontEntry> entries, string name)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void SetOkEnabled(bool enabled)
        {
            if (okButton != null)
            {
                okButton.Enabled = enabled;
            }
        }

        private void SelectCurrentFont(Font currentFont)
        {
            int index = fontList.FindStringExact(currentFont.FontFamily.Name);
            if (index < 0)
            {
                fontList.Items.Insert(0, currentFont.FontFamily.Name);
                index = 0;
            }

            fontList.SelectedIndex = index;
            decimal size = (decimal)Math.Round(currentFont.Size);
            if (size < sizeInput.Minimum)
            {
                size = sizeInput.Minimum;
            }
            else if (size > sizeInput.Maximum)
            {
                size = sizeInput.Maximum;
            }

            sizeInput.Value = size;
        }

        private void UpdateStyleList(FontStyle preferredStyle)
        {
            string selectedFontName = GetSelectedFontName();
            styleList.BeginUpdate();
            styleList.Items.Clear();

            FontEntry entry;
            if (!fontEntries.TryGetValue(selectedFontName, out entry))
            {
                entry = FontEntry.FromFontFamily(selectedFontName);
                fontEntries[selectedFontName] = entry;
            }

            AddStyleIfAvailable(entry, FontStyle.Regular, "Regular");
            AddStyleIfAvailable(entry, FontStyle.Bold, "Bold");
            AddStyleIfAvailable(entry, FontStyle.Italic, "Italic");
            AddStyleIfAvailable(entry, FontStyle.Bold | FontStyle.Italic, "Bold Italic");

            if (styleList.Items.Count == 0)
            {
                styleList.Items.Add(new FontStyleItem("Regular", FontStyle.Regular));
            }

            int selectedIndex = 0;
            for (int i = 0; i < styleList.Items.Count; i++)
            {
                FontStyleItem item = (FontStyleItem)styleList.Items[i];
                if (item.Style == preferredStyle)
                {
                    selectedIndex = i;
                    break;
                }
            }

            styleList.SelectedIndex = selectedIndex;
            styleList.EndUpdate();
        }

        private void AddStyleIfAvailable(FontEntry entry, FontStyle style, string name)
        {
            if (entry.IsStyleAvailable(style))
            {
                styleList.Items.Add(new FontStyleItem(name, style));
            }
        }

        private void UpdatePreview()
        {
            if (fontList.SelectedItem == null || styleList.SelectedItem == null)
            {
                return;
            }

            Font font = CreateSelectedFont();
            if (font == null)
            {
                previewFont?.Dispose();
                previewFont = null;
                previewPanel.Invalidate();
                return;
            }

            Font oldFont = previewFont;
            previewFont = font;
            oldFont?.Dispose();
            previewPanel.Invalidate();
        }

        private void ApplySelectedFont()
        {
            Font font = CreateSelectedFont();
            if (font == null)
            {
                DialogResult = DialogResult.None;
                return;
            }

            SelectedFont.Dispose();
            SelectedFont = font;
        }

        private Font CreateSelectedFont()
        {
            string fontName = GetSelectedFontName();
            FontStyle style = GetSelectedStyle();
            float size = (float)sizeInput.Value;

            try
            {
                Font font = new Font(fontName, size, style, GraphicsUnit.Pixel);
                if (!IsUsableFont(font))
                {
                    font.Dispose();
                    return null;
                }

                return font;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsUsableFont(Font font)
        {
            try
            {
                using (Bitmap bitmap = new Bitmap(1, 1))
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    font.GetHeight(graphics);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void PreviewPanelPaint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(previewPanel.BackColor);
            e.Graphics.PageUnit = GraphicsUnit.Pixel;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            if (previewFont == null)
            {
                return;
            }

            try
            {
                DrawGeneratedPreview(e.Graphics);
            }
            catch
            {
                using (StringFormat format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    e.Graphics.DrawString(
                        "Preview unavailable",
                        Font,
                        Brushes.Black,
                        previewPanel.ClientRectangle,
                        format);
                }
            }
        }

        private void DrawGeneratedPreview(Graphics graphics)
        {
            PreviewText previewText = PreviewText.ForEncoding(encodingCodePage, asciiOnly);
            Font singleFont = editingDoubleByteFont ? singleByteFont : previewFont;
            Font doubleFont = editingDoubleByteFont ? previewFont : doubleByteFont;

            using (StringFormat format = new StringFormat())
            using (SolidBrush fillBrush = new SolidBrush(fontColor))
            {
                float y = 10f;
                int effectShift = glow + outline;
                float lineHeight = previewText.HasDoubleByteText
                    ? Math.Max(GetLineHeight(singleFont), GetLineHeight(doubleFont))
                    : GetLineHeight(singleFont);
                format.FormatFlags = StringFormatFlags.NoClip;
                format.Trimming = StringTrimming.None;

                DrawPreviewLine(
                    graphics,
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
                        format,
                        fillBrush,
                        y,
                        new PreviewRun("SBCS: " + previewText.SingleByteOnlyText, singleFont));
                    y += lineHeight;

                    DrawPreviewLine(
                        graphics,
                        format,
                        fillBrush,
                        y,
                        new PreviewRun("DBCS: " + previewText.DoubleByteOnlyText, doubleFont));
                }
                else
                {
                    DrawPreviewLine(
                        graphics,
                        format,
                        fillBrush,
                        y,
                        new PreviewRun(previewText.SingleByteOnlyText, singleFont));
                }
            }
        }

        private void DrawPreviewLine(
            Graphics graphics,
            StringFormat format,
            Brush fillBrush,
            float y,
            params PreviewRun[] runs)
        {
            float x = 10f;
            int effectShift = glow + outline;
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

                    DrawGlow(graphics, path);
                    DrawOutline(graphics, path);
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

        private void DrawGlow(Graphics graphics, GraphicsPath path)
        {
            if (glow <= 0)
            {
                return;
            }

            int size = outline + glow;
            int glowStep = 0x80 / (glow + 1);
            int alpha = glowStep;
            for (int i = 0; i < glow; i++)
            {
                using (Pen pen = new Pen(Color.FromArgb(alpha, glowColor.R, glowColor.G, glowColor.B), Math.Max(1, size - i)))
                {
                    pen.LineJoin = LineJoin.Round;
                    graphics.DrawPath(pen, path);
                }

                if (i >= outline)
                {
                    alpha += glowStep;
                    if (alpha > 0x80)
                    {
                        alpha = 0x80;
                    }
                }
            }
        }

        private void DrawOutline(Graphics graphics, GraphicsPath path)
        {
            if (outline <= 0)
            {
                return;
            }

            using (Pen pen = new Pen(outlineColor, outline))
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

        private static Task<List<FontEntry>> EnsureFontLoadTask()
        {
            lock (FontCacheLock)
            {
                if (fontLoadTask == null)
                {
                    fontLoadTask = Task.Run(LoadInstalledFontEntries);
                }

                return fontLoadTask;
            }
        }

        private static List<FontEntry> LoadInstalledFontEntries()
        {
            List<FontEntry> entries = new List<FontEntry>();
            using (InstalledFontCollection fonts = new InstalledFontCollection())
            {
                foreach (FontFamily family in fonts.Families)
                {
                    FontEntry entry = FontEntry.FromFamily(family);
                    if (entry.HasAnyStyle)
                    {
                        entries.Add(entry);
                    }
                }
            }

            entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
            return entries;
        }

        private string GetSelectedFontName()
        {
            if (fontList.SelectedItem == null)
            {
                return Font.FontFamily.Name;
            }

            return fontList.SelectedItem.ToString();
        }

        private FontStyle GetSelectedStyle()
        {
            if (styleList.SelectedItem is FontStyleItem item)
            {
                return item.Style;
            }

            return FontStyle.Regular;
        }

        private sealed class FontStyleItem
        {
            public FontStyleItem(string name, FontStyle style)
            {
                Name = name;
                Style = style;
            }

            public string Name { get; }
            public FontStyle Style { get; }

            public override string ToString()
            {
                return Name;
            }
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

        private sealed class FontEntry
        {
            private readonly bool regular;
            private readonly bool bold;
            private readonly bool italic;
            private readonly bool boldItalic;

            private FontEntry(string name, bool regular, bool bold, bool italic, bool boldItalic)
            {
                Name = name;
                this.regular = regular;
                this.bold = bold;
                this.italic = italic;
                this.boldItalic = boldItalic;
            }

            public string Name { get; }
            public bool HasAnyStyle => regular || bold || italic || boldItalic;

            public static FontEntry FromFontFamily(string fontName)
            {
                try
                {
                    using (FontFamily family = new FontFamily(fontName))
                    {
                        return FromFamily(family);
                    }
                }
                catch
                {
                    return new FontEntry(fontName, true, false, false, false);
                }
            }

            public static FontEntry FromFamily(FontFamily family)
            {
                return new FontEntry(
                    family.Name,
                    family.IsStyleAvailable(FontStyle.Regular),
                    family.IsStyleAvailable(FontStyle.Bold),
                    family.IsStyleAvailable(FontStyle.Italic),
                    family.IsStyleAvailable(FontStyle.Bold | FontStyle.Italic));
            }

            public bool IsStyleAvailable(FontStyle style)
            {
                switch (style)
                {
                    case FontStyle.Regular:
                        return regular;
                    case FontStyle.Bold:
                        return bold;
                    case FontStyle.Italic:
                        return italic;
                    case FontStyle.Bold | FontStyle.Italic:
                        return boldItalic;
                    default:
                        return false;
                }
            }
        }

        private sealed class BufferedPreviewPanel : Panel
        {
            public BufferedPreviewPanel()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
            }
        }
    }
}
