using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml;

namespace DC_Font_Generator
{
    public sealed class ProjectSaveRequest
    {
        public int EncodingIndex { get; set; }
        public int SizeXIndex { get; set; }
        public int SizeYIndex { get; set; }
        public string TexFileName { get; set; }
        public decimal Gap { get; set; }
        public int BackGroundColorArgb { get; set; }
        public int ArrangeMethod { get; set; }
        public IList<Main> FontSections { get; set; } = Array.Empty<Main>();
    }

    public sealed class ProjectDocument
    {
        public int EncodingIndex { get; set; }
        public int SizeXIndex { get; set; } = -1;
        public int SizeYIndex { get; set; } = -1;
        public string TexFileName { get; set; }
        public decimal Gap { get; set; }
        public int BackGroundColorArgb { get; set; }
        public int ArrangeMethod { get; set; }
        public int FontListCount { get; set; } = 1;
        public List<ProjectFontSection> FontSections { get; } = new List<ProjectFontSection>();
        public List<PostAmendment> PostAmendments { get; } = new List<PostAmendment>();
    }

    public sealed class ProjectFontSection
    {
        public int ID { get; set; }
        public string ImportFontName { get; set; } = "";
        public string SCFontName { get; set; }
        public float SCFontSize { get; set; }
        public FontStyle SCFontStyle { get; set; } = FontStyle.Regular;
        public string SCFontDescriptor { get; set; }
        public string DCFontName { get; set; }
        public float DCFontSize { get; set; }
        public FontStyle DCFontStyle { get; set; } = FontStyle.Regular;
        public string DCFontDescriptor { get; set; }
        public bool HasSCFont { get; set; }
        public bool HasDCFont { get; set; }
        public bool HasDCFontLink { get; set; }
        public int DCFontLink { get; set; } = -1;
        public string FntName { get; set; } = "";
        public int Glow { get; set; }
        public bool HasGlow { get; set; }
        public int GlowColorArgb { get; set; }
        public bool HasGlowColor { get; set; }
        public int Outline { get; set; }
        public bool HasOutline { get; set; }
        public int OutlineColorArgb { get; set; }
        public bool HasOutlineColor { get; set; }
        public int FontColorArgb { get; set; }
        public bool HasFontColor { get; set; }
        public bool HasFntName { get; set; }
        public bool[] Fallout3INI { get; } = new bool[8];
        public bool FixedFont { get; set; }
        public float FontMaxWidth { get; set; }
        public bool HasUseProportionalDoubleByteSpacing { get; set; }
        public bool UseProportionalDoubleByteSpacing { get; set; }
        public bool HasUseManualBaseLine { get; set; }
        public bool UseManualBaseLine { get; set; }
        public bool HasManualBaseLine { get; set; }
        public float ManualBaseLine { get; set; }
    }

    public static class ProjectSerializationService
    {
        public static string GetSavePath(string selectedPath)
        {
            string filename = Path.GetFileNameWithoutExtension(selectedPath);
            if (filename.Length > 8 && filename.Substring(filename.Length - 8).ToLower() == ".project")
            {
                filename += ".xml";
            }
            else
            {
                filename += ".Project.xml";
            }

            return Path.Combine(Path.GetDirectoryName(selectedPath), filename);
        }

        public static string GetLoadPath(string selectedPath)
        {
            string filename = Path.GetFileNameWithoutExtension(selectedPath) + ".xml";
            return Path.Combine(Path.GetDirectoryName(selectedPath), filename);
        }

        public static void Save(string path, ProjectSaveRequest request)
        {
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    "
            };

            using (XmlWriter writer = XmlWriter.Create(path, settings))
            {
                writer.WriteStartElement("main");
                writer.WriteElementString("Encoding", request.EncodingIndex.ToString());
                writer.WriteElementString("SizeX", request.SizeXIndex.ToString());
                writer.WriteElementString("SizeY", request.SizeYIndex.ToString());
                writer.WriteElementString("TexFileName", request.TexFileName);
                writer.WriteElementString("Gap", request.Gap.ToString());
                writer.WriteElementString("BackGroundColor", request.BackGroundColorArgb.ToString());
                writer.WriteElementString("ArrangeMethod", request.ArrangeMethod.ToString());
                writer.WriteElementString("FontLists", request.FontSections.Count.ToString());

                int index = 1;
                foreach (Main main in request.FontSections)
                {
                    WriteFontSection(writer, main, request.EncodingIndex, index);
                    index++;
                }

                writer.WriteEndElement();
            }
        }

