using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;

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
        [DisplayName("Opacity percent")]
        [Description("Opacity from 10 to 100. Watermark mode also uses this value.")]
        public int OpacityPercent { get; set; }

        [Category("Highlighting")]
        [DisplayName("Highlighted folders")]
        [Description("Semicolon-separated folder=color entries. Example: src=#E4572E;Models=DodgerBlue.")]
        public string HighlightFolders { get; set; }

        public override void SaveSettingsToStorage()
        {
            base.SaveSettingsToStorage();
            OptionsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
