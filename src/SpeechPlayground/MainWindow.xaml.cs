using System.Windows;
using System.Threading.Tasks;
using SpeechPlayground.Services;
using System.Windows.Data;
using System.Windows.Controls;

namespace SpeechPlayground
{
    public partial class MainWindow : Window
    {

        private BlurredWindow _window;
        
        private readonly IBibleAiDetectionService _aiService;
        private readonly IAudioTranscriptionService _transcriptService;
        public MainWindow(IBibleAiDetectionService aiService, IAudioTranscriptionService transcriptService)
        {
            _aiService = aiService;
            _window = new BlurredWindow(aiService);
            InitializeComponent();
            this.Closing += MainWindow_Closing;
            _window.Show();
            _transcriptService = transcriptService;
            _transcriptService.AudioTranscribed += TranscriptService_AudioTranscribed;
            txtUnfiltered.DataContext = _transcriptService;
            txtGoogle.DataContext = _aiService;

            _transcriptService.Start();
        }

        private async void TranscriptService_AudioTranscribed(string transcription)
        {
            await ProcessTranscriptAsync(transcription);

            await Dispatcher.InvokeAsync(() =>
            {
                txtUnfiltered.ScrollToEnd();
            });
        }

        private async Task ProcessTranscriptAsync(string transcript)
        {
            var verses = await _aiService.ProcessTranscriptAsync(transcript);

            if (verses.MainVerses != null || verses.ReferenceVerses != null)
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    await _window.LoadVerses(verses.MainVerses, verses.ReferenceVerses);
                });
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _transcriptService.Stop();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await ProcessTranscriptAsync(txtUnfiltered.Text);
        }
    }
}
