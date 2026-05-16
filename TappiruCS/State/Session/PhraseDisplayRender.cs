using Gdk;
using OpenTK.Mathematics;
using TappiruCS.Core.GameObject;
using TappiruCS.GameLogic;
using TappiruCS.GameLogic.Logic;
using TappiruCS.Render.Text;
using TappiruCS.Render.Text.FreeType;

namespace TappiruCS.State.Session
{
    public class PhraseDisplayRenderer
    {
        private readonly RenderContext _context;
        private readonly MapData _mapData;

        private static readonly Color4 SliderBarBackground = new(0.18f, 0.18f, 0.18f, 1.0f);
        private static readonly Color4 SliderBarReady = new(1.0f, 0.55f, 0.1f, 1.0f);
        private static readonly Color4 SliderBarHolding = new(0.15f, 0.8f, 1.0f, 1.0f);
        private static readonly Color4 SliderBarSuccess = new(0.1f, 1.0f, 0.35f, 1.0f);
        private static readonly Color4 SliderBarOutline = new(1f, 1f, 1f, 0.5f);

        private const float BaseFontSize = 144f;

        // Удобное сокращение
        private const string FontKey = "Game";
        private FreeTypeRender FT => FontManager.Get(FontKey);

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

            string originalText = session.CurrentPhaseDisplayText ?? string.Empty;
            string transcription = new string(session.CurrentPhaseChars);

            // === УЛУЧШЕННАЯ ПРОВЕРКА ===
            bool shouldShowTranscription = !string.IsNullOrWhiteSpace(transcription) &&
                                           !string.Equals(transcription.Trim(),
                                                          CleanForComparison(originalText),
                                                          StringComparison.OrdinalIgnoreCase);

            float maxPixelWidth = _context.Game.ClientSize.X * 0.95f;

            float originalScale = CalculateBestTextScale(originalText, maxPixelWidth);
            float screenX = centerX * Scene.CanvasScale.X;
            float screenY = y * Scene.CanvasScale.Y;

            float originalScaleX = originalScale * Scene.CanvasScale.X;
            float originalScaleY = originalScale * Scene.CanvasScale.Y;

            float alphaOverall = CalculateFadeAlpha(session, currentAudioTime);

            // ─── Транскрипция ───────────────────────────────────────────────────────
            float transcriptionY = 0;
            float transcriptionScaleX = 0;
            float transcriptionScaleY = 0;
            var charBoundsTrans = Array.Empty<(float, float, float, float)>();

            if (shouldShowTranscription)
            {
                float transcriptionOffsetY = 80f;
                float transcriptionLogicalY = y + transcriptionOffsetY;
                transcriptionY = transcriptionLogicalY * Scene.CanvasScale.Y;

                float transcriptionScale = originalScale * 0.6f;
                transcriptionScaleX = transcriptionScale * Scene.CanvasScale.X;
                transcriptionScaleY = transcriptionScale * Scene.CanvasScale.Y;

                charBoundsTrans = FT.GetCharBounds(
                    transcription, centerX, transcriptionLogicalY,
                    Scene.CanvasScale, transcriptionScale, 1.0f, TextAlign.Center);
            }

            // === ЦВЕТА ===
            Color4[] displayColors = ComputeCharColors(session, originalText, currentAudioTime);
            ApplyAlphaToColors(displayColors, alphaOverall);

            Color4[] transColors = ComputeCharColorsForTranscription(session, transcription);
            ApplyAlphaToColors(transColors, alphaOverall);

            // Glow (сзади)
            if (!session.PhaseComplete)
            {
                int currentDisplayIdx = session.GetDisplayProgressIndex(session.CurrentCharIndex);
                var japaneseBounds = FT.GetCharBounds(
                    originalText, centerX, y, Scene.CanvasScale, originalScale, 1.0f, TextAlign.Center);

                DrawCurrentCharGlow(currentDisplayIdx, originalText, japaneseBounds,
                                    originalScaleX, originalScaleY, projection,
                                    currentAudioTime, alphaOverall);
            }

            // Основной японский текст
            FT.DrawStringWithCharColors(originalText, screenX, screenY, BaseFontSize,
                originalScaleX, originalScaleY, displayColors, projection, TextAlign.Center);

            // Транскрипция только если нужно
            if (shouldShowTranscription && charBoundsTrans.Length > 0)
            {
                FT.DrawStringWithCharColors(transcription, screenX, transcriptionY, BaseFontSize,
                    transcriptionScaleX, transcriptionScaleY, transColors, projection, TextAlign.Center);
            }

