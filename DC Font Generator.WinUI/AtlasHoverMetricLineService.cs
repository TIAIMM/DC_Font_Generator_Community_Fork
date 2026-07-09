using System;
using System.ComponentModel;
using System.Reflection;
using DC_Font_Generator;
using DC_Font_Generator.WinUI.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace DC_Font_Generator.WinUI;

internal static class AtlasHoverMetricLineService
{
    private const double LineInset = 1d;

    public static void Attach(MainWindow window)
    {
        if (window?.Content is not DependencyObject root)
        {
            return;
        }

        Grid atlasContentGrid = FindElement<Grid>(root, "AtlasContentGrid");
        Image atlasImage = FindElement<Image>(root, "AtlasImage");
        Canvas atlasOverlay = FindElement<Canvas>(root, "AtlasOverlay");
        MainWindowViewModel viewModel = GetViewModel(window);
        if (atlasContentGrid == null || atlasImage == null || atlasOverlay == null || viewModel == null)
        {
            return;
        }

        Line topEdgeLine = new Line
        {
            Stroke = new SolidColorBrush(Microsoft.UI.Colors.Yellow),
            StrokeThickness = 1,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        atlasOverlay.Children.Add(topEdgeLine);

        atlasContentGrid.AddHandler(
            UIElement.PointerMovedEvent,
            new PointerEventHandler((_, e) => UpdateTopEdgeLine(e, atlasContentGrid, atlasImage, topEdgeLine, viewModel)),
            true);
        atlasContentGrid.AddHandler(
            UIElement.PointerReleasedEvent,
            new PointerEventHandler((_, e) => UpdateTopEdgeLine(e, atlasContentGrid, atlasImage, topEdgeLine, viewModel)),
            true);
        atlasContentGrid.AddHandler(
            UIElement.PointerExitedEvent,
            new PointerEventHandler((_, _) => HideTopEdgeLine(topEdgeLine)),
            true);

        if (viewModel is INotifyPropertyChanged notify)
        {
            notify.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainWindowViewModel.AtlasBitmap)
                    || e.PropertyName == nameof(MainWindowViewModel.HasAtlas))
                {
                    HideTopEdgeLine(topEdgeLine);
                }
            };
        }
    }

    private static void UpdateTopEdgeLine(
        PointerRoutedEventArgs e,
        FrameworkElement atlasContentGrid,
        FrameworkElement atlasImage,
        Line topEdgeLine,
        MainWindowViewModel viewModel)
    {
        if (!TryGetAtlasPixelPosition(e.GetCurrentPoint(atlasImage).Position, atlasImage, viewModel, out int x, out int y))
        {
            HideTopEdgeLine(topEdgeLine);
            return;
        }

        GlyphInteractionResult result = viewModel.HandleGlyphPointer(x, y, false, false, false);
        if (result == null || !result.HasGlyph)
        {
            HideTopEdgeLine(topEdgeLine);
            return;
        }

        double scaleX = viewModel.TextImageSize.Width > 0
            ? (atlasImage.ActualWidth > 0 ? atlasImage.ActualWidth : viewModel.TextImageSize.Width) / viewModel.TextImageSize.Width
            : 1d;
        double scaleY = viewModel.TextImageSize.Height > 0
            ? (atlasImage.ActualHeight > 0 ? atlasImage.ActualHeight : viewModel.TextImageSize.Height) / viewModel.TextImageSize.Height
            : 1d;

        double left = result.Hit.Bounds.Left * scaleX;
        double right = result.Hit.Bounds.Right * scaleX;
        double yPosition = (result.Hit.Bounds.Top + result.Hit.EditableGlyph.fTopEdge) * scaleY;
        if (right - left > LineInset * 2d)
        {
            left += LineInset;
            right -= LineInset;
        }

        topEdgeLine.X1 = left;
        topEdgeLine.X2 = right;
        topEdgeLine.Y1 = yPosition;
        topEdgeLine.Y2 = yPosition;
        topEdgeLine.Visibility = Visibility.Visible;
    }

    private static bool TryGetAtlasPixelPosition(
        Point imagePosition,
        FrameworkElement atlasImage,
        MainWindowViewModel viewModel,
        out int x,
        out int y)
    {
        x = 0;
        y = 0;
        if (viewModel?.TextImageSize.Width <= 0 || viewModel.TextImageSize.Height <= 0)
        {
            return false;
        }

        double displayWidth = atlasImage.ActualWidth > 0 ? atlasImage.ActualWidth : viewModel.TextImageSize.Width;
        double displayHeight = atlasImage.ActualHeight > 0 ? atlasImage.ActualHeight : viewModel.TextImageSize.Height;
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

    private static void HideTopEdgeLine(Line topEdgeLine)
    {
        if (topEdgeLine != null)
        {
            topEdgeLine.Visibility = Visibility.Collapsed;
        }
    }

    private static MainWindowViewModel GetViewModel(MainWindow window)
    {
        return typeof(MainWindow)
            .GetField("viewModel", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(window) as MainWindowViewModel;
    }

    private static T FindElement<T>(DependencyObject root, string name) where T : FrameworkElement
    {
        if (root is T element && element.Name == name)
        {
            return element;
        }

        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            T match = FindElement<T>(VisualTreeHelper.GetChild(root, i), name);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
}
