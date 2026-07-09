using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIColor = Windows.UI.Color;

namespace DC_Font_Generator.WinUI;

internal static class ColorPickerDialog
{
    public static async Task<WinUIColor?> ShowAsync(XamlRoot xamlRoot, WinUIColor initialColor)
    {
        ColorPicker picker = new ColorPicker
        {
            Color = initialColor,
            IsAlphaEnabled = true,
            IsAlphaSliderVisible = true,
            IsAlphaTextInputVisible = true,
            IsColorSliderVisible = true,
            IsColorChannelTextInputVisible = true,
            IsHexInputVisible = true,
            MinWidth = 500
        };

        ScrollViewer contentScroller = new ScrollViewer
        {
            Content = picker,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollMode = ScrollMode.Auto,
            HorizontalScrollMode = ScrollMode.Disabled,
            MaxHeight = 640
        };

        ContentDialog dialog = new ContentDialog
        {
            Title = "选择颜色",
            Content = contentScroller,
            PrimaryButtonText = "确定",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot
        };

        ContentDialogResult result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? picker.Color : null;
    }
}