        public static ProjectDocument Load(string path)
        {
            XmlReaderSettings settings = new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true,
                ValidationType = ValidationType.None
            };

            ProjectDocument document = new ProjectDocument();
            ProjectFontSection currentFont = null;
            PostAmendment currentAmendment = null;

            using (XmlReader reader = XmlReader.Create(path, settings))
            {
                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element)
                    {
                        continue;
                    }

                    string localName = reader.LocalName;
                    switch (localName)
                    {
                        case "Encoding":
                            document.EncodingIndex = ReadInt(reader);
                            break;
                        case "SizeX":
                            document.SizeXIndex = ReadInt(reader);
                            break;
                        case "SizeY":
                            document.SizeYIndex = ReadInt(reader);
                            break;
                        case "Gap":
                            document.Gap = ReadDecimal(reader);
                            break;
                        case "BackGroundColor":
                            document.BackGroundColorArgb = ReadInt(reader);
                            break;
                        case "TexFileName":
                            document.TexFileName = ReadString(reader);
                            break;
                        case "ArrangeMethod":
                            document.ArrangeMethod = ReadInt(reader);
                            break;
                        case "FontLists":
                            document.FontListCount = ReadInt(reader);
                            break;
                        case "font":
                            currentFont = new ProjectFontSection { ID = int.Parse(reader.NamespaceURI) - 1 };
                            document.FontSections.Add(currentFont);
                            currentAmendment = new PostAmendment { ID = currentFont.ID };
                            document.PostAmendments.Add(currentAmendment);
                            break;
                        case "Adjust":
                            break;
                        default:
                            ReadFontElement(reader, localName, currentFont, currentAmendment);
                            break;
                    }
                }
            }

            return document;
        }

        private static void WriteFontSection(XmlWriter writer, Main main, int encodingIndex, int index)
        {
            writer.WriteStartElement("font", index.ToString());
            if (main.ImportFont1name == "")
            {
                writer.WriteElementString("SCFontName", main.font1.FamilyName);
                writer.WriteElementString("SCFontSize", main.font1.SizePixels.ToString());
                writer.WriteElementString("SCFontStyle", ToLegacyFontStyle(main.font1).ToString());
                WriteFontDescriptor(writer, "SCFontDescriptor", main.font1, main.font1StyleDescriptor);
            }
            else
            {
                writer.WriteElementString("import_font", main.ImportFont1name);
            }

            if (main.ImportFont2name == "")
            {
                if (main.DCfontLink > -1)
                {
                    writer.WriteElementString("DCFontLink", main.DCfontLink.ToString());
                }
                else if (encodingIndex != 0)
                {
                    writer.WriteElementString("DCFontName", main.font2.FamilyName);
                    writer.WriteElementString("DCFontSize", main.font2.SizePixels.ToString());
                    writer.WriteElementString("DCFontStyle", ToLegacyFontStyle(main.font2).ToString());
                    WriteFontDescriptor(writer, "DCFontDescriptor", main.font2, main.font2StyleDescriptor);
                }
            }

            writer.WriteElementString("FntName", main.name);
            writer.WriteElementString("Glow", main.Glow.ToString());
            writer.WriteElementString("GlowColor", main.GlowColor.ToArgb().ToString());
            writer.WriteElementString("Outline", main.Outline.ToString());
            writer.WriteElementString("OutlineColor", main.OutlineColor.ToArgb().ToString());
            writer.WriteElementString("FontColor", main.FontColor.ToArgb().ToString());
            writer.WriteElementString("UseManualBaseLine", main.UseManualBaseLine.ToString());
            if (main.UseManualBaseLine)
            {
                writer.WriteElementString("ManualBaseLine", main.ManualBaseLine.ToString());
            }
            for (int i = 0; i < 8; i++)
            {
                writer.WriteElementString("LinkINI" + (i + 1), main.Fallout3INI[i].ToString());
            }

            writer.WriteElementString("LineHeight", main.FntFile.Header.fBaseLineFixed.ToString());
            if (main.fixedFont)
            {
                writer.WriteElementString("FontMaxWidth", main.FontMaxWidth.ToString());
            }
            writer.WriteElementString(
                "UseProportionalDoubleByteSpacing",
                main.UseProportionalDoubleByteSpacing.ToString());

            writer.WriteStartElement("Adjust");
            foreach (Fnt_char fnt in main.FntFile.CharList)
            {
                if (!fnt.Enable) continue;
                if (fnt.fLeadingEdgeFixed != 0)
                    writer.WriteElementString("LeftSpacing", fnt.HEX, fnt.fLeadingEdgeFixed.ToString());
                if (fnt.fSpacingFixed != 0)
                    writer.WriteElementString("RightSpacing", fnt.HEX, fnt.fSpacingFixed.ToString());
                if (fnt.fTopEdgeFixed != 0)
                    writer.WriteElementString("BottomAlign", fnt.HEX, fnt.fTopEdgeFixed.ToString());
                if (fnt.fHeightFixed != 0)
                    writer.WriteElementString("CharViewHeight", fnt.HEX, fnt.fHeightFixed.ToString());
                if (fnt.fWidthFixed != 0)
                    writer.WriteElementString("CharViewWidth", fnt.HEX, fnt.fWidthFixed.ToString());
            }
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        private static FontStyle ToLegacyFontStyle(FontDescriptor font)
        {
            FontStyle style = FontStyle.Regular;
            if (font != null && font.Weight >= 600) style |= FontStyle.Bold;
            if (font != null && font.Slant != SkiaSharp.SKFontStyleSlant.Upright) style |= FontStyle.Italic;
            return style;
        }

        private static void WriteFontDescriptor(
            XmlWriter writer,
            string elementName,
            FontDescriptor font,
            FontStyleDescriptor styleDescriptor)
        {
            FontStyleDescriptor exactDescriptor = styleDescriptor ?? FontStyleDescriptor.FromFontDescriptor(font);
            string serialized = exactDescriptor?.Serialize();
            if (!string.IsNullOrWhiteSpace(serialized))
            {
                writer.WriteElementString(elementName, serialized);
            }
        }

        private static void ReadFontElement(
            XmlReader reader,
            string localName,
            ProjectFontSection currentFont,
            PostAmendment currentAmendment)
        {
            if (currentFont == null)
            {
                return;
            }

            string value;
            string hex;
            FntFixed fixedValue;
            switch (localName)
            {
                case "SCFontName":
                    currentFont.SCFontName = ReadString(reader);
                    break;
                case "SCFontSize":
                    currentFont.SCFontSize = ReadFloat(reader);
                    break;
                case "SCFontStyle":
                    currentFont.SCFontStyle = ConvertFontStyle(ReadString(reader));
                    currentFont.HasSCFont = true;
                    break;
                case "SCFontDescriptor":
                    currentFont.SCFontDescriptor = ReadString(reader);
                    currentFont.HasSCFont = true;
                    break;
                case "DCFontName":
                    currentFont.DCFontName = ReadString(reader);
                    break;
                case "DCFontSize":
                    currentFont.DCFontSize = ReadFloat(reader);
                    break;
                case "DCFontStyle":
                    currentFont.DCFontStyle = ConvertFontStyle(ReadString(reader));
                    currentFont.HasDCFont = true;
                    break;
                case "DCFontDescriptor":
                    currentFont.DCFontDescriptor = ReadString(reader);
                    currentFont.HasDCFont = true;
                    break;
                case "DCFontLink":
                    currentFont.DCFontLink = ReadInt(reader);
                    currentFont.HasDCFontLink = true;
                    break;
                case "import_font":
                    currentFont.ImportFontName = ReadString(reader);
                    break;
                case "Glow":
                    currentFont.Glow = ReadInt(reader);
                    currentFont.HasGlow = true;
                    break;
                case "GlowColor":
                    currentFont.GlowColorArgb = ReadInt(reader);
                    currentFont.HasGlowColor = true;
                    break;
                case "Outline":
                    currentFont.Outline = ReadInt(reader);
                    currentFont.HasOutline = true;
                    break;
                case "OutlineColor":
                    currentFont.OutlineColorArgb = ReadInt(reader);
                    currentFont.HasOutlineColor = true;
                    break;
                case "FontMaxWidth":
                    currentFont.FixedFont = true;
                    currentFont.FontMaxWidth = ReadFloat(reader);
                    break;
                case "UseProportionalDoubleByteSpacing":
                    currentFont.UseProportionalDoubleByteSpacing = ReadString(reader).ToLower() == "true";
                    currentFont.HasUseProportionalDoubleByteSpacing = true;
                    break;
                case "FontColor":
                    currentFont.FontColorArgb = ReadInt(reader);
                    currentFont.HasFontColor = true;
                    break;
                case "UseManualBaseLine":
                    currentFont.UseManualBaseLine = ReadString(reader).ToLower() == "true";
                    currentFont.HasUseManualBaseLine = true;
                    break;
                case "ManualBaseLine":
                    currentFont.ManualBaseLine = ReadFloat(reader);
                    currentFont.HasManualBaseLine = true;
                    break;
                case "FntName":
                    currentFont.FntName = ReadString(reader);
                    currentFont.HasFntName = true;
                    break;
                case "LineHeight":
                    if (currentAmendment != null)
                    {
                        currentAmendment.fBaseLineFixed = ReadFloat(reader);
                    }
                    break;
                case "LeftSpacing":
                    hex = reader.NamespaceURI;
                    value = ReadString(reader);
                    fixedValue = currentAmendment[hex];
                    fixedValue.hex = hex;
                    fixedValue.fLeadingEdgeFixed = float.Parse(value);
                    currentAmendment[hex] = fixedValue;
                    break;
                case "RightSpacing":
                    hex = reader.NamespaceURI;
                    value = ReadString(reader);
                    fixedValue = currentAmendment[hex];
                    fixedValue.hex = hex;
                    fixedValue.fSpacingFixed = float.Parse(value);
                    currentAmendment[hex] = fixedValue;
                    break;
                case "BottomAlign":
                    hex = reader.NamespaceURI;
                    value = ReadString(reader);
                    fixedValue = currentAmendment[hex];
                    fixedValue.hex = hex;
                    fixedValue.fTopEdgeFixed = float.Parse(value);
                    currentAmendment[hex] = fixedValue;
                    break;
                case "CharViewHeight":
                    hex = reader.NamespaceURI;
                    value = ReadString(reader);
                    fixedValue = currentAmendment[hex];
                    fixedValue.hex = hex;
                    fixedValue.fHeightFixed = float.Parse(value);
                    currentAmendment[hex] = fixedValue;
                    break;
                case "CharViewWidth":
                    hex = reader.NamespaceURI;
                    value = ReadString(reader);
                    fixedValue = currentAmendment[hex];
                    fixedValue.hex = hex;
                    fixedValue.fWidthFixed = float.Parse(value);
                    currentAmendment[hex] = fixedValue;
                    break;
                default:
                    if (localName.StartsWith("LinkINI", StringComparison.Ordinal)
                        && int.TryParse(localName.Substring(7), out int iniIndex)
                        && iniIndex >= 1
                        && iniIndex <= 8)
                    {
                        currentFont.Fallout3INI[iniIndex - 1] = ReadString(reader).ToLower() == "true";
                    }
                    break;
            }
        }

        private static string ReadString(XmlReader reader)
        {
            reader.Read();
            return reader.Value;
        }

        private static int ReadInt(XmlReader reader)
        {
            return int.Parse(ReadString(reader));
        }

        private static float ReadFloat(XmlReader reader)
        {
            return float.Parse(ReadString(reader));
        }

        private static decimal ReadDecimal(XmlReader reader)
        {
            return decimal.Parse(ReadString(reader));
        }

        private static FontStyle ConvertFontStyle(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return FontStyle.Regular;
            }

            FontStyle style = FontStyle.Regular;
            if (value.IndexOf("Bold", StringComparison.OrdinalIgnoreCase) >= 0)
                style |= FontStyle.Bold;
            if (value.IndexOf("Italic", StringComparison.OrdinalIgnoreCase) >= 0)
                style |= FontStyle.Italic;
            if (value.IndexOf("Strikeout", StringComparison.OrdinalIgnoreCase) >= 0)
                style |= FontStyle.Strikeout;
            if (value.IndexOf("Underline", StringComparison.OrdinalIgnoreCase) >= 0)
                style |= FontStyle.Underline;
            return style;
        }
    }
}
