using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.Wave;
using SpeechPlayground.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpeechPlayground.Audio
{
    public class NAudioStream : PullAudioInputStreamCallback
    {
        private MemoryStream memoryStream;
        private AsyncAutoResetEvent newData;
        private TimeSpan _buffer;
        private bool _resetFlag = false;
        private WaveFormat format = new WaveFormat(44100, 16, 2);
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public NAudioStream(TimeSpan buffer)
        {
            _buffer = buffer;
            this.memoryStream = new MemoryStream();
            this.newData = new AsyncAutoResetEvent(false);
        }

        public async Task<bool> Reset()
        {
            if (_resetFlag)
            {
                await _semaphore.WaitAsync();

                try
                {
                    _resetFlag = false;
                    bytesCounter = 17640;
                    memoryStream.SetLength(17640);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    Debug.WriteLine("Memory stream reset.");
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }

            }
            return false;
        }

        public async Task Write(byte[] buffer, int offset, int count)
        {
            await _semaphore.WaitAsync();
            try
            {
                // Write data to the memory stream
                await memoryStream.WriteAsync(buffer, offset, count);


                long streamLength = memoryStream.Length;
                using (MemoryStream copy = new MemoryStream((int)streamLength))
                {
                    using (var writer = new WaveFileWriter(copy, format))
                    {
                        memoryStream.Position = 0; // Reset the position to the beginning
                        await memoryStream.CopyToAsync(writer);
                        await writer.FlushAsync();
                        copy.Seek(0, SeekOrigin.Begin);
                        await Task.Run(() =>
                        {
                            try
                            {
                                using (var audioFile = new WaveFileReader(copy))
                                {
                                    TimeSpan duration = audioFile.TotalTime;
                                    if (duration > _buffer)
                                    {
                                        _resetFlag = true;
                                    }
                                }
                            }
                            catch (System.FormatException) { }
                        });
                    }
                }

                newData.Set();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override void Close()
        {
            newData.Set();
        }

        private int bytesCounter = 0;

        public override int Read(byte[] dataBuffer, uint size)
        {
            return ReadAsync(dataBuffer, size).GetAwaiter().GetResult();
        }

        public async Task<int> ReadAsync(byte[] dataBuffer, uint size)
        {
            if (memoryStream == null)
            {
                return 0;
            }

            int bytesRead = 0;

            while (bytesRead < size)
            {
                await newData.WaitAsync(); // Block until there are bytes to read

                await _semaphore.WaitAsync();

                if (memoryStream.Length == 0)
                {
                    _semaphore.Release();
                    continue;
                }

                byte[] wavBuffer = memoryStream.ToArray();
                int bytesToRead = (int)(size - bytesRead);

                if (bytesToRead > wavBuffer.Length - bytesCounter)
                {
                    bytesToRead = wavBuffer.Length - bytesCounter;
                }

                if (bytesToRead > 0)
                {
                    Array.Copy(wavBuffer, bytesCounter, dataBuffer, bytesRead, bytesToRead);
                    bytesRead += bytesToRead;
                    bytesCounter += bytesToRead;
                }


                newData.Reset();
                _semaphore.Release();
            }

            return bytesRead;
        }

    }
}
