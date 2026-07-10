using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DC_Font_Generator;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;

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
        private readonly object entryLock = new object();
        private FontPickerFontEntry entry;

        public string Name { get; set; }

        public FontPickerFontEntry GetEntry()
        {
            lock (entryLock)
            {
                entry ??= FontPickerFontEntry.FromFontFamily(Name);
                return entry;
            }
        }

        public override string ToString() => Name;
    }

    public static async Task<FontPickerDialogResult> ShowAsync(
        XamlRoot xamlRoot,
        FontSectionPickerState pickerState,
        FontStyleDescriptor currentStyle)
    {
        FontDescriptor currentFont = pickerState.CurrentFont;

        // Enumerating family names is inexpensive. Do not expand every family's style
        // set before showing the dialog: Super TTC installations can contain hundreds
        // of logical family/style combinations.
        List<string> familyNames = await Task.Run(LoadInstalledFamilyNames);
        if (!string.IsNullOrWhiteSpace(currentFont?.FamilyName)
            && !familyNames.Contains(currentFont.FamilyName, StringComparer.OrdinalIgnoreCase))
        {
            familyNames.Add(currentFont.FamilyName);
            familyNames.Sort(StringComparer.CurrentCultureIgnoreCase);
        }

        List<FontChoice> allChoices = familyNames
            .Select(name => new FontChoice { Name = name })
            .ToList();
        Color previewBackColor = Color.FromArgb(255, 32, 32, 32);

        TextBox searchBox = new TextBox
        {
            PlaceholderText = "搜索字体",
            Margin = new Thickness(0, 0, 0, 6)
        };
        ListView fontList = new ListView
        {
            Height = 460,
            SelectionMode = ListViewSelectionMode.Single
        };
        ComboBox styleBox = new ComboBox
        {
            Header = "样式",
            Width = 300,
            Margin = new Thickness(0, 6, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            IsEnabled = false
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

        int styleLoadVersion = 0;
        int previewVersion = 0;
        bool updatingStyles = false;
        bool dialogClosed = false;

        static string FormatSize(float size) =>
            Math.Round(size).ToString(CultureInfo.InvariantCulture);

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
            string previousName = (fontList.SelectedItem as FontChoice)?.Name;
            string text = searchBox.Text?.Trim() ?? "";
            IEnumerable<FontChoice> filtered = string.IsNullOrEmpty(text)
                ? allChoices
                : allChoices.Where(c => c.Name.StartsWith(text, StringComparison.CurrentCultureIgnoreCase))
                    .Concat(allChoices.Where(c =>
                        !c.Name.StartsWith(text, StringComparison.CurrentCultureIgnoreCase)
                        && c.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase)));

            List<FontChoice> filteredList = filtered.ToList();
            fontList.ItemsSource = filteredList;
            FontChoice selected = filteredList
                .FirstOrDefault(c => string.Equals(c.Name, previousName, StringComparison.OrdinalIgnoreCase))
                ?? filteredList.FirstOrDefault(c =>
                    string.Equals(c.Name, currentFont?.FamilyName, StringComparison.OrdinalIgnoreCase))
                ?? filteredList.FirstOrDefault();

            if (!ReferenceEquals(fontList.SelectedItem, selected))
            {
                fontList.SelectedItem = selected;
            }
            else
            {
                _ = RefreshStylesAsync();
            }
        }

        async Task RefreshStylesAsync()
        {
            int version = ++styleLoadVersion;
            ++previewVersion;
            FontChoice choice = fontList.SelectedItem as FontChoice;

            updatingStyles = true;
            styleBox.IsEnabled = false;
            styleBox.ItemsSource = null;
            previewImage.Source = null;
            updatingStyles = false;

            if (choice == null || dialogClosed)
            {
                return;
            }

            FontPickerFontEntry entry;
            try
            {
                entry = await Task.Run(choice.GetEntry);
            }
            catch
            {
                return;
            }

            if (dialogClosed
                || version != styleLoadVersion
                || !ReferenceEquals(choice, fontList.SelectedItem))
            {
                return;
            }

            FontPickerStyleResult styles = FontPickerCatalogService.GetStyles(entry, currentStyle);
            updatingStyles = true;
            try
            {
                styleBox.ItemsSource = styles.Styles;
                styleBox.SelectedIndex = styles.SelectedIndex >= 0 ? styles.SelectedIndex : 0;
                styleBox.IsEnabled = styles.Styles.Count > 0;
            }
            finally
            {
                updatingStyles = false;
            }

            SchedulePreview();
        }

        async void SchedulePreview()
        {
            int version = ++previewVersion;
            FontChoice choice = fontList.SelectedItem as FontChoice;
            FontPickerStyleItem selectedStyle = styleBox.SelectedItem as FontPickerStyleItem;
            previewImage.Source = null;
            if (choice == null || dialogClosed)
            {
                return;
            }

            float size = GetSelectedSize();
            FontDescriptor previewFont = FontPickerCatalogService.CreateSelectedFont(
                choice.Name,
                selectedStyle?.Descriptor,
                size);
            FontDescriptor singleBytePreviewFont = pickerState.EditingDoubleByteFont
                ? pickerState.SingleByteFont ?? previewFont
                : previewFont;
            FontDescriptor doubleBytePreviewFont = pickerState.EditingDoubleByteFont
                ? previewFont
                : pickerState.DoubleByteFont ?? previewFont;

            FontPickerPreviewRequest previewRequest = new FontPickerPreviewRequest
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
            };

            Bitmap preview = null;
            try
            {
                preview = await Task.Run(() =>
                    SkiaFontPickerPreviewRenderer.Render(new Size(540, 360), previewRequest));

                if (dialogClosed || version != previewVersion)
                {
                    return;
                }

                previewImage.Source = WinUiImageAdapter.ToWriteableBitmap(
                    Bgra32Image.FromBitmap(preview));
            }
            catch
            {
                // Keep the dialog responsive even when a malformed installed font fails.
            }
            finally
            {
                preview?.Dispose();
            }
        }

        static int NormalizePreviewCodePage(int codePage)
        {
            return codePage == 932 || codePage == 936 || codePage == 949 || codePage == 950
                ? codePage
                : 936;
        }

        searchBox.TextChanged += (_, _) => ApplyFilter();
        fontList.SelectionChanged += (_, _) => _ = RefreshStylesAsync();
        styleBox.SelectionChanged += (_, _) =>
        {
            if (!updatingStyles)
            {
                SchedulePreview();
            }
        };
        sizeBox.TextChanged += (_, _) => SchedulePreview();

        ApplyFilter();

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
        dialogClosed = true;
        ++styleLoadVersion;
        ++previewVersion;

        if (result != ContentDialogResult.Primary
            || fontList.SelectedItem is not FontChoice selectedChoice)
        {
            return null;
        }

        FontPickerStyleItem selectedStyle = styleBox.SelectedItem as FontPickerStyleItem;
        float selectedSize = GetSelectedSize();
        return new FontPickerDialogResult
        {
            Style = selectedStyle?.Descriptor,
            Font = FontPickerCatalogService.CreateSelectedFont(
                selectedChoice.Name,
                selectedStyle?.Descriptor,
                selectedSize)
        };
    }

    private static List<string> LoadInstalledFamilyNames()
    {
        List<string> names = new List<string>();
        HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        SKFontManager manager = SKFontManager.Default;
        for (int i = 0; i < manager.FontFamilyCount; i++)
        {
            string familyName = manager.GetFamilyName(i);
            if (!string.IsNullOrWhiteSpace(familyName) && seen.Add(familyName))
            {
                names.Add(familyName);
            }
        }

        names.Sort(StringComparer.CurrentCultureIgnoreCase);
        return names;
    }
}
