using OpenTK.Mathematics;
using TappiruCS.Core.GameObject;
using TappiruCS.GameLogic;
using TappiruCS.GameLogic.Logic;
using static TappiruCS.Render.TextRender;

namespace TappiruCS.State.Session
{
    public class PhraseDisplayRenderer
    {
        private readonly RenderContext _context;
        private readonly MapData _mapData;

        // Константы слайдеров
        private static readonly Color4 SliderBarBackground = new Color4(0.18f, 0.18f, 0.18f, 1.0f);
        private static readonly Color4 SliderBarReady = new Color4(1.0f, 0.55f, 0.1f, 1.0f);
        private static readonly Color4 SliderBarHolding = new Color4(0.15f, 0.8f, 1.0f, 1.0f);
        private static readonly Color4 SliderBarSuccess = new Color4(0.1f, 1.0f, 0.35f, 1.0f);
        private static readonly Color4 SliderBarOutline = new Color4(1f, 1f, 1f, 0.5f);

        // Базовый размер шрифта, от которого считаются все масштабы
        private const float BaseFontSize = 144f;

        public PhraseDisplayRenderer(RenderContext context, MapData mapData)
        {
            _context = context;
            _mapData = mapData;
        }

        public void Draw(GameSession session, Matrix4 projection, float centerX, float y)
        {
            if (session.CurrentPhaseChars == null || session.CurrentPhaseChars.Length == 0 || session.IsPause)
                return;

            double currentAudioTime = _context.Audio?.GetCurrentTime() ?? 0.0;

            bool isPhraseActive = currentAudioTime >= session.CurrentPhaseStartTime &&
                                  currentAudioTime <= session.CurrentPhaseEndTime;

            if (!isPhraseActive) return;

            string text = new string(session.CurrentPhaseChars);
            float maxPixelWidth = _context.Game.ClientSize.X * 0.85f;

            float bestScale = CalculateBestTextScale(text, maxPixelWidth);
            float screenX = centerX * Scene.CanvasScale.X;
            float screenY = y * Scene.CanvasScale.Y;
            float finalScaleX = bestScale * Scene.CanvasScale.X;
            float finalScaleY = bestScale * Scene.CanvasScale.Y;

            float alpha = CalculateFadeAlpha(session, currentAudioTime);

            Color4[] colors = ComputeCharColors(session, text, currentAudioTime);
            ApplyAlphaToColors(colors, alpha);

            var charBounds = _context.TextRenderer.GetCharBounds(text, centerX, y, Scene.CanvasScale, bestScale, 1.0f, TextAlign.Center);
            if (charBounds == null || charBounds.Length == 0) return;

            // 1. Рамки слайдеров
            DrawSliderFrames(text, charBounds, session, currentAudioTime, projection);

            // 2. Свечение текущей буквы
            if (!session.PhaseComplete)
                DrawCurrentCharGlow(session.CurrentCharIndex, text, charBounds, finalScaleX, finalScaleY, projection, currentAudioTime, alpha);

            // 3. Свечение завершённой фразы
            if (session.PhaseComplete)
                DrawCompletedPhraseGlow(text, screenX, screenY, finalScaleX, finalScaleY, projection, currentAudioTime, alpha);

            // 4. Основной текст
            _context.TextRenderer.DrawStringWithCharColors(
                text, screenX, screenY,
                BaseFontSize, finalScaleX, finalScaleY,
                colors, projection, TextAlign.Center);

            // 5. Прогресс-бар холда
            if (session.IsHoldingSlider)
                DrawSliderHoldBar(session, projection);
        }

        // ==================== Вспомогательные методы ====================

        private float CalculateBestTextScale(string text, float maxPixelWidth)
        {
            for (float testScale = 1.8f; testScale >= 0.5f; testScale -= 0.02f)
            {
                float estimatedWidth = _context.TextRenderer.CalculateTextWidth(text, testScale * Scene.CanvasScale.X);
                if (estimatedWidth <= maxPixelWidth)
                    return testScale;
            }
            return 0.5f;
        }

        private float CalculateFadeAlpha(GameSession session, double currentAudioTime)
        {
            double timeLeft = session.CurrentPhaseEndTime - currentAudioTime;
            if (timeLeft < 0.18)
                return Math.Max(0.0f, (float)(timeLeft / 0.18));
            return 1.0f;
        }

