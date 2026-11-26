using System;
using System.Drawing;
using System.Windows.Forms;

namespace ITDigitacao.App.Controls
{
    internal sealed class CategoryCard : UserControl
    {
        public CategoryCard(string title, string description, int count, string iconPath, Action select)
        {
            Width = 192;
            Height = 190;
            Margin = new Padding(8);
            BackColor = Color.White;
            BorderStyle = BorderStyle.FixedSingle;
            Cursor = Cursors.Hand;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(10) };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var icon = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, Margin = new Padding(2) };
            if (System.IO.File.Exists(iconPath)) icon.Image = Image.FromFile(iconPath);
            var name = new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Theme.Ink, TextAlign = ContentAlignment.MiddleLeft };
            var desc = new Label { Text = description, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(80, 80, 80) };
            var total = new Label { Text = count + (count == 1 ? " atividade" : " atividades"), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Theme.Red };
            layout.Controls.Add(icon, 0, 0); layout.Controls.Add(name, 0, 1); layout.Controls.Add(desc, 0, 2); layout.Controls.Add(total, 0, 3); Controls.Add(layout);
            Click += delegate { select(); };
            WireChildren(this, select);
        }
        private static void WireChildren(Control parent, Action select)
        {
            foreach (Control child in parent.Controls)
            {
                child.Click += delegate { select(); };
                WireChildren(child, select);
            }
        }
    }
}
