using OpenTK.Mathematics;
using TappiruCS.Core;
using TappiruCS.Render;

namespace TappiruCS.UI
{
    public class Background : GameObject
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly Game _game;

        public bool ParalaxEffect = false;
        public bool AutoBreathingParallax = false;

        public float BreathingSpeed = 0.4f;          // частота "дыхания" (меньше = медленнее)
        public float BreathingStrength = 12f;        // максимальное смещение в пикселях (10-20 обычно хватает)

        public int _textureId;

        public float Opacity = 1f;
        public Background(SpriteBatch spriteBatch, int textureId,Game game)
        {
            _spriteBatch = spriteBatch;
            _textureId = textureId;
            _game = game;
        }
        double time = 0.0;
        public override void Draw(Matrix4 projection)
        {
            const float bgScale = 1.2f;
            const float strength = 0.25f;         // запас от краёв

            float screenW = _game.ClientSize.X;
            float screenH = _game.ClientSize.Y;

            float bgW = screenW * bgScale;
            float bgH = screenH * bgScale;
            float extraW = bgW - screenW;
            float extraH = bgH - screenH;

            float baseX = -extraW * 0.5f;
            float baseY = -extraH * 0.5f;

            float offsetX = 0f;
            float offsetY = 0f;

            

            time += _game.UpdateTime;   // или Scene.CurrentTime / Audio.GetCurrentTime() — что удобнее

            if (ParalaxEffect)
            {
                // твой старый код с мышью (оставляем как есть)
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
                // === Автоматический "дышащий" параллакс ===
                float breathX = (float)Math.Sin(time * BreathingSpeed) * BreathingStrength;
                float breathY = (float)Math.Sin(time * BreathingSpeed * 0.7f + 1.3f) * (BreathingStrength * 0.6f);
                // 0.7f и фазовый сдвиг +1.3f — чтобы движение не было идеально круговым/синхронным (выглядит живее)

                // Ограничиваем, чтобы не вылезать за края
                float safeMaxX = extraW * 0.5f * strength * 0.6f;   // чуть слабее, чем у мыши
                float safeMaxY = extraH * 0.5f * strength * 0.6f;

                offsetX = Math.Clamp(breathX, -safeMaxX, safeMaxX);
                offsetY = Math.Clamp(breathY, -safeMaxY, safeMaxY);
            }

            float drawX = baseX + offsetX;
            float drawY = baseY + offsetY;

            _spriteBatch.Draw(_textureId, drawX, drawY, bgW, bgH, 0, 0, 1, 1,
                1f, 1f, 1f, Opacity, projection);
        }
    }
}
