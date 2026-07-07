using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DC_Font_Generator.WinUI.ViewModels;
using DC_Font_Generator;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;
using WinRT.Interop;
using WinUIColor = Windows.UI.Color;

namespace DC_Font_Generator.WinUI;

public sealed partial class MainWindow : Window
{
    private const int DefaultWindowWidthDip = 1180;
    private const int DefaultWindowHeightDip = 720;
    private const int MinimumWindowWidthDip = 1080;
    private const int MinimumWindowHeightDip = 680;
    private readonly MainWindowViewModel viewModel;
    private readonly WinUiFilePickerService filePicker;
    private bool syncing;
    private bool syncingRange;
    private string falloutIniPath = "";
    private string falloutFontPath = "";
    private IntPtr windowHandle;
    private IntPtr oldWndProc;
    private WndProcDelegate windowProcDelegate;

    public MainWindow()
    {
        InitializeComponent();
        ConfigureWindow();
        viewModel = new MainWindowViewModel(DispatcherQueue);
        filePicker = new WinUiFilePickerService(this);
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        InitializeControls();
        UpdateAllControls();
    }

    private XamlRoot DialogRoot => (Content as FrameworkElement)?.XamlRoot;

    private void ConfigureWindow()
    {
        windowHandle = WindowNative.GetWindowHandle(this);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
        AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
        double scale = GetWindowScale();
        appWindow.Resize(new SizeInt32(ToPhysicalPixels(DefaultWindowWidthDip, scale), ToPhysicalPixels(DefaultWindowHeightDip, scale)));

        windowProcDelegate = WindowProc;
        oldWndProc = SetWindowLongPtr(windowHandle, GwlWndProc, Marshal.GetFunctionPointerForDelegate(windowProcDelegate));
        Closed += MainWindow_Closed;
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        if (windowHandle != IntPtr.Zero && oldWndProc != IntPtr.Zero)
        {
            SetWindowLongPtr(windowHandle, GwlWndProc, oldWndProc);
            oldWndProc = IntPtr.Zero;
        }
    }

    private IntPtr WindowProc(IntPtr hwnd, uint message, UIntPtr wParam, IntPtr lParam)
    {
        if (message == WmGetMinMaxInfo)
        {
            MINMAXINFO info = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            double scale = GetWindowScale();
            info.ptMinTrackSize.X = ToPhysicalPixels(MinimumWindowWidthDip, scale);
            info.ptMinTrackSize.Y = ToPhysicalPixels(MinimumWindowHeightDip, scale);
            Marshal.StructureToPtr(info, lParam, true);
            return IntPtr.Zero;
        }

        return CallWindowProc(oldWndProc, hwnd, message, wParam, lParam);
    }

    private double GetWindowScale()
    {
        uint dpi = windowHandle == IntPtr.Zero ? 96u : GetDpiForWindow(windowHandle);
        return Math.Max(1d, dpi / 96d);
    }

    private static int ToPhysicalPixels(int effectivePixels, double scale)
    {
        return (int)Math.Ceiling(effectivePixels * scale);
    }