        private Color4[] ComputeCharColors(GameSession session, string text, double currentTime)
        {
            Color4[] colors = new Color4[text.Length];

            for (int i = 0; i < text.Length; i++)
            {
                bool isSlider = session.CurrentSliders?.ContainsKey(i) ?? false;
                bool isHoldingThis = session.IsHoldingSlider && session.CurrentSliderCharIndex == i;
                bool isSuccessfullyHeld = session.SuccessfullyHeldSliders.Contains(i);
                bool isSuccessfullyCompleted = session.SuccessfullyCompletedSliders.Contains(i);

                if (session.PhaseComplete || isSuccessfullyCompleted)
                {
                    colors[i] = new Color4(_mapData.completeR, _mapData.completeG, _mapData.completeB, 1f);
                }
                else if (isSuccessfullyHeld)
                {
                    colors[i] = new Color4(0.1f, 1.0f, 0.3f, 1f);
                }
                else if (isHoldingThis)
                {
                    colors[i] = new Color4(0.2f, 0.85f, 1.0f, 1f);
                }
                else if (i < session.CurrentCharIndex)
                {
                    colors[i] = new Color4(_mapData.tappedR, _mapData.tappedG, _mapData.tappedB, 1f);
                }
                else if (isSlider)
                {
                    if (session.CurrentSliders.TryGetValue(i, out var slider))
                    {
                        bool isTimeForSlider = currentTime >= slider.startTime - 0.5 && currentTime <= slider.endTime + 0.3;
                        colors[i] = isTimeForSlider
                            ? new Color4(1.0f, 0.4f, 0.0f, 1f)
                            : new Color4(1.0f, 1.0f, 1.0f, 1f);
                    }
                }
                else if (i == session.CurrentCharIndex)
                {
                    colors[i] = new Color4(_mapData.needR, _mapData.needG, _mapData.needB, 1f);
                }
                else
                {
                    colors[i] = new Color4(1f, 1f, 1f, 1f);
                }
            }

            return colors;
        }

        private void ApplyAlphaToColors(Color4[] colors, float alpha)
        {
            for (int i = 0; i < colors.Length; i++)
                colors[i].A *= alpha;
        }

        private void DrawSliderFrames(string text, (float x, float y, float width, float height)[] charBounds,
            GameSession session, double currentTime, Matrix4 projection)
        {
            if (session.CurrentSliders == null) return;

            for (int i = 0; i < text.Length; i++)
            {
                if (!session.CurrentSliders.ContainsKey(i)) continue;
                if (session.SuccessfullyHeldSliders.Contains(i) || session.PhaseComplete) continue;

                var bounds = charBounds[i];
                if (bounds.width <= 0 || bounds.height <= 0) continue;

                const float padding = 8f;
                float rectX = bounds.x - padding;
                float rectY = bounds.y - padding;
                float rectW = bounds.width + padding * 2;
                float rectH = bounds.height + padding * 2;

                bool isActiveSliderTime = false;
                if (session.CurrentSliders.TryGetValue(i, out var slider))
                {
                    isActiveSliderTime = currentTime >= slider.startTime - 0.5 && currentTime <= slider.endTime + 0.3;
                }

                Color4 borderColor = isActiveSliderTime
                    ? new Color4(1.0f, 0.55f, 0.1f, 1f)
                    : new Color4(1f, 1f, 1f, 0.8f);

                const float thickness = 3f;

                DrawGlow(rectX, rectY, rectW, rectH, borderColor, 0.3f, projection, currentTime);

                _context.SpriteBatch.DrawRect(rectX, rectY, rectW, thickness, borderColor, projection);
                _context.SpriteBatch.DrawRect(rectX, rectY + rectH - thickness, rectW, thickness, borderColor, projection);
                _context.SpriteBatch.DrawRect(rectX, rectY, thickness, rectH, borderColor, projection);
                _context.SpriteBatch.DrawRect(rectX + rectW - thickness, rectY, thickness, rectH, borderColor, projection);
            }
        }

        private void DrawCurrentCharGlow(int currentCharIdx, string text, (float x, float y, float width, float height)[] charBounds,
            float finalScaleX, float finalScaleY, Matrix4 projection, double currentTime, float alpha)
        {
            if (currentCharIdx < 0 || currentCharIdx >= text.Length || currentCharIdx >= charBounds.Length)
                return;

            var bounds = charBounds[currentCharIdx];
            if (bounds.width <= 0 || bounds.height <= 0) return;

            char currentChar = text[currentCharIdx];
            if (!_context.TextRenderer.TryGetGlyph(currentChar, out var glyph)) return;

            float baseX = bounds.x - glyph.XOffset * finalScaleX;
            float baseY = bounds.y - glyph.YOffset * finalScaleY;

            Color4 glowColor = GetCurrentCharGlowColor();
            glowColor.A *= alpha;

            DrawTextGlow(currentChar.ToString(), baseX, baseY, finalScaleX, finalScaleY, glowColor, projection, currentTime, alpha);
        }

