using GLib;
using NAudio.Wave;
using OpenTK.Audio.OpenAL;
using System.Runtime.InteropServices;

namespace TappiruCS.Render
{
    public class AudioManager : IDisposable
    {
        private readonly ALDevice _device;
        private readonly ALContext _context;

        public static AudioManager Instance;
        // Основной источник для музыки
        private int _musicSource;
        private int _musicBuffer;

        public float Duration { get; private set; }
        public bool IsPlaying { get; private set; }

        // Реальная waveform-превью (peak envelope, 0..1) — вычисляется один раз при LoadMusic
        public float[] WaveformPreview { get; private set; } = Array.Empty<float>();

        // Для звуковых эффектов (параллельное воспроизведение)
        private readonly Dictionary<string, int> _effectBuffers = new();
        private readonly List<int> _activeEffectSources = new();

        public AudioManager()
        {
            Instance = this;
            _device = ALC.OpenDevice(null);
            _context = ALC.CreateContext(_device, (int[])null);
            ALC.MakeContextCurrent(_context);

            _musicSource = AL.GenSource();
            _musicBuffer = AL.GenBuffer();
        }

        // ====================== МУЗЫКА ======================

        public void LoadMusic(string filePath)
        {
            Stop();

            AL.Source(_musicSource, ALSourcei.Buffer, 0);
            if (_musicBuffer != 0) AL.DeleteBuffer(_musicBuffer);

            _musicBuffer = AL.GenBuffer();

            byte[] data;
            WaveFormat waveFormat;

            using (var reader = new Mp3FileReader(filePath))
            {
                waveFormat = reader.WaveFormat;
                data = new byte[reader.Length];
                reader.Read(data, 0, data.Length);

                var format = GetALFormat(waveFormat);

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    AL.BufferData(_musicBuffer, format, handle.AddrOfPinnedObject(), data.Length, waveFormat.SampleRate);
                }
                finally
                {
                    handle.Free();
                }

                Duration = (float)reader.TotalTime.TotalSeconds;
            }

            // === НОВОЕ: реальная waveform-превью из PCM данных ===
            WaveformPreview = ComputeWaveformPreview(data, waveFormat);

            AL.Source(_musicSource, ALSourcei.Buffer, _musicBuffer);
            AL.Source(_musicSource, ALSourcef.SecOffset, 0.0f);
        }

        private float[] ComputeWaveformPreview(byte[] pcmData, WaveFormat waveFormat)
        {
            int bytesPerSample = waveFormat.BitsPerSample / 8;
            int channels = waveFormat.Channels;
            long totalBytes = pcmData.Length;
            if (totalBytes == 0) return Array.Empty<float>();

            long totalSamples = totalBytes / (bytesPerSample * channels);
            if (totalSamples == 0) return Array.Empty<float>();

            const int previewBins = 16384; // достаточно высокое разрешение для любого зума
            float[] preview = new float[previewBins];
            long samplesPerBin = totalSamples / previewBins;
            if (samplesPerBin < 1) samplesPerBin = 1;

            for (int bin = 0; bin < previewBins; bin++)
            {
                long startSample = (long)bin * samplesPerBin;
                long endSample = Math.Min(startSample + samplesPerBin, totalSamples);

                float maxAmp = 0f;

                for (long s = startSample; s < endSample; s++)
                {
                    long byteOffset = s * (long)bytesPerSample * channels;

                    float sampleAmp = 0f;

                    if (bytesPerSample == 2) // 16-bit (самый частый случай для MP3)
                    {
                        if (byteOffset + 1 < totalBytes)
                        {
                            short left = BitConverter.ToInt16(pcmData, (int)byteOffset);
                            sampleAmp = Math.Abs(left / 32768f);

                            if (channels >= 2 && byteOffset + 3 < totalBytes)
                            {
                                short right = BitConverter.ToInt16(pcmData, (int)byteOffset + 2);
                                float rAmp = Math.Abs(right / 32768f);
                                if (rAmp > sampleAmp) sampleAmp = rAmp;
                            }
                        }
                    }
                    else if (bytesPerSample == 1) // 8-bit (редко)
                    {
                        if (byteOffset < totalBytes)
                        {
                            byte sampleByte = pcmData[byteOffset];
                            float sample = (sampleByte - 128) / 128f;
                            sampleAmp = Math.Abs(sample);
                        }
                    }

                    if (sampleAmp > maxAmp)
                        maxAmp = sampleAmp;
                }

                preview[bin] = maxAmp;
            }

            Console.WriteLine($"[AudioManager] Waveform preview computed: {previewBins} bins for {Duration:F1}s track");
            return preview;
        }