    private void InitializeControls()
    {
        EncodingComboBox.ItemsSource = viewModel.EncodingItems;
        SizeXComboBox.ItemsSource = viewModel.TexSizes;
        SizeYComboBox.ItemsSource = viewModel.TexSizes;
        ArrangeComboBox.ItemsSource = viewModel.ArrangeItems;
        AdjustmentModeComboBox.ItemsSource = viewModel.AdjustmentItems;
        AdjustmentModeComboBox.SelectedIndex = 0;
        IniSlotsList.ItemsSource = viewModel.IniSlots;
        SampleTextBox.Text = viewModel.SampleText;
        ProgressBar.Value = 0;
        ShowPage(nameof(FontPagePanel));
    }

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainWindowViewModel.AtlasBitmap):
                AtlasImage.Source = viewModel.AtlasBitmap;
                UpdateAtlasSurfaceSize();
                break;
            case nameof(MainWindowViewModel.SamplePreviewBitmap):
                SamplePreviewImage.Source = viewModel.SamplePreviewBitmap;
                break;
            case nameof(MainWindowViewModel.StatusText):
                StatusTextBlock.Text = viewModel.StatusText;
                break;
            case nameof(MainWindowViewModel.LogText):
                LogTextBox.Text = viewModel.LogText;
                break;
            case nameof(MainWindowViewModel.Progress):
                UpdateProgress();
                break;
            case nameof(MainWindowViewModel.HasAtlas):
            case nameof(MainWindowViewModel.IsBusy):
                UpdateCommandState();
                UpdateProgress();
                break;
            case nameof(MainWindowViewModel.FontSections):
            case nameof(MainWindowViewModel.CurrentSection):
            case nameof(MainWindowViewModel.SelectedFontIndex):
                UpdateSectionControls();
                UpdateCommandState();
                break;
            case nameof(MainWindowViewModel.DoubleByteFontEnabled):
                UpdateSectionControls();
                UpdateCommandState();
                break;
            case nameof(MainWindowViewModel.TexSizeText):
                TexSizeTextBlock.Text = viewModel.TexSizeText;
                break;
            case nameof(MainWindowViewModel.ProjectTexName):
                if (!syncing)
                {
                    syncing = true;
                    TexNameTextBox.Text = viewModel.ProjectTexName;
                    syncing = false;
                }
                break;
        }
    }

    private void UpdateAllControls()
    {
        syncing = true;
        try
        {
            EncodingComboBox.SelectedIndex = viewModel.EncodingIndex;
            SizeXComboBox.SelectedIndex = viewModel.SizeXIndex;
            SizeYComboBox.SelectedIndex = viewModel.SizeYIndex;
            GapNumberBox.Value = viewModel.Gap;
            ArrangeComboBox.SelectedIndex = viewModel.ArrangeMethod;
            TexSizeTextBlock.Text = viewModel.TexSizeText;
            StatusTextBlock.Text = viewModel.StatusText;
            LogTextBox.Text = viewModel.LogText;
            TexNameTextBox.Text = viewModel.ProjectTexName;
            SetButtonColor(BackgroundColorButton, viewModel.BackgroundColor);
            UpdateSectionControls();
            UpdateProgress();
            UpdateCommandState();
        }
        finally
        {
            syncing = false;
        }
    }

    private void UpdateSectionControls()
    {
        if (viewModel.CurrentSection == null)
        {
            return;
        }

        syncing = true;
        try
        {
            FontSectionViewState state = viewModel.GetSectionState();
            FntLabelTextBlock.Text = state.FontLabel;
            SingleFontTextBox.Text = state.SingleByteFontText;
            DoubleFontTextBox.Text = state.DoubleByteFontText;
            DoubleFontTextBox.IsEnabled = viewModel.DoubleByteFontEnabled;
            SelectDoubleFontButton.IsEnabled = viewModel.DoubleByteFontEnabled && !viewModel.IsBusy;
            if (!viewModel.DoubleByteFontEnabled)
            {
                SelectDoubleByteCheckBox.IsChecked = false;
            }
            GlowNumberBox.Value = state.Glow;
            OutlineNumberBox.Value = state.Outline;
            FixedFontCheckBox.IsChecked = state.FixedFont;
            FixedWidthNumberBox.Value = state.FontMaxWidth;
            FixedWidthNumberBox.IsEnabled = state.FixedFont;
            AutoBaseLineCheckBox.IsChecked = !state.UseManualBaseLine;
            BaseLineNumberBox.Value = state.ManualBaseLine;
            BaseLineNumberBox.IsEnabled = state.UseManualBaseLine;
            FntNameTextBox.Text = state.FntName;
            UpdateIniLinkControls(state);
            SetButtonColor(GlowColorButton, WinUiColorAdapter.ToWinUiColor(state.GlowColor));
            SetButtonColor(OutlineColorButton, WinUiColorAdapter.ToWinUiColor(state.OutlineColor));
            SetButtonColor(FontColorButton, WinUiColorAdapter.ToWinUiColor(state.FontColor));
        }
        finally
        {
            syncing = false;
        }
    }

    private void UpdateProgress()
    {
        WinUiFontProgress progress = viewModel.Progress;
        bool visible = viewModel.IsBusy || progress != null;
        Visibility visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        ProgressLabelTextBlock.Visibility = visibility;
        ProgressBar.Visibility = visibility;
        ProgressTextBlock.Visibility = visibility;
        ProgressBar.IsIndeterminate = viewModel.IsBusy && progress == null;
        ProgressBar.Value = progress?.Percent ?? 0d;
        ProgressTextBlock.Text = progress == null
            ? ""
            : $"{progress.Stage}: {progress.Value}/{progress.Maximum}";
    }

    private void UpdateCommandState()
    {
        bool enabled = !viewModel.IsBusy;
        bool hasAtlas = viewModel.HasAtlas;
        bool canLink = viewModel.FontSections.Count > 1
            && FontLinkService.GetCandidates(viewModel.FontSections, viewModel.SelectedFontIndex).Count > 0;
        RenderButton.IsEnabled = enabled;
        LoadProjectButton.IsEnabled = enabled;
        SaveProjectButton.IsEnabled = enabled;
        SaveFontButton.IsEnabled = enabled && hasAtlas;
        AdjustPageButton.IsEnabled = enabled && hasAtlas;
        AdjustPagePanel.IsHitTestVisible = enabled && hasAtlas;
        AdjustPagePanel.Opacity = hasAtlas ? 1d : 0.45d;
        SelectDoubleByteCheckBox.IsEnabled = enabled && hasAtlas && viewModel.DoubleByteFontEnabled;
        SelectDoubleFontButton.IsEnabled = enabled && viewModel.DoubleByteFontEnabled;
        LinkFontButton.IsEnabled = enabled && canLink;
    }

    private void MainPageButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton button && button.Tag is string pageName)
        {
            ShowPage(pageName);
        }
    }

    private void ShowPage(string pageName)
    {
        (ToggleButton Button, FrameworkElement Panel)[] pages =
        {
            (FontPageButton, FontPagePanel),
            (AdjustPageButton, AdjustPagePanel),
            (AdvancedPageButton, AdvancedPagePanel),
            (IniPageButton, IniPagePanel),
            (LogPageButton, LogPagePanel)
        };

        foreach ((ToggleButton button, FrameworkElement panel) in pages)
        {
            bool active = panel.Name == pageName;
            panel.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
            button.IsChecked = active;
        }
    }

    private async void RenderButton_Click(object sender, RoutedEventArgs e)
    {
        SyncViewModelFromInputs();
        await viewModel.RenderAsync();
        UpdateAllControls();
    }

    private async void SaveFontButton_Click(object sender, RoutedEventArgs e)
    {
        string texPath = await filePicker.SaveFileAsync("Save Tex", viewModel.ProjectTexName, ".Tex");
        if (string.IsNullOrWhiteSpace(texPath))
        {
            viewModel.StatusText = "Save Cancel.";
            return;
        }

        List<string> fntPaths = new List<string>();
        List<string> suggestedNames = FontSaveWorkflowService.GetSuggestedFontNames(viewModel.FontSections);
        if (viewModel.FontSections.Count == 1)
        {
            fntPaths.Add(FontSaveWorkflowService.GetFntPath(texPath));
        }
        else
        {
            for (int i = 0; i < viewModel.FontSections.Count; i++)
            {
                string suggested = string.IsNullOrWhiteSpace(suggestedNames[i]) ? $"Font{i + 1}" : suggestedNames[i];
                string fntPath = await filePicker.SaveFileAsync($"Save Fnt {i + 1}", suggested, ".fnt");
                if (string.IsNullOrWhiteSpace(fntPath))
                {
                    viewModel.StatusText = "Save Cancel.";
                    return;
                }

                fntPaths.Add(fntPath);
            }
        }

        await viewModel.SaveFontAsync(texPath, fntPaths);
        UpdateAllControls();
    }

    private async void SaveProjectButton_Click(object sender, RoutedEventArgs e)
    {
        SyncViewModelFromInputs();
        string path = await filePicker.SaveFileAsync("Save Project", "FontProject", ".project.xml");
        if (string.IsNullOrWhiteSpace(path))
        {
            viewModel.StatusText = "Save Cancel.";
            return;
        }

        await viewModel.SaveProjectAsync(path, viewModel.ProjectTexName);
    }

    private void SyncViewModelFromInputs()
    {
        if (syncing)
        {
            return;
        }

        if (EncodingComboBox.SelectedIndex >= 0 && EncodingComboBox.SelectedIndex != viewModel.EncodingIndex)
        {
            viewModel.EncodingIndex = EncodingComboBox.SelectedIndex;
        }

        if (SizeXComboBox.SelectedIndex >= 0 && SizeXComboBox.SelectedIndex != viewModel.SizeXIndex)
        {
            viewModel.SizeXIndex = SizeXComboBox.SelectedIndex;
        }

        if (SizeYComboBox.SelectedIndex >= 0 && SizeYComboBox.SelectedIndex != viewModel.SizeYIndex)
        {
            viewModel.SizeYIndex = SizeYComboBox.SelectedIndex;
        }

        if (ArrangeComboBox.SelectedIndex >= 0 && ArrangeComboBox.SelectedIndex != viewModel.ArrangeMethod)
        {
            viewModel.ArrangeMethod = ArrangeComboBox.SelectedIndex;
        }

        if (!double.IsNaN(GapNumberBox.Value))
        {
            int gap = (int)Math.Round(GapNumberBox.Value);
            if (gap != viewModel.Gap)
            {
                viewModel.Gap = gap;
            }
        }

        FontSectionViewState state = viewModel.GetSectionState();
        if (!double.IsNaN(GlowNumberBox.Value))
        {
            int glow = (int)Math.Round(GlowNumberBox.Value);
            if (glow != state.Glow)
            {
                viewModel.SetEffectValue("Glow", glow);
                state = viewModel.GetSectionState();
            }
        }

        if (!double.IsNaN(OutlineNumberBox.Value))
        {
            int outline = (int)Math.Round(OutlineNumberBox.Value);
            if (outline != state.Outline)
            {
                viewModel.SetEffectValue("Outline", outline);
                state = viewModel.GetSectionState();
            }
        }

        bool fixedFont = FixedFontCheckBox.IsChecked == true;
        float fixedWidth = !double.IsNaN(FixedWidthNumberBox.Value)
            ? (float)FixedWidthNumberBox.Value
            : state.FontMaxWidth;
        if (fixedFont != state.FixedFont)
        {
            viewModel.SetFixedFont(fixedFont, fixedWidth);
            state = viewModel.GetSectionState();
        }
        else if (fixedFont && Math.Abs(fixedWidth - state.FontMaxWidth) > 0.001f)
        {
            viewModel.SetFixedFontWidth(fixedFont, fixedWidth);
            state = viewModel.GetSectionState();
        }

        bool autoBaseLine = AutoBaseLineCheckBox.IsChecked == true;
        bool useManualBaseLine = !autoBaseLine;
        float manualBaseLine = !double.IsNaN(BaseLineNumberBox.Value)
            ? (float)BaseLineNumberBox.Value
            : state.ManualBaseLine;
        if (useManualBaseLine != state.UseManualBaseLine
            || Math.Abs(manualBaseLine - state.ManualBaseLine) > 0.001f)
        {
            viewModel.SetManualBaseLine(autoBaseLine, manualBaseLine);
        }

        if (!string.Equals(FntNameTextBox.Text, state.FntName, StringComparison.Ordinal))
        {
            viewModel.SetFntName(FntNameTextBox.Text);
        }

        if (!string.Equals(TexNameTextBox.Text, viewModel.ProjectTexName, StringComparison.Ordinal))
        {
            viewModel.ProjectTexName = TexNameTextBox.Text;
        }
    }

    private async void LoadProjectButton_Click(object sender, RoutedEventArgs e)
    {
        string path = await filePicker.OpenFileAsync("Load Project", ".project.xml");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await viewModel.LoadProjectAsync(path);
        UpdateAllControls();
    }

    private async void SelectSingleFont_Click(object sender, RoutedEventArgs e)
    {
        await SelectFontAsync(false);
    }

    private async void SelectDoubleFont_Click(object sender, RoutedEventArgs e)
    {
        if (!viewModel.DoubleByteFontEnabled)
        {
            return;
        }

        await SelectFontAsync(true);
    }

    private async Task SelectFontAsync(bool doubleByte)
    {
        Main section = viewModel.CurrentSection;
        FontPickerDialogResult result = await FontPickerDialog.ShowAsync(
            DialogRoot,
            FontSectionStateService.CreatePickerState(viewModel.FontSections, viewModel.SelectedFontIndex, doubleByte, viewModel.Encoding),
            doubleByte ? section.font2StyleDescriptor : section.font1StyleDescriptor);
        if (result == null)
        {
            return;
        }

        viewModel.SetSectionFont(doubleByte, result.Font, result.Style);
        UpdateSectionControls();
    }

    private void EncodingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (syncing || EncodingComboBox.SelectedIndex < 0) return;
        viewModel.EncodingIndex = EncodingComboBox.SelectedIndex;
        UpdateSectionControls();
    }

    private void TexSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (syncing) return;
        if (SizeXComboBox.SelectedIndex >= 0) viewModel.SizeXIndex = SizeXComboBox.SelectedIndex;
        if (SizeYComboBox.SelectedIndex >= 0) viewModel.SizeYIndex = SizeYComboBox.SelectedIndex;
    }

    private void GapNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (syncing || double.IsNaN(args.NewValue)) return;
        viewModel.Gap = (int)Math.Round(args.NewValue);
    }

    private void ArrangeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (syncing || ArrangeComboBox.SelectedIndex < 0) return;
        viewModel.ArrangeMethod = ArrangeComboBox.SelectedIndex;
    }

    private void GlowNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (syncing || double.IsNaN(args.NewValue)) return;
        viewModel.SetEffectValue("Glow", (float)args.NewValue);
    }

    private void OutlineNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (syncing || double.IsNaN(args.NewValue)) return;
        viewModel.SetEffectValue("Outline", (float)args.NewValue);
    }

    private async void GlowColorButton_Click(object sender, RoutedEventArgs e)
    {
        await PickEffectColorAsync("Glow", GlowColorButton);
    }

    private async void OutlineColorButton_Click(object sender, RoutedEventArgs e)
    {
        await PickEffectColorAsync("Outline", OutlineColorButton);
    }

    private async void FontColorButton_Click(object sender, RoutedEventArgs e)
    {
        await PickEffectColorAsync("FontColor", FontColorButton);
    }

    private async void BackgroundColorButton_Click(object sender, RoutedEventArgs e)
    {
        WinUIColor? color = await ColorPickerDialog.ShowAsync(DialogRoot, viewModel.BackgroundColor);
        if (color == null) return;
        viewModel.BackgroundColor = color.Value;
        SetButtonColor(BackgroundColorButton, color.Value);
    }

    private async Task PickEffectColorAsync(string tag, Button button)
    {
        SolidColorBrush brush = button.Background as SolidColorBrush;
        WinUIColor current = brush?.Color ?? WinUIColor.FromArgb(255, 255, 255, 255);
        WinUIColor? color = await ColorPickerDialog.ShowAsync(DialogRoot, current);
        if (color == null)
        {
            return;
        }

        viewModel.SetEffectColor(tag, color.Value);
        SetButtonColor(button, color.Value);
    }

    private void FixedFontCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (syncing) return;
        viewModel.SetFixedFont(FixedFontCheckBox.IsChecked == true, (float)FixedWidthNumberBox.Value);
        UpdateSectionControls();
    }

    private void FixedWidthNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (syncing || double.IsNaN(args.NewValue)) return;
        viewModel.SetFixedFontWidth(FixedFontCheckBox.IsChecked == true, (float)args.NewValue);
    }

    private void AutoBaseLineCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (syncing) return;
        viewModel.SetManualBaseLine(AutoBaseLineCheckBox.IsChecked == true, (float)BaseLineNumberBox.Value);
        UpdateSectionControls();
    }

    private void BaseLineNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (syncing || double.IsNaN(args.NewValue)) return;
        viewModel.SetManualBaseLine(AutoBaseLineCheckBox.IsChecked == true, (float)args.NewValue);
    }

    private void FntNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (syncing) return;
        viewModel.SetFntName(FntNameTextBox.Text);
    }

    private void TexNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (syncing) return;
        viewModel.ProjectTexName = TexNameTextBox.Text;
    }

    private void IniLinkCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (syncing || sender is not CheckBox checkBox)
        {
            return;
        }

        CheckBox[] checkBoxes = GetIniLinkCheckBoxes();
        int index = Array.IndexOf(checkBoxes, checkBox);
        if (index >= 0)
        {
            viewModel.SetIniLink(index, checkBox.IsChecked == true);
        }
    }

    private async void ImportFnt_Click(object sender, RoutedEventArgs e)
    {
        string path = await filePicker.OpenFileAsync("Open FNT", ".fnt");
        if (string.IsNullOrWhiteSpace(path)) return;
        await viewModel.ImportFntAsync(path);
        UpdateAllControls();
    }

    private async void LinkFont_Click(object sender, RoutedEventArgs e)
    {
        List<FontLinkCandidate> candidates = FontLinkService.GetCandidates(viewModel.FontSections, viewModel.SelectedFontIndex);
        if (candidates.Count == 0)
        {
            await ShowMessageAsync("没有可链接的字体段。");
            return;
        }

        ListView list = new ListView { ItemsSource = candidates, SelectionMode = ListViewSelectionMode.Single, Height = 260 };
        ContentDialog dialog = new ContentDialog
        {
            Title = "Link Font",
            Content = list,
            PrimaryButtonText = "确定",
            CloseButtonText = "取消",
            XamlRoot = DialogRoot
        };

        ContentDialogResult result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && list.SelectedItem is FontLinkCandidate candidate)
        {
            FontLinkService.ApplyLink(viewModel.FontSections, viewModel.SelectedFontIndex, candidate.Index);
            viewModel.InvalidateGeneratedOutput();
            UpdateSectionControls();
        }
    }

    private void PreviousSection_Click(object sender, RoutedEventArgs e)
    {
        viewModel.SelectedFontIndex--;
        UpdateSectionControls();
    }

    private void NextSection_Click(object sender, RoutedEventArgs e)
    {
        viewModel.SelectedFontIndex++;
        UpdateSectionControls();
    }

    private void AddSection_Click(object sender, RoutedEventArgs e)
    {
        viewModel.AddSection();
        UpdateSectionControls();
    }

    private void RemoveSection_Click(object sender, RoutedEventArgs e)
    {
        viewModel.RemoveSection();
        UpdateSectionControls();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        viewModel.Clear();
        UpdateAllControls();
    }

    private void SelectRange_Click(object sender, RoutedEventArgs e)
    {
        ApplyRangeSelectionFromInputs();
    }

    private void SelectSingleByteCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (syncing) return;
        viewModel.SetSingleByteSelection(SelectSingleByteCheckBox.IsChecked == true);
        viewModel.RefreshSelectionOverlay();
    }

    private void SelectDoubleByteCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (syncing) return;
        viewModel.SetDoubleByteSelection(SelectDoubleByteCheckBox.IsChecked == true);
        viewModel.RefreshSelectionOverlay();
    }

    private void AdjustDecrease_Click(object sender, RoutedEventArgs e)
    {
        ApplyAdjustment("Dec");
    }

    private void AdjustIncrease_Click(object sender, RoutedEventArgs e)
    {
        ApplyAdjustment("Add");
    }

    private void ApplyAdjustment(string command)
    {
        int mode = Math.Max(0, AdjustmentModeComboBox.SelectedIndex);
        float increment = (float)(double.IsNaN(IncrementNumberBox.Value) ? 1 : IncrementNumberBox.Value);
        GlyphAdjustmentWorkflowResult result = viewModel.AdjustGlyphs(mode, command, increment);
        if (result.MissingSelection)
        {
            viewModel.StatusText = "Has not selected any font.";
        }

        viewModel.RefreshSelectionOverlay();
        UpdateSectionControls();
    }

    private void RestoreAdjust_Click(object sender, RoutedEventArgs e)
    {
        viewModel.RestoreAdjustment();
        viewModel.RefreshSelectionOverlay();
        UpdateSectionControls();
    }

    private void SampleTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (syncing) return;
        viewModel.SampleText = SampleTextBox.Text;
    }

    private void RangeCharTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (syncing || syncingRange || sender is not TextBox textBox)
        {
            return;
        }

        syncingRange = true;
        try
        {
            if (textBox.Text.Length > 1)
            {
                textBox.Text = textBox.Text[^1].ToString();
                textBox.SelectionStart = textBox.Text.Length;
            }

            string hex = EncodingInputService.TextToHex(textBox.Text, viewModel.Encoding.enc);
            if (ReferenceEquals(textBox, FromCharTextBox))
            {
                RangeStartTextBox.Text = hex;
            }
            else
            {
                RangeEndTextBox.Text = hex;
            }
        }
        finally
        {
            syncingRange = false;
        }

        ApplyRangeSelectionFromInputs();
    }

    private void RangeHexTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (syncing || syncingRange || sender is not TextBox textBox)
        {
            return;
        }

        syncingRange = true;
        try
        {
            string normalized = NormalizeHex(textBox.Text);
            if (normalized != textBox.Text)
            {
                textBox.Text = normalized;
                textBox.SelectionStart = normalized.Length;
            }

            string text = EncodingInputService.HexToText(normalized, viewModel.Encoding.enc);
            if (ReferenceEquals(textBox, RangeStartTextBox))
            {
                FromCharTextBox.Text = text;
            }
            else
            {
                ToCharTextBox.Text = text;
            }
        }
        finally
        {
            syncingRange = false;
        }

        ApplyRangeSelectionFromInputs();
    }

    private async void ImportEncodingText_Click(object sender, RoutedEventArgs e)
    {
        string path = await filePicker.OpenFileAsync("Import Encoding Text", ".txt");
        if (string.IsNullOrWhiteSpace(path)) return;
        int count = viewModel.Encoding.ImportEncoding(path);
        viewModel.InvalidateGeneratedOutput();
        viewModel.StatusText = "Import characters count = " + count;
    }

    private void ExportCodepageDebug_Click(object sender, RoutedEventArgs e)
    {
        viewModel.ExportCodepageDebug();
    }

    private async void ImportTex_Click(object sender, RoutedEventArgs e)
    {
        string path = await filePicker.OpenFileAsync("Import Tex", ".Tex");
        if (string.IsNullOrWhiteSpace(path)) return;
        await viewModel.ImportTextureAsync(path, TextureWorkflowFormat.Tex);
    }

    private async void ImportPng_Click(object sender, RoutedEventArgs e)
    {
        string path = await filePicker.OpenFileAsync("Import PNG", ".png");
        if (string.IsNullOrWhiteSpace(path)) return;
        await viewModel.ImportTextureAsync(path, TextureWorkflowFormat.Png);
    }

    private async void ConvertTexToPng_Click(object sender, RoutedEventArgs e)
    {
        string texPath = await filePicker.OpenFileAsync("Open Tex", ".Tex");
        if (string.IsNullOrWhiteSpace(texPath)) return;
        string pngPath = await filePicker.SaveFileAsync("Save PNG", Path.GetFileNameWithoutExtension(texPath), ".png");
        if (string.IsNullOrWhiteSpace(pngPath)) return;
        await viewModel.ConvertTexToPngAsync(texPath, pngPath);
    }

    private async void ConvertPngToTex_Click(object sender, RoutedEventArgs e)
    {
        string pngPath = await filePicker.OpenFileAsync("Open PNG", ".png");
        if (string.IsNullOrWhiteSpace(pngPath)) return;
        string texPath = await filePicker.SaveFileAsync("Save Tex", Path.GetFileNameWithoutExtension(pngPath), ".Tex");
        if (string.IsNullOrWhiteSpace(texPath)) return;
        await viewModel.ConvertPngToTexAsync(pngPath, texPath);
    }

    private void DetectIni_Click(object sender, RoutedEventArgs e)
    {
        FalloutEnvironmentInfo info = FalloutEnvironmentService.Detect();
        falloutIniPath = info.IniPath;
        falloutFontPath = info.FontPath;
        if (info.IniAvailable)
        {
            viewModel.LoadIniState(falloutIniPath, falloutFontPath);
        }
        else
        {
            viewModel.StatusText = "FALLOUT.INI Not Found.";
        }
    }

    private void DefaultIni_Click(object sender, RoutedEventArgs e)
    {
        viewModel.SetDefaultIniSelections();
    }

    private async void LoadIni_Click(object sender, RoutedEventArgs e)
    {
        string path = await filePicker.OpenFileAsync("Load INI", ".ini");
        if (string.IsNullOrWhiteSpace(path)) return;
        bool hasCurrentIni = !string.IsNullOrWhiteSpace(falloutIniPath);
        if (string.IsNullOrWhiteSpace(falloutFontPath))
        {
            falloutFontPath = Path.GetDirectoryName(path) ?? AppContext.BaseDirectory;
        }

        if (!hasCurrentIni)
        {
            falloutIniPath = path;
            viewModel.LoadIniState(falloutIniPath, falloutFontPath);
        }
        else
        {
            viewModel.CopyIniSlotsFrom(path);
        }
    }

    private async void SaveIni_Click(object sender, RoutedEventArgs e)
    {
        string path = await filePicker.SaveFileAsync("Save INI", "Fallout3Fonts", ".ini");
        if (string.IsNullOrWhiteSpace(path)) return;
        viewModel.SaveIniSlots(path);
    }

    private void IniSlotComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (syncing || sender is not ComboBox comboBox || comboBox.DataContext is not IniSlotViewModel slot)
        {
            return;
        }

        viewModel.WriteIniSlot(slot.Index);
    }

    private void AtlasImage_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        PointerPoint pointer = e.GetCurrentPoint(AtlasContentGrid);
        PointerPointProperties properties = pointer.Properties;
        bool right = properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased;
        bool left = properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased;
        if (!left && !right)
        {
            return;
        }

        Point imagePosition = e.GetCurrentPoint(AtlasImage).Position;
        if (!TryGetAtlasPixelPosition(imagePosition, out int x, out int y))
        {
            HideGlyphHover();
            return;
        }

        GlyphInteractionResult result = viewModel.HandleGlyphPointer(x, y, true, right, true);
        ShowGlyphHover(result, e.GetCurrentPoint(RootGrid).Position);
        e.Handled = true;
    }

    private void AtlasImage_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        Point imagePosition = e.GetCurrentPoint(AtlasImage).Position;
        if (!TryGetAtlasPixelPosition(imagePosition, out int x, out int y))
        {
            HideGlyphHover();
            return;
        }

        GlyphInteractionResult result = viewModel.HandleGlyphPointer(x, y, false, false, false);
        ShowGlyphHover(result, e.GetCurrentPoint(RootGrid).Position);
    }

    private void AtlasImage_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        HideGlyphHover();
    }

    private bool TryGetAtlasPixelPosition(Point imagePosition, out int x, out int y)
    {
        x = 0;
        y = 0;
        if (viewModel?.TextImageSize.Width <= 0 || viewModel.TextImageSize.Height <= 0)
        {
            return false;
        }

        double displayWidth = AtlasImage.ActualWidth > 0 ? AtlasImage.ActualWidth : viewModel.TextImageSize.Width;
        double displayHeight = AtlasImage.ActualHeight > 0 ? AtlasImage.ActualHeight : viewModel.TextImageSize.Height;
        if (displayWidth <= 0 || displayHeight <= 0)
        {
            return false;
        }

        if (imagePosition.X < 0 || imagePosition.Y < 0 || imagePosition.X >= displayWidth || imagePosition.Y >= displayHeight)
        {
            return false;
        }

        x = (int)Math.Floor(imagePosition.X * viewModel.TextImageSize.Width / displayWidth);
        y = (int)Math.Floor(imagePosition.Y * viewModel.TextImageSize.Height / displayHeight);
        x = Math.Clamp(x, 0, viewModel.TextImageSize.Width - 1);
        y = Math.Clamp(y, 0, viewModel.TextImageSize.Height - 1);
        return true;
    }

    private void ShowGlyphHover(GlyphInteractionResult result, Point windowPosition)
    {
        if (result == null || !result.HasGlyph)
        {
            HideGlyphHover();
            return;
        }

        double scaleX = viewModel.TextImageSize.Width > 0
            ? (AtlasImage.ActualWidth > 0 ? AtlasImage.ActualWidth : viewModel.TextImageSize.Width) / viewModel.TextImageSize.Width
            : 1d;
        double scaleY = viewModel.TextImageSize.Height > 0
            ? (AtlasImage.ActualHeight > 0 ? AtlasImage.ActualHeight : viewModel.TextImageSize.Height) / viewModel.TextImageSize.Height
            : 1d;

        HoverGlyphBorder.Width = Math.Max(1d, result.Hit.Bounds.Width * scaleX);
        HoverGlyphBorder.Height = Math.Max(1d, result.Hit.Bounds.Height * scaleY);
        Canvas.SetLeft(HoverGlyphBorder, result.Hit.Bounds.Left * scaleX);
        Canvas.SetTop(HoverGlyphBorder, result.Hit.Bounds.Top * scaleY);
        HoverGlyphBorder.Visibility = Visibility.Visible;

        GlyphToolTipTextBlock.Text = result.ToolTip;
        GlyphToolTipBorder.Measure(new Size(280d, double.PositiveInfinity));
        Size desired = GlyphToolTipBorder.DesiredSize;
        double rootWidth = RootGrid.ActualWidth > 0 ? RootGrid.ActualWidth : 1100d;
        double rootHeight = RootGrid.ActualHeight > 0 ? RootGrid.ActualHeight : 720d;
        double offsetX = windowPosition.X + 14d;
        double offsetY = windowPosition.Y + 14d;

        if (offsetX + desired.Width > rootWidth - 8d)
        {
            offsetX = windowPosition.X - desired.Width - 14d;
        }

        if (offsetY + desired.Height > rootHeight - 8d)
        {
            offsetY = windowPosition.Y - desired.Height - 14d;
        }

        GlyphToolTipPopup.HorizontalOffset = Math.Clamp(offsetX, 8d, Math.Max(8d, rootWidth - desired.Width - 8d));
        GlyphToolTipPopup.VerticalOffset = Math.Clamp(offsetY, 8d, Math.Max(8d, rootHeight - desired.Height - 8d));
        GlyphToolTipPopup.IsOpen = true;
    }

    private void HideGlyphHover()
    {
        HoverGlyphBorder.Visibility = Visibility.Collapsed;
        GlyphToolTipPopup.IsOpen = false;
    }

    private void UpdateAtlasSurfaceSize()
    {
        if (viewModel?.AtlasBitmap == null || viewModel.TextImageSize.Width <= 0 || viewModel.TextImageSize.Height <= 0)
        {
            AtlasImage.Width = double.NaN;
            AtlasImage.Height = double.NaN;
            AtlasContentGrid.Width = double.NaN;
            AtlasContentGrid.Height = double.NaN;
            AtlasOverlay.Width = double.NaN;
            AtlasOverlay.Height = double.NaN;
            HideGlyphHover();
            return;
        }

        AtlasImage.Width = viewModel.TextImageSize.Width;
        AtlasImage.Height = viewModel.TextImageSize.Height;
        AtlasContentGrid.Width = viewModel.TextImageSize.Width;
        AtlasContentGrid.Height = viewModel.TextImageSize.Height;
        AtlasOverlay.Width = viewModel.TextImageSize.Width;
        AtlasOverlay.Height = viewModel.TextImageSize.Height;
        HideGlyphHover();
    }

    private async Task ShowMessageAsync(string message)
    {
        ContentDialog dialog = new ContentDialog
        {
            Title = "DC Font Generator",
            Content = message,
            CloseButtonText = "确定",
            XamlRoot = DialogRoot
        };
        await dialog.ShowAsync();
    }

    private static void SetButtonColor(Button button, WinUIColor color)
    {
        button.Background = new SolidColorBrush(color);
    }

    private void ApplyRangeSelectionFromInputs()
    {
        viewModel.SelectRange(
            RangeStartTextBox.Text,
            RangeEndTextBox.Text,
            SelectSingleByteCheckBox.IsChecked == true,
            viewModel.DoubleByteFontEnabled && SelectDoubleByteCheckBox.IsChecked == true);
        if (viewModel.HasAtlas)
        {
            viewModel.RefreshSelectionOverlay();
        }
    }

    private void UpdateIniLinkControls(FontSectionViewState state)
    {
        CheckBox[] checkBoxes = GetIniLinkCheckBoxes();
        for (int i = 0; i < checkBoxes.Length; i++)
        {
            bool hasState = i < state.IniLinks.Count;
            checkBoxes[i].IsEnabled = hasState && state.IniLinks[i].Enabled;
            checkBoxes[i].IsChecked = hasState && state.IniLinks[i].Checked;
        }
    }

    private CheckBox[] GetIniLinkCheckBoxes()
    {
        return new[]
        {
            IniLink1CheckBox,
            IniLink2CheckBox,
            IniLink3CheckBox,
            IniLink4CheckBox,
            IniLink5CheckBox,
            IniLink6CheckBox,
            IniLink7CheckBox,
            IniLink8CheckBox
        };
    }

    private static string NormalizeHex(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        char[] buffer = new char[Math.Min(4, text.Length)];
        int count = 0;
        foreach (char c in text)
        {
            char upper = char.ToUpperInvariant(c);
            bool isHex = upper >= '0' && upper <= '9' || upper >= 'A' && upper <= 'F';
            if (!isHex)
            {
                continue;
            }

            buffer[count++] = upper;
            if (count == 4)
            {
                break;
            }
        }

        return new string(buffer, 0, count);
    }

    private const int GwlWndProc = -4;
    private const uint WmGetMinMaxInfo = 0x0024;

    private delegate IntPtr WndProcDelegate(IntPtr hwnd, uint message, UIntPtr wParam, IntPtr lParam);

    private static IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr newLong)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(hwnd, index, newLong)
            : SetWindowLong32(hwnd, index, newLong);
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hwnd, int index, IntPtr newLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern IntPtr SetWindowLong32(IntPtr hwnd, int index, IntPtr newLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallWindowProc(
        IntPtr previousWindowProc,
        IntPtr hwnd,
        uint message,
        UIntPtr wParam,
        IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }
}
