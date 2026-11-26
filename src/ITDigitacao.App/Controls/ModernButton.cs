using System;
using System.Drawing;
using System.Windows.Forms;

namespace ITDigitacao.App.Controls
{
    internal sealed class ModernButton : Button
    {
        public ModernButton(string text, Action action, bool primary)
        {
            Text = text;
            AutoSize = true;
            MinimumSize = new Size(112, 40);
            Padding = new Padding(12, 6, 12, 6);
            Margin = new Padding(6);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderColor = primary ? Theme.Red : Theme.Border;
            FlatAppearance.BorderSize = 1;
            BackColor = primary ? Theme.Red : Color.White;
            ForeColor = primary ? Color.White : Theme.Ink;
            Font = new Font("Segoe UI", 10, FontStyle.Bold);
            Cursor = Cursors.Hand;
            Click += delegate { if (action != null) action(); };
        }
    }
}
