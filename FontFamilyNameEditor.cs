using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Text;
using System.Windows.Forms;

namespace EasyFilePath
{
    internal sealed class FontFamilyNameEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            EasyFilePathOptions options = context?.Instance as EasyFilePathOptions;

            using (FontDialog dialog = new FontDialog())
            {
                dialog.ShowEffects = false;
                dialog.FontMustExist = true;

                string fontName = Convert.ToString(value);
                float fontSize = 11.0f;

                if (options != null && options.FontSize >= 6.0 && options.FontSize <= 48.0)
                {
                    fontSize = (float)options.FontSize;
                }

                try
                {
                    dialog.Font = new Font(string.IsNullOrWhiteSpace(fontName) ? "Consolas" : fontName, fontSize);
                }
                catch (ArgumentException)
                {
                    dialog.Font = new Font("Consolas", fontSize);
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (options != null)
                    {
                        options.FontSize = Math.Round(dialog.Font.SizeInPoints, 1);
                    }

                    return dialog.Font.FontFamily.Name;
                }
            }

            return value;
        }
    }

    internal sealed class FontFamilyNameConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> fontNames = new List<string>();

            using (InstalledFontCollection fonts = new InstalledFontCollection())
            {
                foreach (FontFamily family in fonts.Families)
                {
                    fontNames.Add(family.Name);
                }
            }

            fontNames.Sort(StringComparer.CurrentCultureIgnoreCase);
            return new StandardValuesCollection(fontNames);
        }
    }
}
