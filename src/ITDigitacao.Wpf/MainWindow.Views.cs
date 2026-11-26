using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using ITDigitacao.Core.Models;
using ITDigitacao.Core.Metrics;
using ITDigitacao.Core.Sessions;
using ITDigitacao.Core.Typing;

namespace ITDigitacao.Wpf
{
    internal static class JsonSerialization
    {
        internal static T JsonRead<T>(string json)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                return (T)serializer.ReadObject(stream);
        }

        internal static string JsonWrite<T>(T value)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, value);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }

    public partial class MainWindow
    {
        private readonly string[] categoryNames = {
            "Informática e tecnologia", "Ciências e curiosidades", "Meio ambiente", "Educação financeira",
            "Segurança digital", "Ética e cidadania", "Desenvolvimento pessoal", "Mundo profissional",
            "História e geografia", "Saúde e bons hábitos"
        };
        private readonly string[] categoryKeys = {
            "tecnologia", "ciencias", "ambiente", "financas", "seguranca", "cidadania", "desenvolvimento", "profissoes", "historia", "saude"
        };
        private readonly string[] categoryDescriptions = {
            "Descubra o mundo digital e suas possibilidades.", "Explore ideias e fenômenos da natureza.",
            "Aprenda a cuidar do nosso planeta.", "Entenda como usar o dinheiro com responsabilidade.",
            "Navegue na internet de forma segura.", "Respeito, direitos e deveres na sociedade.",
            "Dicas para estudar, se organizar e evoluir.", "Conheça profissões e o mundo do trabalho.",
            "Viaje pela história e conheça diferentes lugares.", "Aprenda a cuidar do corpo e da mente."
        };
        private readonly List<Activity> activities = new List<Activity>();
        private readonly DispatcherTimer sessionTimer = new DispatcherTimer();
        private readonly Stopwatch currentTextWatch = new Stopwatch();
        private readonly AppPreferences preferences = AppPreferences.Load();
        private SessionClock sessionClock;
        private Activity activeActivity;
        private TypingEngine typingEngine;
        private int sessionCorrectAttempts;
        private int sessionIncorrectAttempts;
        private int sessionErrors;
        private int sessionCorrectCharacters;
        private int completedTexts;
        private int durationMinutesAtStart;
        private bool warnedFiveMinutes;
        private bool warnedOneMinute;
        private TimeSpan accumulatedTextTime;
        private string selectedDifficulty = "Todas";
        private string librarySearch = String.Empty;
        private TextBox librarySearchBox;
        private WrapPanel libraryFilters;
        private WrapPanel categoryWrap;
        private StackPanel activityResults;
        private TextBlock libraryEmptyMessage;
        private Button libraryClearButton;
        private TextBlock editorClock;
        private TextBox typingInput;
        private TextBlock referenceText;
        private ProgressBar editorProgress;
        private TextBlock ppmValue;
        private TextBlock accuracyValue;
        private TextBlock errorsValue;
        private TextBlock progressValue;
        private Button finishTextButton;
        private int fontSize = 18;
        private bool isConcentrationMode;
        private int aggregateAttempts { get { return sessionCorrectAttempts + sessionIncorrectAttempts + (typingEngine == null ? 0 : typingEngine.Attempts); } }
        private double aggregateAccuracy { get { return aggregateAttempts == 0 ? 0 : 100.0 * (sessionCorrectAttempts + (typingEngine == null ? 0 : typingEngine.CorrectAttempts)) / aggregateAttempts; } }
        private int aggregateErrors { get { return sessionErrors + (typingEngine == null ? 0 : typingEngine.Errors); } }
        private int aggregateCorrectCharacters { get { return sessionCorrectCharacters + (typingEngine == null ? 0 : typingEngine.CorrectCharacters); } }
        private TimeSpan aggregateTextTime { get { return accumulatedTextTime + currentTextWatch.Elapsed; } }

        private void InitializeNavigation()
        {
            LoadActivities();
            fontSize = Math.Max(14, Math.Min(26, preferences.FontSize));
            isConcentrationMode = preferences.FocusMode;
            sessionTimer.Interval = TimeSpan.FromSeconds(1);
            sessionTimer.Tick += SessionTimer_Tick;
        }

        private void LoadActivities()
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "activities.json");
            if (!File.Exists(path)) throw new FileNotFoundException("A biblioteca educativa local não foi encontrada.", path);
            var loaded = JsonSerialization.JsonRead<List<Activity>>(File.ReadAllText(path));
            if (loaded == null || loaded.Count != 30 || loaded.Any(a => String.IsNullOrWhiteSpace(a.Id) || String.IsNullOrWhiteSpace(a.Content))
                || loaded.Select(a => a.Id).Distinct().Count() != loaded.Count)
                throw new InvalidDataException("A biblioteca local contém dados incompletos ou IDs repetidos.");
            activities.AddRange(loaded);
        }

        private Brush BrushFrom(string hex)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }

        private TextBlock Text(string value, double size, bool bold, Brush color, TextWrapping wrap = TextWrapping.NoWrap)
        {
            return new TextBlock { Text = value, FontSize = size, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = color, TextWrapping = wrap, FontFamily = (FontFamily)FindResource("BrandFont") };
        }

        private Button UiButton(string label, RoutedEventHandler click, bool primary = false)
        {
            var button = new Button { Content = label, Style = (Style)FindResource(primary ? "PrimaryButton" : "BaseButton"),
                Margin = new Thickness(4), MinHeight = 44, Cursor = Cursors.Hand };
            if (click != null) button.Click += click;
            return button;
        }

        private Border Card(UIElement content, Thickness? padding = null)
        {
            return new Border { Background = Brushes.White, BorderBrush = BrushFrom("#E2E5EA"), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(15), Padding = padding ?? new Thickness(16), Margin = new Thickness(6),
                Effect = (System.Windows.Media.Effects.Effect)FindResource("CardShadow"), Child = content };
        }

        private Image LocalImage(string relative, double width, double height, Stretch stretch = Stretch.Uniform)
        {
            return new Image { Source = new BitmapImage(new Uri("pack://application:,,,/IT-Digitacao-3.0;component/Assets/" + relative, UriKind.Absolute)),
                Width = width, Height = height, Stretch = stretch };
        }

        private Grid ScreenShell(string selected, out StackPanel content)
        {
            var root = new Grid { Background = (Brush)FindResource("BrandBackground") };
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var side = new Grid { Background = new LinearGradientBrush((Color)ColorConverter.ConvertFromString("#A60000"), (Color)ColorConverter.ConvertFromString("#760000"), 35) };
            side.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            side.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            side.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            side.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            side.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            side.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            side.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            side.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var miniBrand = new StackPanel { Margin = new Thickness(16, 20, 12, 18) };
            miniBrand.Children.Add(LocalImage("logo-instituto.png", 68, 68, Stretch.UniformToFill));
            miniBrand.Children.Add(Text("EXPLORE", 11, true, BrushFrom("#FFFFCCCC")));
            Grid.SetRow(miniBrand, 0); side.Children.Add(miniBrand);
            AddSideButton(side, "⌂   Início", 1, ShowHome);
            AddSideButton(side, "▤   Biblioteca", 2, ShowLibrary);
            AddSideButton(side, "⌨   Como digitar", 3, ShowGuide);
            AddSideButton(side, "⚙   Configurações", 4, ShowSettings);
            if (activeActivity != null) AddSideButton(side, "←   Voltar à digitação", 5, ShowEditor);
            var sideFoot = new StackPanel { Margin = new Thickness(16, 10, 12, 16), VerticalAlignment = VerticalAlignment.Bottom };
            sideFoot.Children.Add(Text("Aprenda.\nPratique.\nTransforme.", 21, true, Brushes.White, TextWrapping.Wrap));
            sideFoot.Children.Add(Text("v3.0  •  Offline", 11, true, Brushes.White));
            Grid.SetRow(sideFoot, 7); side.Children.Add(sideFoot);
            Grid.SetColumn(side, 0); root.Children.Add(side);

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
            content = new StackPanel { Margin = new Thickness(24, 20, 24, 24), MaxWidth = 1600, HorizontalAlignment = HorizontalAlignment.Stretch };
            scroll.Content = content;
            Grid.SetColumn(scroll, 1); root.Children.Add(scroll);
            return root;
        }

        private void AddSideButton(Grid side, string text, int row, Action action)
        {
            var button = new Button { Content = text, Style = (Style)FindResource("SidebarButton"), Margin = new Thickness(8, 4, 8, 4),
                Padding = new Thickness(12, 10, 8, 10), HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = !String.IsNullOrEmpty(selected) && text.Contains(selected) ? Brushes.White : Brushes.Transparent,
                Foreground = !String.IsNullOrEmpty(selected) && text.Contains(selected) ? BrushFrom("#A60000") : Brushes.White, BorderThickness = new Thickness(0), MinHeight = 44 };
            button.Click += delegate { action(); };
            Grid.SetRow(button, row); side.Children.Add(button);
        }

        private Border Hero(string title, string subtitle, string pose = "ensinando.png")
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            var copy = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8) };
            copy.Children.Add(Text(title, 30, true, Brushes.White, TextWrapping.Wrap));
            copy.Children.Add(Text(subtitle, 15, false, BrushFrom("#FFFFF4F4"), TextWrapping.Wrap));
            grid.Children.Add(copy);
            var mascot = LocalImage("Mascot/" + pose, 300, 190);
            mascot.HorizontalAlignment = HorizontalAlignment.Right; mascot.VerticalAlignment = VerticalAlignment.Bottom;
            Grid.SetColumn(mascot, 1); grid.Children.Add(mascot);
            var banner = new Border { CornerRadius = new CornerRadius(18), Padding = new Thickness(24, 18, 18, 12), Height = 156,
                Background = new LinearGradientBrush((Color)ColorConverter.ConvertFromString("#A60000"), (Color)ColorConverter.ConvertFromString("#D90000"), 0),
                ClipToBounds = true, Child = grid, Margin = new Thickness(0, 0, 0, 18) };
            return banner;
        }

        private void ShowLibrary()
        {
            var root = ScreenShell("Biblioteca", out var content);
            content.Children.Add(Hero("Biblioteca Educativa", "Escolha um assunto, descubra novos conhecimentos e pratique."));
            var filters = new WrapPanel { Margin = new Thickness(0, 0, 0, 8), VerticalAlignment = VerticalAlignment.Center };
            foreach (string difficulty in new[] { "Todas", "Iniciante", "Intermediário", "Avançado" })
            {
                string value = difficulty;
                var filter = UiButton(value, delegate { selectedDifficulty = value; RefreshLibrary(); }, value == selectedDifficulty);
                filter.Tag = value; filters.Children.Add(filter);
            }
            libraryFilters = filters;
            librarySearchBox = new TextBox { Height = 44, MinWidth = 240, Width = 300, Padding = new Thickness(12, 8, 12, 8),
                FontSize = 14, ToolTip = "Pesquisar atividade pelo título", VerticalContentAlignment = VerticalAlignment.Center };
            librarySearchBox.Text = librarySearch;
            var searchHint = Text("Pesquisar atividade pelo título…", 14, false, (Brush)FindResource("BrandMuted"));
            searchHint.Margin = new Thickness(13, 0, 4, 0); searchHint.VerticalAlignment = VerticalAlignment.Center; searchHint.IsHitTestVisible = false;
            var searchHost = new Grid { Height = 44, Width = 300, Margin = new Thickness(8, 4, 4, 4) };
            searchHost.Children.Add(librarySearchBox); searchHost.Children.Add(searchHint);
            librarySearchBox.TextChanged += delegate { searchHint.Visibility = librarySearchBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed; librarySearch = librarySearchBox.Text.Trim(); RefreshLibrary(); };
            searchHint.Visibility = librarySearchBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            filters.Children.Add(searchHost);
            content.Children.Add(filters);

            categoryWrap = new WrapPanel { ItemWidth = 220, ItemHeight = 222, Margin = new Thickness(-6, 0, -6, 0) };
            var categoriesHost = new StackPanel(); categoriesHost.Children.Add(categoryWrap);
            activityResults = new StackPanel();
            libraryEmptyMessage = Text("Nenhuma atividade encontrada. Limpe a busca ou escolha outro nível.", 15, false, (Brush)FindResource("BrandMuted"), TextWrapping.Wrap);
            libraryEmptyMessage.Margin = new Thickness(12); libraryEmptyMessage.Visibility = Visibility.Collapsed;
            libraryClearButton = UiButton("Limpar busca e filtros", delegate { selectedDifficulty = "Todas"; librarySearch = String.Empty; librarySearchBox.Text = String.Empty; RefreshLibrary(); });
            libraryClearButton.HorizontalAlignment = HorizontalAlignment.Left; libraryClearButton.Visibility = Visibility.Collapsed;
            categoriesHost.Children.Add(activityResults); categoriesHost.Children.Add(libraryEmptyMessage); categoriesHost.Children.Add(libraryClearButton);
            content.Children.Add(categoriesHost);
            var surprise = new Button { Style = (Style)FindResource("BaseButton"), Margin = new Thickness(0, 14, 0, 4), Padding = new Thickness(16),
                HorizontalContentAlignment = HorizontalAlignment.Stretch, Background = (Brush)FindResource("BrandTint"), BorderBrush = BrushFrom("#F4C7C7") };
            var surpriseText = new StackPanel(); surpriseText.Children.Add(Text("Surpreenda-me!", 20, true, BrushFrom("#A60000")));
            surpriseText.Children.Add(Text("Escolha uma atividade aleatória dentro dos filtros atuais.", 14, false, (Brush)FindResource("BrandInk"), TextWrapping.Wrap));
            surprise.Content = surpriseText; surprise.Click += RandomActivity_Click; content.Children.Add(surprise);
            content.SizeChanged += delegate { ResizeCategoryCards(); };
            PageArea.Children.Clear(); PageArea.Children.Add(root); RefreshLibrary();
        }

        private void RefreshLibrary()
        {
            if (categoryWrap == null || librarySearchBox == null) return;
            foreach (Button filter in libraryFilters.Children.OfType<Button>())
            {
                bool active = String.Equals(filter.Tag as string, selectedDifficulty, StringComparison.Ordinal);
                filter.Style = (Style)FindResource(active ? "PrimaryButton" : "BaseButton");
            }
            string query = librarySearchBox.Text.Trim();
            bool searching = query.Length > 0;
            categoryWrap.Children.Clear(); activityResults.Children.Clear();
            categoryWrap.Visibility = searching ? Visibility.Collapsed : Visibility.Visible;
            activityResults.Visibility = searching ? Visibility.Visible : Visibility.Collapsed;
            libraryEmptyMessage.Visibility = Visibility.Collapsed;
            libraryClearButton.Visibility = Visibility.Collapsed;
            if (searching)
            {
                var matches = MatchingActivities().Where(a => a.Title.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();
                foreach (Activity activity in matches) activityResults.Children.Add(ActivityCard(activity));
                libraryEmptyMessage.Visibility = matches.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                libraryClearButton.Visibility = matches.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                for (int i = 0; i < categoryNames.Length; i++)
                {
                    int index = i;
                    int count = activities.Count(a => a.Category == categoryNames[index] && (selectedDifficulty == "Todas" || a.Difficulty == selectedDifficulty));
                    categoryWrap.Children.Add(CategoryCard(index, count));
                }
            }
            ResizeCategoryCards();
        }

        private IEnumerable<Activity> MatchingActivities()
        {
            return activities.Where(a => selectedDifficulty == "Todas" || a.Difficulty == selectedDifficulty);
        }

        private UIElement CategoryCard(int index, int count)
        {
            var card = new Grid(); card.RowDefinitions.Add(new RowDefinition { Height = new GridLength(82) });
            card.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            card.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            card.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var art = LocalImage("Categories/" + categoryKeys[index] + ".png", Double.NaN, 78, Stretch.UniformToFill); art.Margin = new Thickness(0, 0, 0, 7); Grid.SetRow(art, 0); card.Children.Add(art);
            var title = Text(ShortCategory(categoryNames[index]), 16, true, (Brush)FindResource("BrandInk"), TextWrapping.Wrap); title.Margin = new Thickness(0, 1, 0, 4); Grid.SetRow(title, 1); card.Children.Add(title);
            var description = Text(categoryDescriptions[index], 13, false, (Brush)FindResource("BrandMuted"), TextWrapping.Wrap); Grid.SetRow(description, 2); card.Children.Add(description);
            var total = Text(count + (count == 1 ? " atividade" : " atividades"), 12, true, (Brush)FindResource("BrandDeepRed")); total.Margin = new Thickness(0, 6, 0, 0); Grid.SetRow(total, 3); card.Children.Add(total);
            var button = new Button { Content = card, Style = (Style)FindResource("BaseButton"), Padding = new Thickness(10), HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch, Height = 218, Margin = new Thickness(6), ToolTip = ShortCategory(categoryNames[index]) + ", " + count + " atividades" };
            button.Click += delegate { ShowCategory(categoryNames[index]); };
            return button;
        }

        private void ResizeCategoryCards()
        {
            if (categoryWrap == null || categoryWrap.ActualWidth <= 0) return;
            int columns = categoryWrap.ActualWidth > 1250 ? 5 : categoryWrap.ActualWidth > 840 ? 4 : categoryWrap.ActualWidth > 600 ? 3 : 2;
            double width = Math.Max(180, (categoryWrap.ActualWidth - columns * 12) / columns);
            foreach (FrameworkElement child in categoryWrap.Children) child.Width = width;
        }

        private string ShortCategory(string value)
        {
            if (value == "Informática e tecnologia") return "Tecnologia";
            if (value == "Ciências e curiosidades") return "Ciências";
            if (value == "Ética e cidadania") return "Cidadania";
            return value;
        }

        private void RandomActivity_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<Activity> candidates = MatchingActivities();
            string query = librarySearchBox == null ? String.Empty : librarySearchBox.Text.Trim();
            if (query.Length > 0) candidates = candidates.Where(a => a.Title.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0);
            var list = candidates.ToList();
            if (list.Count == 0) { MessageBox.Show(this, "Não há atividades para essa combinação de filtros.", "Biblioteca", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            var random = new Random(); OpenActivity(list[random.Next(list.Count)]);
        }

        private void ShowCategory(string category)
        {
            var root = ScreenShell("Biblioteca", out var content);
            var back = UiButton("← Biblioteca", delegate { ShowLibrary(); }); back.HorizontalAlignment = HorizontalAlignment.Left; content.Children.Add(back);
            int index = Array.IndexOf(categoryNames, category);
            content.Children.Add(Hero(ShortCategory(category), categoryDescriptions[index]));
            var filters = new WrapPanel();
            foreach (string difficulty in new[] { "Todas", "Iniciante", "Intermediário", "Avançado" })
            {
                string selected = difficulty;
                var button = UiButton(difficulty, delegate { selectedDifficulty = selected; ShowCategory(category); }, selectedDifficulty == difficulty);
                filters.Children.Add(button);
            }
            content.Children.Add(filters);
            var rows = new StackPanel();
            var chosen = activities.Where(a => a.Category == category && (selectedDifficulty == "Todas" || a.Difficulty == selectedDifficulty)).ToList();
            foreach (Activity activity in chosen) rows.Children.Add(ActivityCard(activity));
            if (chosen.Count == 0) rows.Children.Add(Text("Não há atividades neste nível. Escolha outro filtro.", 15, false, (Brush)FindResource("BrandMuted"), TextWrapping.Wrap));
            content.Children.Add(rows);
            PageArea.Children.Clear(); PageArea.Children.Add(root);
        }

        private Border ActivityCard(Activity activity)
        {
            int words = activity.Content.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;
            var grid = new Grid(); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var info = new StackPanel(); info.Children.Add(Text(activity.Difficulty.ToUpperInvariant(), 11, true, BrushFrom(DifficultyColor(activity.Difficulty))));
            info.Children.Add(Text(activity.Title, 19, true, (Brush)FindResource("BrandInk"), TextWrapping.Wrap));
            info.Children.Add(Text(activity.Description, 14, false, (Brush)FindResource("BrandMuted"), TextWrapping.Wrap));
            info.Children.Add(Text(String.Format("{0} palavras   •   ≈ {1} min (estimativa)", words, Math.Max(1, (int)Math.Ceiling(words / 15.0))), 13, true, (Brush)FindResource("BrandDeepRed")));
            grid.Children.Add(info);
            var practice = UiButton("Praticar  ▶", delegate { OpenActivity(activity); }, true); practice.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(practice, 1); grid.Children.Add(practice);
            var card = Card(grid, new Thickness(18)); card.Margin = new Thickness(4, 8, 4, 8); return card;
        }

        private string DifficultyColor(string value)
        {
            return "#A60000";
        }

        private void OpenActivity(Activity activity)
        {
            if (typingEngine != null) RecordCurrentText();
            activeActivity = activity; typingEngine = new TypingEngine(activity.Content); currentTextWatch.Reset();
            ShowEditor();
        }

        private void RecordCurrentText()
        {
            if (typingEngine == null) return;
            accumulatedTextTime += currentTextWatch.Elapsed;
            sessionCorrectAttempts += typingEngine.CorrectAttempts;
            sessionIncorrectAttempts += typingEngine.IncorrectAttempts;
            sessionErrors += typingEngine.Errors;
            sessionCorrectCharacters += typingEngine.CorrectCharacters;
            currentTextWatch.Reset();
        }

        private void ShowEditor()
        {
            var content = new StackPanel { Margin = new Thickness(20) };
            var top = new Grid { Background = (Brush)FindResource("BrandDeepRed"), Height = 70, Margin = new Thickness(0, 0, 0, 14) };
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var title = Text(activeActivity.Title + "  |  " + ShortCategory(activeActivity.Category) + "  •  " + activeActivity.Difficulty, 17, true, Brushes.White, TextWrapping.Wrap);
            title.Margin = new Thickness(18); title.VerticalAlignment = VerticalAlignment.Center; top.Children.Add(title);
            var leaveFocus = UiButton("Sair da concentração", delegate { isConcentrationMode = false; ShowEditor(); });
            leaveFocus.Visibility = isConcentrationMode ? Visibility.Visible : Visibility.Collapsed;
            leaveFocus.Margin = new Thickness(4); Grid.SetColumn(leaveFocus, 1); top.Children.Add(leaveFocus);
            editorClock = Text(FormatRemaining(), 24, true, Brushes.White); editorClock.Margin = new Thickness(16); editorClock.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(editorClock, 2); top.Children.Add(editorClock);
            content.Children.Add(top);

            var toolbar = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
            toolbar.Children.Add(UiButton("A−", delegate { fontSize = Math.Max(14, fontSize - 1); if (typingInput != null) typingInput.FontSize = fontSize; RefreshReference(); }));
            toolbar.Children.Add(UiButton("A+", delegate { fontSize = Math.Min(26, fontSize + 1); if (typingInput != null) typingInput.FontSize = fontSize; RefreshReference(); }));
            toolbar.Children.Add(UiButton("Configurações", delegate { ShowSettings(); }));
            toolbar.Children.Add(UiButton(isConcentrationMode ? "Sair da concentração" : "Concentração", delegate { isConcentrationMode = !isConcentrationMode; ShowEditor(); }));
            toolbar.Visibility = isConcentrationMode ? Visibility.Collapsed : Visibility.Visible;
            content.Children.Add(toolbar);

            var panes = new Grid(); panes.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); panes.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            panes.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            panes.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var referenceCard = new Border { Background = Brushes.White, BorderBrush = (Brush)FindResource("BrandBorder"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(14), Margin = new Thickness(5), Padding = new Thickness(16), MinHeight = 330 };
            var referenceStack = new DockPanel(); var refLabel = Text("Texto de referência", 17, true, (Brush)FindResource("BrandDeepRed")); DockPanel.SetDock(refLabel, Dock.Top); referenceStack.Children.Add(refLabel);
            var refScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 10, 0, 0) };
            referenceText = Text(String.Empty, fontSize, false, (Brush)FindResource("BrandInk"), TextWrapping.Wrap); referenceText.LineHeight = fontSize * 1.65; refScroll.Content = referenceText; referenceStack.Children.Add(refScroll); referenceCard.Child = referenceStack; panes.Children.Add(referenceCard);
            var inputCard = new Border { Background = Brushes.White, BorderBrush = (Brush)FindResource("BrandBorder"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(14), Margin = new Thickness(5), Padding = new Thickness(16), MinHeight = 330 };
            var inputStack = new DockPanel(); var inputLabel = Text("Sua digitação", 17, true, (Brush)FindResource("BrandDeepRed")); DockPanel.SetDock(inputLabel, Dock.Top); inputStack.Children.Add(inputLabel);
            typingInput = new TextBox { AcceptsReturn = true, AcceptsTab = false, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, BorderThickness = new Thickness(0), Background = Brushes.White, Padding = new Thickness(0, 10, 0, 0),
                FontSize = fontSize, FontFamily = (FontFamily)FindResource("BrandFont"), UndoLimit = 0 };
            typingInput.Text = typingEngine.EnteredText;
            typingInput.TextChanged += TypingInput_TextChanged;
            typingInput.PreviewKeyDown += TypingInput_PreviewKeyDown;
            DataObject.AddPastingHandler(typingInput, TypingInput_Pasting);
            inputStack.Children.Add(typingInput); inputCard.Child = inputStack; Grid.SetColumn(inputCard, 1); Grid.SetRow(inputCard, 0); panes.Children.Add(inputCard);
            panes.SizeChanged += delegate
            {
                bool stacked = panes.ActualWidth < 1040;
                panes.ColumnDefinitions[0].Width = stacked ? new GridLength(1, GridUnitType.Star) : new GridLength(1, GridUnitType.Star);
                panes.ColumnDefinitions[1].Width = stacked ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
                Grid.SetColumn(inputCard, stacked ? 0 : 1);
                Grid.SetRow(inputCard, stacked ? 1 : 0);
            };
            content.Children.Add(panes);

            var progressRow = new Grid { Margin = new Thickness(8, 10, 8, 4), Visibility = preferences.ShowMetrics ? Visibility.Visible : Visibility.Collapsed };
            progressRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); progressRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            editorProgress = new ProgressBar { Minimum = 0, Maximum = 100, Height = 10, Foreground = (Brush)FindResource("BrandRed"), Background = (Brush)FindResource("BrandBorder"), VerticalAlignment = VerticalAlignment.Center };
            progressRow.Children.Add(editorProgress); progressValue = Text("0%", 13, true, (Brush)FindResource("BrandDeepRed")); progressValue.Margin = new Thickness(10, 0, 0, 0); Grid.SetColumn(progressValue, 1); progressRow.Children.Add(progressValue); content.Children.Add(progressRow);
            var metrics = new UniformGrid { Rows = 1, Columns = 3, Margin = new Thickness(0, 4, 0, 0), Visibility = preferences.ShowMetrics ? Visibility.Visible : Visibility.Collapsed };
            ppmValue = AddMetric(metrics, "PPM", "0"); accuracyValue = AddMetric(metrics, "Precisão", "—"); errorsValue = AddMetric(metrics, "Erros", "0"); content.Children.Add(metrics);
            var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
            actions.Children.Add(UiButton("Biblioteca", delegate { ShowLibrary(); }));
            actions.Children.Add(UiButton("Encerrar sessão", EndSession));
            finishTextButton = UiButton("Concluir texto", CompleteText, true); finishTextButton.IsEnabled = typingEngine.IsComplete; actions.Children.Add(finishTextButton);
            content.Children.Add(actions);
            var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = content };
            PageArea.Children.Clear(); PageArea.Children.Add(root);
            RefreshReference(); UpdateEditorMetrics();
            if (sessionClock != null && sessionClock.HasStarted && !sessionTimer.IsEnabled) sessionTimer.Start();
        }

        private TextBlock AddMetric(Panel target, string label, string value)
        {
            var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            stack.Children.Add(Text(label, 12, true, (Brush)FindResource("BrandMuted")));
            var number = Text(value, 23, true, (Brush)FindResource("BrandDeepRed")); stack.Children.Add(number);
            target.Children.Add(Card(stack, new Thickness(12)));
            return number;
        }

        private void RefreshReference()
        {
            if (referenceText == null || typingEngine == null) return;
            referenceText.Inlines.Clear();
            string reference = activeActivity.Content;
            for (int i = 0; i < reference.Length; i++)
            {
                var run = new Run(reference[i].ToString());
                if (i < typingEngine.Position) run.Foreground = (Brush)FindResource(typingEngine.IsCorrectAt(i) ? "BrandGreen" : "BrandRed");
                if (i == typingEngine.Position) run.Background = BrushFrom("#FFFFE6A6");
                referenceText.Inlines.Add(run);
            }
            if (typingInput != null) typingInput.FontSize = fontSize;
        }

        private void TypingInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (typingEngine == null || typingInput == null) return;
            string current = typingInput.Text;
            string previous = typingEngine.EnteredText;
            int prefix = 0;
            while (prefix < current.Length && prefix < previous.Length && current[prefix] == previous[prefix]) prefix++;
            int suffix = 0;
            while (suffix < current.Length - prefix && suffix < previous.Length - prefix && current[current.Length - 1 - suffix] == previous[previous.Length - 1 - suffix]) suffix++;
            int remove = previous.Length - prefix - suffix;
            int insert = current.Length - prefix - suffix;
            if (remove == 0 && insert == 0) return;
            int previousErrors = typingEngine.Errors;
            typingEngine.Edit(prefix, remove, current.Substring(prefix, insert));
            if (preferences.Sounds && typingEngine.Errors > previousErrors) System.Media.SystemSounds.Beep.Play();
            if (insert > 0 && sessionClock == null)
            {
                durationMinutesAtStart = selectedDuration; sessionClock = new SessionClock(selectedDuration); sessionClock.StartOnFirstCharacter();
                sessionTimer.Start(); GlobalClockLabel.Text = FormatRemaining(); GlobalClockLabel.Visibility = Visibility.Visible;
            }
            if (insert > 0 && sessionClock != null && !sessionClock.HasStarted) sessionClock.StartOnFirstCharacter();
            if (insert > 0 && !currentTextWatch.IsRunning) currentTextWatch.Start();
            if (typingInput.Text != typingEngine.EnteredText)
            {
                int caret = typingInput.CaretIndex; typingInput.Text = typingEngine.EnteredText; typingInput.CaretIndex = Math.Min(caret, typingInput.Text.Length);
            }
            finishTextButton.IsEnabled = typingEngine.IsComplete;
            RefreshReference(); UpdateEditorMetrics();
        }

        private void TypingInput_Pasting(object sender, DataObjectPastingEventArgs e) { e.CancelCommand(); }
        private void TypingInput_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Paste) { e.Handled = true; }
        }
        private void TypingInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && (e.Key == Key.V || e.Key == Key.Z || e.Key == Key.Y))
                || (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && e.Key == Key.Insert)) e.Handled = true;
        }

        private void UpdateEditorMetrics()
        {
            if (typingEngine == null || ppmValue == null) return;
            ppmValue.Text = MetricsCalculator.WordsPerMinute(typingEngine.CorrectCharacters, currentTextWatch.Elapsed).ToString("0");
            accuracyValue.Text = typingEngine.Attempts == 0 ? "—" : typingEngine.Accuracy.ToString("0") + "%";
            errorsValue.Text = typingEngine.Errors.ToString();
            progressValue.Text = typingEngine.ProgressPercent + "%";
            editorProgress.Value = typingEngine.ProgressPercent;
        }

        private string FormatRemaining()
        {
            if (sessionClock == null) return String.Format("{0:00}:00", selectedDuration);
            var remaining = sessionClock.Remaining;
            return String.Format("{0:00}:{1:00}", (int)remaining.TotalMinutes, remaining.Seconds);
        }

        private void SessionTimer_Tick(object sender, EventArgs e)
        {
            if (editorClock != null) editorClock.Text = FormatRemaining();
            if (sessionClock != null) { GlobalClockLabel.Text = FormatRemaining(); GlobalClockLabel.Visibility = Visibility.Visible; }
            if (sessionClock == null) return;
            var remaining = sessionClock.Remaining;
            if (durationMinutesAtStart > 5 && !warnedFiveMinutes && remaining <= TimeSpan.FromMinutes(5) && remaining > TimeSpan.FromMinutes(1))
            {
                warnedFiveMinutes = true;
                MessageBox.Show(this, "Faltam 5 minutos para encerrar sua prática.", "Tempo de prática", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            if (!warnedOneMinute && remaining <= TimeSpan.FromMinutes(1) && remaining > TimeSpan.Zero)
            {
                warnedOneMinute = true;
                MessageBox.Show(this, "Falta 1 minuto para encerrar sua prática.", "Tempo de prática", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            if (sessionClock.IsExpired) EndSession();
            else UpdateEditorMetrics();
        }

        private void CompleteText(object sender, RoutedEventArgs e)
        {
            if (typingEngine == null || !typingEngine.IsComplete) return;
            completedTexts++;
            RecordCurrentText();
            typingEngine = null; activeActivity = null;
            ShowLibrary();
        }

        private void EndSession(object sender, RoutedEventArgs e) { EndSession(); }
        private void EndSession()
        {
            if (sessionClock == null || !sessionClock.HasStarted)
            {
                var result = MessageBox.Show(this, "Você ainda não digitou. Deseja encerrar esta sessão?", "Encerrar sessão", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
            }
            sessionTimer.Stop(); sessionClock?.Stop(); currentTextWatch.Stop(); GlobalClockLabel.Visibility = Visibility.Collapsed;
            ShowConclusion();
        }

        private void ShowConclusion()
        {
            var root = ScreenShell(String.Empty, out var content);
            content.Children.Add(Hero("Parabéns pelo seu esforço!", "Cada palavra digitada é um passo de aprendizado.", "celebrando.png"));
            var cards = new UniformGrid { Rows = 2, Columns = 3, Margin = new Thickness(-6, 0, -6, 8) };
            AddResultMetric(cards, "Tempo de prática", sessionClock == null ? "00:00" : sessionClock.Elapsed.ToString("mm\\:ss"));
            AddResultMetric(cards, "Precisão", aggregateAttempts == 0 ? "—" : aggregateAccuracy.ToString("0") + "%");
            AddResultMetric(cards, "PPM", MetricsCalculator.WordsPerMinute(aggregateCorrectCharacters, aggregateTextTime).ToString("0"));
            AddResultMetric(cards, "Erros", aggregateErrors.ToString());
            AddResultMetric(cards, "Acertos", (sessionCorrectAttempts + (typingEngine == null ? 0 : typingEngine.CorrectAttempts)).ToString());
            AddResultMetric(cards, "Textos concluídos", completedTexts.ToString());
            content.Children.Add(cards);
            int stars = MetricsCalculator.Stars(aggregateAttempts, aggregateAccuracy);
            var reward = Text(stars == 0 ? "Você praticou e aprendeu algo novo." : new string('★', stars), 26, true, BrushFrom(stars == 0 ? "#171717" : "#F5B82E"));
            reward.HorizontalAlignment = HorizontalAlignment.Center; reward.Margin = new Thickness(12); content.Children.Add(reward);
            var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center };
            buttons.Children.Add(UiButton("Nova atividade", delegate { ResetSession(); ShowLibrary(); }, true));
            buttons.Children.Add(UiButton("Voltar ao início", delegate { ResetSession(); ShowHome(); }));
            buttons.Children.Add(UiButton("Sair", delegate { Close(); })); content.Children.Add(buttons);
            PageArea.Children.Clear(); PageArea.Children.Add(root);
        }

        private void AddResultMetric(Panel host, string title, string value)
        {
            var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            stack.Children.Add(Text(title, 13, true, (Brush)FindResource("BrandMuted")));
            stack.Children.Add(Text(value, 25, true, (Brush)FindResource("BrandDeepRed")));
            host.Children.Add(Card(stack, new Thickness(18)));
        }

        private void ResetSession()
        {
            sessionTimer.Stop(); sessionClock = null; currentTextWatch.Reset(); typingEngine = null; activeActivity = null;
            GlobalClockLabel.Visibility = Visibility.Collapsed;
            warnedFiveMinutes = false; warnedOneMinute = false;
            sessionCorrectAttempts = 0; sessionIncorrectAttempts = 0; sessionErrors = 0; sessionCorrectCharacters = 0;
            accumulatedTextTime = TimeSpan.Zero; completedTexts = 0;
        }

        private void ShowHome()
        {
            librarySearch = String.Empty; selectedDifficulty = "Todas";
            if (typingEngine == null && sessionClock != null && sessionClock.IsExpired) ResetSession();
            PageArea.Children.Clear(); PageArea.Children.Add(HomeScroll);
        }

        private void ShowGuide()
        {
            var root = ScreenShell("Como digitar", out var content);
            content.Children.Add(Hero("Como digitar", "Posição dos dedos, postura confortável e atenção em cada palavra."));
            var body = new Grid(); body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }); body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var keyboard = Card(CreateKeyboard(), new Thickness(16)); body.Children.Add(keyboard);
            var tips = new StackPanel { Margin = new Thickness(10) };
            tips.Children.Add(Text("Mãos na posição inicial", 20, true, (Brush)FindResource("BrandInk"), TextWrapping.Wrap));
            tips.Children.Add(Text("Esquerda: A S D F. Direita: J K L Ç. Polegares na barra de espaço.", 14, false, (Brush)FindResource("BrandMuted"), TextWrapping.Wrap));
            tips.Children.Add(Text("Indicadores: F/G e H/J · médios: D/K · anelares: S/L · mínimos: A/Ç e teclas externas.", 14, false, (Brush)FindResource("BrandMuted"), TextWrapping.Wrap));
            tips.Children.Add(Text("Sente-se com os pés apoiados, ombros relaxados, cotovelos confortáveis e a tela na altura dos olhos. Digite com calma e faça pausas.", 14, false, (Brush)FindResource("BrandMuted"), TextWrapping.Wrap));
            tips.Children.Add(Card(CreatePostureDiagram(), new Thickness(8)));
            tips.Children.Add(LocalImage("Mascot/ensinando.png", 220, 160)); Grid.SetColumn(tips, 1); body.Children.Add(tips); content.Children.Add(body);
            PageArea.Children.Clear(); PageArea.Children.Add(root);
        }

        private UIElement CreateKeyboard()
        {
            var stack = new StackPanel(); stack.Children.Add(Text("Teclado ABNT2 — posição dos dedos", 20, true, (Brush)FindResource("BrandInk"), TextWrapping.Wrap));
            string[] rows = { "º 1 2 3 4 5 6 7 8 9 0 - = BACKSPACE", "TAB Q W E R T Y U I O P ´ [ ] \\", "CAPS A S D F G H J K L Ç ~ ENTER", "SHIFT < > Z X C V B N M , . ; / SHIFT", "CTRL WIN ALT ESPAÇO ALTGR MENU CTRL" };
            for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                string row = rows[rowIndex];
                double indent = rowIndex == 1 ? 18 : rowIndex == 2 ? 34 : rowIndex == 3 ? 50 : rowIndex == 4 ? 120 : 0;
                var keys = new WrapPanel { Margin = new Thickness(indent, 5, 0, 5), HorizontalAlignment = HorizontalAlignment.Left };
                foreach (string key in row.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    Brush fill = BrushFrom(FingerColor(key));
                    bool indexMarker = key == "F" || key == "J";
                    var keyBorder = new Border { Background = fill, BorderBrush = indexMarker ? (Brush)FindResource("BrandRed") : BrushFrom("#CBD2DD"),
                        BorderThickness = indexMarker ? new Thickness(2) : new Thickness(1), CornerRadius = new CornerRadius(5),
                        MinWidth = KeyWidth(key), Height = 39, Margin = new Thickness(2), Padding = new Thickness(5, 8, 5, 6),
                        ToolTip = indexMarker ? "Marca tátil de referência para o dedo indicador" : key };
                    keyBorder.Child = Text(key, 11, true, (Brush)FindResource("BrandInk")); keys.Children.Add(keyBorder);
                }
                stack.Children.Add(keys);
            }
            var legend = Text("Indicadores F/G e H/J · médios D/K · anelares S/L · mínimos A/Ç e teclas externas · polegares: espaço", 13, false,
                (Brush)FindResource("BrandMuted"), TextWrapping.Wrap); legend.Margin = new Thickness(4, 8, 4, 0); stack.Children.Add(legend);
            return stack;
        }

        private double KeyWidth(string key)
        {
            if (key == "ESPAÇO") return 220;
            if (key == "BACKSPACE" || key == "ENTER") return 72;
            if (key == "SHIFT") return 64;
            if (key == "CAPS" || key == "CTRL" || key == "ALTGR" || key == "MENU" || key == "TAB" || key == "ALT" || key == "WIN") return 48;
            return 34;
        }

        private UIElement CreatePostureDiagram()
        {
            var scene = new Canvas { Width = 270, Height = 176, Background = Brushes.White };
            var stroke = BrushFrom("#394150");
            scene.Children.Add(new Line { X1 = 22, Y1 = 126, X2 = 250, Y2 = 126, Stroke = stroke, StrokeThickness = 3 });
            scene.Children.Add(new Rectangle { Width = 50, Height = 48, Fill = BrushFrom("#EAF2FF"), Stroke = stroke, StrokeThickness = 2 });
            Canvas.SetLeft(scene.Children[scene.Children.Count - 1], 196); Canvas.SetTop(scene.Children[scene.Children.Count - 1], 36);
            scene.Children.Add(new Line { X1 = 222, Y1 = 84, X2 = 222, Y2 = 105, Stroke = stroke, StrokeThickness = 3 });
            scene.Children.Add(new Line { X1 = 208, Y1 = 105, X2 = 236, Y2 = 105, Stroke = stroke, StrokeThickness = 3 });
            scene.Children.Add(new Rectangle { Width = 110, Height = 10, Fill = BrushFrom("#DAB78B"), Stroke = stroke, StrokeThickness = 2 });
            Canvas.SetLeft(scene.Children[scene.Children.Count - 1], 145); Canvas.SetTop(scene.Children[scene.Children.Count - 1], 108);
            scene.Children.Add(new Rectangle { Width = 72, Height = 9, Fill = BrushFrom("#20242C"), Stroke = stroke, StrokeThickness = 1 });
            Canvas.SetLeft(scene.Children[scene.Children.Count - 1], 160); Canvas.SetTop(scene.Children[scene.Children.Count - 1], 98);
            scene.Children.Add(new Ellipse { Width = 30, Height = 30, Fill = BrushFrom("#FFD8BD"), Stroke = stroke, StrokeThickness = 2 });
            Canvas.SetLeft(scene.Children[scene.Children.Count - 1], 95); Canvas.SetTop(scene.Children[scene.Children.Count - 1], 35);
            scene.Children.Add(new Line { X1 = 110, Y1 = 66, X2 = 112, Y2 = 105, Stroke = BrushFrom("#D90000"), StrokeThickness = 12, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
            scene.Children.Add(new Line { X1 = 110, Y1 = 78, X2 = 167, Y2 = 100, Stroke = BrushFrom("#FFD8BD"), StrokeThickness = 8, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
            scene.Children.Add(new Line { X1 = 112, Y1 = 102, X2 = 160, Y2 = 108, Stroke = BrushFrom("#FFD8BD"), StrokeThickness = 7, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
            scene.Children.Add(new Line { X1 = 112, Y1 = 102, X2 = 90, Y2 = 142, Stroke = stroke, StrokeThickness = 5, StrokeStartLineCap = PenLineCap.Round });
            scene.Children.Add(new Line { X1 = 90, Y1 = 142, X2 = 134, Y2 = 142, Stroke = stroke, StrokeThickness = 5, StrokeStartLineCap = PenLineCap.Round });
            scene.Children.Add(new Line { X1 = 108, Y1 = 105, X2 = 82, Y2 = 126, Stroke = stroke, StrokeThickness = 5, StrokeStartLineCap = PenLineCap.Round });
            scene.Children.Add(Text("Costas apoiadas · tela na altura dos olhos", 11, true, (Brush)FindResource("BrandMuted"), TextWrapping.Wrap));
            Canvas.SetLeft(scene.Children[scene.Children.Count - 1], 20); Canvas.SetTop(scene.Children[scene.Children.Count - 1], 150);
            return scene;
        }

        private string FingerColor(string key)
        {
            if ("FGR TVB HJYUNM".Replace(" ", String.Empty).Contains(key)) return "#FFFFCFD4";
            if ("EDC IK,".Replace(" ", String.Empty).Contains(key)) return "#FFFFE2C6";
            if ("WSX OL.".Replace(" ", String.Empty).Contains(key)) return "#FFFFEBAE";
            if ("QAZPÇ;'/[]´~=-º".Contains(key)) return "#FFDDEBFF";
            return "#FFFFFFFF";
        }

        private void ShowSettings()
        {
            var root = ScreenShell("Configurações", out var content);
            content.Children.Add(Hero("Configurações", "Personalize a leitura e o modo de prática."));
            var card = new StackPanel();
            card.Children.Add(Text("Tamanho da fonte", 18, true, (Brush)FindResource("BrandInk")));
            var slider = new Slider { Minimum = 14, Maximum = 26, Value = fontSize, TickFrequency = 1, IsSnapToTickEnabled = true,
                Margin = new Thickness(0, 10, 0, 4), Height = 32, Style = (Style)FindResource("BrandSlider") };
            var preview = Text("Prévia: O computador é uma ferramenta para aprender.", fontSize, false, (Brush)FindResource("BrandInk"), TextWrapping.Wrap);
            slider.ValueChanged += delegate { fontSize = (int)slider.Value; preview.FontSize = fontSize; };
            card.Children.Add(slider); card.Children.Add(preview);
            var sounds = new CheckBox { Content = "Efeitos sonoros (desativados por padrão)", IsChecked = preferences.Sounds, Margin = new Thickness(0, 14, 0, 4), FontSize = 15 };
            var metrics = new CheckBox { Content = "Exibir métricas durante a digitação", IsChecked = preferences.ShowMetrics, Margin = new Thickness(0, 8, 0, 4), FontSize = 15 };
            var focus = new CheckBox { Content = "Modo concentração", IsChecked = isConcentrationMode, Margin = new Thickness(0, 8, 0, 4), FontSize = 15 };
            card.Children.Add(sounds); card.Children.Add(metrics); card.Children.Add(focus);
            var cardBorder = Card(card, new Thickness(22)); content.Children.Add(cardBorder);
            var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
            actions.Children.Add(UiButton("Restaurar padrões", delegate { fontSize = 18; sounds.IsChecked = false; metrics.IsChecked = true; focus.IsChecked = false; slider.Value = 18; }));
            actions.Children.Add(UiButton("Voltar", delegate { if (activeActivity != null) ShowEditor(); else ShowHome(); }));
            actions.Children.Add(UiButton("Salvar configurações", delegate
            {
                preferences.FontSize = fontSize; preferences.Sounds = sounds.IsChecked == true;
                preferences.ShowMetrics = metrics.IsChecked == true; preferences.FocusMode = focus.IsChecked == true;
                isConcentrationMode = preferences.FocusMode; preferences.Save();
                MessageBox.Show(this, "Preferências salvas neste computador.", "Configurações", MessageBoxButton.OK, MessageBoxImage.Information);
            }, true)); content.Children.Add(actions);
            PageArea.Children.Clear(); PageArea.Children.Add(root);
        }
    }

    internal sealed class AppPreferences
    {
        public int FontSize { get; set; } = 18;
        public bool Sounds { get; set; }
        public bool ShowMetrics { get; set; } = true;
        public bool FocusMode { get; set; }
        private static string FilePath { get { return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ITDigitacao", "settings.json"); } }
        public static AppPreferences Load()
        {
            try { if (File.Exists(FilePath)) return JsonSerialization.JsonRead<AppPreferences>(File.ReadAllText(FilePath)) ?? new AppPreferences(); } catch { }
            return new AppPreferences();
        }
        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(FilePath, JsonSerialization.JsonWrite(this));
        }
    }
}
