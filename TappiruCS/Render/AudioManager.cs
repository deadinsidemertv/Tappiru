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
        public float duration; // длительность в секундах

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
            using (var reader = new Mp3FileReader(filePath))
            {
                var waveFormat = reader.WaveFormat;
                byte[] data = new byte[reader.Length];
                int bytesRead = reader.Read(data, 0, data.Length);
                var format = GetALFormat(waveFormat);
                // Фиксируем массив для получения указателя
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
            AL.Source(source, ALSourcei.Buffer, buffer);
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
        }
        public void Stop()
        {
            AL.SourceStop(source);
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
