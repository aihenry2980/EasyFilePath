using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Windows.Forms;

namespace EasyFilePath
{
    internal sealed class AccentColorListEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            using (AccentColorListForm form = new AccentColorListForm(Convert.ToString(value)))
            {
                return form.ShowDialog() == DialogResult.OK ? form.SerializedColors : value;
            }
        }
    }

    internal sealed class ColorValueEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            using (ColorDialog dialog = new ColorDialog())
            {
                Color? existingColor = AccentColorParser.TryParseDrawingColor(Convert.ToString(value));
                if (existingColor.HasValue)
                {
                    dialog.Color = existingColor.Value;
                }

                dialog.FullOpen = true;
                return dialog.ShowDialog() == DialogResult.OK
                    ? AccentColorParser.ToHex(dialog.Color)
                    : value;
            }
        }
    }

    internal static class AccentColorParser
    {
        internal static List<AccentColorEntry> ParseEntries(string value)
        {
            List<AccentColorEntry> entries = new List<AccentColorEntry>();

            if (string.IsNullOrWhiteSpace(value))
            {
                return entries;
            }

            foreach (string rawEntry in value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string entry = rawEntry.Trim();
                if (entry.Length == 0)
                {
                    continue;
                }

                string name = null;
                string colorText = entry;
                string[] parts = entry.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    name = parts[0].Trim();
                    colorText = parts[1].Trim();
                }

                Color? color = TryParseDrawingColor(colorText);
                if (color.HasValue)
                {
                    entries.Add(new AccentColorEntry(
                        string.IsNullOrWhiteSpace(name) ? CreateDefaultName(color.Value) : name,
                        ToHex(color.Value)));
                }
            }

            return entries;
        }

        internal static string SerializeEntries(IEnumerable<AccentColorEntry> entries)
        {
            List<string> serializedEntries = new List<string>();

            foreach (AccentColorEntry entry in entries)
            {
                if (!string.IsNullOrWhiteSpace(entry.Name) && !string.IsNullOrWhiteSpace(entry.ColorText))
                {
                    serializedEntries.Add(entry.Name.Trim() + "=" + entry.ColorText.Trim());
                }
            }

            return string.Join(";", serializedEntries);
        }

        internal static string NormalizeSerializedEntries(string value)
        {
            return SerializeEntries(ParseEntries(value));
        }

        internal static string ToHex(Color color)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
        }

        internal static Color? TryParseDrawingColor(string colorText)
        {
            if (string.IsNullOrWhiteSpace(colorText))
            {
                return null;
            }

            try
            {
                Color color = ColorTranslator.FromHtml(colorText.Trim());
                return color.IsEmpty ? (Color?)null : color;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string CreateDefaultName(Color color)
        {
            string colorText = ToHex(color);
            switch (colorText.ToUpperInvariant())
            {
                case "#C17D11":
                    return "Amber";
                case "#2E86AB":
                    return "Ocean Blue";
                case "#E4572E":
                    return "Coral";
                case "#22863A":
                    return "Forest Green";
                case "#7C3AED":
                    return "Violet";
                case "#B83280":
                    return "Rose";
            }

            if (color.IsKnownColor)
            {
                return color.Name;
            }

            return "Custom " + colorText;
        }
    }

    internal sealed class AccentColorEntry
    {
        internal AccentColorEntry(string name, string colorText)
        {
            Name = name;
            ColorText = colorText;
        }

        internal string Name { get; set; }

        internal string ColorText { get; set; }

        public override string ToString()
        {
            return Name + " (" + ColorText + ")";
        }
    }

    internal sealed class AccentColorListForm : Form
    {
        private readonly ListBox listBox;
        private readonly List<AccentColorEntry> entries;

        internal AccentColorListForm(string serializedColors)
        {
            entries = AccentColorParser.ParseEntries(serializedColors);

            Text = "Accent Colors";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(430, 280);

            listBox = new ListBox
            {
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 24,
                IntegralHeight = false,
                Location = new Point(12, 12),
                Size = new Size(300, 220)
            };
            listBox.DrawItem += DrawColorItem;
            listBox.DoubleClick += (sender, e) => EditSelectedColor();
            Controls.Add(listBox);

            Button addButton = CreateButton("Add", 326, 12, (sender, e) => AddColor());
            Button editButton = CreateButton("Edit", 326, 43, (sender, e) => EditSelectedColor());
            Button removeButton = CreateButton("Remove", 326, 74, (sender, e) => RemoveSelectedColor());
            Button upButton = CreateButton("Up", 326, 118, (sender, e) => MoveSelectedColor(-1));
            Button downButton = CreateButton("Down", 326, 149, (sender, e) => MoveSelectedColor(1));
            Button okButton = CreateButton("OK", 244, 244, (sender, e) => DialogResult = DialogResult.OK);
            Button cancelButton = CreateButton("Cancel", 326, 244, (sender, e) => DialogResult = DialogResult.Cancel);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            Controls.Add(addButton);
            Controls.Add(editButton);
            Controls.Add(removeButton);
            Controls.Add(upButton);
            Controls.Add(downButton);
            Controls.Add(okButton);
            Controls.Add(cancelButton);

            RefreshList();
        }

        internal string SerializedColors => AccentColorParser.SerializeEntries(entries);

        private static Button CreateButton(string text, int x, int y, EventHandler clickHandler)
        {
            Button button = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(82, 25)
            };
            button.Click += clickHandler;
            return button;
        }

        private void AddColor()
        {
            using (ColorDialog dialog = new ColorDialog())
            {
                dialog.FullOpen = true;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    string colorText = AccentColorParser.ToHex(dialog.Color);
                    entries.Add(new AccentColorEntry("Custom " + colorText, colorText));
                    RefreshList();
                    listBox.SelectedIndex = entries.Count - 1;
                }
            }
        }

        private void EditSelectedColor()
        {
            int index = listBox.SelectedIndex;
            if (index < 0 || index >= entries.Count)
            {
                return;
            }

            using (ColorDialog dialog = new ColorDialog())
            {
                Color? existingColor = AccentColorParser.TryParseDrawingColor(entries[index].ColorText);
                if (existingColor.HasValue)
                {
                    dialog.Color = existingColor.Value;
                }

                dialog.FullOpen = true;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    string colorText = AccentColorParser.ToHex(dialog.Color);
                    entries[index].ColorText = colorText;
                    if (entries[index].Name.StartsWith("Custom ", StringComparison.OrdinalIgnoreCase))
                    {
                        entries[index].Name = "Custom " + colorText;
                    }

                    RefreshList();
                    listBox.SelectedIndex = index;
                }
            }
        }

        private void RemoveSelectedColor()
        {
            int index = listBox.SelectedIndex;
            if (index < 0 || index >= entries.Count)
            {
                return;
            }

            entries.RemoveAt(index);
            RefreshList();
            if (entries.Count > 0)
            {
                listBox.SelectedIndex = Math.Min(index, entries.Count - 1);
            }
        }

        private void MoveSelectedColor(int delta)
        {
            int index = listBox.SelectedIndex;
            int newIndex = index + delta;
            if (index < 0 || newIndex < 0 || newIndex >= entries.Count)
            {
                return;
            }

            AccentColorEntry entry = entries[index];
            entries.RemoveAt(index);
            entries.Insert(newIndex, entry);
            RefreshList();
            listBox.SelectedIndex = newIndex;
        }

        private void RefreshList()
        {
            listBox.Items.Clear();
            foreach (AccentColorEntry entry in entries)
            {
                listBox.Items.Add(entry);
            }
        }

        private void DrawColorItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();

            if (e.Index < 0 || e.Index >= entries.Count)
            {
                return;
            }

            AccentColorEntry entry = entries[e.Index];
            Color color = AccentColorParser.TryParseDrawingColor(entry.ColorText) ?? Color.Transparent;

            Rectangle swatch = new Rectangle(e.Bounds.Left + 4, e.Bounds.Top + 4, 32, e.Bounds.Height - 8);
            using (Brush brush = new SolidBrush(color))
            {
                e.Graphics.FillRectangle(brush, swatch);
            }

            e.Graphics.DrawRectangle(SystemPens.ControlDark, swatch);

            using (Brush textBrush = new SolidBrush(e.ForeColor))
            {
                e.Graphics.DrawString(entry.ToString(), e.Font, textBrush, e.Bounds.Left + 44, e.Bounds.Top + 4);
            }

            e.DrawFocusRectangle();
        }
    }
}
