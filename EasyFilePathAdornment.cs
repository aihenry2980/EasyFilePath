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

        private const double MinimumVisibleHeight = 28.0;

        private readonly IWpfTextView textView;
        private readonly ITextDocumentFactoryService documentFactoryService;
        private readonly PathAdornmentPlacement placement;
        private readonly Border pathContainer;
        private readonly TextBlock pathTextBlock;
        private readonly StackPanel pathSegmentPanel;
        private readonly ScrollViewer pathSegmentScroller;
        private readonly Button copyFileNameButton;
        private readonly Button settingsButton;

        private bool isDisposed;
        private string currentPath;
        private Brush normalSegmentBrush = Brushes.WhiteSmoke;
        private Brush separatorBrush = new SolidColorBrush(Color.FromRgb(190, 190, 190));
        private Brush fileNameBrush = Brushes.LightSkyBlue;

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
            Background = CreateSolidBrush("#2D2D30", Color.FromRgb(45, 45, 48));

            pathTextBlock = new TextBlock
            {
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 6, 0)
            };

            pathSegmentPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            pathSegmentScroller = new ScrollViewer
            {
                Content = pathSegmentPanel,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 1, 6, 1)
            };

            Grid pathHost = new Grid();
            pathHost.Children.Add(pathTextBlock);
            pathHost.Children.Add(pathSegmentScroller);

            copyFileNameButton = new Button
            {
                Content = "Copy",
                Height = 16,
                MinWidth = 38,
                Padding = new Thickness(5, 0, 5, 0),
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Copy file name"
            };
            copyFileNameButton.Click += OnCopyFileNameButtonClick;

            settingsButton = new Button
            {
                Content = "\uE713",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 12.0,
                Width = 20,
                Height = 16,
                Padding = new Thickness(0),
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Easy File Path settings"
            };
            settingsButton.Click += OnSettingsButtonClick;

            Grid contentGrid = new Grid();
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(pathHost, 0);
            Grid.SetColumn(copyFileNameButton, 1);
            Grid.SetColumn(settingsButton, 2);
            contentGrid.Children.Add(pathHost);
            contentGrid.Children.Add(copyFileNameButton);
            contentGrid.Children.Add(settingsButton);

            pathContainer = new Border
            {
                Child = contentGrid,
                Background = CreateSolidBrush("#2D2D30", Color.FromRgb(45, 45, 48)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(88, 88, 88)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 0, 8, 0),
                IsHitTestVisible = true
            };

            Children.Add(pathContainer);

            Loaded += OnMarginLoaded;
            textView.Closed += OnTextViewClosed;
            textView.GotAggregateFocus += OnTextViewGotAggregateFocus;
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
                textView.GotAggregateFocus -= OnTextViewGotAggregateFocus;
                Loaded -= OnMarginLoaded;
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

        private void OnMarginLoaded(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            UpdateMargin();
        }

        private void OnTextViewGotAggregateFocus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            UpdateMargin();
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
            ApplyBackground(options);
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
            double verticalPadding = options.DisplayStyle == PathDisplayStyle.Segmented ? 12.0 : 4.0;
            return Math.Max(MinimumVisibleHeight, GetFontSize(options) + verticalPadding);
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

        private void ApplyBackground(EasyFilePathOptions options)
        {
            SolidColorBrush backgroundBrush = CreateSolidBrush(options.BackgroundColor, Color.FromRgb(45, 45, 48));
            Background = backgroundBrush;
            pathContainer.Background = backgroundBrush;
            pathContainer.BorderBrush = CreateBorderBrush(backgroundBrush.Color);
            ApplyForegroundPalette(options, backgroundBrush.Color);
        }

        private void ApplyForegroundPalette(EasyFilePathOptions options, Color background)
        {
            Brush selectedFontBrush = TryCreateBrush(options.FontColor);
            if (selectedFontBrush != null)
            {
                normalSegmentBrush = selectedFontBrush;
                separatorBrush = selectedFontBrush;
                fileNameBrush = selectedFontBrush;
                return;
            }

            bool isLightBackground = GetLuminance(background) > 0.55;
            normalSegmentBrush = isLightBackground
                ? Brushes.Black
                : Brushes.WhiteSmoke;
            separatorBrush = isLightBackground
                ? CreateFrozenBrush(Color.FromRgb(70, 70, 70))
                : CreateFrozenBrush(Color.FromRgb(190, 190, 190));
            fileNameBrush = isLightBackground
                ? CreateFrozenBrush(Color.FromRgb(0, 90, 158))
                : Brushes.LightSkyBlue;
        }

        private void RenderPath(string filePath, EasyFilePathOptions options)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (options.DisplayStyle == PathDisplayStyle.Segmented)
            {
                try
                {
                    RenderSegmentedPath(filePath, options);
                    pathTextBlock.Visibility = Visibility.Collapsed;
                    pathSegmentScroller.Visibility = Visibility.Visible;
                    return;
                }
                catch (Exception exception)
                {
                    Debug.WriteLine("EasyFilePath segmented rendering failed: " + exception);
                    pathSegmentPanel.Children.Clear();
                }
            }

            pathTextBlock.Visibility = Visibility.Visible;
            pathSegmentScroller.Visibility = Visibility.Collapsed;
            pathSegmentPanel.Children.Clear();

            RenderTextPath(filePath, options);
        }

        private void RenderTextPath(string filePath, EasyFilePathOptions options)
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
                        Foreground = separatorBrush
                    });
                }

                PathSegment segment = segments[i];
                pathTextBlock.Inlines.Add(CreateSegmentRun(segment, highlightBrushes));
            }
        }

        private void RenderSegmentedPath(string filePath, EasyFilePathOptions options)
        {
            pathTextBlock.Inlines.Clear();
            pathSegmentPanel.Children.Clear();

            Dictionary<string, Brush> highlightBrushes = ParseHighlightBrushes(options.HighlightFolders);
            List<Brush> pastelBrushes = ParseAccentBrushes(options.PastelColors);
            IReadOnlyList<PathSegment> segments = BuildPathSegments(filePath);
            double fontSize = GetFontSize(options);
            FontFamily fontFamily = new FontFamily(string.IsNullOrWhiteSpace(options.FontFamilyName)
                ? "Consolas"
                : options.FontFamilyName.Trim());

            for (int i = 0; i < segments.Count; i++)
            {
                PathSegment segment = segments[i];
                Brush background = GetSegmentBackground(segment, highlightBrushes, pastelBrushes, i);
                Brush foreground = segment.IsHighlighted ? Brushes.White : Brushes.Black;

                TextBlock label = new TextBlock
                {
                    Text = segment.Text,
                    FontFamily = fontFamily,
                    FontSize = fontSize,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = foreground,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 260
                };

                Border pill = new Border
                {
                    Child = label,
                    Background = background,
                    CornerRadius = new CornerRadius(0, 14, 14, 0),
                    Padding = new Thickness(i == 0 ? 12 : 22, 3, 16, 3),
                    Margin = new Thickness(i == 0 ? 0 : -12, 0, 0, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = segment.IsFileName
                        ? "Click to copy file name. Double-click to copy full path."
                        : "Double-click to open folder. Right-click to cycle highlight color. Ctrl+right-click to remove highlight."
                };

                Panel.SetZIndex(pill, segments.Count - i);
                if (segment.IsFileName)
                {
                    pill.MouseLeftButtonDown += OnFileNameMouseLeftButtonDown;
                }
                else
                {
                    pill.MouseLeftButtonDown += (sender, e) => OnFolderMouseLeftButtonDown(segment, e);
                    pill.MouseRightButtonDown += (sender, e) =>
                    {
                        ThreadHelper.ThrowIfNotOnUIThread();
                        OnFolderMouseRightButtonDown(segment.Text, e);
                    };
                }

                pathSegmentPanel.Children.Add(pill);
            }
        }

        private static Brush GetSegmentBackground(
            PathSegment segment,
            Dictionary<string, Brush> highlightBrushes,
            List<Brush> pastelBrushes,
            int index)
        {
            Brush highlight;
            if (!segment.IsFileName && highlightBrushes.TryGetValue(segment.Text, out highlight))
            {
                segment.IsHighlighted = true;
                return highlight;
            }

            if (pastelBrushes.Count > 0)
            {
                return pastelBrushes[index % pastelBrushes.Count];
            }

            return CreateFrozenBrush(Color.FromRgb(184, 225, 255));
        }

        private static List<Brush> ParseAccentBrushes(string accentColors)
        {
            List<Brush> brushes = new List<Brush>();
            foreach (string color in ParseAccentColors(accentColors))
            {
                Brush brush = TryCreateBrush(color);
                if (brush != null)
                {
                    brushes.Add(brush);
                }
            }

            return brushes;
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

            return segment.IsFileName ? fileNameBrush : normalSegmentBrush;
        }

        private static Brush GetReadableForeground(Brush background)
        {
            SolidColorBrush solid = background as SolidColorBrush;
            if (solid == null)
            {
                return Brushes.White;
            }

            Color color = solid.Color;
            return GetLuminance(color) > 0.55 ? Brushes.Black : Brushes.White;
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

        private void OnCopyFileNameButtonClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(currentPath))
            {
                return;
            }

            try
            {
                Clipboard.SetText(Path.GetFileName(currentPath));
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

        private static SolidColorBrush CreateSolidBrush(string colorText, Color fallback)
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

            SolidColorBrush fallbackBrush = new SolidColorBrush(fallback);
            fallbackBrush.Freeze();
            return fallbackBrush;
        }

        private static SolidColorBrush CreateBorderBrush(Color background)
        {
            byte red = ShiftChannel(background.R);
            byte green = ShiftChannel(background.G);
            byte blue = ShiftChannel(background.B);
            SolidColorBrush brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
            brush.Freeze();
            return brush;
        }

        private static SolidColorBrush CreateFrozenBrush(Color color)
        {
            SolidColorBrush brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static double GetLuminance(Color color)
        {
            return ((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B)) / 255.0;
        }

        private static byte ShiftChannel(byte value)
        {
            return value < 128
                ? (byte)Math.Min(255, value + 43)
                : (byte)Math.Max(0, value - 43);
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
