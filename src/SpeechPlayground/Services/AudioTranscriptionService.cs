using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SpeechPlayground.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SpeechPlayground.Services
{
    public interface IAudioTranscriptionService
    {
        string Log { get; set; }
        string Transcript { get; set; }

        event AudioTranscriptionService.AudioTranscribedEventHandler AudioTranscribed;

        void Start();
        void Stop();
        Task<WaveFormat> WriteToPCM16(byte[] inputBuffer, int length, WaveFormat format, NAudioStream convertedStream);
    }

    public class AudioTranscriptionService : ObservableObject, IAudioTranscriptionService
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();

        private WasapiLoopbackCapture _waveIn;
        private NAudioStream _convertedStream;
        private SilenceProvider _silenceProvider = null;
        private WasapiOut _wasapiOut = null;
        private SpeechRecognizer _speechRecognizer;
        private bool _initialized = false;

        public AudioTranscriptionService()
        {

        }

        public delegate void AudioTranscribedEventHandler(string transcription);

        public event AudioTranscribedEventHandler AudioTranscribed;

        private string _log;

        public string Log
        {
            get { return _log; }
            set { SetProperty(ref _log, value); }
        }

        private string _transcript;

        public string Transcript
        {
            get { return _transcript; }
            set
            {
                SetProperty(ref _transcript, value);
                if (AudioTranscribed != null)
                    AudioTranscribed(Transcript);
            }
        }

        public void Start()
        {
            InitializeSpeechRecognition();
            _stopwatch.Start();
        }

        public void Stop()
        {
            _stopwatch.Stop();

            try
            {
                _wasapiOut.Stop();
                _wasapiOut.Dispose();
                // Stop recording and dispose of resources
                if (_waveIn != null)
                {
                    _waveIn.StopRecording();
                    Thread.Sleep(500); // Allow cleanup time
                    _waveIn.Dispose();
                }

                _speechRecognizer?.StopContinuousRecognitionAsync();

                _initialized = false;

                Log += "Audio Transcription Service Stopped." + Environment.NewLine;
            }
            catch (Exception ex)
            {
                Log += "Error when stopping Audio Transcription Service: " + ex.Message + Environment.NewLine;
            }
        }

        private void InitializeSpeechRecognition()
        {
            try
            {
                _convertedStream = new NAudioStream(TimeSpan.FromSeconds(5));

                var device = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, NAudio.CoreAudioApi.Role.Multimedia);

                _waveIn = new WasapiLoopbackCapture(device);

                _silenceProvider = new SilenceProvider(_waveIn.WaveFormat);

                _wasapiOut = new WasapiOut(device, AudioClientShareMode.Shared, false, 0);

                _wasapiOut.Init(_silenceProvider);
                _wasapiOut.Play();

                _waveIn.DataAvailable += WaveIn_DataAvailable;

                _waveIn.StartRecording();
            }
            catch (Exception ex)
            {
                Log += "Error initializing speech recognition: " + ex.Message + Environment.NewLine;
            }
        }

        private void SpeechRecognizer_Recognized(object sender, SpeechRecognitionEventArgs e)
        {
            string recognizedText = e.Result.Properties.GetProperty("Lexical");

            Transcript += "[" + _stopwatch.Elapsed.ToString(@"hh\:mm\:ss") + "] " + recognizedText + Environment.NewLine;
        }

        private async void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (e.BytesRecorded > 0)
            {
                var reset = _convertedStream.Reset();
                var convertedFormat = await WriteToPCM16(e.Buffer, e.BytesRecorded, _waveIn.WaveFormat, _convertedStream);

                if (!_initialized)
                {
                    _initialized = true;
                    var format = AudioStreamFormat.GetWaveFormatPCM(
                        samplesPerSecond: 44100, bitsPerSample: 16, channels: 2);
                    //var recognitionModel = SpeechRecognitionModel.FromModelId("builtin-model-20240614");
                    AudioConfig audioConfig = AudioConfig.FromStreamInput(_convertedStream, format);
                    string subscriptionKey = "B4d9tqxOPewMK1mzMHYHAdOyi6OVAsMEXxaLmihzJmvH2krT7tlwJQQJ99BBACYeBjFXJ3w3AAAYACOGHwjx";
                    string region = "eastus";
                    var config = Microsoft.CognitiveServices.Speech.SpeechConfig.FromSubscription(subscriptionKey, region);
                    config.OutputFormat = OutputFormat.Detailed;
                    config.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "150");
                    _speechRecognizer = new SpeechRecognizer(config, audioConfig);
                    _speechRecognizer.Recognized += SpeechRecognizer_Recognized;

                    await _speechRecognizer.StartContinuousRecognitionAsync();

                }
            }
        }

        /// <summary>
        /// Converts an IEEE Floating Point audio buffer into a 16bit PCM compatible buffer.
        /// </summary>
        /// <param name="inputBuffer">The buffer in IEEE Floating Point format.</param>
        /// <param name="length">The number of bytes in the buffer.</param>
        /// <param name="format">The WaveFormat of the buffer.</param>
        /// <returns>A byte array that represents the given buffer converted into PCM format.</returns>
        public async Task<WaveFormat> WriteToPCM16(byte[] inputBuffer, int length, WaveFormat format, NAudioStream convertedStream)
        {
            if (length == 0)
            {
                return null;
            }

            return await Task.Run(async () =>
            {
                // Create a WaveStream from the input buffer.
                using var memStream = new MemoryStream(inputBuffer, 0, length);
                using var inputStream = new RawSourceWaveStream(memStream, format);

                // Convert the input stream to a WaveProvider in 16bit PCM format with sample rate of 48000 Hz.
                var convertedPCM = new SampleToWaveProvider16(
                    new WdlResamplingSampleProvider(
                        new WaveToSampleProvider(inputStream),
                        44100)
                    );

                var convertedFormat = convertedPCM.WaveFormat;

                byte[] convertedBuffer = new byte[length / 2];

                convertedPCM.Read(convertedBuffer, 0, length);

                await convertedStream.Write(convertedBuffer, 0, length / 2);

                return convertedFormat;
            });
        }

    }
}
