using GLib;
using NAudio.Wave;
using OpenTK.Audio.OpenAL;
using System.Runtime.InteropServices;

OpenALLibraryNameContainer.OverridePath = "OpenAL32.dll";
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

            using (var reader = new Mp3FileReader(filePath))
            {
                var waveFormat = reader.WaveFormat;
                byte[] data = new byte[reader.Length];
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

            AL.Source(_musicSource, ALSourcei.Buffer, _musicBuffer);
            AL.Source(_musicSource, ALSourcef.SecOffset, 0.0f);
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

        // ====================== ЗВУКОВЫЕ ЭФФЕКТЫ (ПАРАЛЛЕЛЬНО) ======================

        /// <summary>
        /// Загружает короткий звуковой эффект (hover, click, start и т.д.)
        /// </summary>
        public void LoadSoundEffect(string name, string filePath)
        {
            if (_effectBuffers.ContainsKey(name))
                AL.DeleteBuffer(_effectBuffers[name]);

            int buffer = AL.GenBuffer();

            using (var reader = new Mp3FileReader(filePath))   // можно также поддерживать .wav
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

        /// <summary>
        /// Проигрывает звуковой эффект параллельно музыке
        /// </summary>
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

            // Очистка завершённых эффектов (опционально, можно вызывать в Update)
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