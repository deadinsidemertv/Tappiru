using OpenTK.Mathematics;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;

namespace TappiruCS.UI
{
    public class Background : GameObject
    {
        public bool ParalaxEffect = false;
        public bool AutoBreathingParallax = false;

        public float BreathingSpeed = 0.4f;
        public float BreathingStrength = 12f;

        public int _textureId;
        public float Opacity = 1f;

        // === Crossfade ===
        private int _nextTextureId = -1;
        private float _fadeProgress = 0f;
        private float _fadeDuration = 0.5f;
        private bool _isFading = false;

        public Background(int textureId)
        {
            _textureId = textureId;
        }

        // Вызови этот метод вместо bg._textureId = ...
        public void TransitionTo(int newTextureId, float duration = 0.5f)
        {
            _nextTextureId = newTextureId;
            _fadeProgress = 0f;
            _fadeDuration = duration;
            _isFading = true;
        }

        double time = 0.0;

        public override void Draw(Matrix4 projection)
        {
            // Тик фейда
            if (_isFading)
            {
                _fadeProgress += (float)Game.UpdateTime / _fadeDuration;
                if (_fadeProgress >= 1f)
                {
                    _textureId = _nextTextureId;
                    _nextTextureId = -1;
                    _fadeProgress = 1f;
                    _isFading = false;
                }
            }

            const float bgScale = 1.2f;
            const float strength = 0.25f;

            float screenW = Game.ClientSize.X;
            float screenH = Game.ClientSize.Y;

            float bgW = screenW * bgScale;
            float bgH = screenH * bgScale;
            float extraW = bgW - screenW;
            float extraH = bgH - screenH;

            float baseX = -extraW * 0.5f;
            float baseY = -extraH * 0.5f;

            float offsetX = 0f;
            float offsetY = 0f;

            time += Game.UpdateTime;

            if (ParalaxEffect)
            {
                float nx = (Scene.LogicMouse.X / screenW) * 2f - 1f;
                float ny = (Scene.LogicMouse.Y / screenH) * 2f - 1f;
                nx = Math.Clamp(nx, -1f, 1f);
                ny = Math.Clamp(ny, -1f, 1f);

                float desiredOffsetX = -nx * 25f * strength;
                float desiredOffsetY = -ny * 25f * strength;

                float safeMaxX = extraW * 0.5f * strength;
                float safeMaxY = extraH * 0.5f * strength;

                offsetX = Math.Clamp(desiredOffsetX, -safeMaxX, safeMaxX);
                offsetY = Math.Clamp(desiredOffsetY, -safeMaxY, safeMaxY);
            }
            else if (AutoBreathingParallax)
            {
                float breathX = (float)Math.Sin(time * BreathingSpeed) * BreathingStrength;
                float breathY = (float)Math.Sin(time * BreathingSpeed * 0.7f + 1.3f) * (BreathingStrength * 0.6f);

                float safeMaxX = extraW * 0.5f * strength * 0.6f;
                float safeMaxY = extraH * 0.5f * strength * 0.6f;

                offsetX = Math.Clamp(breathX, -safeMaxX, safeMaxX);
                offsetY = Math.Clamp(breathY, -safeMaxY, safeMaxY);
            }

            float drawX = baseX + offsetX;
            float drawY = baseY + offsetY;

            // Если идёт фейд — рисуем два слоя
            if (_isFading && _nextTextureId != -1)
            {
                // Смягчение через SmoothStep (плавнее чем линейный)
                float t = _fadeProgress * _fadeProgress * (3f - 2f * _fadeProgress);

                // Старый фон уходит
                SB.Draw(_textureId, drawX, drawY, bgW, bgH, 0, 0, 1, 1,
                    1f, 1f, 1f, Opacity * (1f - t), projection);

                // Новый фон приходит
                SB.Draw(_nextTextureId, drawX, drawY, bgW, bgH, 0, 0, 1, 1,
                    1f, 1f, 1f, Opacity * t, projection);
            }
            else
            {
                SB.Draw(_textureId, drawX, drawY, bgW, bgH, 0, 0, 1, 1,
                    1f, 1f, 1f, Opacity, projection);
            }
        }
    }
}