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
            MinWidth = 560,
            MaxWidth = 680,
            MinHeight = 720
        };

        Grid contentHost = new Grid
        {
            MinWidth = 600,
            MinHeight = 720,
            Padding = new Thickness(0, 0, 0, 0)
        };
        contentHost.Children.Add(picker);

        ContentDialog dialog = new ContentDialog
        {
            Title = "选择颜色",
            Content = contentHost,
            PrimaryButtonText = "确定",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            MinWidth = 680,
            MaxWidth = 760,
            MinHeight = 820,
            XamlRoot = xamlRoot
        };

        ContentDialogResult result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? picker.Color : null;
    }
}