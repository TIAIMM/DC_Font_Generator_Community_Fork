using System.Text;
using DC_Font_Generator;
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

        LanguageData language = new LanguageData(Encoding.Default);

        ConfigureButton(root, "RenderButton", 0, language.GetString("Render"));
        ConfigureButton(root, "SaveFontButton", 1, language.GetString("Save Font"));
        ConfigureButton(root, "LoadProjectButton", 2, language.GetString("Load Project"));
        ConfigureButton(root, "SaveProjectButton", 3, language.GetString("Save Project"));
        ConfigureEncodingComboBox(root, language);
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

    private static void ConfigureEncodingComboBox(DependencyObject root, LanguageData language)
    {
        ComboBox comboBox = FindElement<ComboBox>(root, "EncodingComboBox");
        if (comboBox == null)
        {
            return;
        }

        int selectedIndex = comboBox.SelectedIndex;
        comboBox.ItemsSource = new[]
        {
            language.GetString("Encoding ANSI"),
            language.GetString("Encoding 932 Japanese"),
            language.GetString("Encoding 936 Simplified Chinese"),
            language.GetString("Encoding 949 Korean"),
            language.GetString("Encoding 950 Traditional Chinese"),
            language.GetString("Encoding 936 GBK"),
            language.GetString("Encoding 1252 Windows")
        };

        if (selectedIndex >= 0)
        {
            comboBox.SelectedIndex = selectedIndex;
        }
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