        private Color4 GetCurrentCharGlowColor()
        {
            float r = _mapData.needR;
            float g = _mapData.needG;
            float b = _mapData.needB;

            if (r < 0.1f && g < 0.1f && b < 0.1f)
                return Color4.White;

            return new Color4(
                MathHelper.Clamp(r * 1.5f, 0f, 3f),
                MathHelper.Clamp(g * 1.5f, 0f, 3f),
                MathHelper.Clamp(b * 1.5f, 0f, 3f),
                1f
            );
        }

        private void DrawCompletedPhraseGlow(string text, float screenX, float screenY,
            float finalScaleX, float finalScaleY,
            Matrix4 projection, double currentTime, float alpha)
        {
            if (string.IsNullOrEmpty(text)) return;

            Color4 baseGlow = GetGlowColor(_mapData.completeR, _mapData.completeG, _mapData.completeB);
            baseGlow.A *= alpha;

            float pulse = 0.8f + 0.2f * (float)Math.Sin(currentTime * 6.0);

            const float outerOffsetPx = 12f;
            const float innerOffsetPx = 6f;

            // Внешнее свечение
            for (int i = 0; i < 16; i++)
            {
                float angle = i * MathHelper.TwoPi / 16f;
                float dx = (float)Math.Cos(angle) * outerOffsetPx;
                float dy = (float)Math.Sin(angle) * outerOffsetPx;

                Color4 col = new Color4(baseGlow.R * 0.7f, baseGlow.G * 0.7f, baseGlow.B * 0.7f, 0.08f * pulse * alpha);
                _context.TextRenderer.DrawStringWithCharColors(
                    text, screenX + dx, screenY + dy,
                    BaseFontSize, finalScaleX, finalScaleY,
                    Enumerable.Repeat(col, text.Length).ToArray(),
                    projection, TextAlign.Center);
            }

            // Внутреннее свечение
            for (int i = 0; i < 12; i++)
            {
                float angle = i * MathHelper.TwoPi / 12f;
                float dx = (float)Math.Cos(angle) * innerOffsetPx;
                float dy = (float)Math.Sin(angle) * innerOffsetPx;

                Color4 col = new Color4(baseGlow.R, baseGlow.G, baseGlow.B, 0.25f * pulse * alpha);
                _context.TextRenderer.DrawStringWithCharColors(
                    text, screenX + dx, screenY + dy,
                    BaseFontSize, finalScaleX, finalScaleY,
                    Enumerable.Repeat(col, text.Length).ToArray(),
                    projection, TextAlign.Center);
            }
        }

        private void DrawSliderHoldBar(GameSession session, Matrix4 projection)
        {
            int sliderIndex = session.CurrentSliderCharIndex;
            if (session.CurrentSliders == null || !session.CurrentSliders.TryGetValue(sliderIndex, out var activeSlider))
                return;

            double currentAudioTime = _context.Audio?.GetCurrentTime() ?? 0.0;

            float barWidth = _context.Game.ClientSize.X * 0.8f;
            float barHeight = 24f;
            float barX = (_context.Game.ClientSize.X - barWidth) / 2f;
            float barY = _context.Game.ClientSize.Y - barHeight - 30f;

            float duration = Math.Max(activeSlider.endTime - activeSlider.startTime, 0.01f);
            float progress = 0f;
            if (currentAudioTime >= activeSlider.startTime)
                progress = (float)((currentAudioTime - activeSlider.startTime) / duration);
            progress = MathHelper.Clamp(progress, 0f, 1f);

            bool isSuccess = session.SuccessfullyHeldSliders.Contains(sliderIndex);

            Color4 fillColor = isSuccess
                ? SliderBarSuccess
                : (currentAudioTime >= activeSlider.startTime - 0.3f ? SliderBarHolding : SliderBarReady);

            _context.SpriteBatch.DrawRect(barX, barY, barWidth, barHeight, SliderBarBackground, projection);

            float fillWidth = barWidth * progress;
            if (fillWidth > 0f)
                _context.SpriteBatch.DrawRect(barX, barY, fillWidth, barHeight, fillColor, projection);

            _context.SpriteBatch.DrawRect(barX, barY, barWidth, 1f, SliderBarOutline, projection);
            _context.SpriteBatch.DrawRect(barX, barY + barHeight - 1f, barWidth, 1f, SliderBarOutline, projection);
            _context.SpriteBatch.DrawRect(barX, barY, 1f, barHeight, SliderBarOutline, projection);
            _context.SpriteBatch.DrawRect(barX + barWidth - 1f, barY, 1f, barHeight, SliderBarOutline, projection);
        }

