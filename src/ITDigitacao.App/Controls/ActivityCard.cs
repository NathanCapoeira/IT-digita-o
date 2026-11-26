using System;
using System.Drawing;
using System.Windows.Forms;
using ITDigitacao.Core.Models;

namespace ITDigitacao.App.Controls
{
    internal sealed class ActivityCard : UserControl
    {
        public ActivityCard(Activity activity, Action practice)
        {
            Height = 132; Dock = DockStyle.Top; Margin = new Padding(5); BackColor = Color.White; BorderStyle = BorderStyle.FixedSingle;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Padding = new Padding(12) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 58)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
            layout.Controls.Add(new Label { Text = activity.Title, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Theme.Ink, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            layout.Controls.Add(new Label { Text = activity.Description, Font = new Font("Segoe UI", 9), ForeColor = Color.DimGray, Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft }, 0, 1);
            int words = activity.Content.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;
            var stats = new Label { Text = activity.Difficulty + "   |   " + words + " palavras   |   ≈ " + Math.Max(1, (int)Math.Ceiling(words / 15.0)) + " min", AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Theme.DarkRed, Anchor = AnchorStyles.Right };
            layout.Controls.Add(stats, 1, 0); layout.SetRowSpan(stats, 2);
            var button = new ModernButton("Praticar", practice, true) { Anchor = AnchorStyles.Right };
            layout.Controls.Add(button, 2, 0); layout.SetRowSpan(button, 2);
            Controls.Add(layout);
        }
    }
}
