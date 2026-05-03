using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace EasyFilePath
{
    [Export(typeof(AdornmentLayerDefinition))]
    [Name(EasyFilePathAdornment.LayerName)]
    [Order(After = PredefinedAdornmentLayers.Text)]
    internal sealed class EasyFilePathAdornmentLayerDefinition
    {
    }

    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class EasyFilePathAdornmentFactory : IWpfTextViewCreationListener
    {
        [Import]
        internal ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public void TextViewCreated(IWpfTextView textView)
        {
            new EasyFilePathAdornment(textView, TextDocumentFactoryService);
        }
    }
}
