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
            IsHexInputVisible = true
        };

        ContentDialog dialog = new ContentDialog
        {
            Title = "选择颜色",
            Content = picker,
            PrimaryButtonText = "确定",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot
        };

        ContentDialogResult result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? picker.Color : null;
    }
}
