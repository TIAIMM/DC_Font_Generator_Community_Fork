using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace DC_Font_Generator
{
    internal sealed class ImportedFontResult
    {
        public bool Success { get; set; }
        public bool IsDoubleByteFont { get; set; }
        public Bitmap Texture { get; set; }
    }

    internal sealed class ImportedFontRequest
    {
        public string Path { get; set; }
        public string FontName { get; set; }
        public Main Target { get; set; }
        public IList<Main> FontSections { get; set; }
        public int SelectedFontIndex { get; set; } = -1;
        public FontEncoding Encoding { get; set; }
        public Array2D.List2D<Fnt_char> CharIndex { get; set; }
        public IProgress<FontProgress> Progress { get; set; }
    }

    internal static class FontImportWorkflowService
    {
        public static string GetImportPath(string selectedPath)
        {
            return Path.ChangeExtension(selectedPath, ".fnt");
        }

        public static string GetImportName(string selectedPath)
        {
            return Path.GetFileNameWithoutExtension(selectedPath);
        }

        public static bool CheckFnt(string path, out bool isDoubleByteFont)
        {
            isDoubleByteFont = false;
            if (!File.Exists(path))
            {
                return false;
            }

            long length = new FileInfo(path).Length;
            if (length == 14632)
            {
                return true;
            }

            if (length == 1362328)
            {
                isDoubleByteFont = true;
                return true;
            }

            return false;
        }

        public static ImportedFontResult Import(ImportedFontRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            Main target = ResolveTarget(request);

            bool isDoubleByteFont;
            if (!CheckFnt(request.Path, out isDoubleByteFont))
            {
                return new ImportedFontResult();
            }

            target.ImportFont1name = request.FontName;
            if (isDoubleByteFont)
            {
                target.ImportFont2name = request.FontName;
            }

            Bitmap texture;
            bool success = target.LoadFnt(
                request.Path,
                true,
                request.CharIndex,
                out texture,
                request.Encoding,
                request.Progress);

            if (!success)
            {
                texture?.Dispose();
                return new ImportedFontResult();
            }

            DisableBandedGlyphs(target.FntFile, request.Encoding);
            return new ImportedFontResult
            {
                Success = true,
                IsDoubleByteFont = isDoubleByteFont,
                Texture = texture
            };
        }

        private static Main ResolveTarget(ImportedFontRequest request)
        {
            if (request.Target != null)
            {
                return request.Target;
            }

            if (request.FontSections == null
                || request.SelectedFontIndex < 0
                || request.SelectedFontIndex >= request.FontSections.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(request.SelectedFontIndex));
            }

            return request.FontSections[request.SelectedFontIndex];
        }

        public static void DisableBandedGlyphs(FL_FONT fontFile, FontEncoding encoding)
        {
            foreach (Fnt_char fnt in fontFile.CharList)
            {
                if (fnt.Enable && encoding.IsBand(fnt.HEX))
                {
                    fnt.Enable = false;
                }
            }
        }
    }

    internal sealed class FontSaveRequest
    {
        public IList<Main> FontSections { get; set; } = Array.Empty<Main>();
        public Bitmap TextImage { get; set; }
        public string TexPath { get; set; }
        public string TexName { get; set; }
        public IList<string> FntPaths { get; set; } = Array.Empty<string>();
        public Encoding Encoding { get; set; }
        public IProgress<FontProgress> Progress { get; set; }
        public FontPerformanceStats PerformanceStats { get; set; }
    }

    internal sealed class FontSaveResult
    {
        public List<string> FontNames { get; } = new List<string>();
        public FontPerformanceStats PerformanceStats { get; set; }
    }

    internal sealed class FontSectionControlResult
    {
        public int SelectedIndex { get; set; }
        public bool Changed { get; set; }
    }

    internal sealed class SavedFontIniSelection
    {
        public int SlotIndex { get; set; }
        public int SelectedIndex { get; set; }
    }

    internal static class FontSaveWorkflowService
    {
        public static string GetTexName(string selectedPath)
        {
            return Path.GetFileNameWithoutExtension(selectedPath);
        }

        public static string GetTexPath(string selectedPath)
        {
            return Path.Combine(
                Path.GetDirectoryName(selectedPath),
                GetTexName(selectedPath) + ".Tex");
        }

        public static string GetFntPath(string selectedPath)
        {
            string fntName = Path.GetFileNameWithoutExtension(selectedPath);
            return Path.Combine(Path.GetDirectoryName(selectedPath), fntName + ".fnt");
        }

        public static string GetDirectory(string path)
        {
            return Path.GetDirectoryName(path);
        }

        public static List<string> GetSuggestedFontNames(IList<Main> fontSections)
        {
            List<string> names = new List<string>(fontSections.Count);
            foreach (Main section in fontSections)
            {
                names.Add(section.name);
            }

            return names;
        }

        public static FontSaveResult Save(FontSaveRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.FontSections.Count != request.FntPaths.Count)
            {
                throw new ArgumentException("Fnt path count does not match font section count.");
            }

            FontSaveResult result = new FontSaveResult();
            FontPerformanceStats stats = request.PerformanceStats ?? new FontPerformanceStats();
            result.PerformanceStats = stats;

            Stopwatch saveTexWatch = Stopwatch.StartNew();
            TextureFileService.SaveTex(request.TexPath, request.TextImage, request.Progress);
            saveTexWatch.Stop();
            stats.Add("SaveTex", saveTexWatch.Elapsed);

            Stopwatch saveFntWatch = Stopwatch.StartNew();
            for (int i = 0; i < request.FontSections.Count; i++)
            {
                Main main = request.FontSections[i];
                string fntPath = request.FntPaths[i];
                string fntName = Path.GetFileNameWithoutExtension(fntPath);
                main.name = fntName;
                main.PictureFileName = request.TexName;
                main.SaveFnt(fntPath, request.Encoding);
                result.FontNames.Add(fntName);
            }
            saveFntWatch.Stop();
            stats.Add("SaveFnt", saveFntWatch.Elapsed);

            return result;
        }

        public static List<SavedFontIniSelection> FindSavedFontIniSelections(
            IList<Main> fontSections,
            IList<string> fontNames,
            IEnumerable<IEnumerable<FontFile>> slotItems)
        {
            List<IEnumerable<FontFile>> slots = new List<IEnumerable<FontFile>>();
            foreach (IEnumerable<FontFile> slot in slotItems)
            {
                slots.Add(slot);
            }

            List<SavedFontIniSelection> selections = new List<SavedFontIniSelection>();
            for (int fontIndex = 0; fontIndex < fontSections.Count && fontIndex < fontNames.Count; fontIndex++)
            {
                Main section = fontSections[fontIndex];
                for (int slot = 0; slot < slots.Count && slot < section.Fallout3INI.Count; slot++)
                {
                    if (!section.Fallout3INI[slot]) continue;

                    int selectedIndex = FalloutIniFontService.FindFontIndex(slots[slot], fontNames[fontIndex]);
                    if (selectedIndex >= 0)
                    {
                        selections.Add(new SavedFontIniSelection
                        {
                            SlotIndex = slot,
                            SelectedIndex = selectedIndex
                        });
                    }
                }
            }

            return selections;
        }
    }

    public sealed class FontLinkCandidate
    {
        public int Index { get; set; }
        public string DisplayName { get; set; }
    }

    internal static class FontLinkService
    {
        public static List<FontLinkCandidate> GetCandidates(IList<Main> sections, int currentIndex)
        {
            List<FontLinkCandidate> candidates = new List<FontLinkCandidate>();
            for (int i = 0; i < sections.Count; i++)
            {
                Main section = sections[i];
                if (section.DCfontLink != -1 || i == currentIndex)
                {
                    continue;
                }

                string name = section.name != ""
                    ? (i + 1) + ". " + section.name
                    : (i + 1) + ". Font" + (i + 1) + " (" + section.font2.FamilyName + "," + section.font2.SizePixels + ")";
                candidates.Add(new FontLinkCandidate { Index = i, DisplayName = name });
            }

            return candidates;
        }

        public static void ApplyLink(IList<Main> sections, int currentIndex, int linkIndex)
        {
            if (currentIndex < 0 || currentIndex >= sections.Count) return;
            if (linkIndex < 0 || linkIndex >= sections.Count) return;
            sections[currentIndex].DCfontLink = linkIndex;
            sections[currentIndex].LinkClone();
        }

        public static int ResolveSelectedIndex(IList<FontLinkCandidate> candidates, int selectedListIndex)
        {
            if (candidates == null || selectedListIndex < 0 || selectedListIndex >= candidates.Count)
            {
                return -1;
            }

            return candidates[selectedListIndex].Index;
        }
    }

    internal sealed class FontSectionRemoveResult
    {
        public int SelectedIndex { get; set; }
        public bool Removed { get; set; }
    }

    internal static class FontSectionService
    {
        public static FontSectionControlResult ApplyControlCommand(
            IList<Main> sections,
            int selectedIndex,
            string command,
            Array2D.List2D<Fnt_char> charIndex,
            Func<int, Main> createMain)
        {
            FontSectionControlResult result = new FontSectionControlResult
            {
                SelectedIndex = selectedIndex
            };

            switch (command)
            {
                case "Up":
                case "Down":
                    FontSectionNavigationResult navigation = FontSectionStateService.Navigate(sections, selectedIndex, command);
                    result.SelectedIndex = navigation.SelectedIndex;
                    result.Changed = navigation.Changed;
                    break;
                case "+":
                    AddSection(sections, createMain);
                    result.SelectedIndex = sections.Count - 1;
                    result.Changed = true;
                    break;
                case "-":
                    FontSectionRemoveResult removeResult = RemoveSection(sections, selectedIndex, charIndex);
                    result.SelectedIndex = removeResult.SelectedIndex;
                    result.Changed = removeResult.Removed;
                    break;
            }

            return result;
        }

        public static Main CreateSection(IList<Main> sections, int id, EventHandler textOverflowHandler)
        {
            Main section = new Main((List<Main>)sections, id);
            if (textOverflowHandler != null)
            {
                section.TextOverFlow += textOverflowHandler;
            }

            return section;
        }

        public static Main AddSection(IList<Main> sections, Func<int, Main> createMain)
        {
            Main section = createMain(sections.Count);
            sections.Add(section);
            return section;
        }

        public static Main ResetSections(IList<Main> sections, Array2D.List2D<Fnt_char> charIndex, Func<int, Main> createMain)
        {
            charIndex.Clear();
            sections.Clear();
            Main section = createMain(0);
            sections.Add(section);
            return section;
        }

        public static FontSectionRemoveResult RemoveSection(IList<Main> sections, int selectedIndex, Array2D.List2D<Fnt_char> charIndex)
        {
            FontSectionRemoveResult result = new FontSectionRemoveResult { SelectedIndex = selectedIndex };
            if (sections.Count == 0 || selectedIndex < 0 || selectedIndex >= sections.Count)
            {
                return result;
            }

            int deletedId = sections[selectedIndex].ID;
            FontDescriptor deletedDoubleByteFont = sections[selectedIndex].font2;
            sections.RemoveAt(selectedIndex);

            foreach (Main section in sections)
            {
                if (section.ID > selectedIndex)
                {
                    section.ID--;
                    foreach (Fnt_char fnt in section.FntFile.CharList)
                    {
                        fnt.ID = section.ID;
                    }
                }

                if (section.DCfontLink == deletedId)
                {
                    section.DCfontLink = -1;
                    section.font2 = deletedDoubleByteFont;
                    section.FntFile.reset(true);
                }
            }

            charIndex.Clear();
            result.SelectedIndex = 0;
            result.Removed = true;
            return result;
        }
    }
}
