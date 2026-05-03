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

    public sealed class EasyFilePathOptions : DialogPage
    {
        public static event EventHandler OptionsChanged;

        public EasyFilePathOptions()
        {
            IsEnabled = true;
            Placement = PathAdornmentPlacement.Top;
            Separator = " > ";
            HighlightFolders = "source=#E4572E;repos=#2E86AB";
            AccentColors = "#C17D11;#2E86AB;#E4572E;#22863A;#7C3AED;#B83280";
            FontFamilyName = "Consolas";
            FontSize = 11.0;
            OpacityPercent = 92;
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

        [Category("Highlighting")]
        [DisplayName("Highlighted folders")]
        [Description("Semicolon-separated folder=background color entries. Example: src=#E4572E;Models=DodgerBlue. Right-click a folder in the path line to cycle its color.")]
        public string HighlightFolders { get; set; }

        [Category("Highlighting")]
        [DisplayName("Accent colors")]
        [Description("Semicolon-separated colors used when right-clicking folder segments. Example: #C17D11;#2E86AB;DodgerBlue.")]
        public string AccentColors { get; set; }

        public override void SaveSettingsToStorage()
        {
            base.SaveSettingsToStorage();
            OptionsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
