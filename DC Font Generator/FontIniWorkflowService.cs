using INI_RW;
using System.Collections.Generic;
using System.Text;

namespace DC_Font_Generator
{
    internal static class FontIniWorkflowService
    {
        public static FontSelectorLoadResult LoadSelectorState(string fontPath, IniFile ini, Encoding encoding)
        {
            return FalloutIniFontService.LoadSelectorState(fontPath, ini, encoding);
        }

        public static void WriteSlot(IniFile ini, int zeroBasedSlot, FontFile font)
        {
            FalloutIniFontService.WriteSlot(ini, zeroBasedSlot, font);
        }

        public static void CopySlots(string sourceIniPath, IniFile targetIni)
        {
            FalloutIniFontService.CopyFontSlots(sourceIniPath, targetIni);
        }

        public static void SaveSlots(string path, IList<FontFile> selectedFonts)
        {
            FalloutIniFontService.SaveFontSlots(path, selectedFonts);
        }

        public static int[] GetDefaultSelections(int slotCount)
        {
            return new int[slotCount];
        }

        public static int ClampSelectedIndex(int selectedIndex, int itemCount)
        {
            if (itemCount <= 0) return -1;
            if (selectedIndex < 0 || selectedIndex >= itemCount) return 0;
            return selectedIndex;
        }
    }
}
