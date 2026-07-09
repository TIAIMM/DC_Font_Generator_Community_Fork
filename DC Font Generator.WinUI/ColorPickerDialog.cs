using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIColor = Windows.UI.Color;

namespace DC_Font_Generator.WinUI;

internal static class ColorPickerDialog
{
    public static async Task<WinUIColor?> ShowAsync(XamlRoot xamlRoot, WinUIColor initialColor)
    {
        bool syncing = false;

        ColorPicker picker = new ColorPicker
        {
            Color = initialColor,
            IsAlphaEnabled = true,
            IsAlphaSliderVisible = true,
            IsAlphaTextInputVisible = false,
            IsColorSliderVisible = true,
            IsColorChannelTextInputVisible = false,
            IsHexInputVisible = false,
            MinWidth = 470,
            MaxWidth = 470
        };

        ComboBox modeComboBox = new ComboBox
        {
            MinWidth = 120,
            SelectedIndex = 0
        };
        modeComboBox.Items.Add("RGB");

        TextBox hexTextBox = new TextBox
        {
            MinWidth = 150
        };

        NumberBox redNumberBox = CreateChannelNumberBox();
        NumberBox greenNumberBox = CreateChannelNumberBox();
        NumberBox blueNumberBox = CreateChannelNumberBox();
        NumberBox alphaNumberBox = new NumberBox
        {
            Minimum = 0,
            Maximum = 100,
            SmallChange = 1,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Width = 120
        };

        Grid inputGrid = CreateInputGrid();
        AddTopRow(inputGrid, 0, modeComboBox, hexTextBox);
        AddInputRow(inputGrid, 1, redNumberBox, "红色");
        AddInputRow(inputGrid, 2, greenNumberBox, "绿色");
        AddInputRow(inputGrid, 3, blueNumberBox, "蓝色");
        AddInputRow(inputGrid, 4, alphaNumberBox, "不透明度 %");

        void UpdateInputs(WinUIColor color)
        {
            syncing = true;
            try
            {
                hexTextBox.Text = FormatHex(color);
                redNumberBox.Value = color.R;
                greenNumberBox.Value = color.G;
                blueNumberBox.Value = color.B;
                alphaNumberBox.Value = Math.Round(color.A * 100d / 255d);
            }
            finally
            {
                syncing = false;
            }
        }

        void ApplyChannelInputs()
        {
            if (syncing)
            {
                return;
            }

            if (!TryGetByte(redNumberBox.Value, out byte r)
                || !TryGetByte(greenNumberBox.Value, out byte g)
                || !TryGetByte(blueNumberBox.Value, out byte b)
                || !TryGetPercentByte(alphaNumberBox.Value, out byte a))
            {
                return;
            }

            WinUIColor color = WinUIColor.FromArgb(a, r, g, b);
            syncing = true;
            try
            {
                picker.Color = color;
                hexTextBox.Text = FormatHex(color);
            }
            finally
            {
                syncing = false;
            }
        }

        void ApplyHexInput()
        {
            if (syncing)
            {
                return;
            }

            if (!TryParseHex(hexTextBox.Text, out WinUIColor color))
            {
                return;
            }

            syncing = true;
            try
            {
                picker.Color = color;
                redNumberBox.Value = color.R;
                greenNumberBox.Value = color.G;
                blueNumberBox.Value = color.B;
                alphaNumberBox.Value = Math.Round(color.A * 100d / 255d);
            }
            finally
            {
                syncing = false;
            }
        }

        UpdateInputs(initialColor);

        picker.ColorChanged += (_, _) =>
        {
            if (!syncing)
            {
                UpdateInputs(picker.Color);
            }
        };
        redNumberBox.ValueChanged += (_, _) => ApplyChannelInputs();
        greenNumberBox.ValueChanged += (_, _) => ApplyChannelInputs();
        blueNumberBox.ValueChanged += (_, _) => ApplyChannelInputs();
        alphaNumberBox.ValueChanged += (_, _) => ApplyChannelInputs();
        hexTextBox.TextChanged += (_, _) => ApplyHexInput();

        Grid contentHost = new Grid
        {
            ColumnSpacing = 18,
            MinWidth = 760
        };
        contentHost.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        contentHost.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        contentHost.Children.Add(picker);
        Grid.SetColumn(inputGrid, 1);
        contentHost.Children.Add(inputGrid);

        ContentDialog dialog = new ContentDialog
        {
            Title = "选择颜色",
            Content = contentHost,
            PrimaryButtonText = "确定",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            MinWidth = 840,
            XamlRoot = xamlRoot
        };

        ContentDialogResult result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? picker.Color : null;
    }

    private static NumberBox CreateChannelNumberBox()
    {
        return new NumberBox
        {
            Minimum = 0,
            Maximum = 255,
            SmallChange = 1,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Width = 120
        };
    }

    private static Grid CreateInputGrid()
    {
        Grid grid = new Grid
        {
            RowSpacing = 10,
            ColumnSpacing = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        for (int i = 0; i < 5; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        return grid;
    }

    private static void AddTopRow(Grid grid, int row, ComboBox modeComboBox, TextBox hexTextBox)
    {
        Grid.SetRow(modeComboBox, row);
        Grid.SetColumn(modeComboBox, 0);
        grid.Children.Add(modeComboBox);

        Grid.SetRow(hexTextBox, row);
        Grid.SetColumn(hexTextBox, 1);
        grid.Children.Add(hexTextBox);
    }

    private static void AddInputRow(Grid grid, int row, NumberBox numberBox, string label)
    {
        TextBlock textBlock = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center
        };

        Grid.SetRow(numberBox, row);
        Grid.SetColumn(numberBox, 0);
        grid.Children.Add(numberBox);

        Grid.SetRow(textBlock, row);
        Grid.SetColumn(textBlock, 1);
        grid.Children.Add(textBlock);
    }

    private static bool TryGetByte(double value, out byte result)
    {
        result = 0;
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return false;
        }

        result = (byte)Math.Clamp((int)Math.Round(value), 0, 255);
        return true;
    }

    private static bool TryGetPercentByte(double value, out byte result)
    {
        result = 0;
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return false;
        }

        double clamped = Math.Clamp(value, 0d, 100d);
        result = (byte)Math.Clamp((int)Math.Round(clamped * 255d / 100d), 0, 255);
        return true;
    }

    private static string FormatHex(WinUIColor color)
    {
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static bool TryParseHex(string text, out WinUIColor color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string value = text.Trim();
        if (value.StartsWith("#", StringComparison.Ordinal))
        {
            value = value.Substring(1);
        }

        if (value.Length == 6)
        {
            value = "FF" + value;
        }

        if (value.Length != 8 || !uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint parsed))
        {
            return false;
        }

        color = WinUIColor.FromArgb(
            (byte)((parsed >> 24) & 0xFF),
            (byte)((parsed >> 16) & 0xFF),
            (byte)((parsed >> 8) & 0xFF),
            (byte)(parsed & 0xFF));
        return true;
    }
}