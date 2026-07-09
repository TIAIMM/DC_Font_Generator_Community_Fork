using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace DC_Font_Generator.WinUI;

internal static class FocusDismissService
{
    private static readonly HashSet<uint> interactivePressedPointers = new HashSet<uint>();

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

        EnableBlankAreaHitTesting(rootElement);

        rootElement.AddHandler(
            UIElement.PointerPressedEvent,
            new PointerEventHandler((_, e) => HandlePointerPressed(e, rootElement, focusTarget)),
            true);
        rootElement.AddHandler(
            UIElement.PointerReleasedEvent,
            new PointerEventHandler((_, e) => HandlePointerReleased(e, rootElement, focusTarget)),
            true);
        rootElement.AddHandler(
            UIElement.PointerCanceledEvent,
            new PointerEventHandler(HandlePointerCanceled),
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
            UseSystemFocusVisuals = false,
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

    private static void EnableBlankAreaHitTesting(DependencyObject root)
    {
        SolidColorBrush transparentBrush = new SolidColorBrush(Colors.Transparent);
        foreach (Panel panel in EnumerateDescendants<Panel>(root))
        {
            if (panel.Background == null)
            {
                panel.Background = transparentBrush;
            }
        }
    }

    private static void HandlePointerPressed(PointerRoutedEventArgs e, UIElement root, UIElement focusTarget)
    {
        uint pointerId = GetPointerId(e, root);
        if (e.OriginalSource is DependencyObject source && IsInsideInteractiveControl(source))
        {
            interactivePressedPointers.Add(pointerId);
            return;
        }

        interactivePressedPointers.Remove(pointerId);
        _ = DismissInputStateAsync(root, focusTarget);
    }

    private static void HandlePointerReleased(PointerRoutedEventArgs e, UIElement root, UIElement focusTarget)
    {
        uint pointerId = GetPointerId(e, root);
        if (interactivePressedPointers.Remove(pointerId))
        {
            return;
        }

        if (e.OriginalSource is DependencyObject source && IsInsideInteractiveControl(source))
        {
            return;
        }

        _ = DismissInputStateAsync(root, focusTarget);
    }

    private static void HandlePointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (sender is UIElement root)
        {
            interactivePressedPointers.Remove(GetPointerId(e, root));
        }
    }

    private static uint GetPointerId(PointerRoutedEventArgs e, UIElement root)
    {
        return e.GetCurrentPoint(root).PointerId;
    }

    private static async Task DismissInputStateAsync(DependencyObject root, UIElement focusTarget)
    {
        await FocusManager.TryFocusAsync(focusTarget, FocusState.Programmatic);
        await CloseCompactNumberBoxFlyoutsAsync(root);
    }

    private static async Task CloseCompactNumberBoxFlyoutsAsync(DependencyObject root)
    {
        List<NumberBox> compactNumberBoxes = new List<NumberBox>();
        foreach (NumberBox numberBox in EnumerateDescendants<NumberBox>(root))
        {
            if (numberBox.SpinButtonPlacementMode == NumberBoxSpinButtonPlacementMode.Compact)
            {
                numberBox.SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden;
                compactNumberBoxes.Add(numberBox);
            }
        }

        if (compactNumberBoxes.Count == 0)
        {
            return;
        }

        await Task.Yield();

        foreach (NumberBox numberBox in compactNumberBoxes)
        {
            if (numberBox.XamlRoot != null)
            {
                numberBox.SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact;
            }
        }
    }

    private static IEnumerable<T> EnumerateDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        if (root == null)
        {
            yield break;
        }

        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                yield return match;
            }

            foreach (T descendant in EnumerateDescendants<T>(child))
            {
                yield return descendant;
            }
        }
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
