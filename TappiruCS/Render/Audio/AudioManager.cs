using NAudio.Wave;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace TappiruCS.Render
{
    public class AudioManager : IDisposable
    {
        private readonly ALDevice _device;
        private readonly ALContext _context;

        public static AudioManager Instance { get; private set; }

        // ====================== ГРОМКОСТИ (Instance) ======================
        private float _masterVolume = 1.0f;
        private float _musicVolume = 0.95f;
        private float _effectsVolume = 1.0f;

        // ====================== СТАТИЧЕСКИЕ СВОЙСТВА ДЛЯ СОВМЕСТИМОСТИ ======================
        public static float MasterVolume
        {
            get => Instance?._masterVolume ?? 1f;
            set
            {
                if (Instance != null) Instance._masterVolume = Math.Clamp(value, 0f, 1f);
                Instance?.ApplyVolumes();
            }
        }

        public static float MusicVolume
        {
            get => Instance?._musicVolume ?? 1f;
            set
            {
                if (Instance != null) Instance._musicVolume = Math.Clamp(value, 0f, 1f);
                Instance?.ApplyVolumes();
            }
        }

        public static float EffectsVolume
        {
            get => Instance?._effectsVolume ?? 1f;
            set
            {
                if (Instance != null) Instance._effectsVolume = Math.Clamp(value, 0f, 1f);
                Instance?.ApplyVolumes();
            }
        }

        // Instance свойства (для внутреннего использования)
        public float MasterVolumeInternal
        {
            get => _masterVolume;
            private set => _masterVolume = Math.Clamp(value, 0f, 1f);
        }

        // ====================== МУЗЫКА ======================
        private int _musicSource;
        private int _musicBuffer;
        private string _currentMusicPath = "";

        public float Duration { get; private set; }
        public bool IsPlaying { get; private set; }
        public float[] WaveformPreview { get; private set; } = Array.Empty<float>();

        private CancellationTokenSource _fadeCts;

        // ====================== ЭФФЕКТЫ ======================
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

            MasterVolume = OptionFile.MasterVolume;
        }

        // ====================== МУЗЫКА С FADE ======================

        public async Task LoadMusicAsync(string filePath, float fadeOut = 0.7f, float fadeIn = 1.0f, bool force = false)
        {
            if (!force && _currentMusicPath == filePath && IsPlaying)
            {
                await FadeToAsync(MusicVolume, fadeIn);
                return;
            }

            await FadeOutAsync(fadeOut);
            Stop();

            _currentMusicPath = filePath;
            LoadMusicInternal(filePath);

            AL.SourcePlay(_musicSource);
            IsPlaying = true;

            await FadeInAsync(fadeIn);
        }

        private void LoadMusicInternal(string filePath)
        {
            AL.Source(_musicSource, ALSourcei.Buffer, 0);
            if (_musicBuffer != 0) AL.DeleteBuffer(_musicBuffer);
            _musicBuffer = AL.GenBuffer();

            using var reader = new Mp3FileReader(filePath);
            var waveFormat = reader.WaveFormat;
            byte[] data = new byte[reader.Length];
            reader.Read(data, 0, data.Length);

            var format = GetALFormat(waveFormat);
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                AL.BufferData(_musicBuffer, format, handle.AddrOfPinnedObject(), data.Length, waveFormat.SampleRate);
            }
            finally { handle.Free(); }

            Duration = (float)reader.TotalTime.TotalSeconds;
            WaveformPreview = ComputeWaveformPreview(data, waveFormat);

            AL.Source(_musicSource, ALSourcei.Buffer, _musicBuffer);
            AL.Source(_musicSource, ALSourcef.SecOffset, 0f);
            ApplyVolumes();
        }

        // ====================== FADE ======================

        public async Task FadeOutAsync(float duration = 0.8f) => await FadeToAsync(0f, duration);
        public async Task FadeInAsync(float duration = 1.0f) => await FadeToAsync(MusicVolume, duration);

        public async Task FadeToAsync(float target, float duration)
        {
            _fadeCts?.Cancel();
            _fadeCts = new CancellationTokenSource();

            float start = AL.GetSource(_musicSource, ALSourcef.Gain) / Math.Max(_masterVolume, 0.001f);
            float time = 0f;

            while (time < duration)
            {
                if (_fadeCts.Token.IsCancellationRequested) break;

                time += 0.016f;
                float val = MathHelper.Lerp(start, target, time / duration);
                AL.Source(_musicSource, ALSourcef.Gain, val * _masterVolume);
                await Task.Delay(16);
            }

            AL.Source(_musicSource, ALSourcef.Gain, target * _masterVolume);
        }

        // ====================== СОВМЕСТИМОСТЬ ======================

        public void LoadMusic(string filePath)
        {
            LoadMusicAsync(filePath, 0f, 0f, force: true).Wait();
        }

        public void Play() { AL.SourcePlay(_musicSource); IsPlaying = true; }
        public void Pause() { AL.SourcePause(_musicSource); IsPlaying = false; }
        public void Resume() { AL.SourcePlay(_musicSource); IsPlaying = true; }
        public void Stop() { AL.SourceStop(_musicSource); IsPlaying = false; }

        public void SetCurrentTime(float seconds) => AL.Source(_musicSource, ALSourcef.SecOffset, seconds);

        public float GetCurrentTime()
        {
            AL.GetSource(_musicSource, ALSourcef.SecOffset, out float sec);
            return sec;
        }

        public void SetLooping(bool loop) => AL.Source(_musicSource, ALSourceb.Looping, loop);

        // ====================== VOLUME ======================

        private void ApplyVolumes()
        {
            if (_musicSource != 0)
                AL.Source(_musicSource, ALSourcef.Gain, _musicVolume * _masterVolume);

            foreach (var src in _activeEffectSources)
                if (src != 0)
                    AL.Source(src, ALSourcef.Gain, _effectsVolume * _masterVolume);
        }

        // ====================== ЭФФЕКТЫ ======================

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
                finally { handle.Free(); }
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
            AL.Source(source, ALSourcef.Gain, _effectsVolume * _masterVolume * volume);
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

        // ====================== ВСПОМОГАТЕЛЬНЫЕ ======================

        private ALFormat GetALFormat(WaveFormat format)
        {
            if (format.BitsPerSample == 16)
                return format.Channels == 1 ? ALFormat.Mono16 : ALFormat.Stereo16;

            if (format.BitsPerSample == 8)
                return format.Channels == 1 ? ALFormat.Mono8 : ALFormat.Stereo8;

            throw new NotSupportedException($"Unsupported audio format: {format.BitsPerSample} bit, {format.Channels} channels");
        }

        private float[] ComputeWaveformPreview(byte[] pcmData, WaveFormat waveFormat)
        {
            int bytesPerSample = waveFormat.BitsPerSample / 8;
            int channels = waveFormat.Channels;
            long totalBytes = pcmData.Length;

            if (totalBytes == 0) return Array.Empty<float>();

            long totalSamples = totalBytes / (bytesPerSample * channels);
            if (totalSamples == 0) return Array.Empty<float>();

            const int previewBins = 16384;
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

                    if (bytesPerSample == 2 && byteOffset + 1 < totalBytes)
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
                    else if (bytesPerSample == 1 && byteOffset < totalBytes)
                    {
                        byte sampleByte = pcmData[byteOffset];
                        float sample = (sampleByte - 128) / 128f;
                        sampleAmp = Math.Abs(sample);
                    }

                    if (sampleAmp > maxAmp) maxAmp = sampleAmp;
                }
                preview[bin] = maxAmp;
            }

            return preview;
        }

        public void Dispose()
        {
            _fadeCts?.Cancel();
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