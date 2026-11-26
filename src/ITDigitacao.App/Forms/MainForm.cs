using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using ITDigitacao.Core.Models;
using ITDigitacao.Core.Typing;

namespace ITDigitacao.App.Forms
{
    public sealed class MainForm : Form
    {
        private readonly Color red = Color.FromArgb(217, 0, 0);
        private readonly Panel pages = new Panel();
        private readonly List<Activity> activities;
        private int duration = 10;
        private TypingEngine engine;
        private Activity selected;
        private readonly Timer ticker = new Timer();
        private DateTime? sessionStart;
        private TimeSpan elapsedBeforeText;
        private Label clockLabel;
        private TextBox input;
        private Label metrics;

        public MainForm()
        {
            Text = "IT Digitação | Instituto de Transformação";
            MinimumSize = new Size(800, 600); Size = new Size(1180, 760); StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(242, 242, 242); Font = new Font("Segoe UI", 10); AutoScaleMode = AutoScaleMode.Font;
            try { Icon = new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico")); } catch { }
            Controls.Add(pages); pages.Dock = DockStyle.Fill;
            activities = LoadActivities(); ticker.Interval = 250; ticker.Tick += UpdateClock;
            ShowHome();
        }
        private List<Activity> LoadActivities()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "activities.json");
            if (!File.Exists(path)) return new List<Activity>();
            var data = new JavaScriptSerializer().Deserialize<List<Activity>>(File.ReadAllText(path));
            if (data == null || data.Select(a => a.Id).Distinct().Count() != data.Count || data.Any(a => String.IsNullOrWhiteSpace(a.Content))) throw new InvalidDataException("A biblioteca contém atividades inválidas.");
            return data;
        }
        private void ResetPage() { pages.Controls.Clear(); }
        private Label TextLabel(string text, int size, bool bold, Color color)
        { return new Label { Text = text, Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), ForeColor = color, AutoSize = true, Margin = new Padding(8) }; }
        private Button Action(string text, Action click, bool primary)
        {
            var button = new Button { Text = text, AutoSize = true, MinimumSize = new Size(140, 46), FlatStyle = FlatStyle.Flat, BackColor = primary ? red : Color.White, ForeColor = primary ? Color.White : Color.FromArgb(23, 23, 23), Font = new Font("Segoe UI", 11, FontStyle.Bold), Margin = new Padding(8), Cursor = Cursors.Hand };
            button.FlatAppearance.BorderColor = red; button.Click += delegate { click(); }; return button;
        }
        private void ShowHome()
        {
            ResetPage(); var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(36) };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 23)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var logo = new PictureBox { Size = new Size(180, 125), SizeMode = PictureBoxSizeMode.Zoom, Anchor = AnchorStyles.None };
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png"); if (File.Exists(logoPath)) logo.Image = Image.FromFile(logoPath);
            layout.Controls.Add(logo, 0, 0);
            var title = TextLabel("IT Digitação", 30, true, red); title.Anchor = AnchorStyles.None; layout.Controls.Add(title, 0, 1);
            var subtitle = TextLabel("Instituto de Transformação", 13, false, Color.DimGray); subtitle.Anchor = AnchorStyles.None; layout.Controls.Add(subtitle, 0, 2);
            var intro = TextLabel("Pratique digitação e descubra algo novo a cada texto.", 15, false, Color.FromArgb(23, 23, 23)); intro.Anchor = AnchorStyles.None; layout.Controls.Add(intro, 0, 3);
            var bottom = new FlowLayoutPanel { AutoSize = true, Anchor = AnchorStyles.None, WrapContents = true };
            var choices = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140, Font = new Font("Segoe UI", 12), Margin = new Padding(8, 15, 8, 8) }; choices.Items.AddRange(new object[] { "5 minutos", "10 minutos", "15 minutos", "20 minutos", "25 minutos", "30 minutos" }); choices.SelectedIndex = 1; choices.SelectedIndexChanged += delegate { duration = (choices.SelectedIndex + 1) * 5; };
            bottom.Controls.Add(choices); bottom.Controls.Add(Action("Escolher atividade", ShowLibrary, true)); bottom.Controls.Add(Action("Configurações", ShowSettings, false)); bottom.Controls.Add(Action("Sair", Close, false)); layout.Controls.Add(bottom, 0, 5);
            pages.Controls.Add(layout);
        }
        private void ShowLibrary()
        {
            ResetPage(); var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(22) };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(TextLabel("Biblioteca educativa", 24, true, red), 0, 0);
            var list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = false, Font = new Font("Segoe UI", 11) };
            list.Columns.Add("Atividade", 280); list.Columns.Add("Categoria", 190); list.Columns.Add("Dificuldade", 130); list.Columns.Add("Palavras", 100); list.Columns.Add("Tempo estimado", 160);
            foreach (var a in activities) { var item = new ListViewItem(a.Title); item.SubItems.Add(a.Category); item.SubItems.Add(a.Difficulty); item.SubItems.Add(a.Content.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length.ToString()); item.SubItems.Add("varia por aluno"); item.Tag = a; list.Items.Add(item); }
            var filters = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
            var category = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230 }; category.Items.Add("Todas as categorias"); category.Items.AddRange(activities.Select(a => a.Category).Distinct().ToArray()); category.SelectedIndex = 0;
            var difficulty = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 }; difficulty.Items.AddRange(new object[] { "Todas", "Iniciante", "Intermediário", "Avançado" }); difficulty.SelectedIndex = 0;
            var search = new TextBox { Width = 220 };
            search.AccessibleName = "Pesquisar título";
            search.Text = "Pesquisar título";
            search.ForeColor = Color.Gray;
            search.GotFocus += delegate { if (search.Text == "Pesquisar título") { search.Text = ""; search.ForeColor = Color.Black; } };
            search.LostFocus += delegate { if (String.IsNullOrWhiteSpace(search.Text)) { search.Text = "Pesquisar título"; search.ForeColor = Color.Gray; } };
            Func<bool> canSearch = delegate { return search.Text != "Pesquisar título"; };
            Action filter = delegate
            {
                list.BeginUpdate();
                list.Items.Clear();
                foreach (var activity in activities)
                {
                    bool matches = (category.SelectedIndex == 0 || activity.Category == category.Text)
                        && (difficulty.SelectedIndex == 0 || activity.Difficulty == difficulty.Text)
                        && (!canSearch() || activity.Title.IndexOf(search.Text, StringComparison.CurrentCultureIgnoreCase) >= 0);
                    if (!matches) continue;
                    var item = new ListViewItem(activity.Title);
                    item.SubItems.Add(activity.Category);
                    item.SubItems.Add(activity.Difficulty);
                    item.SubItems.Add(activity.Content.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length.ToString());
                    item.SubItems.Add("varia por aluno");
                    item.Tag = activity;
                    list.Items.Add(item);
                }
                list.EndUpdate();
            };
            category.SelectedIndexChanged += delegate { filter(); }; difficulty.SelectedIndexChanged += delegate { filter(); }; search.TextChanged += delegate { filter(); };
            filters.Controls.Add(category); filters.Controls.Add(difficulty); filters.Controls.Add(search); layout.Controls.Add(filters, 0, 0); layout.Controls.Add(list, 0, 1);
            var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill }; buttons.Controls.Add(Action("Voltar", ShowHome, false)); buttons.Controls.Add(Action("Aleatória", delegate { var candidates = activities.ToList(); if (candidates.Count > 0) OpenActivity(candidates[new Random().Next(candidates.Count)]); }, false)); buttons.Controls.Add(Action("Começar", delegate { if (list.SelectedItems.Count > 0) OpenActivity((Activity)list.SelectedItems[0].Tag); }, true)); layout.Controls.Add(buttons, 0, 2);
            pages.Controls.Add(layout);
        }
        private void OpenActivity(Activity activity)
        {
            selected = activity; engine = new TypingEngine(activity.Content); ShowEditor();
        }
        private void ShowEditor()
        {
            ResetPage(); var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(16) }; layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 44)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 48)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            clockLabel = TextLabel("Tempo restante: " + duration + ":00", 14, true, red); layout.Controls.Add(TextLabel(selected.Title + "  |  " + selected.Category + "  |  " + selected.Difficulty, 17, true, Color.FromArgb(23, 23, 23)), 0, 0); layout.Controls.Add(clockLabel, 1, 0);
            var reference = new RichTextBox { ReadOnly = true, Dock = DockStyle.Fill, Text = selected.Content, Font = new Font("Segoe UI", 14), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, DetectUrls = false }; layout.Controls.Add(reference, 0, 1); layout.SetColumnSpan(reference, 2);
            input = new TextBox { Multiline = true, AcceptsReturn = true, AcceptsTab = false, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 14), BorderStyle = BorderStyle.FixedSingle, AllowDrop = false };
            input.TextChanged += InputChanged; input.KeyDown += InputKeyDown; layout.Controls.Add(input, 0, 2); layout.SetColumnSpan(input, 2);
            metrics = TextLabel("PPM 0  |  Precisão 100%  |  Erros 0  |  Progresso 0%", 12, true, Color.FromArgb(23, 23, 23)); layout.Controls.Add(metrics, 0, 3);
            var controls = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft }; controls.Controls.Add(Action("Encerrar sessão", ConfirmFinish, false)); controls.Controls.Add(Action("Biblioteca", ShowLibrary, false)); controls.Controls.Add(Action("A−", delegate { AdjustFont(-1); }, false)); controls.Controls.Add(Action("A+", delegate { AdjustFont(1); }, false)); layout.Controls.Add(controls, 1, 3);
            pages.Controls.Add(layout); input.Focus();
        }
        private void InputKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Control && e.KeyCode == Keys.V) || (e.Shift && e.KeyCode == Keys.Insert)) { e.SuppressKeyPress = true; return; }
            if (e.KeyCode == Keys.Back) engine.Backspace();
        }
        private void InputChanged(object sender, EventArgs e)
        {
            string value = input.Text; if (value.Length > engine.Position) { char c = value[engine.Position]; if (sessionStart == null) { sessionStart = DateTime.UtcNow; ticker.Start(); } engine.Type(c); }
            else if (value.Length < engine.Position) engine.Backspace();
            if (engine.EnteredText != input.Text) { int caret = input.SelectionStart; input.Text = engine.EnteredText; input.SelectionStart = Math.Min(caret, input.Text.Length); }
            var span = sessionStart == null ? TimeSpan.Zero : DateTime.UtcNow - sessionStart.Value;
            metrics.Text = String.Format("PPM {0:0}  |  Precisão {1:0}%  |  Erros {2}  |  Progresso {3}%", span.TotalSeconds < 5 ? 0 : engine.CorrectCharacters / 5.0 / span.TotalMinutes, engine.Accuracy, engine.Errors, engine.ProgressPercent);
            if (engine.IsComplete) { elapsedBeforeText += span; ticker.Stop(); MessageBox.Show("Muito bem! Você concluiu o texto.", "Atividade concluída", MessageBoxButtons.OK, MessageBoxIcon.Information); ShowLibrary(); }
        }
        private void UpdateClock(object sender, EventArgs e)
        {
            var elapsed = sessionStart == null ? TimeSpan.Zero : elapsedBeforeText + (DateTime.UtcNow - sessionStart.Value);
            var left = TimeSpan.FromMinutes(duration) - elapsed; if (left < TimeSpan.Zero) left = TimeSpan.Zero;
            clockLabel.Text = "Tempo restante: " + ((int)left.TotalMinutes).ToString("00") + ":" + left.Seconds.ToString("00");
            if (left == TimeSpan.Zero) { ticker.Stop(); Finish(); }
        }
        private void AdjustFont(int amount) { if (input != null) input.Font = new Font(input.Font.FontFamily, Math.Max(10, Math.Min(24, input.Font.Size + amount))); }
        private void ConfirmFinish() { if (MessageBox.Show("Deseja finalizar sua atividade agora?", "Encerrar sessão", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) Finish(); }
        private void Finish()
        {
            ticker.Stop(); ResetPage(); var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(50) };
            panel.Controls.Add(TextLabel("Parabéns pelo seu esforço!", 28, true, red));
            int attempts = engine == null ? 0 : engine.Attempts; double accuracy = engine == null ? 100 : engine.Accuracy;
            panel.Controls.Add(TextLabel("Tempo utilizado: " + (sessionStart == null ? TimeSpan.Zero : elapsedBeforeText + (DateTime.UtcNow - sessionStart.Value)).ToString("mm\\:ss"), 15, false, Color.Black));
            panel.Controls.Add(TextLabel("Precisão " + accuracy.ToString("0") + "%  |  Erros " + (engine == null ? 0 : engine.Errors) + "  |  Textos concluídos " + (engine != null && engine.IsComplete ? 1 : 0), 15, false, Color.Black));
            panel.Controls.Add(TextLabel(attempts < 20 ? "Continue praticando. Cada palavra é um novo passo." : new string('★', ITDigitacao.Core.Metrics.MetricsCalculator.Stars(attempts, accuracy)) + "  Você está evoluindo!", 16, true, Color.FromArgb(166, 0, 0)));
            panel.Controls.Add(Action("Nova atividade", delegate { elapsedBeforeText = TimeSpan.Zero; sessionStart = null; ShowLibrary(); }, true)); panel.Controls.Add(Action("Voltar ao início", delegate { elapsedBeforeText = TimeSpan.Zero; sessionStart = null; ShowHome(); }, false)); panel.Controls.Add(Action("Sair", Close, false)); pages.Controls.Add(panel);
        }
        private void ShowSettings()
        {
            using (var form = new Form { Text = "Configurações", Size = new Size(360, 220), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false })
            { var content = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), FlowDirection = FlowDirection.TopDown }; content.Controls.Add(new Label { Text = "O tamanho da fonte pode ser ajustado durante a atividade pelos botões A− e A+.", AutoSize = true, MaximumSize = new Size(300, 0) }); content.Controls.Add(new Label { Text = "Os efeitos sonoros estão desativados.", AutoSize = true }); content.Controls.Add(Action("Fechar", form.Close, true)); form.Controls.Add(content); form.ShowDialog(this); }
        }
    }
}
