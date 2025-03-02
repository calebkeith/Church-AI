using CommunityToolkit.Mvvm.ComponentModel;
using Mscc.GenerativeAI;
using OpenAI.Chat;
using SpeechPlayground.Data.Model;
using SpeechPlayground.Data.Model.Ai;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SpeechPlayground.Services
{
    public interface IBibleAiDetectionService
    {
        string Log { get; set; }
        Task<(List<Verse> MainVerses, List<Verse> ReferenceVerses)> ProcessTranscriptAsync(string transcript, bool manual = false);
        Task<string> GetScriptureTitle(string reference);
    }

    public class BibleAiDetectionService : ObservableObject, IBibleAiDetectionService
    {
        private readonly SemaphoreSlim _executeSemaphore = new SemaphoreSlim(1);
        private readonly int _pollingInterval = 20;
        private DateTime? _rateLimitDate = null;
        private readonly bool _useOpenAiDection = false;
        private readonly ChatClient _chat;
        private readonly GoogleAI _googleAi;
        private readonly GenerativeModel _model;

        private List<DetectedVerse> _mainVersesCache = new List<DetectedVerse>();
        private List<DetectedVerse> _contextualVersesCache = new List<DetectedVerse>();
        private List<Verse> _contextVersesDisplay = new List<Verse>();
        private List<Verse> _mainVersesDisplay = new List<Verse>();
        private Dictionary<string, string> _referenceTitlesCache = new Dictionary<string, string>();

        public BibleAiDetectionService()
        {
            _googleAi = new GoogleAI(apiKey: "AIzaSyDTFaRaGbQHvAyBP-GzdivyxXX-ffpSMls");
            _model = _googleAi.GenerativeModel(model: Model.Gemini20Flash);
            var cred = new ApiKeyCredential("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");
            var opts = new OpenAI.OpenAIClientOptions() { Endpoint = new Uri("https://openrouter.ai/api/v1") };
            OpenAI.OpenAIClient client = new OpenAI.OpenAIClient(cred, opts);
            _chat = client.GetChatClient("deepseek/deepseek-chat:free");
        }

        private string _log;

        public string Log
        {
            get { return _log; }
            set { SetProperty(ref _log, value); }
        }

        public async Task<string> GetScriptureTitle(string reference)
        {
            if (_referenceTitlesCache.ContainsKey(reference))
                return _referenceTitlesCache[reference];

            string prompt = @$"
Give me a succinct title for the following scripture in the KJV bible:

{reference}

Only include the title in your response. No other text.";

            GenerateContentResponse response = null;
            try
            {
                response = await _model.GenerateContent(prompt);
                _referenceTitlesCache.Add(reference, response.Text);
                return response.Text;
            }
            catch
            {
                return string.Empty;
            }

        }

        public async Task<(List<Verse> MainVerses, List<Verse> ReferenceVerses)> ProcessTranscriptAsync(string transcript, bool manual = false)
        {
            try
            {
                await _executeSemaphore.WaitAsync();

                if (_rateLimitDate == null
                    || (DateTime.Now - _rateLimitDate.Value).TotalSeconds >= _pollingInterval
                    || manual)
                {
                    _rateLimitDate = DateTime.Now;

                    var response = await ExecuteDetectionAsync(transcript);

                    if (response != null)
                    {
                        var mainVerses = new List<(Book book, Chapter chapter, List<Verse> verses)>();
                        var contextVerses = new List<Verse>();
                        var verses = new List<Verse>();
                        try
                        {
                            if (response.ReferenceVerses?.Any() ?? false)
                            {
                                _contextualVersesCache.AddRange(response.ReferenceVerses);

                                contextVerses = _contextualVersesCache
                                    .Where(w => w.Confidence >= (decimal)0.8 && w.Relevance >= (decimal)0.6)
                                    .SelectMany(cv =>
                                    {
                                        cv.Verses = cv.Verses.OrderBy(v => v).ToList();
                                        return ProcessDetectedVerse(cv).verses ?? new List<Verse>();
                                    }).Distinct().ToList();
                            }

                            if (response.MainVerses?.Any() ?? false)
                            {
                                _mainVersesCache.AddRange(response.MainVerses.Where(w => w.Confidence >= (decimal)0.8 && w.Relevance >= (decimal)0.6));
                                foreach (var mainVerse in _mainVersesCache)
                                {
                                    var result = ProcessDetectedVerse(mainVerse);
                                    if (result.book == null || result.chapter == null || result.verses == null)
                                        continue;
                                    mainVerses.Add(result);
                                }

                                if (mainVerses?.Any() ?? false)
                                {
                                    var mainGrouping = mainVerses.GroupBy(mv => new { mv.book?.Name, mv.chapter?.Number });

                                    foreach (var grouping in mainGrouping)
                                    {
                                        var bookName = grouping.Key.Name;
                                        var chapterNumber = grouping.Key.Number;

                                        var mainVerseGrouped = _mainVersesCache.Where(mv => (mv.Book == bookName?.Replace("I", "1").Replace("II", "2").Replace("III", "3") || mv.Book == bookName) && mv.Chapter == chapterNumber);

                                        DetectedVerse verse = new DetectedVerse();

                                        foreach (var detectedVerse in mainVerseGrouped)
                                        {
                                            if (detectedVerse.Verses?.Any() ?? false)
                                            {
                                                verse.Verses.AddRange(detectedVerse.Verses);
                                            }
                                            verse.Book = detectedVerse.Book;
                                            verse.Chapter = detectedVerse.Chapter;
                                        }

                                        verse.Verses = verse.Verses.OrderBy(v => v).Distinct().ToList();

                                        verses.AddRange(ProcessDetectedVerse(verse).verses);
                                    }

                                }
                            }

                            var verseObjs = verses.Distinct().ToList();

                            contextVerses = contextVerses.Where(x => !verseObjs.Contains(x)).ToList();

                            if (!(_contextVersesDisplay == contextVerses || _contextVersesDisplay.SequenceEqual(contextVerses))
                            || !(_mainVersesDisplay == verseObjs || _mainVersesDisplay.SequenceEqual(verseObjs)))
                            {
                                _contextVersesDisplay = contextVerses ?? new List<Verse>();
                                _mainVersesDisplay = verseObjs ?? new List<Verse>();

                                var commonChapterVerses = contextVerses.Where(cv => verseObjs.Any(mv => mv.Chapter == cv.Chapter && mv.Book == cv.Book));

                                verseObjs.AddRange(commonChapterVerses);
                                verseObjs = verses.Distinct().ToList();
                                contextVerses.RemoveAll(cv => commonChapterVerses.Contains(cv));

                                return (verseObjs, contextVerses);
                            }
                        }
                        catch { }
                    }
                }

            }
            finally
            {
                _executeSemaphore.Release();
            }

            return (null, null);
        }

        private async Task<AiVerseDetectionResult> ExecuteDetectionAsync(string transcript)
        {
            var prompt = ServiceConstants.VerseDetectionPrompt + transcript;

            AiVerseDetectionResult detectionResult = null;

            if (_useOpenAiDection)
            {
                var cm = OpenAI.Chat.ChatMessage.CreateUserMessage(prompt);
                var chatCompletion = await _chat.CompleteChatAsync(cm);
                try
                {
                    var content = chatCompletion.Value.Content[0].Text;
                    Log += "DeepSeek: " + content + "\r\n\r\n";
                    detectionResult = ParseAiResponse(content);
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                try
                {
                    GenerateContentResponse response = await _model.GenerateContent(prompt);

                    Log += "Google: " + response.Text + "\r\n\r\n";

                    detectionResult = JsonSerializer.Deserialize<AiVerseDetectionResult>(response.Text.Replace("```json", "").Replace("```", ""), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true });
                }
                catch { return null; }
            }

            return detectionResult;
        }

        private (Book book, Chapter chapter, List<Verse> verses) ProcessDetectedVerse(DetectedVerse verse)
        {
            try
            {
                verse.Book = verse.Book?.Replace("1", "I").Replace("2", "II").Replace("3", "III");
                var bookObj = Bible.Instance.Books.FirstOrDefault(b => b.Name.ToLower() == verse.Book?.ToLower());
                var chapterObj = bookObj?.Chapters.FirstOrDefault(c => c.Number == verse.Chapter);

                if (bookObj == null || chapterObj == null)
                    return (null, null, null);

                List<Verse> verses = new List<Verse>();

                var filteredVerses = verse.Verses?.Where(v => v != null).OrderBy(v => v).Distinct();

                foreach (var verseNum in filteredVerses)
                {
                    var verseObj = chapterObj.Verses.FirstOrDefault(v => v.Number == verseNum);
                    if (verseObj != null)
                    {
                        verseObj.Book = bookObj.Name;
                        verses.Add(verseObj);
                    }
                }

                return (bookObj, chapterObj, verses);
            }
            catch { }

            return (default, default, default);
        }

        private AiVerseDetectionResult ParseAiResponse(string jsonContent)
        {
            try
            {
                return JsonSerializer.Deserialize<AiVerseDetectionResult>(jsonContent.Replace("```json", "").Replace("```", ""), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true });
            }
            catch { } //ignore malformed json errors
            return null;
        }
    }
}