            DrawSliderFramesSafe(originalText, shouldShowTranscription, charBoundsTrans,
                               session, currentAudioTime, projection, centerX, y, originalScale);

            if (session.PhaseComplete)
            {
                DrawCompletedPhraseGlow(originalText, screenX, screenY,
                                        originalScaleX, originalScaleY,
                                        projection, currentAudioTime, alphaOverall);
            }

            if (session.IsHoldingSlider)
                DrawSliderHoldBar(session, projection);
        }

        // ── Вспомогательные ───────────────────────────────────────────────────────
        private void DrawSliderFramesSafe(
            string originalText,
            bool shouldShowTranscription,
            (float x, float y, float width, float height)[] charBoundsTrans,
            GameSession session,
            double currentTime,
            Matrix4 projection,
            float centerX,
            float y,
            float originalScale)
        {
            if (session.CurrentSliders == null || session.CurrentSliders.Count == 0)
                return;

            string textForBounds = shouldShowTranscription && session.CurrentPhaseChars != null
                ? new string(session.CurrentPhaseChars)
                : originalText;

            var boundsToUse = shouldShowTranscription && charBoundsTrans != null && charBoundsTrans.Length > 0
                ? charBoundsTrans
                : FT.GetCharBounds(originalText, centerX, y, Scene.CanvasScale, originalScale, 1.0f, TextAlign.Center);

            DrawSliderFrames(textForBounds, boundsToUse, session, currentTime, projection);
        }
        private float CalculateBestTextScale(string text, float maxPixelWidth)
        {
            for (float s = 1.8f; s >= 0.5f; s -= 0.02f)
            {
                if (FT.CalculateTextWidth(text, s * Scene.CanvasScale.X) <= maxPixelWidth)
                    return s;
            }
            return 0.5f;
        }

        private static float CalculateFadeAlpha(GameSession session, double currentAudioTime)
        {
            double timeLeft = session.CurrentPhaseEndTime - currentAudioTime;
            return timeLeft < 0.18 ? Math.Max(0f, (float)(timeLeft / 0.18)) : 1f;
        }

        private Color4[] ComputeCharColors(GameSession session, string displayText, double currentTime)
        {
            int transProgress = session.CurrentCharIndex;

            Console.WriteLine($"[DEBUG ComputeCharColors] transProgress={transProgress} | displayText='{displayText}' | mapping.Length={session.CurrentPhaseMapping.Length}");

            int displayProgress = session.GetDisplayProgressIndex(transProgress);

            Console.WriteLine($"[DEBUG ComputeCharColors] → displayProgress = {displayProgress}");

            var colors = new Color4[displayText.Length];

            for (int i = 0; i < displayText.Length; i++)
            {
                if (session.PhaseComplete)
                {
                    colors[i] = new Color4(_mapData.completeR, _mapData.completeG, _mapData.completeB, 1f);
                }
                else if (i < displayProgress)
                {
                    colors[i] = new Color4(_mapData.tappedR, _mapData.tappedG, _mapData.tappedB, 1f);
                }
                else if (i == displayProgress)
                {
                    colors[i] = new Color4(_mapData.needR, _mapData.needG, _mapData.needB, 1f);
                }
                else
                {
                    colors[i] = Color4.White;
                }
            }

            return colors;
        }
        private Color4[] ComputeCharColorsForTranscription(GameSession session, string transcription)
        {
            var colors = new Color4[transcription.Length];
            for (int i = 0; i < transcription.Length; i++)
            {
                if (session.PhaseComplete)
                    colors[i] = new Color4(_mapData.completeR, _mapData.completeG, _mapData.completeB, 1f);
                else if (i < session.CurrentCharIndex)
                    colors[i] = new Color4(_mapData.tappedR, _mapData.tappedG, _mapData.tappedB, 1f);
                else if (i == session.CurrentCharIndex)
                    colors[i] = new Color4(_mapData.needR, _mapData.needG, _mapData.needB, 1f);
                else
                    colors[i] = Color4.White;
            }
            return colors;
        }

        private static void ApplyAlphaToColors(Color4[] colors, float alpha)
        {
            for (int i = 0; i < colors.Length; i++)
                colors[i].A *= alpha;
        }

