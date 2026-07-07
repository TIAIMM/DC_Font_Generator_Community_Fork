using INI_RW;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DC_Font_Generator
{
    internal sealed class FalloutEnvironmentInfo
    {
        public string GamePath { get; set; } = "";
        public string FontPath { get; set; } = "";
        public string IniPath { get; set; } = "";
        public bool GameInstalled => GamePath != "";
        public bool IniAvailable => IniPath != "";
    }

    internal sealed class FontSelectorLoadResult
    {
        public FontSelectorLoadResult()
        {
            SlotItems = new List<FontFile>[8];
            SelectedIndices = new int[8];
            Errors = new List<string>();
            for (int i = 0; i < SlotItems.Length; i++)
            {
                SlotItems[i] = new List<FontFile>();
            }
        }

        public List<FontFile>[] SlotItems { get; }
        public int[] SelectedIndices { get; }
        public List<string> Errors { get; }
    }

    internal static class FalloutEnvironmentService
    {
        public static FalloutEnvironmentInfo Detect()
        {
            FalloutEnvironmentInfo result = new FalloutEnvironmentInfo();

            string gamePath = GetGamePath();
            if (gamePath != "" && Directory.Exists(gamePath))
            {
                result.GamePath = gamePath;
                result.FontPath = Path.Combine(gamePath, @"Data\textures\Fonts\");
                Directory.CreateDirectory(result.FontPath);
            }

            if (result.GameInstalled)
            {
                string myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string myGamesPath = Path.Combine(myDocumentsPath, "My Games", "Fallout3");
                string iniPath = Path.Combine(myGamesPath, "FALLOUT.INI");
                if (Directory.Exists(myGamesPath) && File.Exists(iniPath))
                {
                    result.IniPath = iniPath;
                }
            }

            return result;
        }

        private static string GetGamePath()
        {
            string path = ReadInstallPath(@"SOFTWARE\Bethesda Softworks\Fallout3");
            if (path != "") return path;
            return ReadInstallPath(@"SOFTWARE\Wow6432Node\Bethesda Softworks\Fallout3");
        }

        private static string ReadInstallPath(string subKey)
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(subKey, false))
            {
                object value = key?.GetValue("Installed Path");
                return value?.ToString() ?? "";
            }
        }
    }

    internal static class FalloutIniFontService
    {
        private static readonly string[] DefaultFonts =
        {
            "Glow_Monofonto_Large.fnt",
            "Monofonto_Large.fnt",
            "Glow_Monofonto_Medium.fnt",
            "Monofonto_VeryLarge02_Dialogs2.fnt",
            "Fixedsys_Comp_uniform_width.fnt",
            "Glow_Monofonto_VL_dialogs.fnt",
            "Baked-in_Monofonto_Large.fnt",
            "Glow_Futura_Caps_Large.fnt"
        };

        public static FontSelectorLoadResult LoadSelectorState(string fontPath, IniFile ini, Encoding encoding)
        {
            FontSelectorLoadResult result = new FontSelectorLoadResult();
            for (int i = 0; i < DefaultFonts.Length; i++)
            {
                result.SlotItems[i].Add(new FontFile(DefaultFonts[i], true, 0, encoding));
            }

            if (!Directory.Exists(fontPath))
            {
                Directory.CreateDirectory(fontPath);
            }

            int index = 1;
            foreach (string path in Directory.GetFiles(fontPath, "*.fnt"))
            {
                FontFile fontFile = new FontFile(path, false, index, encoding);
                if (fontFile.Enable)
                {
                    for (int slot = 0; slot < result.SlotItems.Length; slot++)
                    {
                        result.SlotItems[slot].Add(fontFile);
                    }
                    index++;
                }
                else
                {
                    result.Errors.Add(fontFile.Err);
                }
            }

            if (ini == null)
            {
                return result;
            }

            for (int slot = 0; slot < result.SlotItems.Length; slot++)
            {
                string configuredPath = ini.IniReadValue("Fonts", GetSlotKey(slot));
                string configuredName = Path.GetFileName(configuredPath);
                int selectedIndex = FindFontIndex(result.SlotItems[slot], configuredName);
                if (selectedIndex >= 0)
                {
                    result.SelectedIndices[slot] = selectedIndex;
                }
                else if (configuredName != "")
                {
                    result.Errors.Add(configuredName + " : File does not exist.");
                }
            }

            return result;
        }

        public static int FindFontIndex(IEnumerable<FontFile> fonts, string fntName)
        {
            if (string.IsNullOrEmpty(fntName))
            {
                return -1;
            }

            int index = 0;
            foreach (FontFile font in fonts)
            {
                if (font.IsThis(fntName) || font.IsThis(fntName + ".fnt"))
                {
                    return index;
                }
                index++;
            }

            return -1;
        }

        public static void WriteSlot(IniFile ini, int zeroBasedSlot, FontFile font)
        {
            if (ini == null || font == null) return;
            ini.IniWriteValue("Fonts", GetSlotKey(zeroBasedSlot), font.intFontPath);
        }

        public static void CopyFontSlots(string sourceIniPath, IniFile targetIni)
        {
            if (targetIni == null) return;

            IniFile source = new IniFile(sourceIniPath);
            for (int i = 0; i < 8; i++)
            {
                string value = source.IniReadValue("Fonts", GetSlotKey(i));
                targetIni.IniWriteValue("Fonts", GetSlotKey(i), value);
            }
        }

        public static void SaveFontSlots(string path, IList<FontFile> selectedFonts)
        {
            using (StreamWriter writer = File.CreateText(path))
            {
                writer.WriteLine("[Fonts]");
                for (int i = 0; i < 8; i++)
                {
                    string value = "";
                    if (selectedFonts != null && i < selectedFonts.Count && selectedFonts[i] != null)
                    {
                        value = selectedFonts[i].intFontPath;
                    }
                    writer.WriteLine(GetSlotKey(i) + "=" + value);
                }
            }
        }

        private static string GetSlotKey(int zeroBasedSlot)
        {
            return "sFontFile_" + (zeroBasedSlot + 1);
        }
    }
}
