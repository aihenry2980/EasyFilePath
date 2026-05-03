using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace EasyFilePath
{
    internal sealed class EasyFilePathAdornment
    {
        internal const string LayerName = "EasyFilePathAdornment";

        private const double EdgeMargin = 8.0;
        private readonly IWpfTextView textView;
        private readonly IAdornmentLayer adornmentLayer;
        private readonly ITextDocumentFactoryService documentFactoryService;
        private readonly Canvas canvas;
        private readonly Border pathContainer;
        private readonly TextBlock pathTextBlock;

        private string currentPath;

        internal EasyFilePathAdornment(IWpfTextView textView, ITextDocumentFactoryService documentFactoryService)
        {
            this.textView = textView ?? throw new ArgumentNullException(nameof(textView));
            this.documentFactoryService = documentFactoryService ?? throw new ArgumentNullException(nameof(documentFactoryService));
            adornmentLayer = textView.GetAdornmentLayer(LayerName);

            canvas = new Canvas
            {
                IsHitTestVisible = true
            };

            pathTextBlock = new TextBlock
            {
                FontSize = 11.0,
                FontFamily = new FontFamily("Consolas"),
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };

            pathContainer = new Border
            {
                Child = pathTextBlock,
                Background = new SolidColorBrush(Color.FromArgb(210, 30, 30, 30)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(6, 2, 6, 2),
                IsHitTestVisible = true
            };

            canvas.Children.Add(pathContainer);
            adornmentLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, canvas, null);

            textView.LayoutChanged += OnLayoutChanged;
            textView.ViewportHeightChanged += OnViewportChanged;
            textView.ViewportWidthChanged += OnViewportChanged;
            textView.Closed += OnTextViewClosed;
            EasyFilePathOptions.OptionsChanged += OnOptionsChanged;

            ThreadHelper.ThrowIfNotOnUIThread();
            UpdateAdornment();
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            UpdateAdornment();
        }

        private void OnViewportChanged(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            UpdateAdornment();
        }

        private void OnOptionsChanged(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            UpdateAdornment();
        }

        private void OnTextViewClosed(object sender, EventArgs e)
        {
            textView.LayoutChanged -= OnLayoutChanged;
            textView.ViewportHeightChanged -= OnViewportChanged;
            textView.ViewportWidthChanged -= OnViewportChanged;
            textView.Closed -= OnTextViewClosed;
            EasyFilePathOptions.OptionsChanged -= OnOptionsChanged;
        }

        private void UpdateAdornment()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EasyFilePathOptions options = EasyFilePathPackage.GetOptions();
            currentPath = TryGetFilePath();

            if (!options.IsEnabled || string.IsNullOrWhiteSpace(currentPath))
            {
                pathContainer.Visibility = Visibility.Collapsed;
                return;
            }

            pathContainer.Visibility = Visibility.Visible;
            pathContainer.Opacity = GetOpacity(options);
            ConfigureContainer(options);
            RenderPath(currentPath, options);
            PositionContainer(options);
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

        private void ConfigureContainer(EasyFilePathOptions options)
        {
            if (options.Placement == PathAdornmentPlacement.Watermark)
            {
                pathContainer.Background = Brushes.Transparent;
                pathContainer.BorderThickness = new Thickness(0);
                pathContainer.Padding = new Thickness(0);
                pathTextBlock.FontSize = 13.0;
            }
            else
            {
                pathContainer.Background = new SolidColorBrush(Color.FromArgb(210, 30, 30, 30));
                pathContainer.BorderThickness = new Thickness(1);
                pathContainer.Padding = new Thickness(6, 2, 6, 2);
                pathTextBlock.FontSize = 11.0;
            }
        }

        private static double GetOpacity(EasyFilePathOptions options)
        {
            int opacityPercent = Math.Max(10, Math.Min(100, options.OpacityPercent));
            return opacityPercent / 100.0;
        }

        private void RenderPath(string filePath, EasyFilePathOptions options)
        {
            pathTextBlock.Inlines.Clear();

            string separator = string.IsNullOrEmpty(options.Separator) ? " > " : options.Separator;
            Dictionary<string, Brush> highlightBrushes = ParseHighlightBrushes(options.HighlightFolders);
            IReadOnlyList<PathSegment> segments = BuildPathSegments(filePath);

            for (int i = 0; i < segments.Count; i++)
            {
                if (i > 0)
                {
                    pathTextBlock.Inlines.Add(CreateSeparatorRun(separator, options));
                }

                PathSegment segment = segments[i];
                Run segmentRun = CreateSegmentRun(segment, highlightBrushes, options);
                pathTextBlock.Inlines.Add(segmentRun);
            }
        }

        private Run CreateSeparatorRun(string separator, EasyFilePathOptions options)
        {
            Brush brush = options.Placement == PathAdornmentPlacement.Watermark
                ? new SolidColorBrush(Color.FromArgb(175, 150, 150, 150))
                : new SolidColorBrush(Color.FromArgb(210, 190, 190, 190));

            return new Run(separator)
            {
                Foreground = brush
            };
        }

        private Run CreateSegmentRun(PathSegment segment, Dictionary<string, Brush> highlightBrushes, EasyFilePathOptions options)
        {
            Run run = new Run(segment.Text)
            {
                Foreground = GetSegmentBrush(segment, highlightBrushes, options)
            };

            if (segment.IsHighlighted)
            {
                run.FontWeight = FontWeights.SemiBold;
            }

            if (segment.IsFileName)
            {
                run.Cursor = Cursors.Hand;
                run.TextDecorations = TextDecorations.Underline;
                run.MouseLeftButtonDown += OnFileNameMouseLeftButtonDown;
            }

            return run;
        }

        private Brush GetSegmentBrush(PathSegment segment, Dictionary<string, Brush> highlightBrushes, EasyFilePathOptions options)
        {
            if (!segment.IsFileName)
            {
                Brush highlightBrush;
                if (highlightBrushes.TryGetValue(segment.Text, out highlightBrush))
                {
                    segment.IsHighlighted = true;
                    return highlightBrush;
                }
            }

            if (segment.IsFileName)
            {
                return options.Placement == PathAdornmentPlacement.Watermark ? Brushes.White : Brushes.LightSkyBlue;
            }

            return options.Placement == PathAdornmentPlacement.Watermark
                ? new SolidColorBrush(Color.FromArgb(220, 230, 230, 230))
                : Brushes.WhiteSmoke;
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

        private void PositionContainer(EasyFilePathOptions options)
        {
            double maxWidth = Math.Max(0, textView.ViewportWidth - (EdgeMargin * 2));
            pathContainer.MaxWidth = maxWidth;
            pathContainer.Measure(new Size(maxWidth, double.PositiveInfinity));

            double width = Math.Min(pathContainer.DesiredSize.Width, maxWidth);
            double height = pathContainer.DesiredSize.Height;

            double left = EdgeMargin;
            double top;

            switch (options.Placement)
            {
                case PathAdornmentPlacement.Bottom:
                    top = Math.Max(EdgeMargin, textView.ViewportHeight - height - EdgeMargin);
                    break;

                case PathAdornmentPlacement.Watermark:
                    left = Math.Max(EdgeMargin, textView.ViewportWidth - width - (EdgeMargin * 2));
                    top = Math.Max(EdgeMargin, textView.ViewportHeight - height - (EdgeMargin * 3));
                    break;

                case PathAdornmentPlacement.Top:
                default:
                    top = EdgeMargin;
                    break;
            }

            Canvas.SetLeft(pathContainer, left);
            Canvas.SetTop(pathContainer, top);
        }

        private static IReadOnlyList<PathSegment> BuildPathSegments(string filePath)
        {
            List<PathSegment> segments = new List<PathSegment>();
            string root = Path.GetPathRoot(filePath);

            if (!string.IsNullOrEmpty(root))
            {
                segments.Add(new PathSegment(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), false));
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
                segments.Add(new PathSegment(parts[i], i == parts.Length - 1));
            }

            return segments;
        }

        private static Dictionary<string, Brush> ParseHighlightBrushes(string value)
        {
            Dictionary<string, Brush> brushes = new Dictionary<string, Brush>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(value))
            {
                return brushes;
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

                Brush brush = TryCreateBrush(colorText);
                if (brush != null)
                {
                    brushes[folder] = brush;
                }
            }

            return brushes;
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

        private sealed class PathSegment
        {
            internal PathSegment(string text, bool isFileName)
            {
                Text = string.IsNullOrEmpty(text) ? Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture) : text;
                IsFileName = isFileName;
            }

            internal string Text { get; }

            internal bool IsFileName { get; }

            internal bool IsHighlighted { get; set; }
        }
    }
}
