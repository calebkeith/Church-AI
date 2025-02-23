using Mscc.GenerativeAI;
using SpeechPlayground;
using SpeechPlayground.Animation;
using SpeechPlayground.Data.Model;
using SpeechPlayground.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SpeechPlayground.Controls
{
    public partial class BibleVerseOverlay : UserControl
    {
        public IBibleAiDetectionService AiDetectionService
        {
            get { return (IBibleAiDetectionService)GetValue(AiDetectionServiceProperty); }
            set { SetValue(AiDetectionServiceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AiDetectionService.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AiDetectionServiceProperty =
            DependencyProperty.Register("AiDetectionService", typeof(IBibleAiDetectionService), typeof(BibleVerseOverlay), new PropertyMetadata(null));


        public BibleVerseOverlay()
        {
            InitializeComponent();
            DataContext = this;
        }

        public async Task SetVerses(List<Verse> verses, List<Verse> seeAlsoVerses)
        {
            MainGrid.ColumnDefinitions.Clear();
            MainGrid.Children.Clear();

            if (verses?.Any() ?? false)
            {
                await AdjustGridLayout(verses);
                UpdateLayout();
                StartScrolling();
            }
        }

        private async Task AdjustGridLayout(List<Verse> verses)
        {
            var groupedVerses = verses.GroupBy(x => new { x.Chapter, x.Book }).Take(5);

            int columnCount = groupedVerses.Count();

            if (columnCount == 0) return;

            for (int i = 0; i < columnCount; i++)
            {
                MainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            for (int i = 0; i < columnCount; i++)
            {
                var versePanel = await CreateVersePanel(groupedVerses.ElementAt(i).ToList());
                Grid.SetColumn(versePanel, i);
                if (i > 0)
                {
                    versePanel.Margin = new Thickness(12, 0, 0, 0);
                }
                MainGrid.Children.Add(versePanel);
            }
        }

        private async Task<Grid> CreateVersePanel(List<Verse> verses)
        {
            verses.Sort((v,y) => v.Number.CompareTo(y.Number));
            var firstVerse = verses.First();
            var lastVerse = verses.Last();
            var formattedVerseRanges = FormatRanges(verses.Select(v => v.Number).ToList());
            var reference = $"{firstVerse.Chapter}: {formattedVerseRanges}";
            var scriptureTitle = await AiDetectionService.GetScriptureTitle(firstVerse.Book + " " + reference);

            Grid headerGrid = new Grid() { VerticalAlignment = VerticalAlignment.Stretch };

            headerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            headerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            headerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            headerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var bookBlock = new TextBlock { Text = reference, FontSize = 20, FontWeight = FontWeights.SemiBold, Foreground = System.Windows.Media.Brushes.White, TextAlignment = TextAlignment.Center };
            var verseHeaderBlock = new TextBlock { Text = firstVerse.Book, FontSize = 25, FontWeight = FontWeights.Bold, Foreground = System.Windows.Media.Brushes.White, TextAlignment = TextAlignment.Center };
            var titleBlock = new TextBlock { Text = scriptureTitle, FontSize = 15, FontStyle = FontStyles.Italic, Foreground = System.Windows.Media.Brushes.White, TextAlignment = TextAlignment.Center };
            Grid.SetRow(verseHeaderBlock, 0);
            Grid.SetRow(bookBlock, 1);
            Grid.SetRow(titleBlock, 2);
            
            headerGrid.Children.Add(bookBlock);
            headerGrid.Children.Add(verseHeaderBlock);
            headerGrid.Children.Add(titleBlock);

            var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, Name = "VerseScrollViewer", HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            var textPanel = new StackPanel() { HorizontalAlignment = HorizontalAlignment.Stretch };

            foreach (var verse in verses)
            {
                var verseBlock = new TextBlock() { FontSize = 18, Foreground = System.Windows.Media.Brushes.White, Margin = new Thickness(0, 2, 0, 2), TextWrapping = TextWrapping.Wrap };
                var numberRun = new Run($"{verse.Number}. ");
                numberRun.FontWeight = FontWeights.Bold;
                verseBlock.Inlines.Add(numberRun);
                var textRun = new Run(verse.Text);
                verseBlock.Inlines.Add(textRun);
                textPanel.Children.Add(verseBlock);
            }

            scrollViewer.Content = textPanel;

            Grid.SetRow(scrollViewer, 3);

            headerGrid.Children.Add(scrollViewer);

            

            return headerGrid;
        }

        public static string FormatRanges(List<int> numbers)
        {
            if (numbers == null || numbers.Count == 0)
                return string.Empty;

            numbers.Sort();
            List<string> ranges = new List<string>();
            int start = numbers[0], end = numbers[0];

            for (int i = 1; i < numbers.Count; i++)
            {
                if (numbers[i] == end + 1)
                {
                    end = numbers[i];
                }
                else
                {
                    ranges.Add(start == end ? $"{start}" : $"{start}-{end}");
                    start = end = numbers[i];
                }
            }

            ranges.Add(start == end ? $"{start}" : $"{start}-{end}");
            return string.Join(", ", ranges);
        }

        private void StartScrolling()
        {
            while (FindVisualChildren<ScrollViewer>(this).Count() == 0)
            {
                //do nothing
            }
            foreach (var item in FindVisualChildren<ScrollViewer>(this))
            {
                item.UpdateLayout();
                if (item.ScrollableHeight > item.ActualHeight)
                {
                    var duration = FindVisualChildren<TextBlock>(item).Count() * 3;
                    double targetOffset = item.ScrollableHeight;
                    AnimateScrollViewer(item, targetOffset, TimeSpan.FromSeconds(duration));
                }
            }
        }

        private async void AnimateScrollViewer(ScrollViewer scrollViewer, double toValue, TimeSpan duration)
        {
            await Task.Delay(4000);
            double fromValue = scrollViewer.VerticalOffset;
            double delta = toValue - fromValue;
            double startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            double endTime = startTime + duration.TotalMilliseconds;

            EventHandler handler = null;
            handler = async (s, e) =>
            {
                double currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                double normalizedTime = (currentTime - startTime) / duration.TotalMilliseconds;

                if (normalizedTime < 1)
                {
                    //double easedTime = EaseOutQuad(normalizedTime);
                    scrollViewer.ScrollToVerticalOffset(fromValue + (delta * normalizedTime));
                }
                else
                {
                    CompositionTarget.Rendering -= handler;
                    await Task.Delay(4000);
                    scrollViewer.ScrollToVerticalOffset(0);
                    await Task.Delay(4000);
                    fromValue = 0;
                    toValue = scrollViewer.ScrollableHeight;
                    startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    endTime = startTime + duration.TotalMilliseconds;
                    CompositionTarget.Rendering += handler;
                }
            };

            CompositionTarget.Rendering += handler;

            // Detach the event handler when the ScrollViewer is unloaded
            scrollViewer.Unloaded += (s, e) =>
            {
                CompositionTarget.Rendering -= handler;
            };
        }

        private double EaseOutQuad(double t)
        {
            return t * (2 - t);
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject dependencyObject) where T : DependencyObject
        {
            if (dependencyObject != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dependencyObject); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(dependencyObject, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
}