        private void DrawSliderFrames(
            string text,
            (float x, float y, float width, float height)[] charBounds,
            GameSession session,
            double currentTime,
            Matrix4 projection)
        {
            if (charBounds == null || charBounds.Length == 0)
                return;

            for (int i = 0; i < text.Length; i++)
            {
                if (i >= charBounds.Length) break;

                if (!session.CurrentSliders!.ContainsKey(i))
                    continue;

                if (session.SuccessfullyHeldSliders.Contains(i) || session.PhaseComplete)
                    continue;

                var (bx, by, bw, bh) = charBounds[i];
                if (bw <= 0.1f || bh <= 0.1f)
                    continue;

                const float pad = 8f;
                float rx = bx - pad, ry = by - pad;
                float rw = bw + pad * 2, rh = bh + pad * 2;

                bool active = session.CurrentSliders.TryGetValue(i, out var slider) &&
                              currentTime >= slider.startTime - 0.5 &&
                              currentTime <= slider.endTime + 0.3;

                Color4 border = active
                    ? new Color4(1f, 0.55f, 0.1f, 1f)
                    : new Color4(1f, 1f, 1f, 0.8f);

                const float t = 3f;

                DrawGlow(rx, ry, rw, rh, border, 0.3f, projection, currentTime);

                _context.SpriteBatch.DrawRect(rx, ry, rw, t, border, projection);
                _context.SpriteBatch.DrawRect(rx, ry + rh - t, rw, t, border, projection);
                _context.SpriteBatch.DrawRect(rx, ry, t, rh, border, projection);
                _context.SpriteBatch.DrawRect(rx + rw - t, ry, t, rh, border, projection);
            }
        }

        private void DrawCurrentCharGlow(
    int idx, string text,
    (float x, float y, float width, float height)[] charBounds,
    float scaleX, float scaleY,
    Matrix4 projection, double currentTime, float alpha)
        {
            if (idx < 0 || idx >= text.Length || idx >= charBounds.Length) return;

            var (bx, by, bw, bh) = charBounds[idx];
            if (bw <= 0 || bh <= 0) return;

            char c = text[idx];
            if (!FT.TryGetGlyph(c, out GlyphInfo glyph)) return;

            Color4 glowColor = GetCurrentCharGlowColor();
            glowColor.A *= alpha;

            float glowX = bx;
            float glowY = by;

            DrawTextGlow(c.ToString(), glowX, glowY, scaleX, scaleY, glowColor,
                         projection, currentTime, alpha);
        }
        private Color4 GetCurrentCharGlowColor()
        {
            float r = _mapData.needR, g = _mapData.needG, b = _mapData.needB;
            if (r < 0.1f && g < 0.1f && b < 0.1f) return Color4.White;
            return new Color4(
                MathHelper.Clamp(r * 1.5f, 0f, 3f),
                MathHelper.Clamp(g * 1.5f, 0f, 3f),
                MathHelper.Clamp(b * 1.5f, 0f, 3f), 1f);
        }

        private void DrawCompletedPhraseGlow(
            string text, float screenX, float screenY,
            float scaleX, float scaleY,
            Matrix4 projection, double currentTime, float alpha)
        {
            if (string.IsNullOrEmpty(text)) return;

            Color4 baseGlow = GetGlowColor(_mapData.completeR, _mapData.completeG, _mapData.completeB);
            baseGlow.A *= alpha;
            float pulse = 0.8f + 0.2f * (float)Math.Sin(currentTime * 6.0);

            void Ring(int steps, float radius, float a)
            {
                for (int i = 0; i < steps; i++)
                {
                    float angle = i * MathHelper.TwoPi / steps;
                    float dx = (float)Math.Cos(angle) * radius;
                    float dy = (float)Math.Sin(angle) * radius;
                    var col = new Color4(baseGlow.R, baseGlow.G, baseGlow.B, a * pulse * alpha);
                    FT.DrawStringWithCharColors(
                        text, screenX + dx, screenY + dy,
                        BaseFontSize, scaleX, scaleY,
                        System.Linq.Enumerable.Repeat(col, text.Length).ToArray(),
                        projection, TextAlign.Center);
                }
            }

            Ring(16, 12f, 0.08f * baseGlow.R); // внешнее (чуть темнее)
            Ring(12, 6f, 0.25f);               // внутреннее
        }

