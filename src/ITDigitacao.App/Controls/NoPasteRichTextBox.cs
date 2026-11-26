using System.Windows.Forms;

namespace ITDigitacao.App.Controls
{
    internal sealed class NoPasteRichTextBox : RichTextBox
    {
        private const int WmPaste = 0x0302;

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WmPaste) return;
            base.WndProc(ref message);
        }
    }
}
