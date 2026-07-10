using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DC_Font_Generator;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DC_Font_Generator.WinUI;

internal sealed class FontPickerDialogResult
{
    public FontDescriptor Font { get; set; }
    public FontStyleDescriptor Style { get; set; }
}

internal static class FontPickerDialog
{
    private sealed class FontChoice
    {
        public FontPickerFontEntry Entry { get; set; }
        public string Name => Entry?.Name ?? "";
        public override string ToString() => Name;
    }

    public static async Task<FontPickerDialogResult> ShowAsync(
        XamlRoot xamlRoot,
        FontSectionPickerState pickerState,
        FontStyleDescriptor currentStyle)
    {
        FontDescriptor currentFont = pickerState.CurrentFont;
        List<FontPickerFontEntry> entries = await FontPickerCatalogService.EnsureFontLoadTask();
        List<FontChoice> allChoices = entries.Select(e => new FontChoice { Entry = e }).ToList();
        Color previewBackColor = Color.FromArgb(255, 32, 32, 32);

        TextBox searchBox = new TextBox { PlaceholderText = "搜索字体", Margin = new Thickness(0, 0, 0, 6) };
        ListView fontList = new ListView { Height = 460, SelectionMode = ListViewSelectionMode.Single };
        ComboBox styleBox = new ComboBox
        {
            Header = "样式",
            Width = 300,
            Margin = new Thickness(0, 6, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        Microsoft.UI.Xaml.Controls.Image previewImage = new Microsoft.UI.Xaml.Controls.Image
        {
            Stretch = Microsoft.UI.Xaml.Media.Stretch.None,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        Viewbox previewViewbox = new Viewbox
        {
            Stretch = Microsoft.UI.Xaml.Media.Stretch.None,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Child = previewImage
        };
        Border previewBorder = new Border
        {
            Width = 540,
            Height = 360,
            Margin = new Thickness(0, 6, 0, 0),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 96, 96, 96)),
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 32, 32, 32)),
            Child = previewViewbox
        };
        TextBox sizeBox = new TextBox
        {
            Header = "字号 px",
            Text = FormatSize(currentFont?.SizePixels ?? 23),
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 6, 0, 0)
        };

        static string FormatSize(float size) => Math.Round(size).ToString(CultureInfo.InvariantCulture);

        float GetSelectedSize()
        {
            if (!float.TryParse(sizeBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out float size)
                && !float.TryParse(sizeBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out size))
            {
                size = currentFont?.SizePixels ?? 23;
            }

            return Math.Clamp(size, 1f, 256f);
        }

        void ApplyFilter()
        {
            string text = searchBox.Text?.Trim() ?? "";
            IEnumerable<FontChoice> filtered = string.IsNullOrEmpty(text)
                ? allChoices
                : allChoices.Where(c => c.Name.StartsWith(text, System.StringComparison.CurrentCultureIgnoreCase))
                    .Concat(allChoices.Where(c =>
                        !c.Name.StartsWith(text, System.StringComparison.CurrentCultureIgnoreCase)
                        && c.Name.Contains(text, System.StringComparison.CurrentCultureIgnoreCase)));

            fontList.ItemsSource = filtered.ToList();
            FontChoice selected = ((IEnumerable<FontChoice>)fontList.ItemsSource)
                .FirstOrDefault(c => string.Equals(c.Name, currentFont?.FamilyName, System.StringComparison.OrdinalIgnoreCase))
                ?? ((IEnumerable<FontChoice>)fontList.ItemsSource).FirstOrDefault();
            fontList.SelectedItem = selected;
        }

        void RefreshStyles()
        {
            styleBox.ItemsSource = null;
            FontChoice choice = fontList.SelectedItem as FontChoice;
            if (choice == null)
            {
                return;
            }

            FontPickerStyleResult styles = FontPickerCatalogService.GetStyles(choice.Entry, currentStyle);
            styleBox.ItemsSource = styles.Styles;
            styleBox.SelectedIndex = styles.SelectedIndex >= 0 ? styles.SelectedIndex : 0;
        }

        void RefreshPreview()
        {
            FontChoice choice = fontList.SelectedItem as FontChoice;
            FontPickerStyleItem selectedStyle = styleBox.SelectedItem as FontPickerStyleItem;
            previewImage.Source = null;
            if (choice == null)
            {
                return;
            }

            float size = GetSelectedSize();
            FontDescriptor previewFont = FontPickerCatalogService.CreateSelectedFont(choice.Name, selectedStyle?.Descriptor, size);
            FontDescriptor singleBytePreviewFont = pickerState.EditingDoubleByteFont
                ? pickerState.SingleByteFont ?? previewFont
                : previewFont;
            FontDescriptor doubleBytePreviewFont = pickerState.EditingDoubleByteFont
                ? previewFont
                : pickerState.DoubleByteFont ?? previewFont;

            using Bitmap preview = FontPickerPreviewRenderer.Render(new Size(540, 360), new FontPickerPreviewRequest
            {
                PreviewFont = previewFont,
                PreviewFontStyleDescriptor = selectedStyle?.Descriptor,
                SingleByteFont = singleBytePreviewFont,
                DoubleByteFont = doubleBytePreviewFont,
                EditingDoubleByteFont = pickerState.EditingDoubleByteFont,
                AsciiOnly = false,
                EncodingCodePage = NormalizePreviewCodePage(pickerState.EncodingCodePage),
                Glow = pickerState.Glow,
                GlowColor = pickerState.GlowColor,
                Outline = pickerState.Outline,
                OutlineColor = pickerState.OutlineColor,
                FontColor = pickerState.FontColor,
                BackColor = previewBackColor
            });
            previewImage.Source = WinUiImageAdapter.ToWriteableBitmap(Bgra32Image.FromBitmap(preview));
        }

        static int NormalizePreviewCodePage(int codePage)
        {
            return codePage == 932 || codePage == 936 || codePage == 949 || codePage == 950
                ? codePage
                : 936;
        }

        searchBox.TextChanged += (_, _) =>
        {
            ApplyFilter();
            RefreshStyles();
            RefreshPreview();
        };
        fontList.SelectionChanged += (_, _) =>
        {
            RefreshStyles();
            RefreshPreview();
        };
        styleBox.SelectionChanged += (_, _) => RefreshPreview();
        sizeBox.TextChanged += (_, _) => RefreshPreview();
        ApplyFilter();
        RefreshStyles();
        RefreshPreview();

        Grid content = new Grid
        {
            Width = 930,
            Height = 560,
            ColumnSpacing = 16
        };
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        StackPanel listPanel = new StackPanel
        {
            Spacing = 4
        };
        listPanel.Children.Add(searchBox);
        listPanel.Children.Add(fontList);

        Grid optionGrid = new Grid
        {
            ColumnSpacing = 16
        };
        optionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });
        optionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        Grid.SetColumn(styleBox, 0);
        Grid.SetColumn(sizeBox, 1);
        optionGrid.Children.Add(styleBox);
        optionGrid.Children.Add(sizeBox);

        StackPanel previewPanel = new StackPanel
        {
            Spacing = 6
        };
        previewPanel.Children.Add(optionGrid);
        previewPanel.Children.Add(new TextBlock { Text = "预览", Margin = new Thickness(0, 8, 0, 0) });
        previewPanel.Children.Add(previewBorder);

        Grid.SetColumn(listPanel, 0);
        Grid.SetColumn(previewPanel, 1);
        content.Children.Add(listPanel);
        content.Children.Add(previewPanel);

        ContentDialog dialog = new ContentDialog
        {
            Title = "选择字体",
            Content = content,
            PrimaryButtonText = "确定",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot,
            MinWidth = 980,
            MaxWidth = 1040,
            MaxHeight = 760
        };
        dialog.Resources["ContentDialogMinWidth"] = 980d;
        dialog.Resources["ContentDialogMaxWidth"] = 1040d;

        ContentDialogResult result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary || fontList.SelectedItem is not FontChoice selectedChoice)
        {
            return null;
        }

        FontPickerStyleItem selectedStyle = styleBox.SelectedItem as FontPickerStyleItem;
        float size = GetSelectedSize();
        return new FontPickerDialogResult
        {
            Style = selectedStyle?.Descriptor,
            Font = FontPickerCatalogService.CreateSelectedFont(
                selectedChoice.Name,
                selectedStyle?.Descriptor,
                size)
        };
    }
}
