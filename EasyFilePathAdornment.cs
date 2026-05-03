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
    internal sealed class EasyFilePathMargin : Grid, IWpfTextViewMargin
    {
        internal const string TopMarginName = "EasyFilePathTopMargin";
        internal const string BottomMarginName = "EasyFilePathBottomMargin";

        private const double VisibleHeight = 24.0;

        private readonly IWpfTextView textView;
        private readonly ITextDocumentFactoryService documentFactoryService;
        private readonly PathAdornmentPlacement placement;
        private readonly Border pathContainer;
        private readonly TextBlock pathTextBlock;

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
            Height = VisibleHeight;
            MinHeight = 0;
            Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));

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
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(88, 88, 88)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(8, 2, 8, 2),
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

            if (!ShouldShow(options) || string.IsNullOrWhiteSpace(currentPath))
            {
                Height = 0;
                Visibility = Visibility.Collapsed;
                return;
            }

            Height = VisibleHeight;
            Visibility = Visibility.Visible;
            Opacity = GetOpacity(options);
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
            Run run = new Run(segment.Text)
            {
                Foreground = GetSegmentBrush(segment, highlightBrushes)
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

        private Brush GetSegmentBrush(PathSegment segment, Dictionary<string, Brush> highlightBrushes)
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

            return segment.IsFileName ? Brushes.LightSkyBlue : Brushes.WhiteSmoke;
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

        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(TopMarginName);
            }
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