        public void Play()
        {
            AL.SourcePlay(_musicSource); IsPlaying = true;
            AL.Source(_musicSource, ALSourcef.Gain, 0.5f);
        }
        public void Pause() { AL.SourcePause(_musicSource); IsPlaying = false; }
        public void Resume() { AL.SourcePlay(_musicSource); IsPlaying = true; }
        public void Stop() { AL.SourceStop(_musicSource); IsPlaying = false; }

        public void SetLooping(bool loop)
        {
            AL.Source(_musicSource, ALSourceb.Looping, loop);
        }

        public void SetCurrentTime(float seconds)
        {
            AL.Source(_musicSource, ALSourcef.SecOffset, seconds);
        }

        public float GetCurrentTime()
        {
            AL.GetSource(_musicSource, ALSourcef.SecOffset, out float seconds);
            return seconds;
        }

        // ====================== ЗВУКОВЫЕ ЭФФЕКТЫ ======================

        public void LoadSoundEffect(string name, string filePath)
        {
            if (_effectBuffers.ContainsKey(name))
                AL.DeleteBuffer(_effectBuffers[name]);

            int buffer = AL.GenBuffer();

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
            }

            _effectBuffers[name] = buffer;
        }

        public void PlaySoundEffect(string name, float volume = 0.8f, float pitch = 1.0f)
        {
            if (!_effectBuffers.TryGetValue(name, out int buffer))
            {
                Console.WriteLine($"Sound effect '{name}' not found!");
                return;
            }

            int source = AL.GenSource();
            AL.Source(source, ALSourcei.Buffer, buffer);
            AL.Source(source, ALSourcef.Gain, volume);
            AL.Source(source, ALSourcef.Pitch, pitch);

            AL.SourcePlay(source);

            _activeEffectSources.Add(source);

            CleanFinishedEffects();
        }

        private void CleanFinishedEffects()
        {
            for (int i = _activeEffectSources.Count - 1; i >= 0; i--)
            {
                int src = _activeEffectSources[i];
                AL.GetSource(src, ALGetSourcei.SourceState, out int state);

                if (state == (int)ALSourceState.Stopped)
                {
                    AL.DeleteSource(src);
                    _activeEffectSources.RemoveAt(i);
                }
            }
        }

        private ALFormat GetALFormat(WaveFormat format)
        {
            if (format.BitsPerSample == 16)
                return format.Channels == 1 ? ALFormat.Mono16 : ALFormat.Stereo16;
            if (format.BitsPerSample == 8)
                return format.Channels == 1 ? ALFormat.Mono8 : ALFormat.Stereo8;

            throw new NotSupportedException("Unsupported audio format");
        }

        // Обновлённый GetWaveformData — теперь возвращает реальные данные (совместимость сохранена)
        public float[] GetWaveformData(int resolution = 1024)
        {
            float[] preview = WaveformPreview;
            if (preview.Length == 0)
            {
                // fallback (старый синус + шум)
                float[] _data = new float[resolution];
                var rnd = new Random();
                for (int i = 0; i < resolution; i++)
                {
                    float t = (float)i / resolution;
                    _data[i] = (float)Math.Sin(t * Math.PI * 20) * 0.6f +
                              (float)Math.Sin(t * Math.PI * 80) * 0.4f +
                              (float)(rnd.NextDouble() - 0.5) * 0.15f;
                    _data[i] = Math.Clamp(_data[i], -1f, 1f);
                }
                return _data;
            }

            // реальные данные (downsample из 16384-bin превью)
            int srcLen = preview.Length;
            float[] data = new float[resolution];
            for (int i = 0; i < resolution; i++)
            {
                int srcIdx = (int)((long)i * srcLen / resolution);
                data[i] = preview[srcIdx];
            }
            return data;
        }

        public void Dispose()
        {
            Stop();

            AL.DeleteSource(_musicSource);
            AL.DeleteBuffer(_musicBuffer);

            foreach (var buffer in _effectBuffers.Values)
                AL.DeleteBuffer(buffer);

            foreach (var source in _activeEffectSources)
                AL.DeleteSource(source);

            ALC.DestroyContext(_context);
            ALC.CloseDevice(_device);
        }
    }
}