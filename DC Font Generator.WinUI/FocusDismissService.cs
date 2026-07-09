using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace DC_Font_Generator.WinUI;

internal static class FocusDismissService
{
    public static void Attach(Window window)
    {
        if (window?.Content is not FrameworkElement rootElement)
        {
            return;
        }

        UIElement focusTarget = CreateFocusTarget(rootElement);
        if (focusTarget == null)
        {
            return;
        }

        if (rootElement is Panel panel && panel.Background == null)
        {
            panel.Background = new SolidColorBrush(Colors.Transparent);
        }

        rootElement.AddHandler(
            UIElement.PointerPressedEvent,
            new PointerEventHandler((_, e) => HandlePointerPressed(e, focusTarget)),
            true);
    }

    private static UIElement CreateFocusTarget(FrameworkElement rootElement)
    {
        Button focusSink = new Button
        {
            Width = 1,
            Height = 1,
            Opacity = 0,
            IsTabStop = true,
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(-8, -8, 0, 0)
        };

        if (rootElement is Panel panel)
        {
            panel.Children.Add(focusSink);
            return focusSink;
        }

        return null;
    }

    private static void HandlePointerPressed(PointerRoutedEventArgs e, UIElement focusTarget)
    {
        if (e.OriginalSource is DependencyObject source && IsInsideInteractiveControl(source))
        {
            return;
        }

        _ = FocusManager.TryFocusAsync(focusTarget, FocusState.Programmatic);
    }

    private static bool IsInsideInteractiveControl(DependencyObject source)
    {
        DependencyObject current = source;
        while (current != null)
        {
            if (current is TextBox
                || current is NumberBox
                || current is ComboBox
                || current is ButtonBase
                || current is Slider
                || current is ScrollBar
                || current is ListViewBase)
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }
}
