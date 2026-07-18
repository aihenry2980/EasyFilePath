using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Drawing.Design;

namespace EasyFilePath
{
    public enum PathAdornmentPlacement
    {
        Top,
        Bottom,
        Watermark
    }

    public enum PathDisplayStyle
    {
        Segmented,
        Text
    }

    public sealed class EasyFilePathOptions : DialogPage
    {
        private const string DefaultPastelColors = "Sky=#B8E1FF;Peach=#FFD6A5;Lavender=#D9C2FF;Mint=#BDECCF;Butter=#FFF0A8;Blush=#FFC8DD;Sage=#CDE7BE;Periwinkle=#C9D6FF;Sand=#E6D5B8;Aqua=#BFE7E5";
        private const string DefaultHighlightColors = "Blue=#1565C0;Burnt Orange=#C2410C;Purple=#6D28D9;Green=#15803D;Red=#B91C1C;Teal=#0F766E;Magenta=#9D174D;Indigo=#4338CA;Brown=#7C2D12;Slate=#374151";
        private const string LegacyAccentColors1 = "Amber=#C17D11;Ocean Blue=#2E86AB;Coral=#E4572E;Forest Green=#22863A;Violet=#7C3AED;Rose=#B83280";
        private const string LegacyAccentColors2 = "Ocean Blue=#2E86AB;Coral=#E4572E;Violet=#7C3AED;Forest Green=#22863A;Amber=#C17D11;Rose=#B83280";

        public static event EventHandler OptionsChanged;
        private string accentColors;
        private string backgroundColor;
        private string fontColor;
        private string pastelColors;

        public EasyFilePathOptions()
        {
            IsEnabled = true;
            Placement = PathAdornmentPlacement.Top;
            DisplayStyle = PathDisplayStyle.Segmented;
            Separator = " > ";
            HighlightFolders = string.Empty;
            PastelColors = DefaultPastelColors;
            AccentColors = DefaultHighlightColors;
            FontFamilyName = "Consolas";
            FontSize = 11.0;
            OpacityPercent = 92;
            BackgroundColor = "#2D2D30";
            FontColor = "Auto";
        }

        [Category("Display")]
        [DisplayName("Enabled")]
        [Description("Show the absolute file path in text editor windows.")]
        public bool IsEnabled { get; set; }

        [Category("Display")]
        [DisplayName("Placement")]
        [Description("Where the path line is shown in the editor. Watermark is rendered as a top line so it does not overlap code.")]
        public PathAdornmentPlacement Placement { get; set; }

        [Category("Display")]
        [DisplayName("Path style")]
        [Description("Segmented shows each path component as an overlapping rounded pill. Text uses the classic separator-based path.")]
        public PathDisplayStyle DisplayStyle { get; set; }

        [Category("Display")]
        [DisplayName("Separator")]
        [Description("Separator text used between path segments. Examples: ' > ', ' / ', ' | ', ' -> '.")]
        public string Separator { get; set; }

        [Category("Display")]
        [DisplayName("Font family")]
        [Description("Font family used for the file path line. Use the chooser button to select font and size together.")]
        [Editor(typeof(FontFamilyNameEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(FontFamilyNameConverter))]
        public string FontFamilyName { get; set; }

        [Category("Display")]
        [DisplayName("Font size")]
        [Description("Font size used for the file path line.")]
        public double FontSize { get; set; }

        [Category("Display")]
        [DisplayName("Opacity percent")]
        [Description("Opacity from 10 to 100. Watermark mode also uses this value.")]
        public int OpacityPercent { get; set; }

        [Category("Display")]
        [DisplayName("Background color")]
        [Description("Background color used for the file path line. Use the chooser button to select a color.")]
        [Editor(typeof(ColorValueEditor), typeof(UITypeEditor))]
        public string BackgroundColor
        {
            get
            {
                return NormalizeColor(backgroundColor, "#2D2D30");
            }

            set
            {
                backgroundColor = value;
            }
        }

        [Category("Display")]
        [DisplayName("Font color")]
        [Description("Text color used for the normal file path text. Use Auto for a readable color based on the background.")]
        [Editor(typeof(ColorValueEditor), typeof(UITypeEditor))]
        public string FontColor
        {
            get
            {
                return NormalizeOptionalColor(fontColor, "Auto");
            }

            set
            {
                fontColor = value;
            }
        }

        [Category("Highlighting")]
        [DisplayName("Default pastel colors")]
        [Description("Pastel background colors used by normal path segments. Normal segment text is always black.")]
        [Editor(typeof(AccentColorListEditor), typeof(UITypeEditor))]
        public string PastelColors
        {
            get
            {
                string normalized = AccentColorParser.NormalizeSerializedEntries(pastelColors);
                return string.IsNullOrWhiteSpace(normalized) ? DefaultPastelColors : normalized;
            }

            set
            {
                pastelColors = value;
            }
        }

        [Category("Highlighting")]
        [DisplayName("Highlighted folders")]
        [Description("Semicolon-separated folder=background color entries. Example: src=#E4572E;Models=DodgerBlue. Right-click a folder in the path line to cycle its color.")]
        public string HighlightFolders { get; set; }

        [Category("Highlighting")]
        [DisplayName("Highlight colors")]
        [Description("Dark colors cycled when right-clicking a folder segment. Highlighted segment text is always white.")]
        [Editor(typeof(AccentColorListEditor), typeof(UITypeEditor))]
        public string AccentColors
        {
            get
            {
                string normalized = AccentColorParser.NormalizeSerializedEntries(accentColors);
                if (string.IsNullOrWhiteSpace(normalized) ||
                    string.Equals(normalized, LegacyAccentColors1, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(normalized, LegacyAccentColors2, StringComparison.OrdinalIgnoreCase))
                {
                    return DefaultHighlightColors;
                }

                return normalized;
            }

            set
            {
                accentColors = value;
            }
        }

        public override void SaveSettingsToStorage()
        {
            base.SaveSettingsToStorage();
            RaiseOptionsChanged(this);
        }

        internal static void RaiseOptionsChanged(object sender)
        {
            OptionsChanged?.Invoke(sender, EventArgs.Empty);
        }

        private static string NormalizeColor(string colorText, string fallback)
        {
            System.Drawing.Color? color = AccentColorParser.TryParseDrawingColor(colorText);
            return color.HasValue ? AccentColorParser.ToHex(color.Value) : fallback;
        }

        private static string NormalizeOptionalColor(string colorText, string fallback)
        {
            System.Drawing.Color? color = AccentColorParser.TryParseDrawingColor(colorText);
            return color.HasValue ? AccentColorParser.ToHex(color.Value) : fallback;
        }
    }
}
