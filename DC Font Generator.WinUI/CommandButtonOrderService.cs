using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DC_Font_Generator.WinUI;

internal static class CommandButtonOrderService
{
    public static void Apply(Window window)
    {
        if (window?.Content is not DependencyObject root)
        {
            return;
        }

        Grid.SetColumn(FindElement<Button>(root, "RenderButton"), 0);
        Grid.SetColumn(FindElement<Button>(root, "SaveFontButton"), 1);
        Grid.SetColumn(FindElement<Button>(root, "LoadProjectButton"), 2);
        Grid.SetColumn(FindElement<Button>(root, "SaveProjectButton"), 3);
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
