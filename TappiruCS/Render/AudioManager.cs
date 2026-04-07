using NAudio.Wave;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TappiruCS.Render
{
    public class AudioManager 
    {
        private ALContext context;
        private ALDevice device;
        private int source;
        private int buffer;
        public float duration { get; private set; } // длительность в секундах
        //public double CurrentTime { get; private set; }

        private bool _isPlaying = false;

        public bool IsPlaying => _isPlaying;
        public float Duration => duration;

        public AudioManager()
        {
            device = ALC.OpenDevice(null);
            context = ALC.CreateContext(device, (int[])null);
            ALC.MakeContextCurrent(context);
            source = AL.GenSource();
            buffer = AL.GenBuffer();
        }

        public void LoadMusic(string filePath)
        {
            Stop(); // Останавливаем воспроизведение

            // 1. Отвязываем текущий буфер от источника
            AL.Source(source, ALSourcei.Buffer, 0);

            // 2. Удаляем старый буфер (если он существует)
            if (buffer != 0)
                AL.DeleteBuffer(buffer);

            // 3. Создаём новый буфер
            buffer = AL.GenBuffer();

            // 4. Загружаем новые аудиоданные из файла
            using (var reader = new Mp3FileReader(filePath))
            {
                var waveFormat = reader.WaveFormat;
                byte[] data = new byte[reader.Length];
                reader.Read(data, 0, data.Length);
                var format = GetALFormat(waveFormat);

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    AL.BufferData(buffer, format, handle.AddrOfPinnedObject(), data.Length, waveFormat.SampleRate);
                }
                finally
                {
                    handle.Free();
                }
                duration = (float)reader.TotalTime.TotalSeconds;
            }

            // 5. Привязываем новый буфер к источнику
            AL.Source(source, ALSourcei.Buffer, buffer);

            // 6. Сбрасываем смещение (на всякий случай)
            AL.Source(source, ALSourcef.SecOffset, 0.0f);
        }

        private ALFormat GetALFormat(WaveFormat format)
        {
            if (format.BitsPerSample == 8)
                return format.Channels == 1 ? ALFormat.Mono8 : ALFormat.Stereo8;
            else if (format.BitsPerSample == 16)
                return format.Channels == 1 ? ALFormat.Mono16 : ALFormat.Stereo16;
            throw new NotSupportedException("Формат не поддерживается");
        }

        public void Play()
        {
            AL.SourcePlay(source);
            _isPlaying = true;
        }
        public void Stop()
        {
            AL.SourceStop(source);
            _isPlaying = false;
        }
        public void SetLooping(bool loop)
        {
            if (source != 0) // предполагая, что твой source хранится в поле класса
            {
                AL.Source(source, ALSourceb.Looping, loop);
            }
        }
        public void Pause()
        {
            if (source != 0)
            {
                AL.SourcePause(source);
                _isPlaying = false; // Обновляем флаг
            }
        }

        public void Resume()
        {
            if (source != 0)
            {
                AL.SourcePlay(source);
                _isPlaying = true;
            }
        }

        public void SetCurrentTime(float seconds)
        {
            if (source != 0)
            {
                AL.Source(source, ALSourcef.SecOffset, seconds);
            }
        }

        public float GetCurrentTime()
        {
            // Получаем смещение в сэмплах

            AL.GetSource(source, ALGetSourcei.SampleOffset, out int sampleOffset);
            AL.GetSource(source, ALSourcef.SecOffset, out float secOffset); // альтернатива
                                                                            // Более точно: используем секунды
            return secOffset;
        }
        public void Dispose()
        {
            AL.DeleteSource(source);
            AL.DeleteBuffer(buffer);
            ALC.DestroyContext(context);
            ALC.CloseDevice(device);
        }
    }
}
