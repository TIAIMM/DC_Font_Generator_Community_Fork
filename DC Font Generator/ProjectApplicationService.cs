using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace DC_Font_Generator
{
    internal sealed class ProjectApplyRequest
    {
        public ProjectDocument Document { get; set; }
        public IList<Main> FontSections { get; set; } = Array.Empty<Main>();
        public string FontPath { get; set; }
        public FontEncoding Encoding { get; set; }
        public Array2D.List2D<Fnt_char> CharIndex { get; set; }
        public Func<int, Main> CreateMain { get; set; }
        public IProgress<FontProgress> Progress { get; set; }
        public Func<string, string> Localize { get; set; } = value => value;
    }

    internal sealed class ProjectApplyResult
    {
        public bool Success { get; set; } = true;
        public int SelectedMainIndex { get; set; }
        public List<string> Logs { get; } = new List<string>();
        public List<Bitmap> ImportedTextures { get; } = new List<Bitmap>();
        public Bitmap LastImportedTexture
        {
            get
            {
                if (ImportedTextures.Count == 0) return null;
                return ImportedTextures[ImportedTextures.Count - 1];
            }
        }
    }

    internal static class ProjectApplicationService
    {
        public static ProjectApplyResult ApplyFontSections(ProjectApplyRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Document == null) throw new ArgumentNullException(nameof(request.Document));

            ProjectApplyResult result = new ProjectApplyResult();
            EnsureFontSections(request.FontSections, request.Document.FontListCount, request.CreateMain);

            foreach (ProjectFontSection section in request.Document.FontSections)
            {
                if (!ApplyFontSection(request, section, result))
                {
                    result.Success = false;
                }
            }

            if (request.Document.FontSections.Count > 0)
            {
                result.SelectedMainIndex = request.Document.FontSections[request.Document.FontSections.Count - 1].ID;
            }

            return result;
        }

        public static bool ApplyPostAmendments(IList<Main> fontSections, IList<PostAmendment> postAmendments, IList<string> logs, Func<string, string> localize)
        {
            bool ok = true;
            foreach (PostAmendment amendment in postAmendments)
            {
                if (amendment.IsEmpty) continue;
                int id = amendment.ID;
                if (id < 0 || id >= fontSections.Count)
                {
                    ok = false;
                    continue;
                }

                Main main = fontSections[id];
                if (amendment.fBaseLineFixed != 0)
                {
                    main.FntFile.Header.fBaseLine += amendment.fBaseLineFixed;
                }

                foreach (string hex in amendment.index)
                {
                    FntFixed fixedValue = amendment[hex];
                    Fnt_char fnt = (Fnt_char)main.FntFile.CharCode[hex];
                    if (fnt == null)
                    {
                        logs.Add("Project Load : (" + hex + ") " + localize("Code does not exist!"));
                        ok = false;
                        continue;
                    }

                    if (!fnt.Enable) continue;
                    fnt.fTopEdge += fixedValue.fTopEdgeFixed;
                    fnt.fTopEdgeFixed = fixedValue.fTopEdgeFixed;
                    fnt.fHeight += fixedValue.fHeightFixed;
                    fnt.fHeightFixed = fixedValue.fHeightFixed;
                    fnt.fWidth += fixedValue.fWidthFixed;
                    fnt.fWidthFixed = fixedValue.fWidthFixed;
                    if (main.fixedFont) continue;
                    fnt.fLeadingEdge += fixedValue.fLeadingEdgeFixed;
                    fnt.fLeadingEdgeFixed = fixedValue.fLeadingEdgeFixed;
                    fnt.fSpacing += fixedValue.fSpacingFixed;
                    fnt.fSpacingFixed = fixedValue.fSpacingFixed;
                }
            }

            return ok;
        }

        public static void LinkCloneAll(IList<Main> fontSections)
        {
            foreach (Main main in fontSections)
            {
                main.LinkClone();
            }
        }

        public static void ApplyFixedFonts(IList<Main> fontSections)
        {
            foreach (Main main in fontSections)
            {
                main.FixedFont(main.fixedFont, main.FontMaxWidth);
            }
        }

        private static void EnsureFontSections(IList<Main> fontSections, int count, Func<int, Main> createMain)
        {
            if (count <= 1)
            {
                return;
            }

            for (int id = fontSections.Count; id < count; id++)
            {
                fontSections.Add(createMain(id));
            }
        }

        private static bool ApplyFontSection(ProjectApplyRequest request, ProjectFontSection section, ProjectApplyResult result)
        {
            bool ok = true;
            if (section.ID < 0 || section.ID >= request.FontSections.Count)
            {
                return false;
            }

            Main main = request.FontSections[section.ID];

            if (section.ImportFontName != "")
            {
                string path = Path.Combine(request.FontPath, section.ImportFontName + ".fnt");
                ImportedFontResult importResult = FontImportWorkflowService.Import(new ImportedFontRequest
                {
                    Path = path,
                    FontName = section.ImportFontName,
                    Target = main,
                    Encoding = request.Encoding,
                    CharIndex = request.CharIndex,
                    Progress = request.Progress
                });

                if (importResult.Success)
                {
                    result.ImportedTextures.Add(importResult.Texture);
                }
                else
                {
                    result.Logs.Add(request.Localize("Project font error : Missing Fallout3 Font file.") + "(" + section.ImportFontName + ".fnt)");
                    ok = false;
                }
            }
            else if (section.HasSCFont)
            {
                ok &= TryApplyProjectFont(main, section.SCFontName, section.SCFontSize, section.SCFontStyle, true, result, request.Localize);
            }

            if (section.HasDCFontLink)
            {
                main.DCfontLink = section.DCFontLink;
            }
            else if (section.HasDCFont)
            {
                ok &= TryApplyProjectFont(main, section.DCFontName, section.DCFontSize, section.DCFontStyle, false, result, request.Localize);
            }

            if (section.HasFntName) main.name = section.FntName;
            if (section.HasGlow) main.Glow = section.Glow;
            if (section.HasGlowColor) main.GlowColor = Color.FromArgb(section.GlowColorArgb);
            if (section.HasOutline) main.Outline = section.Outline;
            if (section.HasOutlineColor) main.OutlineColor = Color.FromArgb(section.OutlineColorArgb);
            if (section.HasFontColor) main.FontColor = Color.FromArgb(section.FontColorArgb);
            if (section.FixedFont)
            {
                main.fixedFont = true;
                main.FontMaxWidth = section.FontMaxWidth;
            }

            for (int i = 0; i < 8; i++)
            {
                main.Fallout3INI[i] = section.Fallout3INI[i];
            }

            return ok;
        }

        private static bool TryApplyProjectFont(
            Main main,
            string fontName,
            float fontSize,
            FontStyle fontStyle,
            bool singleByteFont,
            ProjectApplyResult result,
            Func<string, string> localize)
        {
            Font font = new Font(fontName, fontSize, fontStyle);
            if (!font.FontFamily.IsStyleAvailable(fontStyle))
            {
                font.Dispose();
                result.Logs.Add(localize("Project font error : Missing Font.") + "(" + fontName + ")");
                return false;
            }

            if (singleByteFont)
            {
                main.font1 = FontDescriptor.FromGdiFont(font);
            }
            else
            {
                main.font2 = FontDescriptor.FromGdiFont(font);
            }

            return true;
        }
    }
}
