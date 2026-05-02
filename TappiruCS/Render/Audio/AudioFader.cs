using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TappiruCS.Render.Audio
{
    public class AudioFader
    {
        private readonly int _source;
        private CancellationTokenSource _cts;
        private float _currentGain = 1f;

        public AudioFader(int source)
        {
            _source = source;
        }

        public async Task FadeToAsync(float targetGain, float durationSeconds, float masterVolume = 1f)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            float startGain = _currentGain;
            float time = 0f;
            const float interval = 0.016f; // ~60 FPS

            while (time < durationSeconds)
            {
                if (_cts.Token.IsCancellationRequested) break;

                time += interval;
                float t = Math.Min(time / durationSeconds, 1f);
                _currentGain = MathHelper.Lerp(startGain, targetGain, t);

                AL.Source(_source, ALSourcef.Gain, _currentGain * masterVolume);
                await Task.Delay((int)(interval * 1000));
            }

            _currentGain = targetGain;
            AL.Source(_source, ALSourcef.Gain, targetGain * masterVolume);
        }

        public void Cancel() => _cts?.Cancel();

        public void SetImmediate(float gain, float masterVolume = 1f)
        {
            _currentGain = gain;
            AL.Source(_source, ALSourcef.Gain, gain * masterVolume);
        }
    }
}