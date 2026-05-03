using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace EasyFilePath
{
    internal sealed class EasyFilePathMargin : Grid, IWpfTextViewMargin
    {
        internal const string TopMarginName = "EasyFilePathTopMargin";
        internal const string BottomMarginName = "EasyFilePathBottomMargin";

        private const double MinimumVisibleHeight = 18.0;

        private readonly IWpfTextView textView;
        private readonly ITextDocumentFactoryService documentFactoryService;
        private readonly PathAdornmentPlacement placement;
        private readonly Border pathContainer;
        private readonly TextBlock pathTextBlock;
        private readonly Button settingsButton;

        private bool isDisposed;
        private string currentPath;

        internal EasyFilePathMargin(
            IWpfTextView textView,
            ITextDocumentFactoryService documentFactoryService,
            PathAdornmentPlacement placement)
        {
            this.textView = textView ?? throw new ArgumentNullException(nameof(textView));
            this.documentFactoryService = documentFactoryService ?? throw new ArgumentNullException(nameof(documentFactoryService));
            this.placement = placement;

            ClipToBounds = true;
            Height = MinimumVisibleHeight;
            MinHeight = 0;
            Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));

            pathTextBlock = new TextBlock
            {
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };

            settingsButton = new Button
            {
                Content = "...",
                Width = 22,
                Height = 18,
                Padding = new Thickness(0),
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Easy File Path settings"
            };
            settingsButton.Click += OnSettingsButtonClick;

            Grid contentGrid = new Grid();
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(pathTextBlock, 0);
            Grid.SetColumn(settingsButton, 1);
            contentGrid.Children.Add(pathTextBlock);
            contentGrid.Children.Add(settingsButton);

            pathContainer = new Border
            {
                Child = contentGrid,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(88, 88, 88)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(8, 0, 8, 0),
                IsHitTestVisible = true
            };

            Children.Add(pathContainer);

            textView.Closed += OnTextViewClosed;
            EasyFilePathOptions.OptionsChanged += OnOptionsChanged;

            ThreadHelper.ThrowIfNotOnUIThread();
            UpdateMargin();
        }

        public FrameworkElement VisualElement
        {
            get
            {
                ThrowIfDisposed();
                return this;
            }
        }

        public double MarginSize
        {
            get
            {
                ThrowIfDisposed();
                return ActualHeight;
            }
        }

        public bool Enabled
        {
            get
            {
                ThrowIfDisposed();
                return Visibility == Visibility.Visible;
            }
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            if (marginName == null)
            {
                throw new ArgumentNullException(nameof(marginName));
            }

            return string.Equals(marginName, TopMarginName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(marginName, BottomMarginName, StringComparison.OrdinalIgnoreCase)
                ? this
                : null;
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                textView.Closed -= OnTextViewClosed;
                EasyFilePathOptions.OptionsChanged -= OnOptionsChanged;
                isDisposed = true;
                GC.SuppressFinalize(this);
            }
        }

        private void OnOptionsChanged(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            UpdateMargin();
        }

        private void OnTextViewClosed(object sender, EventArgs e)
        {
            Dispose();
        }

        private void UpdateMargin()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EasyFilePathOptions options = EasyFilePathPackage.GetOptions();
            currentPath = TryGetFilePath();
            double visibleHeight = GetVisibleHeight(options);

            if (!ShouldShow(options) || string.IsNullOrWhiteSpace(currentPath))
            {
                Height = 0;
                Visibility = Visibility.Collapsed;
                return;
            }

            Height = visibleHeight;
            Visibility = Visibility.Visible;
            Opacity = GetOpacity(options);
            ApplyFont(options);
            RenderPath(currentPath, options);
        }

        private bool ShouldShow(EasyFilePathOptions options)
        {
            if (!options.IsEnabled)
            {
                return false;
            }

            if (placement == PathAdornmentPlacement.Top)
            {
                return options.Placement == PathAdornmentPlacement.Top ||
                       options.Placement == PathAdornmentPlacement.Watermark;
            }

            return options.Placement == PathAdornmentPlacement.Bottom;
        }

        private string TryGetFilePath()
        {
            ITextDocument document;
            if (documentFactoryService.TryGetTextDocument(textView.TextBuffer, out document))
            {
                return document.FilePath;
            }

            return null;
        }

        private static double GetOpacity(EasyFilePathOptions options)
        {
            int opacityPercent = Math.Max(10, Math.Min(100, options.OpacityPercent));
            return opacityPercent / 100.0;
        }

        private static double GetVisibleHeight(EasyFilePathOptions options)
        {
            return Math.Max(MinimumVisibleHeight, GetFontSize(options) + 4.0);
        }

        private static double GetFontSize(EasyFilePathOptions options)
        {
            if (options.FontSize < 6.0 || options.FontSize > 48.0)
            {
                return 11.0;
            }

            return options.FontSize;
        }

        private void ApplyFont(EasyFilePathOptions options)
        {
            pathTextBlock.FontSize = GetFontSize(options);
            pathTextBlock.FontFamily = new FontFamily(string.IsNullOrWhiteSpace(options.FontFamilyName)
                ? "Consolas"
                : options.FontFamilyName.Trim());
        }

        private void RenderPath(string filePath, EasyFilePathOptions options)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            pathTextBlock.Inlines.Clear();

            string separator = string.IsNullOrEmpty(options.Separator) ? " > " : options.Separator;
            Dictionary<string, Brush> highlightBrushes = ParseHighlightBrushes(options.HighlightFolders);
            IReadOnlyList<PathSegment> segments = BuildPathSegments(filePath);

            for (int i = 0; i < segments.Count; i++)
            {
                if (i > 0)
                {
                    pathTextBlock.Inlines.Add(new Run(separator)
                    {
                        Foreground = new SolidColorBrush(Color.FromRgb(190, 190, 190))
                    });
                }

                PathSegment segment = segments[i];
                pathTextBlock.Inlines.Add(CreateSegmentRun(segment, highlightBrushes));
            }
        }

        private Run CreateSegmentRun(PathSegment segment, Dictionary<string, Brush> highlightBrushes)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Brush highlightBrush = null;
            Run run = new Run(segment.Text)
            {
                Foreground = GetSegmentBrush(segment, highlightBrushes, out highlightBrush)
            };

            if (segment.IsHighlighted)
            {
                run.FontWeight = FontWeights.SemiBold;
                run.Background = highlightBrush;
            }

            if (segment.IsFileName)
            {
                run.Cursor = Cursors.Hand;
                run.TextDecorations = TextDecorations.Underline;
                run.MouseLeftButtonDown += OnFileNameMouseLeftButtonDown;
                run.ToolTip = "Click to copy file name. Double-click to copy full path.";
            }
            else
            {
                run.Cursor = Cursors.Hand;
                run.MouseLeftButtonDown += (sender, e) => OnFolderMouseLeftButtonDown(segment, e);
                run.MouseRightButtonDown += (sender, e) =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    OnFolderMouseRightButtonDown(segment.Text, e);
                };
                run.ToolTip = "Double-click to open folder. Right-click to cycle highlight color. Ctrl+right-click to remove highlight.";
            }

            return run;
        }

        private Brush GetSegmentBrush(PathSegment segment, Dictionary<string, Brush> highlightBrushes, out Brush highlightBrush)
        {
            highlightBrush = null;
            if (!segment.IsFileName)
            {
                if (highlightBrushes.TryGetValue(segment.Text, out highlightBrush))
                {
                    segment.IsHighlighted = true;
                    return GetReadableForeground(highlightBrush);
                }
            }

            return segment.IsFileName ? Brushes.LightSkyBlue : Brushes.WhiteSmoke;
        }

        private static Brush GetReadableForeground(Brush background)
        {
            SolidColorBrush solid = background as SolidColorBrush;
            if (solid == null)
            {
                return Brushes.White;
            }

            Color color = solid.Color;
            double luminance = ((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B)) / 255.0;
            return luminance > 0.55 ? Brushes.Black : Brushes.White;
        }

        private void OnFileNameMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(currentPath))
            {
                return;
            }

            try
            {
                string textToCopy = e.ClickCount >= 2 ? currentPath : Path.GetFileName(currentPath);
                Clipboard.SetText(textToCopy);
                e.Handled = true;
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
            }
        }

        private void OnFolderMouseRightButtonDown(string folderName, MouseButtonEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EasyFilePathOptions options = EasyFilePathPackage.GetOptions();
            Dictionary<string, string> entries = ParseHighlightEntries(options.HighlightFolders);

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                entries.Remove(folderName);
            }
            else
            {
                string currentColor;
                entries.TryGetValue(folderName, out currentColor);
                entries[folderName] = GetNextAccentColor(options.AccentColors, currentColor);
            }

            options.HighlightFolders = FormatHighlightEntries(entries);
            options.SaveSettingsToStorage();
            e.Handled = true;
        }

        private static void OnFolderMouseLeftButtonDown(PathSegment segment, MouseButtonEventArgs e)
        {
            if (e.ClickCount < 2 || string.IsNullOrWhiteSpace(segment.FullPath) || !Directory.Exists(segment.FullPath))
            {
                return;
            }

            try
            {
                Process.Start("explorer.exe", "\"" + segment.FullPath + "\"");
                e.Handled = true;
            }
            catch (Exception)
            {
            }
        }

        private void OnSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            EasyFilePathPackage.ShowOptions();
        }

        private static IReadOnlyList<PathSegment> BuildPathSegments(string filePath)
        {
            List<PathSegment> segments = new List<PathSegment>();
            string root = Path.GetPathRoot(filePath);
            string currentFolderPath = null;

            if (!string.IsNullOrEmpty(root))
            {
                currentFolderPath = root;
                segments.Add(new PathSegment(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), false, currentFolderPath));
            }

            string remainingPath = filePath;
            if (!string.IsNullOrEmpty(root) && filePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                remainingPath = filePath.Substring(root.Length);
            }

            string[] parts = remainingPath
                .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parts.Length; i++)
            {
                bool isFileName = i == parts.Length - 1;
                if (!isFileName)
                {
                    currentFolderPath = string.IsNullOrEmpty(currentFolderPath)
                        ? parts[i]
                        : Path.Combine(currentFolderPath, parts[i]);
                }

                segments.Add(new PathSegment(parts[i], isFileName, isFileName ? null : currentFolderPath));
            }

            return segments;
        }

        private static Dictionary<string, Brush> ParseHighlightBrushes(string value)
        {
            Dictionary<string, Brush> brushes = new Dictionary<string, Brush>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> entries = ParseHighlightEntries(value);

            foreach (KeyValuePair<string, string> entry in entries)
            {
                Brush brush = TryCreateBrush(entry.Value);
                if (brush != null)
                {
                    brushes[entry.Key] = brush;
                }
            }

            return brushes;
        }

        private static Dictionary<string, string> ParseHighlightEntries(string value)
        {
            Dictionary<string, string> entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(value))
            {
                return entries;
            }

            foreach (string entry in value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = entry.Split(new[] { '=' }, 2);
                if (parts.Length != 2)
                {
                    continue;
                }

                string folder = parts[0].Trim();
                string colorText = parts[1].Trim();
                if (folder.Length == 0 || colorText.Length == 0)
                {
                    continue;
                }

                entries[folder] = colorText;
            }

            return entries;
        }

        private static string FormatHighlightEntries(Dictionary<string, string> entries)
        {
            List<string> formattedEntries = new List<string>();

            foreach (KeyValuePair<string, string> entry in entries)
            {
                formattedEntries.Add(entry.Key + "=" + entry.Value);
            }

            return string.Join(";", formattedEntries);
        }

        private static string GetNextAccentColor(string accentColors, string currentColor)
        {
            List<string> colors = ParseAccentColors(accentColors);

            if (colors.Count == 0)
            {
                colors.Add("#C17D11");
                colors.Add("#2E86AB");
                colors.Add("#E4572E");
            }

            if (string.IsNullOrWhiteSpace(currentColor))
            {
                return colors[0];
            }

            int currentIndex = colors.FindIndex(color => string.Equals(color, currentColor, StringComparison.OrdinalIgnoreCase));
            if (currentIndex < 0 || currentIndex == colors.Count - 1)
            {
                return colors[0];
            }

            return colors[currentIndex + 1];
        }

        private static List<string> ParseAccentColors(string accentColors)
        {
            List<string> colors = new List<string>();

            if (string.IsNullOrWhiteSpace(accentColors))
            {
                return colors;
            }

            foreach (string color in accentColors.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmedColor = GetAccentColorValue(color);
                if (trimmedColor.Length > 0 && TryCreateBrush(trimmedColor) != null)
                {
                    colors.Add(trimmedColor);
                }
            }

            return colors;
        }

        private static string GetAccentColorValue(string entry)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                return string.Empty;
            }

            string[] parts = entry.Split(new[] { '=' }, 2);
            return parts.Length == 2 ? parts[1].Trim() : entry.Trim();
        }

        private static Brush TryCreateBrush(string colorText)
        {
            try
            {
                object converted = ColorConverter.ConvertFromString(colorText);
                if (converted is Color)
                {
                    SolidColorBrush brush = new SolidColorBrush((Color)converted);
                    brush.Freeze();
                    return brush;
                }
            }
            catch (FormatException)
            {
            }

            return null;
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(TopMarginName);
            }
        }

        private sealed class PathSegment
        {
            internal PathSegment(string text, bool isFileName, string fullPath)
            {
                Text = string.IsNullOrEmpty(text) ? Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture) : text;
                IsFileName = isFileName;
                FullPath = fullPath;
            }

            internal string Text { get; }

            internal bool IsFileName { get; }

            internal string FullPath { get; }

            internal bool IsHighlighted { get; set; }
        }
    }
}
