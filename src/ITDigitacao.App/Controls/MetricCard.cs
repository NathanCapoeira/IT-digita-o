using System.Drawing;
using System.Windows.Forms;

namespace ITDigitacao.App.Controls
{
    internal sealed class MetricCard : UserControl
    {
        private readonly Label valueLabel;
        private readonly Label titleLabel;
        public MetricCard(string title, string value)
        {
            Height = 72; MinimumSize = new Size(112, 72); Dock = DockStyle.Fill; Margin = new Padding(4); BackColor = Color.White; BorderStyle = BorderStyle.FixedSingle;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            titleLabel = new Label { Text = title, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomCenter, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Theme.Ink };
            layout.Controls.Add(titleLabel, 0, 0);
            valueLabel = new Label { Text = value, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 15, FontStyle.Bold), ForeColor = Theme.Red };
            layout.Controls.Add(valueLabel, 0, 1); Controls.Add(layout);
        }
        public void SetValue(string value) { valueLabel.Text = value; }
        public void SetTitle(string value) { titleLabel.Text = value; }
    }
}
