using SpeechPlayground.Data.Model;
using SpeechPlayground.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;

namespace SpeechPlayground
{
    /// <summary>
    /// Interaction logic for BlurredWindow.xaml
    /// </summary>
    public partial class BlurredWindow : Window
    {
        public Book Book
        {
            get { return (Book)GetValue(BookProperty); }
            set { SetValue(BookProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Book.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BookProperty =
            DependencyProperty.Register("Book", typeof(Book), typeof(BlurredWindow), new PropertyMetadata(null));

        public Chapter Chapter
        {
            get { return (Chapter)GetValue(ChapterProperty); }
            set { SetValue(ChapterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Chapter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ChapterProperty =
            DependencyProperty.Register("Chapter", typeof(Chapter), typeof(BlurredWindow), new PropertyMetadata(null));


        public BlurredWindow(IBibleAiDetectionService aiService)
        {
            this.Loaded += Window_Loaded;
            this.MouseLeftButtonDown += Window_MouseDown;
            Top = System.Windows.SystemParameters.WorkArea.Bottom - 300;
            
            Width = 1920;
            Left = -1920;
            Height = 300;
            InitializeComponent();
            verseOverlay.AiDetectionService = aiService;
        }

        public async Task LoadVerses(List<Verse> verses, List<Verse> contextualVerses)
        {
            await verseOverlay.SetVerses(verses, contextualVerses);

            contextTxt.Inlines.Clear();

            if (contextualVerses?.Any() ?? false)
            {
                var verseLeadingRun = new Run($"See also: ");
                verseLeadingRun.FontWeight = FontWeights.Bold;
                verseLeadingRun.FontSize = 15;
                contextTxt.Inlines.Add(verseLeadingRun);

                var groupings = contextualVerses.GroupBy(x => new { x.Chapter, x.Book }).Select(g => g.OrderBy(v => v.Number)).ToList();
                var references = groupings.Select(grouping =>
                {
                    var firstVerse = grouping.First();
                    var lastVerse = grouping.Last();
                    var reference = $"{firstVerse.Book} {firstVerse.Chapter}:{firstVerse.Number}{(lastVerse.Number != firstVerse.Number ? $"-{lastVerse.Number}" : "")}";
                    return reference;
                });
                   
                string verseList = string.Join(", ", references);

                var verseRun = new Run(verseList);
                verseRun.FontSize = 15;
                verseRun.FontStyle = FontStyles.Italic;

                contextTxt.Inlines.Add(verseRun);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EnableBlur();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        public void EnableBlur()
        {
            var windowHelper = new WindowInteropHelper(this);
            var accent = new AccentPolicy();
            accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;
            int accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);
            var Data = new WindowCompositionAttributeData();
            Data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            Data.SizeOfData = accentStructSize;
            Data.Data = accentPtr;
            SetWindowCompositionAttribute(windowHelper.Handle, ref Data);
            Marshal.FreeHGlobal(accentPtr);
        }

        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_INVALID_STATE = 4
        }

        internal partial struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        internal partial struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        internal enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }
    }
}
