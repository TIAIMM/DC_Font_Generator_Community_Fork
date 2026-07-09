using System;
using System.ComponentModel;
using DC_Font_Generator;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace DC_Font_Generator.WinUI;

public sealed partial class MainWindow
{
    private const double HoverTopEdgeLineInset = 1d;
    private Line hoverTopEdgeLine;

    internal void AttachHoverMetricLine()
    {
        if (Content is not FrameworkElement root)
        {
            return;
        }

        if (root.IsLoaded)
        {
            InitializeHoverMetricLine();
            return;
        }

        root.Loaded += Root_LoadedAttachHoverMetricLine;
    }

    private void Root_LoadedAttachHoverMetricLine(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement root)
        {
            root.Loaded -= Root_LoadedAttachHoverMetricLine;
        }

        InitializeHoverMetricLine();
    }

    private void InitializeHoverMetricLine()
    {
        if (hoverTopEdgeLine != null)
        {
            return;
        }

        hoverTopEdgeLine = new Line
        {
            Stroke = new SolidColorBrush(Microsoft.UI.Colors.Yellow),
            StrokeThickness = 1,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        Canvas.SetLeft(hoverTopEdgeLine, 0d);
        Canvas.SetTop(hoverTopEdgeLine, 0d);
        Canvas.SetZIndex(hoverTopEdgeLine, 1000);
        AtlasOverlay.Children.Add(hoverTopEdgeLine);

        AtlasContentGrid.AddHandler(
            UIElement.PointerMovedEvent,
            new PointerEventHandler(AtlasHoverMetricLine_PointerMoved),
            true);
        AtlasContentGrid.AddHandler(
            UIElement.PointerReleasedEvent,
            new PointerEventHandler(AtlasHoverMetricLine_PointerReleased),
            true);
        AtlasContentGrid.AddHandler(
            UIElement.PointerExitedEvent,
            new PointerEventHandler(AtlasHoverMetricLine_PointerExited),
            true);

        viewModel.PropertyChanged += ViewModel_PropertyChangedHideHoverMetricLine;
    }

    private void AtlasHoverMetricLine_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        UpdateHoverTopEdgeLine(e);
    }

    private void AtlasHoverMetricLine_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        UpdateHoverTopEdgeLine(e);
    }

    private void AtlasHoverMetricLine_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        HideHoverTopEdgeLine();
    }

    private void ViewModel_PropertyChangedHideHoverMetricLine(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.MainWindowViewModel.AtlasBitmap)
            || e.PropertyName == nameof(ViewModels.MainWindowViewModel.HasAtlas))
        {
            HideHoverTopEdgeLine();
        }
    }

    private void UpdateHoverTopEdgeLine(PointerRoutedEventArgs e)
    {
        if (hoverTopEdgeLine == null)
        {
            return;
        }

        Point imagePosition = e.GetCurrentPoint(AtlasContentGrid).Position;
        if (!TryGetAtlasPixelPosition(imagePosition, out int x, out int y))
        {
            HideHoverTopEdgeLine();
            return;
        }

        GlyphInteractionResult result = viewModel.HandleGlyphPointer(x, y, false, false, false);
        if (result == null || !result.HasGlyph)
        {
            HideHoverTopEdgeLine();
            return;
        }

        ShowHoverTopEdgeLine(result);
    }

    private void ShowHoverTopEdgeLine(GlyphInteractionResult result)
    {
        double scaleX = viewModel.TextImageSize.Width > 0
            ? (AtlasImage.ActualWidth > 0 ? AtlasImage.ActualWidth : viewModel.TextImageSize.Width) / viewModel.TextImageSize.Width
            : 1d;
        double scaleY = viewModel.TextImageSize.Height > 0
            ? (AtlasImage.ActualHeight > 0 ? AtlasImage.ActualHeight : viewModel.TextImageSize.Height) / viewModel.TextImageSize.Height
            : 1d;

        double left = result.Hit.Bounds.Left * scaleX;
        double right = result.Hit.Bounds.Right * scaleX;
        double yPosition = (result.Hit.Bounds.Top + result.Hit.EditableGlyph.fTopEdge) * scaleY;
        if (right - left > HoverTopEdgeLineInset * 2d)
        {
            left += HoverTopEdgeLineInset;
            right -= HoverTopEdgeLineInset;
        }

        hoverTopEdgeLine.X1 = left;
        hoverTopEdgeLine.X2 = right;
        hoverTopEdgeLine.Y1 = yPosition;
        hoverTopEdgeLine.Y2 = yPosition;
        hoverTopEdgeLine.Visibility = Visibility.Visible;
    }

    private void HideHoverTopEdgeLine()
    {
        if (hoverTopEdgeLine != null)
        {
            hoverTopEdgeLine.Visibility = Visibility.Collapsed;
        }
    }
}
