using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using TappiruCS.Core;
using TappiruCS.Core.TappiruCS.Core;
using TappiruCS.Render;

namespace TappiruCS.UI
{
    public class Background : GameObject
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly Game _game;

        public bool ParalaxEffect = false;

        public int _textureId;

        public float Opacity = 1f;
        public Background(SpriteBatch spriteBatch, int textureId,Game game)
        {
            _spriteBatch = spriteBatch;
            _textureId = textureId;
            _game = game;
        }

        public override void Draw(Matrix4 projection)
        {
            // === НАСТРОЙКИ (лучше вынести в поля класса, чтобы менять в инспекторе) ===
            const float bgScale = 1.2f;           // во сколько раз больше экрана рисуем фон
            const float maxParallaxOffset = 25f;  // максимальное смещение в пикселях (подбери под вкус)
            const float strength = 0.25f;         // небольшой запас, чтобы никогда не было пустых краёв

            float screenW = _game.ClientSize.X;
            float screenH = _game.ClientSize.Y;

            // Размер фона и сколько у нас «запаса» по краям
            float bgW = screenW * bgScale;
            float bgH = screenH * bgScale;
            float extraW = bgW - screenW;   // сколько пикселей «лишних» по ширине
            float extraH = bgH - screenH;

            // Базовая позиция — фон всегда центрирован по умолчанию
            float baseX = -extraW * 0.5f;
            float baseY = -extraH * 0.5f;

            float offsetX = 0f;
            float offsetY = 0f;

            if (ParalaxEffect)
            {
                // Нормализованные координаты мыши [-1 … 1]
                float nx = (Scene.LogicMouse.X / screenW) * 2f - 1f;
                float ny = (Scene.LogicMouse.Y / screenH) * 2f - 1f;

                // Ограничиваем, чтобы мышь за пределами окна не ломала эффект
                nx = Math.Clamp(nx, -1f, 1f);
                ny = Math.Clamp(ny, -1f, 1f);

                // Считаем смещение (инверсия направления — мышь вправо → фон влево)
                float desiredOffsetX = -nx * maxParallaxOffset * strength;
                float desiredOffsetY = -ny * maxParallaxOffset * strength;

                // Дополнительная страховка: никогда не выходим за пределы безопасной зоны
                float safeMaxX = extraW * 0.5f * strength;
                float safeMaxY = extraH * 0.5f * strength;

                offsetX = Math.Clamp(desiredOffsetX, -safeMaxX, safeMaxX);
                offsetY = Math.Clamp(desiredOffsetY, -safeMaxY, safeMaxY);
            }

            float drawX = baseX + offsetX;
            float drawY = baseY + offsetY;

            _spriteBatch.Draw(_textureId,
                drawX, drawY,
                bgW, bgH,                  // используем рассчитанный размер
                0, 0, 1, 1,
                1f, 1f, 1f, Opacity,
                projection);
        }
    }
}
