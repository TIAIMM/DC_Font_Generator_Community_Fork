using System.Collections.Generic;
using DC_Font_Generator;

namespace DC_Font_Generator.WinUI.ViewModels;

internal sealed class IniSlotViewModel : ObservableObject
{
    private int selectedIndex;

    public IniSlotViewModel(int index)
    {
        Index = index;
        DisplayName = $"{index + 1}.";
    }

    public int Index { get; }
    public string DisplayName { get; }
    public List<FontFile> Items { get; } = new List<FontFile>();

    public int SelectedIndex
    {
        get => selectedIndex;
        set => SetProperty(ref selectedIndex, value);
    }

    public FontFile SelectedFont =>
        selectedIndex >= 0 && selectedIndex < Items.Count ? Items[selectedIndex] : null;
}
