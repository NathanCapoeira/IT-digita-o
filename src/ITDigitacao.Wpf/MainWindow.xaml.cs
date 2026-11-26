using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ITDigitacao.Wpf
{
    public partial class MainWindow : Window
    {
        private int selectedDuration = 20;

        public MainWindow()
        {
            InitializeComponent();
            InitializeNavigation();
        }

        private void HomeContent_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DurationGrid == null || FeatureGrid == null) return;
            bool compact = e.NewSize.Width < 1080;
            DurationGrid.Columns = compact ? 3 : 6;
            DurationGrid.Rows = 6 / DurationGrid.Columns;
            FeatureGrid.Columns = e.NewSize.Width < 940 ? 1 : 3;
            FeatureGrid.Rows = 3 / FeatureGrid.Columns;
            HomeHeroTitle.FontSize = compact ? 29 : 34;
            HomeHeroTitle.LineHeight = compact ? 34 : 39;
            HomeContent.RowDefinitions[0].Height = new GridLength(compact ? 204 : 190);
        }

        private void Duration_Click(object sender, RoutedEventArgs e)
        {
            var choice = sender as Button;
            if (choice == null) return;
            selectedDuration = Convert.ToInt32(choice.Tag);
            foreach (var button in DurationGrid.Children.OfType<Button>())
                DurationChoice.SetIsSelected(button, ReferenceEquals(button, choice));
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            ShowLibrary();
        }

        private void Guide_Click(object sender, RoutedEventArgs e)
        {
            ShowGuide();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            ShowSettings();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
