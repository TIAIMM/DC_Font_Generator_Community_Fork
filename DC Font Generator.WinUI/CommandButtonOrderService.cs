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

        ConfigureButton(root, "RenderButton", 0, "渲染");
        ConfigureButton(root, "SaveFontButton", 1, "保存字体");
        ConfigureButton(root, "LoadProjectButton", 2, "加载项目");
        ConfigureButton(root, "SaveProjectButton", 3, "保存项目");
    }

    private static void ConfigureButton(DependencyObject root, string name, int column, string content)
    {
        Button button = FindElement<Button>(root, name);
        if (button == null)
        {
            return;
        }

        Grid.SetColumn(button, column);
        button.Content = content;
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
