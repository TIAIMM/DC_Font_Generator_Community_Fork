using System;
using System.Collections.Generic;
using System.Drawing;

namespace DC_Font_Generator
{
    internal sealed class GlyphRangeSelectionRequest
    {
        public IList<Main> FontSections { get; set; } = Array.Empty<Main>();
        public int SelectedFontIndex { get; set; }
        public GlyphSelectionState Selection { get; set; }
        public string StartHex { get; set; }
        public string EndHex { get; set; }
        public bool IncludeSingleByte { get; set; }
        public bool IncludeDoubleByte { get; set; }
    }

    internal sealed class GlyphSetSelectionRequest
    {
        public IList<Main> FontSections { get; set; } = Array.Empty<Main>();
        public int SelectedFontIndex { get; set; }
        public GlyphSelectionState Selection { get; set; }
        public bool Selected { get; set; }
    }

    internal sealed class GlyphAdjustmentWorkflowRequest
    {
        public IList<Main> FontSections { get; set; } = Array.Empty<Main>();
        public int SelectedFontIndex { get; set; }
        public GlyphSelectionState Selection { get; set; }
        public bool FixedFont { get; set; }
        public bool LeftSpacing { get; set; }
        public bool RightSpacing { get; set; }
        public bool LineSpacing { get; set; }
        public bool BottomAlign { get; set; }
        public bool Scale { get; set; }
        public string Command { get; set; }
        public float Increment { get; set; }
    }

    internal sealed class GlyphAdjustmentWorkflowResult
    {
        public bool Applied { get; set; }
        public bool MissingSelection { get; set; }
    }

    internal static class GlyphSelectionWorkflowService
    {
        public static void RenderPreview(Bitmap target, string text, IList<Main> fontSections, int selectedFontIndex)
        {
            Main section;
            if (!TryGetSection(fontSections, selectedFontIndex, out section))
            {
                return;
            }

            GlyphPreviewRenderer.Render(target, text, section, fontSections);
        }

        public static void SelectRange(GlyphRangeSelectionRequest request)
        {
            Main section;
            if (!TryGetSection(request.FontSections, request.SelectedFontIndex, out section) || request.Selection == null)
            {
                return;
            }

            request.Selection.SelectRange(
                section.FntFile,
                request.StartHex,
                request.EndHex,
                request.IncludeSingleByte,
                request.IncludeDoubleByte);
        }

        public static bool SetSingleByteSelection(GlyphSetSelectionRequest request)
        {
            Main section;
            if (!TryGetSection(request.FontSections, request.SelectedFontIndex, out section) || request.Selection == null)
            {
                return false;
            }

            request.Selection.SetSingleByteSelection(section.FntFile, request.Selected);
            return true;
        }

        public static bool SetDoubleByteSelection(GlyphSetSelectionRequest request)
        {
            Main section;
            if (!TryGetSection(request.FontSections, request.SelectedFontIndex, out section) || request.Selection == null)
            {
                return false;
            }

            request.Selection.SetDoubleByteSelection(section.FntFile, request.Selected);
            return true;
        }

        public static GlyphAdjustmentWorkflowResult ApplyAdjustment(GlyphAdjustmentWorkflowRequest request)
        {
            Main section;
            if (!TryGetSection(request.FontSections, request.SelectedFontIndex, out section) || request.Selection == null)
            {
                return new GlyphAdjustmentWorkflowResult();
            }

            GlyphAdjustmentTarget target = GetAdjustmentTarget(request);
            if (!request.Selection.HasAdjustableSelection && target != GlyphAdjustmentTarget.LineSpacing)
            {
                return new GlyphAdjustmentWorkflowResult { MissingSelection = true };
            }

            float delta = GetAdjustmentDelta(request.Command, request.Increment);
            bool applied = GlyphAdjustmentService.Apply(
                section,
                request.Selection.GetAdjustableFonts(),
                target,
                delta);
            return new GlyphAdjustmentWorkflowResult { Applied = applied };
        }

        public static void RestoreAdjustment(IList<Main> fontSections, int selectedFontIndex, GlyphSelectionState selection)
        {
            Main section;
            if (!TryGetSection(fontSections, selectedFontIndex, out section) || selection == null)
            {
                return;
            }

            GlyphAdjustmentService.Restore(section, selection.GetAdjustableFonts());
        }

        private static GlyphAdjustmentTarget GetAdjustmentTarget(GlyphAdjustmentWorkflowRequest request)
        {
            if (request.LeftSpacing && !request.FixedFont) return GlyphAdjustmentTarget.LeftSpacing;
            if (request.RightSpacing && !request.FixedFont) return GlyphAdjustmentTarget.RightSpacing;
            if (request.LineSpacing) return GlyphAdjustmentTarget.LineSpacing;
            if (request.BottomAlign) return GlyphAdjustmentTarget.BottomAlign;
            if (request.Scale) return GlyphAdjustmentTarget.Scale;
            return GlyphAdjustmentTarget.None;
        }

        private static float GetAdjustmentDelta(string command, float increment)
        {
            if (command == "Dec") return -increment;
            if (command == "Add") return increment;
            return 0;
        }

        private static bool TryGetSection(IList<Main> fontSections, int selectedFontIndex, out Main section)
        {
            section = null;
            if (fontSections == null || selectedFontIndex < 0 || selectedFontIndex >= fontSections.Count)
            {
                return false;
            }

            section = fontSections[selectedFontIndex];
            return true;
        }
    }
}
