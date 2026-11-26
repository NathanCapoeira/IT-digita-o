using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using ITDigitacao.App.Controls;
using ITDigitacao.App.Services;
using ITDigitacao.Core.Models;
using ITDigitacao.Core.Typing;
using ITDigitacao.Core.Metrics;

namespace ITDigitacao.App.Forms
{
    public sealed class MainForm : Form
    {
        private readonly string root = AppDomain.CurrentDomain.BaseDirectory;
        private readonly Panel page = new Panel();
        private readonly List<Activity> activities;
        private readonly AppSettings settings;
        private readonly Timer timer = new Timer();
        private readonly Stopwatch sessionWatch = new Stopwatch();
        private readonly Stopwatch textWatch = new Stopwatch();
        private int duration = 10;
        private int completedTexts;
        private int sessionAttemptsCorrect;
        private int sessionAttemptsIncorrect;
        private int sessionErrors;
        private int sessionCorrectCharacters;
        private TimeSpan accumulatedText;
        private Activity activeActivity;
        private TypingEngine engine;
        private CategoryCard[] categoryCards;
        private FlowLayoutPanel categoryGrid;
        private FlowLayoutPanel searchResults;
        private TextBox searchBox;
        private string selectedDifficulty = "Todas";
        private Label clockLabel;
        private RichTextBox referenceBox;
        private RichTextBox typingBox;
        private MetricCard ppmCard, accuracyCard, errorsCard, progressCard;

        private sealed class CategoryInfo
        {
            public string Name, Description, Icon;
            public CategoryInfo(string name, string description, string icon) { Name = name; Description = description; Icon = icon; }
        }
        private readonly CategoryInfo[] categories = {
            new CategoryInfo("Informática e tecnologia", "Descubra computadores e o mundo digital.", "tecnologia"),
            new CategoryInfo("Ciências e curiosidades", "Explore ideias e fenômenos da natureza.", "ciencias"),
            new CategoryInfo("Meio ambiente", "Aprenda a cuidar do planeta.", "ambiente"),
            new CategoryInfo("Educação financeira", "Faça escolhas conscientes com recursos.", "financas"),
            new CategoryInfo("Segurança digital", "Navegue e compartilhe com cuidado.", "seguranca"),
            new CategoryInfo("Ética e cidadania", "Respeito, direitos e deveres na comunidade.", "cidadania"),
            new CategoryInfo("Desenvolvimento pessoal", "Estude, organize-se e evolua.", "desenvolvimento"),
            new CategoryInfo("Mundo profissional", "Conheça habilidades e profissões.", "profissoes"),
            new CategoryInfo("História e geografia", "Explore lugares, culturas e histórias.", "historia"),
            new CategoryInfo("Saúde e bons hábitos", "Cuide do corpo e da mente.", "saude")
        };

        public MainForm()
        {
            Text = "IT Digitação | Instituto de Transformação";
            Size = new Size(1366, 900); MinimumSize = new Size(900, 700); StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Background; ForeColor = Theme.Ink; Font = new Font("Segoe UI", 10); AutoScaleMode = AutoScaleMode.Dpi;
            try { Icon = new Icon(Path.Combine(root, "app.ico")); } catch { }
            Controls.Add(page); page.Dock = DockStyle.Fill;
            settings = SettingsService.Load();
            activities = LoadActivities();
            timer.Interval = 250; timer.Tick += UpdateSession;
            ShowHome();
        }