        private void DrawGlow(float x, float y, float w, float h, Color4 color, float strength, Matrix4 projection, double currentTime)
        {
            float pulse = 0.6f + 0.4f * (float)Math.Sin(currentTime * 5.0);
            for (int i = 1; i <= 5; i++)
            {
                float offset = i * 1.8f;
                float alpha = strength * (0.18f / i) * pulse;
                Color4 glowCol = new Color4(color.R, color.G, color.B, alpha);
                _context.SpriteBatch.DrawRect(x - offset, y - offset, w + offset * 2, h + offset * 2, glowCol, projection);
            }
            Color4 nearCol = new Color4(color.R, color.G, color.B, strength * 0.4f * pulse);
            _context.SpriteBatch.DrawRect(x - 1, y - 1, w + 2, h + 2, nearCol, projection);
        }

        private Color4 GetGlowColor(float r, float g, float b)
        {
            if (r < 0.12f && g < 0.12f && b < 0.12f)
                return Color4.White;

            return new Color4(
                MathHelper.Clamp(r * 1.6f, 0f, 3f),
                MathHelper.Clamp(g * 1.6f, 0f, 3f),
                MathHelper.Clamp(b * 1.6f, 0f, 3f),
                1f
            );
        }

        private void DrawTextGlow(string text, float baseX, float baseY, float scaleX, float scaleY,
                          Color4 baseGlowColor, Matrix4 projection, double currentTime, float alpha = 1.0f)
        {
            float pulse = 0.7f + 0.3f * (float)Math.Sin(currentTime * 10.0);

            float outerAlpha = 0.12f * pulse * alpha;
            float midAlpha = 0.35f * pulse * alpha;
            float innerAlpha = 0.70f * pulse * alpha;

            Color4 outerColor = new Color4(
                MathHelper.Clamp(baseGlowColor.R * 0.6f, 0f, 1f),
                MathHelper.Clamp(baseGlowColor.G * 0.8f, 0f, 1f),
                MathHelper.Clamp(baseGlowColor.B * 1.2f, 0f, 1f),
                outerAlpha);

            Color4 midColor = new Color4(baseGlowColor.R, baseGlowColor.G, baseGlowColor.B, midAlpha);
            Color4 innerColor = new Color4(
                MathHelper.Clamp(baseGlowColor.R * 1.4f, 0f, 1f),
                MathHelper.Clamp(baseGlowColor.G * 1.2f, 0f, 1f),
                MathHelper.Clamp(baseGlowColor.B * 0.9f, 0f, 1f),
                innerAlpha);

            const float outerRadius = 7.5f;
            const float midRadius = 4.2f;
            const float innerRadius = 2.2f;

            // Внешний слой
            for (int dir = 0; dir < 24; dir++)
            {
                float angle = dir * MathHelper.TwoPi / 24f;
                float dx = (float)Math.Cos(angle) * outerRadius;
                float dy = (float)Math.Sin(angle) * outerRadius;

                _context.TextRenderer.DrawString(text, baseX + dx, baseY + dy,
                    scaleX, scaleY,
                    outerColor.R, outerColor.G, outerColor.B, outerColor.A,
                    projection);
            }

            // Средний слой
            for (int dir = 0; dir < 18; dir++)
            {
                float angle = dir * MathHelper.TwoPi / 18f;
                float dx = (float)Math.Cos(angle) * midRadius;
                float dy = (float)Math.Sin(angle) * midRadius;

                _context.TextRenderer.DrawString(text, baseX + dx, baseY + dy,
                    scaleX, scaleY,
                    midColor.R, midColor.G, midColor.B, midColor.A,
                    projection);
            }

            // Внутренний слой
            for (int dir = 0; dir < 12; dir++)
            {
                float angle = dir * MathHelper.TwoPi / 12f;
                float dx = (float)Math.Cos(angle) * innerRadius;
                float dy = (float)Math.Sin(angle) * innerRadius;

                _context.TextRenderer.DrawString(text, baseX + dx, baseY + dy,
                    scaleX, scaleY,
                    innerColor.R, innerColor.G, innerColor.B, innerColor.A,
                    projection);
            }
        }
    }
}