        private void DrawSliderHoldBar(GameSession session, Matrix4 projection)
        {
            int sliderIdx = session.CurrentSliderCharIndex;
            if (session.CurrentSliders == null ||
                !session.CurrentSliders.TryGetValue(sliderIdx, out var slider)) return;

            double t = _context.Audio?.GetCurrentTime() ?? 0.0;
            float barW = _context.Game.ClientSize.X * 0.8f;
            float barH = 24f;
            float barX = (_context.Game.ClientSize.X - barW) / 2f;
            float barY = _context.Game.ClientSize.Y - barH - 30f;

            float duration = Math.Max((float)(slider.endTime - slider.startTime), 0.01f);
            float progress = t >= slider.startTime
                ? MathHelper.Clamp((float)((t - slider.startTime) / duration), 0f, 1f)
                : 0f;

            bool isSuccess = session.SuccessfullyHeldSliders.Contains(sliderIdx);
            Color4 fill = isSuccess ? SliderBarSuccess
                            : t >= slider.startTime - 0.3 ? SliderBarHolding : SliderBarReady;

            _context.SpriteBatch.DrawRect(barX, barY, barW, barH, SliderBarBackground, projection);
            if (progress > 0f)
                _context.SpriteBatch.DrawRect(barX, barY, barW * progress, barH, fill, projection);

            _context.SpriteBatch.DrawRect(barX, barY, barW, 1f, SliderBarOutline, projection);
            _context.SpriteBatch.DrawRect(barX, barY + barH - 1f, barW, 1f, SliderBarOutline, projection);
            _context.SpriteBatch.DrawRect(barX, barY, 1f, barH, SliderBarOutline, projection);
            _context.SpriteBatch.DrawRect(barX + barW - 1f, barY, 1f, barH, SliderBarOutline, projection);
        }

        private void DrawGlow(float x, float y, float w, float h,
            Color4 color, float strength, Matrix4 projection, double currentTime)
        {
            float pulse = 0.6f + 0.4f * (float)Math.Sin(currentTime * 5.0);
            for (int i = 1; i <= 5; i++)
            {
                float off = i * 1.8f;
                float a = strength * (0.18f / i) * pulse;
                _context.SpriteBatch.DrawRect(x - off, y - off, w + off * 2, h + off * 2,
                    new Color4(color.R, color.G, color.B, a), projection);
            }
            _context.SpriteBatch.DrawRect(x - 1, y - 1, w + 2, h + 2,
                new Color4(color.R, color.G, color.B, strength * 0.4f * pulse), projection);
        }

        private static Color4 GetGlowColor(float r, float g, float b)
        {
            if (r < 0.12f && g < 0.12f && b < 0.12f) return Color4.White;
            return new Color4(
                MathHelper.Clamp(r * 1.6f, 0f, 3f),
                MathHelper.Clamp(g * 1.6f, 0f, 3f),
                MathHelper.Clamp(b * 1.6f, 0f, 3f), 1f);
        }

        private void DrawTextGlow(
    string text,           // ожидается один символ
    float baseX,           // уже правильная X из charBounds (верхний левый угол растра)
    float baseY,           // уже правильная Y из charBounds (верхний левый угол растра)
    float scaleX,
    float scaleY,
    Color4 baseGlowColor,
    Matrix4 projection,
    double currentTime,
    float alpha = 1f)
        {
            if (string.IsNullOrEmpty(text)) return;
            char c = text[0];

            if (!FT.TryGetRenderedGlyph(c, out var glyph) || glyph == null)
                return;

            float pulse = 0.7f + 0.3f * (float)Math.Sin(currentTime * 10.0);

            Color4 Tint(float rMul, float gMul, float bMul, float aMul) => new(
                MathHelper.Clamp(baseGlowColor.R * rMul, 0f, 1f),
                MathHelper.Clamp(baseGlowColor.G * gMul, 0f, 1f),
                MathHelper.Clamp(baseGlowColor.B * bMul, 0f, 1f),
                aMul * pulse * alpha);

            void Ring(int steps, float radius, Color4 color)
            {
                for (int i = 0; i < steps; i++)
                {
                    float angle = i * MathHelper.TwoPi / steps;
                    float dx = (float)Math.Cos(angle) * radius;
                    float dy = (float)Math.Sin(angle) * radius;

                    // Рисуем глиф **точно в тех же координатах**, что и основной текст
                    FT.DrawSingleGlyph(
                        c,
                        baseX + dx,
                        baseY + dy,
                        scaleX,
                        scaleY,
                        color.R, color.G, color.B, color.A,
                        projection);
                }
            }

            // Внешнее свечение
            Ring(24, 8.5f, Tint(0.55f, 0.75f, 1.1f, 0.10f));
            // Среднее
            Ring(18, 5.0f, Tint(0.9f, 1.0f, 1.0f, 0.28f));
            // Внутреннее (самое яркое)
            Ring(14, 2.8f, Tint(1.3f, 1.15f, 0.95f, 0.65f));
        }

        private static string CleanForComparison(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Trim().ToLowerInvariant();
        }
    }
}