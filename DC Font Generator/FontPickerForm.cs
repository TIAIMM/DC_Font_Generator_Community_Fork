using System;
using System.Collections.Generic;
using System.Drawing;
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
        private readonly Dictionary<string, FontPickerFontEntry> fontEntries = new Dictionary<string, FontPickerFontEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly bool editingDoubleByteFont;
        private readonly bool asciiOnly;
        private readonly int encodingCodePage;
        private readonly int glow;
        private readonly Color glowColor;
        private readonly int outline;
        private readonly Color outlineColor;
        private readonly Color fontColor;
        private readonly FontDescriptor singleByteFont;
        private readonly FontDescriptor doubleByteFont;
        private List<FontPickerFontEntry> allFontEntries = new List<FontPickerFontEntry>();
        private FontDescriptor previewFont;
        private bool preserveStyleOnFontSelection;
        private Button okButton;

        public FontPickerForm(FontDescriptor currentFont)
            : this(currentFont, 0, Color.FromArgb(0x80, 0x80, 0x80, 0x80), 0, Color.FromArgb(0xFF, 80, 80, 80), Color.Black)
        {
        }

        public FontPickerForm(FontDescriptor currentFont, int glow, Color glowColor, int outline, Color outlineColor, Color fontColor)
            : this(currentFont, currentFont, currentFont, false, true, 0, glow, glowColor, outline, outlineColor, fontColor)
        {
        }

        public FontPickerForm(
            FontDescriptor currentFont,
            FontDescriptor singleByteFont,
            FontDescriptor doubleByteFont,
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

            this.singleByteFont = singleByteFont;
            this.doubleByteFont = doubleByteFont;
            this.editingDoubleByteFont = editingDoubleByteFont;
            this.asciiOnly = asciiOnly;
            this.encodingCodePage = encodingCodePage;
            this.glow = Math.Max(0, glow);
            this.glowColor = glowColor;
            this.outline = Math.Max(0, outline);
            this.outlineColor = outlineColor;
            this.fontColor = fontColor;
            SelectedFont = currentFont;
            InitializeComponent();
            LoadFonts(currentFont);
            SelectCurrentFont(currentFont);
            UpdateStyleList(MakeStyleDescriptorFromFont(currentFont));
            UpdatePreview();
        }

        public FontDescriptor SelectedFont { get; private set; }
        public FontStyleDescriptor SelectedFontStyleDescriptor { get; private set; }

        public static void BeginWarmup()
        {
            FontPickerCatalogService.EnsureFontLoadTask();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SelectedFont = null;
                previewFont = null;
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
                UpdateStyleList(preserveStyleOnFontSelection
                    ? GetSelectedStyleDescriptor()
                    : GetRegularStyleDescriptor());
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
            sizeInput.PreviewKeyDown += delegate { BeginInvoke((Action)UpdatePreview); };
            // Subscribe to the inner TextBox for real-time preview on manual typing
            if (sizeInput.Controls.Count > 1 && sizeInput.Controls[1] is TextBox innerBox)
            {
                innerBox.TextChanged += delegate { UpdatePreview(); };
            }
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

        private void LoadFonts(FontDescriptor currentFont)
        {
            Task<List<FontPickerFontEntry>> task = FontPickerCatalogService.EnsureFontLoadTask();
            if (task.IsCompletedSuccessfully)
            {
                PopulateFontList(task.Result, currentFont.FamilyName);
                return;
            }

            PopulateFontList(new List<FontPickerFontEntry> { FontPickerFontEntry.FromFontFamily(currentFont.FamilyName) }, currentFont.FamilyName);
            task.ContinueWith(delegate(Task<List<FontPickerFontEntry>> completedTask)
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
                            UpdateStyleList(GetSelectedStyleDescriptor());
                            UpdatePreview();
                        }
                    });
                }
                catch
                {
                }
            });
        }

        private void PopulateFontList(List<FontPickerFontEntry> entries, string selectedFontName)
        {
            allFontEntries = FontPickerCatalogService.EnsureSelectedEntry(entries, selectedFontName);
            ApplyFontFilter(selectedFontName);
        }

        private void ApplyFontFilter(string selectedFontName)
        {
            FontPickerFilterResult filterResult = FontPickerCatalogService.Filter(
                allFontEntries,
                fontSearch.Text,
                selectedFontName);

            fontList.BeginUpdate();
            fontList.Items.Clear();
            fontEntries.Clear();

            foreach (FontPickerFontEntry entry in filterResult.Entries)
            {
                fontEntries[entry.Name] = entry;
                fontList.Items.Add(entry.Name);
            }

            if (filterResult.Entries.Count == 0)
            {
                fontList.EndUpdate();
                styleList.Items.Clear();
                previewFont = null;
                previewPanel.Invalidate();
                SetOkEnabled(false);
                return;
            }

            SetFontListSelectedIndex(filterResult.SelectedIndex, true);
            fontList.EndUpdate();
            SetOkEnabled(true);
        }

        private void SetOkEnabled(bool enabled)
        {
            if (okButton != null)
            {
                okButton.Enabled = enabled;
            }
        }

        private void SelectCurrentFont(FontDescriptor currentFont)
        {
            int index = fontList.FindStringExact(currentFont.FamilyName);
            if (index < 0)
            {
                fontList.Items.Insert(0, currentFont.FamilyName);
                index = 0;
            }

            SetFontListSelectedIndex(index, true);
            sizeInput.Value = FontPickerCatalogService.ClampFontSize(
                currentFont.SizePixels,
                sizeInput.Minimum,
                sizeInput.Maximum);
            UpdateStyleList(MakeStyleDescriptorFromFont(currentFont));
        }

        private void SetFontListSelectedIndex(int index, bool preserveStyle)
        {
            preserveStyleOnFontSelection = preserveStyle;
            try
            {
                fontList.SelectedIndex = index;
            }
            finally
            {
                preserveStyleOnFontSelection = false;
            }
        }

        private void UpdateStyleList(FontStyleDescriptor preferredDescriptor)
        {
            string selectedFontName = GetSelectedFontName();
            styleList.BeginUpdate();
            styleList.Items.Clear();

            FontPickerFontEntry entry = FontPickerCatalogService.GetEntryOrFallback(fontEntries, selectedFontName);
            FontPickerStyleResult result = FontPickerCatalogService.GetStyles(entry, preferredDescriptor);
            foreach (FontPickerStyleItem item in result.Styles)
            {
                styleList.Items.Add(item);
            }

            styleList.SelectedIndex = result.SelectedIndex;
            styleList.EndUpdate();
        }

        private void UpdatePreview()
        {
            if (fontList.SelectedItem == null || styleList.SelectedItem == null)
            {
                return;
            }

            FontDescriptor font = CreateSelectedFont();
            if (font == null)
            {
                previewFont = null;
                previewPanel.Invalidate();
                return;
            }

            previewFont = font;
            previewPanel.Invalidate();
        }

        private void ApplySelectedFont()
        {
            FontDescriptor font = CreateSelectedFont();
            if (font == null)
            {
                DialogResult = DialogResult.None;
                return;
            }

            SelectedFont = font;
            SelectedFontStyleDescriptor = GetSelectedStyleDescriptor();
        }

        private FontDescriptor CreateSelectedFont()
        {
            string fontName = GetSelectedFontName();
            FontStyleDescriptor descriptor = GetSelectedStyleDescriptor();
            float size = (float)sizeInput.Value;

            return FontPickerCatalogService.CreateSelectedFont(fontName, descriptor, size);
        }

        private void PreviewPanelPaint(object sender, PaintEventArgs e)
        {
            try
            {
                FontPickerPreviewRenderer.Draw(e.Graphics, new FontPickerPreviewRequest
                {
                    PreviewFont = previewFont,
                    PreviewFontStyleDescriptor = GetSelectedStyleDescriptor(),
                    SingleByteFont = singleByteFont,
                    DoubleByteFont = doubleByteFont,
                    EditingDoubleByteFont = editingDoubleByteFont,
                    AsciiOnly = asciiOnly,
                    EncodingCodePage = encodingCodePage,
                    Glow = glow,
                    GlowColor = glowColor,
                    Outline = outline,
                    OutlineColor = outlineColor,
                    FontColor = fontColor,
                    BackColor = previewPanel.BackColor
                });
            }
            catch
            {
                e.Graphics.Clear(previewPanel.BackColor);
            }
        }

        private string GetSelectedFontName()
        {
            if (fontList.SelectedItem == null)
            {
                return SelectedFont?.FamilyName ?? "Arial";
            }

            return fontList.SelectedItem.ToString();
        }

        private static FontStyleDescriptor MakeStyleDescriptorFromFont(FontDescriptor font)
        {
            return new FontStyleDescriptor(
                FontStyleDescriptor.StyleNameFromValues(font.Weight, font.Slant),
                font.Weight,
                font.Width,
                font.Slant);
        }

        private static FontStyleDescriptor GetRegularStyleDescriptor()
        {
            return FontStyleDescriptor.FromLegacyFontStyle(FontStyle.Regular);
        }

        private FontStyleDescriptor GetSelectedStyleDescriptor()
        {
            if (styleList.SelectedItem is FontPickerStyleItem item)
            {
                return item.Descriptor;
            }

            return GetRegularStyleDescriptor();
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