        private List<Activity> LoadActivities()
        {
            string path = Path.Combine(root, "activities.json");
            var data = new JavaScriptSerializer().Deserialize<List<Activity>>(File.ReadAllText(path));
            if (data == null || data.Count != 30 || data.Any(a => String.IsNullOrWhiteSpace(a.Id) || String.IsNullOrWhiteSpace(a.Content)) || data.Select(a => a.Id).Distinct().Count() != data.Count)
                throw new InvalidDataException("A biblioteca educativa contém atividades inválidas.");
            return data;
        }
        private string Asset(string file) { return Path.Combine(root, file); }
        private Image LoadImage(string path) { try { return File.Exists(path) ? Image.FromFile(path) : null; } catch { return null; } }
        private void ClearPage() { page.Controls.Clear(); }
        private Label Label(string text, float size, bool bold, Color color)
        { return new Label { Text = text, AutoSize = true, Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), ForeColor = color, Margin = new Padding(4) }; }
        private PictureBox Mascot(string pose)
        { return new PictureBox { Image = LoadImage(Asset(pose + ".png")), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent, Dock = DockStyle.Fill }; }
        private TableLayoutPanel RootLayout()
        { return new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Theme.Background, Padding = new Padding(16) }; }

        private Control Header()
        {
            var header = new Panel { Height = 66, Dock = DockStyle.Top, BackColor = Color.White, Padding = new Padding(16, 8, 16, 8) };
            var logo = new PictureBox { Image = LoadImage(Asset("logo-conceitual.png")), SizeMode = PictureBoxSizeMode.Zoom, Width = 56, Dock = DockStyle.Left };
            var brand = new Label { Text = "IT Digitação  |  Instituto de Transformação", AutoSize = true, Dock = DockStyle.Left, Font = new Font("Segoe UI", 15, FontStyle.Bold), ForeColor = Theme.Ink, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 10, 0, 0) };
            var settingsButton = new ModernButton("Configurações", ShowSettings, false) { Dock = DockStyle.Right };
            header.Controls.Add(settingsButton); header.Controls.Add(brand); header.Controls.Add(logo);
            return header;
        }
        private Control Sidebar(string selected)
        {
            var side = new Panel { Width = 184, Dock = DockStyle.Left, BackColor = Theme.DarkRed, Padding = new Padding(8) };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Theme.DarkRed };
            var insignia = new PictureBox { Image = LoadImage(Asset("logo-conceitual.png")), Size = new Size(150, 85), SizeMode = PictureBoxSizeMode.Zoom, Margin = new Padding(6) }; flow.Controls.Add(insignia);
            AddNav("⌂   Início", selected == "Início", ShowHome);
            AddNav("▤   Biblioteca", selected == "Biblioteca", ShowLibrary);
            AddNav("⌨   Como digitar", selected == "Como digitar", ShowGuide);
            AddNav("⚙   Configurações", selected == "Configurações", ShowSettings);
            void AddNav(string text, bool active, Action action)
            {
                var button = new Button { Text = text, Width = 158, Height = 46, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0), Margin = new Padding(2, 5, 2, 5), FlatStyle = FlatStyle.Flat, BackColor = active ? Color.White : Theme.DarkRed, ForeColor = active ? Theme.Red : Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
                button.FlatAppearance.BorderSize = 0; button.Click += delegate { action(); }; flow.Controls.Add(button);
            }
            var bottom = Label("Instituto de\nTransformação\n\nv2.0  •  Offline", 9, true, Color.White); bottom.TextAlign = ContentAlignment.MiddleCenter; bottom.Dock = DockStyle.Bottom;
            side.Controls.Add(flow); side.Controls.Add(bottom); return side;
        }
        private Control Banner(string title, string subtitle, string pose, int height)
        {
            var banner = new Panel { Height = height, Dock = DockStyle.Top, BackColor = Theme.Red, Padding = new Padding(24, 12, 12, 12), Margin = new Padding(0, 8, 0, 12) };
            var mascot = Mascot(pose); mascot.Width = Math.Min(250, height * 2); mascot.Dock = DockStyle.Right;
            var copy = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent, ColumnCount = 1, RowCount = 2 };
            copy.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            copy.RowStyles.Add(new RowStyle(SizeType.Percent, 58)); copy.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
            copy.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, Font = new Font("Segoe UI", height > 125 ? 22 : 18, FontStyle.Bold), ForeColor = Color.White, TextAlign = ContentAlignment.BottomLeft }, 0, 0);
            copy.Controls.Add(new Label { Text = subtitle, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12), ForeColor = Color.White, TextAlign = ContentAlignment.TopLeft }, 0, 1);
            banner.Controls.Add(copy); banner.Controls.Add(mascot); return banner;
        }

        private void ShowHome()
        {
            ClearPage(); var shell = RootLayout(); shell.RowStyles.Add(new RowStyle(SizeType.AutoSize)); shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            shell.Controls.Add(Header(), 0, 0);
            var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(0, 12, 0, 0) };
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 172)); content.RowStyles.Add(new RowStyle(SizeType.AutoSize)); content.RowStyles.Add(new RowStyle(SizeType.AutoSize)); content.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.Controls.Add(Banner("Vamos aprender algo incrível hoje?", "Descubra novos conhecimentos enquanto desenvolve suas habilidades no teclado.", "boas-vindas", 158), 0, 0);
            var prompt = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(4, 8, 4, 4) };
            prompt.Controls.Add(Label("◷  Escolha seu tempo de prática", 16, true, Theme.Ink)); prompt.Controls.Add(Label("Quanto tempo você tem disponível hoje?", 11, false, Color.DimGray)); content.Controls.Add(prompt, 0, 1);
            var durations = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, FlowDirection = FlowDirection.LeftToRight };
            foreach (int minutes in new[] { 5, 10, 15, 20, 25, 30 })
            {
                int selectedMinutes = minutes;
                var button = new Button { Text = (minutes == duration ? "✓\n" : "◷\n") + minutes + "\nminutos", Width = 132, Height = 78, Margin = new Padding(7), FlatStyle = FlatStyle.Flat, BackColor = minutes == duration ? Theme.Tint : Color.White, ForeColor = Theme.Ink, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
                button.FlatAppearance.BorderColor = minutes == duration ? Theme.Red : Theme.Border;
                button.Click += delegate { duration = selectedMinutes; ShowHome(); };
                durations.Controls.Add(button);
            }
            content.Controls.Add(durations, 0, 2);
            var start = new ModernButton("▶   Começar minha aventura   →", ShowLibrary, true) { Height = 54, Width = 360, Anchor = AnchorStyles.None, Font = new Font("Segoe UI", 14, FontStyle.Bold) }; content.Controls.Add(start, 0, 3);
            var cards = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
            cards.Controls.Add(InfoCard("Aprenda", "Descubra conteúdos úteis e curiosos.", "▤")); cards.Controls.Add(InfoCard("Pratique", "Melhore sua digitação de forma divertida.", "⌨")); cards.Controls.Add(InfoCard("Evolua", "Acompanhe seu progresso em cada desafio.", "★")); content.Controls.Add(cards, 0, 4);
            shell.Controls.Add(content, 0, 1); page.Controls.Add(shell);
        }
        private Control InfoCard(string title, string text, string symbol)
        {
            var box = new Panel { Width = 300, Height = 76, BackColor = Color.White, Margin = new Padding(6), Padding = new Padding(10) };
            box.Controls.Add(new Label { Text = symbol, Dock = DockStyle.Left, Width = 52, ForeColor = Theme.Red, Font = new Font("Segoe UI", 24, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter });
            box.Controls.Add(new Label { Text = title + "\r\n" + text, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), ForeColor = Theme.Ink }); return box;
        }

        private void ShowLibrary()
        {
            ClearPage(); categoryCards = null;
            var shell = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
            var main = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), AutoScroll = true };
            var host = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 4 };
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            host.Controls.Add(Banner("Biblioteca Educativa", "Escolha um assunto, descubra novos conhecimentos e pratique.", "ensinando", 128), 0, 0);
            var filterBar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true, Padding = new Padding(0, 10, 0, 10) };
            foreach (string difficulty in new[] { "Todas", "Iniciante", "Intermediário", "Avançado" })
            {
                string value = difficulty; var b = new ModernButton(value, delegate { selectedDifficulty = value; ApplyCategoryFilter(); }, value == selectedDifficulty); filterBar.Controls.Add(b);
            }
            searchBox = new TextBox { Width = 240, Font = new Font("Segoe UI", 11), Margin = new Padding(8, 10, 8, 8), AccessibleName = "Pesquisar atividade pelo título" }; searchBox.TextChanged += delegate { ApplyCategoryFilter(); }; filterBar.Controls.Add(searchBox);
            host.Controls.Add(filterBar, 0, 1);
            var grid = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 4, 0, 8) };
            categoryGrid = grid;
            categoryCards = new CategoryCard[categories.Length];
            for (int i = 0; i < categories.Length; i++)
            {
                int index = i; var info = categories[i]; int count = activities.Count(a => a.Category == info.Name);
                categoryCards[i] = new CategoryCard(ShortCategory(info.Name), info.Description, count, Asset(Path.Combine("icones", info.Icon + ".svg.png")), delegate { ShowCategory(categories[index].Name); });
                categoryCards[i].Tag = info.Name; grid.Controls.Add(categoryCards[i]);
            }
            host.Controls.Add(grid, 0, 2);
            searchResults = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Visible = false };
            host.Controls.Add(searchResults, 0, 3);
            var random = new ModernButton("↗   Surpreenda-me!   Escolher atividade aleatória", delegate { if (activities.Count > 0) OpenActivity(activities[new Random().Next(activities.Count)]); }, true) { Dock = DockStyle.Top, Height = 50 };
            host.Controls.Add(random, 0, 4); host.RowCount = 5; main.Controls.Add(host); shell.Controls.Add(main); shell.Controls.Add(Sidebar("Biblioteca")); page.Controls.Add(shell);
            main.SizeChanged += delegate { int columns = Math.Max(1, Math.Min(5, (main.ClientSize.Width - 48) / 208)); foreach (var card in categoryCards) card.Width = Math.Max(150, (main.ClientSize.Width - 48) / columns - 18); };
        }
        private string ShortCategory(string name)
        {
            if (name == "Informática e tecnologia") return "Tecnologia";
            if (name == "Ciências e curiosidades") return "Ciências";
            if (name == "Ética e cidadania") return "Cidadania";
            return name;
        }
        private void ApplyCategoryFilter()
        {
            if (categoryCards == null) return;
            string query = searchBox == null ? "" : searchBox.Text.Trim();
            searchResults.Controls.Clear();
            var matchingActivities = activities.Where(a => (selectedDifficulty == "Todas" || a.Difficulty == selectedDifficulty)
                && a.Title.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();
            bool searching = !String.IsNullOrWhiteSpace(query);
            searchResults.Visible = searching;
            categoryGrid.Visible = !searching;
            foreach (var activity in matchingActivities)
            {
                var selected = activity;
                searchResults.Controls.Add(new ActivityCard(activity, delegate { OpenActivity(selected); }) { Width = Math.Max(500, searchResults.ClientSize.Width - 32) });
            }
            foreach (var card in categoryCards)
            {
                string cat = (string)card.Tag;
                int count = activities.Count(a => a.Category == cat && (selectedDifficulty == "Todas" || a.Difficulty == selectedDifficulty));
                card.SetCount(count);
                card.Visible = count > 0;
            }
        }
        private void ShowCategory(string category)
        {
            ClearPage(); var shell = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
            var main = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };
            var host = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 3 };
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int row = 0; row < 3; row++) host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var info = categories.First(c => c.Name == category);
            var top = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top }; top.Controls.Add(new ModernButton("← Biblioteca", ShowLibrary, false)); top.Controls.Add(Label("Biblioteca  ›  " + ShortCategory(category), 20, true, Theme.Ink)); host.Controls.Add(top, 0, 0);
            host.Controls.Add(Banner(ShortCategory(category), info.Description, "ensinando", 118), 0, 1);
            var list = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            foreach (var activity in activities.Where(a => a.Category == category).OrderBy(a => DifficultyOrder(a.Difficulty)))
            { var selected = activity; list.Controls.Add(new ActivityCard(activity, delegate { OpenActivity(selected); }) { Width = Math.Max(540, main.ClientSize.Width - 64) }); }
            host.Controls.Add(list, 0, 2); main.Controls.Add(host); shell.Controls.Add(main); shell.Controls.Add(Sidebar("Biblioteca")); page.Controls.Add(shell);
            main.SizeChanged += delegate { foreach (Control item in list.Controls) item.Width = Math.Max(540, main.ClientSize.Width - 64); };
        }
        private int DifficultyOrder(string value) { return value == "Iniciante" ? 0 : value == "Intermediário" ? 1 : 2; }

        private void OpenActivity(Activity activity)
        {
            if (activeActivity != null && engine != null && textWatch.IsRunning)
            {
                accumulatedText += textWatch.Elapsed;
                sessionAttemptsCorrect += engine.CorrectAttempts;
                sessionAttemptsIncorrect += engine.IncorrectAttempts;
                sessionErrors += engine.Errors;
                sessionCorrectCharacters += engine.CorrectCharacters;
            }
            activeActivity = activity; engine = new TypingEngine(activity.Content); textWatch.Reset();
            if (sessionWatch.IsRunning) textWatch.Start();
            ShowEditor();
        }
        private void ShowEditor()
        {
            ClearPage(); var shell = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
            var header = new Panel { Dock = DockStyle.Top, Height = 66, BackColor = Theme.DarkRed, Padding = new Padding(18, 10, 18, 8) };
            header.Controls.Add(new Label { Text = activeActivity.Title + "  |  " + ShortCategory(activeActivity.Category) + "  •  " + activeActivity.Difficulty, Dock = DockStyle.Left, Width = 620, ForeColor = Color.White, Font = new Font("Segoe UI", 14, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });
            clockLabel = new Label { Text = "Tempo restante: " + duration + ":00", Dock = DockStyle.Right, Width = 210, ForeColor = Color.White, Font = new Font("Segoe UI", 14, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight }; header.Controls.Add(clockLabel); shell.Controls.Add(header);
            var toolRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, BackColor = Color.White, Padding = new Padding(10, 4, 4, 4), WrapContents = false };
            toolRow.Controls.Add(new ModernButton("A−", delegate { SetFontSize(settings.FontSize - 1); }, false)); toolRow.Controls.Add(new ModernButton("A+", delegate { SetFontSize(settings.FontSize + 1); }, false));
            toolRow.Controls.Add(new ModernButton("Configurações", ShowSettings, false)); toolRow.Controls.Add(new ModernButton("Concentração", delegate { settings.FocusMode = !settings.FocusMode; toolRow.Visible = !settings.FocusMode; }, false)); shell.Controls.Add(toolRow);
            var panels = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(12) };
            panels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); panels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); panels.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); panels.RowStyles.Add(new RowStyle(SizeType.Absolute, settings.ShowMetrics ? 116 : 52));
            var left = DocumentPanel("Texto de referência", out referenceBox); referenceBox.ReadOnly = true; referenceBox.Text = activeActivity.Content; referenceBox.SelectionStart = 0;
            var right = DocumentPanel("Sua digitação", out typingBox); typingBox.ReadOnly = false; typingBox.AllowDrop = false; typingBox.AcceptsTab = false; typingBox.TextChanged += TypingChanged; typingBox.KeyDown += TypingKeyDown; typingBox.Font = new Font("Segoe UI", settings.FontSize); right.Tag = typingBox;
            panels.Controls.Add(left, 0, 0); panels.Controls.Add(right, 1, 0); panels.SetColumnSpan(left, 1); panels.SetColumnSpan(right, 1);
            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1, Padding = new Padding(2) };
            for (int i = 0; i < 4; i++) footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
            ppmCard = new MetricCard("PPM", "0"); accuracyCard = new MetricCard("Precisão", "—"); errorsCard = new MetricCard("Erros", "0"); progressCard = new MetricCard("Progresso", "0%");
            if (!settings.ShowMetrics) { ppmCard.SetTitle("Prática"); accuracyCard.SetTitle("Ativa"); errorsCard.SetTitle(""); progressCard.SetTitle(""); }
            footer.Controls.Add(ppmCard, 0, 0); footer.Controls.Add(accuracyCard, 1, 0); footer.Controls.Add(errorsCard, 2, 0); footer.Controls.Add(progressCard, 3, 0);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = true };
            actions.Controls.Add(new ModernButton("Encerrar sessão", ConfirmFinish, false)); actions.Controls.Add(new ModernButton("Biblioteca", ShowLibrary, false)); actions.Controls.Add(new ModernButton("Concluir texto", CompleteText, true)); footer.Controls.Add(actions, 4, 0); panels.Controls.Add(footer, 0, 1); panels.SetColumnSpan(footer, 2);
            shell.Controls.Add(panels); page.Controls.Add(shell);
            if (sessionWatch.IsRunning && !textWatch.IsRunning) textWatch.Start();
            typingBox.Focus();
        }
        private Panel DocumentPanel(string title, out RichTextBox box)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12), Margin = new Padding(6) };
            panel.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 32, ForeColor = Theme.Ink, Font = new Font("Segoe UI", 12, FontStyle.Bold) });
            box = new NoPasteRichTextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", settings.FontSize), WordWrap = true, DetectUrls = false, BackColor = Color.White, ScrollBars = RichTextBoxScrollBars.Vertical };
            panel.Controls.Add(box); return panel;
        }
        private void SetFontSize(int size)
        {
            settings.FontSize = Math.Max(14, Math.Min(26, size));
            if (referenceBox != null) referenceBox.Font = new Font("Segoe UI", settings.FontSize);
            if (typingBox != null) typingBox.Font = new Font("Segoe UI", settings.FontSize);
            SettingsService.Save(settings);
        }
        private void TypingKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Control && (e.KeyCode == Keys.V || e.KeyCode == Keys.Z || e.KeyCode == Keys.Y))
                || (e.Shift && e.KeyCode == Keys.Insert)) { e.SuppressKeyPress = true; return; }
        }
        private void TypingChanged(object sender, EventArgs e)
        {
            if (typingBox == null || engine == null) return;
            string current = typingBox.Text;
            string previous = engine.EnteredText;
            int prefix = 0;
            while (prefix < previous.Length && prefix < current.Length && previous[prefix] == current[prefix]) prefix++;
            int suffix = 0;
            while (suffix < previous.Length - prefix && suffix < current.Length - prefix
                && previous[previous.Length - 1 - suffix] == current[current.Length - 1 - suffix]) suffix++;
            int removed = previous.Length - prefix - suffix;
            int insertedLength = current.Length - prefix - suffix;
            if (insertedLength > 0 && !sessionWatch.IsRunning)
            {
                sessionWatch.Start(); timer.Start(); textWatch.Start();
            }
            if (removed > 0 || insertedLength > 0)
                engine.Edit(prefix, removed, current.Substring(prefix, insertedLength));
            if (engine.EnteredText != current) { int caret = typingBox.SelectionStart; typingBox.Text = engine.EnteredText; typingBox.SelectionStart = Math.Min(caret, typingBox.Text.Length); }
            RenderTypedFeedback(); UpdateMetrics();
        }
        private void RenderTypedFeedback()
        {
            if (typingBox == null || engine == null) return;
            int caret = typingBox.SelectionStart; int scroll = typingBox.GetCharIndexFromPosition(new Point(1, 1));
            typingBox.SelectAll(); typingBox.SelectionColor = Theme.Ink; typingBox.SelectionBackColor = Color.White; typingBox.SelectionFont = typingBox.Font;
            for (int i = 0; i < engine.Position; i++)
            {
                typingBox.Select(i, 1);
                typingBox.SelectionColor = engine.IsCorrectAt(i) ? Theme.Green : Theme.Red;
                if (!engine.IsCorrectAt(i))
                {
                    using (var errorFont = new Font(typingBox.Font, FontStyle.Underline)) typingBox.SelectionFont = errorFont;
                }
            }
            if (engine.Position < engine.ReferenceLength)
            {
                typingBox.Select(engine.Position, 0); typingBox.SelectionBackColor = Theme.Tint;
            }
            typingBox.Select(caret, 0); typingBox.SelectionColor = Theme.Ink;
            if (scroll > 0 && scroll < typingBox.TextLength) { typingBox.Select(scroll, 0); typingBox.ScrollToCaret(); typingBox.Select(caret, 0); }
        }
        private void UpdateMetrics()
        {
            if (engine == null || ppmCard == null) return;
            ppmCard.SetValue(MetricsCalculator.WordsPerMinute(engine.CorrectCharacters, textWatch.Elapsed).ToString("0"));
            accuracyCard.SetValue(engine.Attempts == 0 ? "—" : engine.Accuracy.ToString("0") + "%");
            errorsCard.SetValue(engine.Errors.ToString()); progressCard.SetValue(engine.ProgressPercent + "%");
        }
        private void UpdateSession(object sender, EventArgs e)
        {
            if (!sessionWatch.IsRunning || clockLabel == null) return;
            var remaining = TimeSpan.FromMinutes(duration) - sessionWatch.Elapsed;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            clockLabel.Text = "Tempo restante: " + ((int)remaining.TotalMinutes).ToString("00") + ":" + remaining.Seconds.ToString("00");
            if (remaining == TimeSpan.Zero) FinishSession(); else UpdateMetrics();
        }
        private void CompleteText()
        {
            if (engine == null || !engine.IsComplete)
            { MessageBox.Show("Continue digitando o texto de referência antes de concluir.", "Texto incompleto", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            completedTexts++; accumulatedText += textWatch.Elapsed;
            sessionAttemptsCorrect += engine.CorrectAttempts; sessionAttemptsIncorrect += engine.IncorrectAttempts; sessionErrors += engine.Errors; sessionCorrectCharacters += engine.CorrectCharacters;
            activeActivity = null; engine = null; textWatch.Reset();
            MessageBox.Show("Muito bem! Você concluiu este texto.", "Atividade concluída", MessageBoxButtons.OK, MessageBoxIcon.Information); ShowLibrary();
        }
        private void ConfirmFinish()
        {
            if (MessageBox.Show("Deseja finalizar sua atividade agora?", "Encerrar sessão", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) FinishSession();
        }
        private void FinishSession()
        {
            timer.Stop(); sessionWatch.Stop(); textWatch.Stop(); ShowConclusion();
        }
        private void ShowConclusion()
        {
            ClearPage(); var rootPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Theme.Background, Padding = new Padding(30) };
            rootPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Color.White, Padding = new Padding(20) };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32)); content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68)); content.RowStyles.Add(new RowStyle(SizeType.Percent, 75)); content.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            content.Controls.Add(Mascot("celebrando"), 0, 0); content.SetRowSpan(content.GetControlFromPosition(0, 0), 2);
            var results = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(16) };
            int totalAttempts = sessionAttemptsCorrect + sessionAttemptsIncorrect + (engine == null ? 0 : engine.Attempts);
            double accuracy = totalAttempts == 0 ? 0 : 100.0 * (sessionAttemptsCorrect + (engine == null ? 0 : engine.CorrectAttempts)) / totalAttempts;
            int stars = MetricsCalculator.Stars(totalAttempts, accuracy);
            results.Controls.Add(Label("Parabéns pelo seu esforço!", 24, true, Theme.Red)); results.Controls.Add(Label("Você praticou digitação e aprendeu algo novo.", 13, false, Theme.Ink));
            results.Controls.Add(Label(stars == 0 ? "Cada palavra digitada é um passo de aprendizado." : new string('★', stars), 23, true, Theme.Gold));
            results.Controls.Add(Label("Tempo de prática: " + sessionWatch.Elapsed.ToString("mm\\:ss") + "     Precisão: " + (totalAttempts == 0 ? "—" : accuracy.ToString("0") + "%"), 13, true, Theme.Ink));
            results.Controls.Add(Label("PPM: " + MetricsCalculator.WordsPerMinute(sessionCorrectCharacters + (engine == null ? 0 : engine.CorrectCharacters), accumulatedText + textWatch.Elapsed).ToString("0") + "     Erros: " + (sessionErrors + (engine == null ? 0 : engine.Errors)) + "     Textos concluídos: " + completedTexts, 13, true, Theme.Ink));
            content.Controls.Add(results, 1, 0);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            buttons.Controls.Add(new ModernButton("Sair", Close, false)); buttons.Controls.Add(new ModernButton("Voltar ao início", delegate { ResetSession(); ShowHome(); }, false)); buttons.Controls.Add(new ModernButton("Nova atividade", delegate { ShowLibrary(); }, true));
            content.Controls.Add(buttons, 1, 1); rootPanel.Controls.Add(content, 0, 0); page.Controls.Add(rootPanel);
        }
        private void ResetSession()
        {
            timer.Stop(); sessionWatch.Reset(); textWatch.Reset(); accumulatedText = TimeSpan.Zero; completedTexts = 0; sessionAttemptsCorrect = 0; sessionAttemptsIncorrect = 0; sessionErrors = 0; sessionCorrectCharacters = 0; activeActivity = null; engine = null;
        }

        private void ShowGuide()
        {
            ClearPage(); var shell = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
            var main = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };
            var host = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 3 };
            host.Controls.Add(Banner("Como digitar", "Posição dos dedos, postura confortável e atenção em cada palavra.", "ensinando", 128), 0, 0);
            var keysPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.White, Padding = new Padding(16) };
            keysPanel.Controls.Add(Label("Teclado ABNT2 — posição inicial", 17, true, Theme.Ink));
            foreach (string row in new[] { "1234567890-=", "QWERTYUIOP´[", "ASDFGHJKLÇ~", "ZXCVBNM,.;/" })
            {
                var keyRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
                foreach (char key in row)
                {
                    bool home = "ASDFGHJKLÇ".Contains(key); var keyButton = new Label { Text = key.ToString(), Width = 42, Height = 38, Margin = new Padding(3), BackColor = home ? Theme.Tint : Color.FromArgb(235, 243, 255), ForeColor = Theme.Ink, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 12, FontStyle.Bold), BorderStyle = BorderStyle.FixedSingle };
                    keyRow.Controls.Add(keyButton);
                }
                keysPanel.Controls.Add(keyRow);
            }
            var space = new Label { Text = "ESPAÇO", Width = 300, Height = 38, Margin = new Padding(60, 4, 4, 12), BackColor = Color.FromArgb(235, 243, 255), TextAlign = ContentAlignment.MiddleCenter, BorderStyle = BorderStyle.FixedSingle }; keysPanel.Controls.Add(space);
            keysPanel.Controls.Add(Label("Mãos na posição inicial: esquerda A S D F · direita J K L Ç · polegares na barra de espaço.", 11, true, Theme.Ink));
            keysPanel.Controls.Add(Label("Indicadores F/G e H/J · médios D/K · anelares S/L · mínimos A/Ç e teclas externas. As marcas F e J ajudam a localizar os indicadores.", 10, false, Theme.Ink));
            keysPanel.Controls.Add(Label("Sente-se com os pés apoiados, ombros relaxados, cotovelos confortáveis e monitor na altura dos olhos. Digite com calma e faça pausas.", 10, false, Theme.Ink));
            host.Controls.Add(keysPanel, 0, 1); host.Controls.Add(new ModernButton("Voltar ao início", ShowHome, false), 0, 2); main.Controls.Add(host); shell.Controls.Add(main); shell.Controls.Add(Sidebar("Como digitar")); page.Controls.Add(shell);
        }

        private void ShowSettings()
        {
            ClearPage(); var shell = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
            var main = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };
            var host = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 3 };
            host.Controls.Add(Banner("Configurações", "Personalize a leitura e o modo de prática.", "ensinando", 120), 0, 0);
            var settingsPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.White, Padding = new Padding(20) };
            settingsPanel.Controls.Add(Label("Tamanho da fonte", 14, true, Theme.Ink));
            var font = new TrackBar { Minimum = 14, Maximum = 26, Value = Math.Max(14, Math.Min(26, settings.FontSize)), TickFrequency = 2, Width = 420 };
            var preview = Label("Prévia: O computador é uma ferramenta para aprender.", settings.FontSize, false, Theme.Ink);
            font.ValueChanged += delegate { preview.Font = new Font("Segoe UI", font.Value); }; settingsPanel.Controls.Add(font); settingsPanel.Controls.Add(preview);
            var sounds = new CheckBox { Text = "Efeitos sonoros (desativados por padrão)", Checked = settings.Sounds, AutoSize = true, Font = new Font("Segoe UI", 11) }; settingsPanel.Controls.Add(sounds);
            var metrics = new CheckBox { Text = "Exibir métricas durante a digitação", Checked = settings.ShowMetrics, AutoSize = true, Font = new Font("Segoe UI", 11) }; settingsPanel.Controls.Add(metrics);
            var focus = new CheckBox { Text = "Modo concentração", Checked = settings.FocusMode, AutoSize = true, Font = new Font("Segoe UI", 11) }; settingsPanel.Controls.Add(focus);
            host.Controls.Add(settingsPanel, 0, 1);
            var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            actions.Controls.Add(new ModernButton("Salvar configurações", delegate { settings.FontSize = font.Value; settings.Sounds = sounds.Checked; settings.ShowMetrics = metrics.Checked; settings.FocusMode = focus.Checked; SettingsService.Save(settings); MessageBox.Show("Preferências salvas neste computador.", "Configurações", MessageBoxButtons.OK, MessageBoxIcon.Information); }, true));
            actions.Controls.Add(new ModernButton("Restaurar padrões", delegate { settings.FontSize = 18; settings.Sounds = false; settings.ShowMetrics = true; settings.FocusMode = false; SettingsService.Save(settings); ShowSettings(); }, false));
            actions.Controls.Add(new ModernButton("Voltar ao início", ShowHome, false)); host.Controls.Add(actions, 0, 2); main.Controls.Add(host); shell.Controls.Add(main); shell.Controls.Add(Sidebar("Configurações")); page.Controls.Add(shell);
        }
    }
}
