using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace EasyFilePath
{
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(EasyFilePathMargin.TopMarginName)]
    [MarginContainer(PredefinedMarginNames.Top)]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class EasyFilePathTopMarginFactory : IWpfTextViewMarginProvider
    {
        [Import]
        internal ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            return new EasyFilePathMargin(wpfTextViewHost.TextView, TextDocumentFactoryService, PathAdornmentPlacement.Top);
        }
    }

    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(EasyFilePathMargin.BottomMarginName)]
    [Order(Before = PredefinedMarginNames.HorizontalScrollBar)]
    [MarginContainer(PredefinedMarginNames.Bottom)]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class EasyFilePathBottomMarginFactory : IWpfTextViewMarginProvider
    {
        [Import]
        internal ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            return new EasyFilePathMargin(wpfTextViewHost.TextView, TextDocumentFactoryService, PathAdornmentPlacement.Bottom);
        }
    }
}
