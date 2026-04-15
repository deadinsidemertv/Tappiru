using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Text.Json;
using TappiruCS.Core;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using TappiruCS.Server.Player;
using TappiruCS.UI;
using static TappiruCS.Render.TextRender;

namespace TappiruCS.State
{
    public class GameSessionState : IGameState
    {
        #region Injected Dependencies

        private readonly Game _game;
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRenderer;
        private readonly AudioManager _audio;

        #endregion

        #region Game Data

        public GameSession session;
        public MapData _mapData;

        #endregion

        #region Textures and Visuals

        public int background;
        public int scoreBarBGTex;

        private readonly Scene _scene = new Scene();
        public Background bg;
        public Background Fade;
        public SpriteObject scorebarBG;

        #endregion

        #region UI Text Elements

        public TextObject score;
        public TextObject Accuraci;
        public TextObject combo;
        public TextObject comboApof;

        #endregion

        #region Display State

        private float _displayedScore;
        private float _displayedAccuraci;

        private readonly HashSet<Keys> _pressedKeys = new HashSet<Keys>();

        #endregion

        #region Slider Visual Constants (для прогресс-бара и рамок)

        private static readonly Color4 SliderBarBackground = new Color4(0.18f, 0.18f, 0.18f, 1.0f);
        private static readonly Color4 SliderBarReady = new Color4(1.0f, 0.55f, 0.1f, 1.0f);     // оранжевый
        private static readonly Color4 SliderBarHolding = new Color4(0.15f, 0.8f, 1.0f, 1.0f);   // голубой
        private static readonly Color4 SliderBarSuccess = new Color4(0.1f, 1.0f, 0.35f, 1.0f);   // зелёный
        private static readonly Color4 SliderBarOutline = new Color4(1f, 1f, 1f, 0.5f);

        #endregion

        #region Static Input Mapping

        private static readonly Dictionary<Keys, char[]> KeyToCharsMap = new Dictionary<Keys, char[]>
        {
            { Keys.A,      new char[] { 'a', 'ф' } },
            { Keys.B,      new char[] { 'b', 'и' } },
            { Keys.C,      new char[] { 'c', 'с' } },
            { Keys.D,      new char[] { 'd', 'в' } },
            { Keys.E,      new char[] { 'e', 'у' } },
            { Keys.F,      new char[] { 'f', 'а' } },
            { Keys.G,      new char[] { 'g', 'п' } },
            { Keys.H,      new char[] { 'h', 'р' } },
            { Keys.I,      new char[] { 'i', 'ш' } },
            { Keys.J,      new char[] { 'j', 'о' } },
            { Keys.K,      new char[] { 'k', 'л' } },
            { Keys.L,      new char[] { 'l', 'д' } },
            { Keys.M,      new char[] { 'm', 'ь' } },
            { Keys.N,      new char[] { 'n', 'т' } },
            { Keys.O,      new char[] { 'o', 'щ' } },
            { Keys.P,      new char[] { 'p', 'з' } },
            { Keys.Q,      new char[] { 'q', 'й' } },
            { Keys.R,      new char[] { 'r', 'к' } },
            { Keys.S,      new char[] { 's', 'ы' } },
            { Keys.T,      new char[] { 't', 'е' } },
            { Keys.U,      new char[] { 'u', 'г' } },
            { Keys.V,      new char[] { 'v', 'м' } },
            { Keys.W,      new char[] { 'w', 'ц' } },
            { Keys.X,      new char[] { 'x', 'ч' } },
            { Keys.Y,      new char[] { 'y', 'н' } },
            { Keys.Z,      new char[] { 'z', 'я' } },
            { Keys.LeftBracket,  new char[] { '[', 'х' } },
            { Keys.RightBracket, new char[] { ']', 'ъ' } },
            { Keys.Semicolon,    new char[] { ';', 'ж' } },
            { Keys.Apostrophe,   new char[] { '\'', 'э' } },
            { Keys.Comma,        new char[] { ',', 'б' } },
            { Keys.Period,       new char[] { '.', 'ю' } },
            { Keys.GraveAccent,  new char[] { '`', 'ё' } },
            { Keys.Space,        new char[] { ' ' } },
        };

        #endregion

        #region Constructor

        public GameSessionState(Game game, SpriteBatch spriteBatch, TextRender textRenderer, AudioManager audio, MapData mapdata)
        {
            _game = game;
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            _audio = audio;
            _mapData = mapdata;
        }

        #endregion

        #region State Lifecycle

        public void OnEnter()
        {
            Console.WriteLine("Запуск уровня");
            GameSession.InitCharToKeyMap(KeyToCharsMap);

            session = new GameSession(_mapData);
            Console.WriteLine(_mapData.audioPath.ToString());

            if (_audio == null)
                Console.WriteLine("_audio РОВНО NULL CYKA");

            background = TextureLoader.Load(_mapData.backGroundPath);
            scoreBarBGTex = TextureManager.GetTexture("scorebg");

            bg = new Background(_spriteBatch, background, _game);
            Fade = new Background(_spriteBatch, 0, _game) { Opacity = 0.7f };
            scorebarBG = new SpriteObject(_spriteBatch, scoreBarBGTex, 960, 540, 1920, 1080) { AllowHover = false };

            _audio.LoadMusic(_mapData.audioPath);
            _audio.Play();

            InitializeUIElements();

            


            _scene.Add(bg);
            _scene.Add(Fade);
            _scene.Add(scorebarBG);
            _scene.Add(Accuraci);
            _scene.Add(score);
            _scene.Add(combo);
            _scene.Add(comboApof);
        }
        
        public void OnExit()
        {
            _audio.Stop();
            _scene.Clear();
            Console.WriteLine("Вы вышли с мапы");
        }

        #endregion

        #region Update and Render

        public void Update(double currentTime)
        {
            var mouse = _game.MouseState;
            _scene.Update(currentTime, mouse, _game);

            if (session == null)
                return;

            float cTime = _audio?.GetCurrentTime() ?? 0f;
            session.Update(cTime, _game.KeyboardState);

            CheckForMapCompletion(cTime);
            UpdateComboPosition();
            UpdateDisplayedValues(currentTime);
            UpdateUIText();
        }

        public void Render(Matrix4 projection)
        {
            _scene.Draw(projection);
            InputCharDraw(session, projection, 960, 440);
        }

        #endregion

        #region Input Handling

        public void HandleKeyDown(KeyboardKeyEventArgs e)
        {
            if (session == null || !session.IsActivePhase)
                return;

            Keys key = e.Key;

            if (_pressedKeys.Contains(key))
                return;                    // ← это важно

            if (!KeyToCharsMap.TryGetValue(key, out char[] possibleChars))
                return;

            int currentIndex = session.CurrentCharIndex;
            if (currentIndex >= session.CurrentPhaseChars.Length)
                return;

            char expectedChar = session.CurrentPhaseChars[currentIndex];

            if (Array.IndexOf(possibleChars, expectedChar) >= 0)
                session.HandleInput(expectedChar, _audio.GetCurrentTime());
            else
                session.HandleInput('\0', _audio.GetCurrentTime());

            _pressedKeys.Add(key);
        }

        public void HandleKeyUp(KeyboardKeyEventArgs e)
        {
            _pressedKeys.Remove(e.Key);
        }

        #endregion

        #region Private Helper Methods

        private void InitializeUIElements()
        {
            score = new TextObject(_textRenderer, session.TotalScore.ToString(), 1900, 0, 0.35f)
            {
                Color = Color4.White,
                Align = TextAlign.Right
            };

            Accuraci = new TextObject(_textRenderer, session.Accuracy.ToString(), 1840, score.Position.Y + 40, 0.3f)
            {
                Color = Color4.White,
                Align = TextAlign.Center
            };

            combo = new TextObject(_textRenderer, session.Combo.ToString(), 70, 900, 0.7f)
            {
                Align = TextAlign.Left
            };

            comboApof = new TextObject(_textRenderer, "x", combo.Position.X - 15, combo.Position.Y + 15, 0.4f);
        }

        private void CheckForMapCompletion(float currentAudioTime)
        {
            if (currentAudioTime < session.endTime)
                return;

            var newScore = new PlayerScore
            {
                MapHash = session.CurrentMap.MapHash,
                _score = session.TotalScore,
                _accuraci = session.Accuracy,
                _maxCobmo = session.MaxCombo,
                _completePhase = session.CompletedPhases,
                _failPhase = session.FailedPhases,
                _completeChar = session.CorrectHits,
                _failChar = session.Misses,
                PlayedAt = DateTime.Now,
            };

            newScore.PlayerName = PlayerProfile.Instance.IsLoggedIn
                ? " " + PlayerProfile.Instance.UserName
                : "offline-mode";

            ScoreManager.AddScore(newScore);
            _audio.Stop();

            _game.ChangeState(new ScoreBoardState(_game, _spriteBatch, _textRenderer, _audio, newScore, _mapData));
        }

        private void UpdateComboPosition()
        {
            comboApof.Position = new Vector2(combo.Position.X - 15, combo.Position.Y + 15);
        }

        private void UpdateDisplayedValues(double currentTime)
        {
            const float lerpSpeed = 25.0f;

            _displayedScore = MathHelper.Lerp(_displayedScore, session.TotalScore, lerpSpeed * (float)currentTime);
            _displayedAccuraci = MathHelper.Lerp(_displayedAccuraci, session.Accuracy, lerpSpeed * (float)currentTime);
        }

        private void UpdateUIText()
        {
            int intScore = (int)Math.Round(_displayedScore);
            score.Text = intScore.ToString("D9");

            Accuraci.Text = (Math.Round(_displayedAccuraci * 100f) / 100f).ToString() + "%";
            combo.Text = session.Combo.ToString();
        }

        #region InputCharDraw — полностью переработан (разделена логика)

        private void InputCharDraw(GameSession session, Matrix4 projection, float centerX, float y)
        {
            if (session.CurrentPhaseChars == null || session.CurrentPhaseChars.Length == 0)
                return;

            string text = new string(session.CurrentPhaseChars);
            float maxPixelWidth = _game.ClientSize.X * 0.85f;

            float bestScale = CalculateBestTextScale(text, maxPixelWidth);
            float finalScaleX = bestScale * Scene.CanvasScale.X;
            float finalScaleY = bestScale * Scene.CanvasScale.Y;

            double currentTime = _audio?.GetCurrentTime() ?? 0.0;

            Color4[] colors = ComputeCharColors(session, text, currentTime);

            var charBounds = _textRenderer.GetCharBounds(text, centerX, y, Scene.CanvasScale, bestScale, 1.0f, TextAlign.Center);
            if (charBounds == null || charBounds.Length == 0)
                return;

            // 1. Рамки слайдеров (рисуются ПОД текстом)
            DrawSliderFrames(text, charBounds, session, currentTime, projection);

            // 2. Свечение текущей буквы (только если фраза ещё не завершена)
            if (!session.PhaseComplete)
            {
                DrawCurrentCharGlow(session.CurrentCharIndex, text, charBounds, finalScaleX, finalScaleY, projection, currentTime);
            }

            // 3. Свечение всей завершённой фразы (если PhaseComplete)
            if (session.PhaseComplete)
            {
                DrawCompletedPhraseGlow(text, centerX, y, bestScale, projection, currentTime);
            }

            // 4. Основной текст с цветами букв
            _textRenderer.DrawStringWithCharColorsScaled(
                text, centerX, y, Scene.CanvasScale, bestScale, 1.0f, colors, projection, TextAlign.Center
            );

            // 5. Прогресс-бар холда слайдера внизу экрана
            if (session.IsHoldingSlider)
            {
                DrawSliderHoldBar(session, projection);
            }
        }

        #endregion

        #region Вспомогательные методы InputCharDraw (чисто, читаемо, без дублирования)

        private float CalculateBestTextScale(string text, float maxPixelWidth)
        {
            for (float testScale = 1.8f; testScale >= 0.5f; testScale -= 0.02f)
            {
                float estimatedWidth = _textRenderer.CalculateTextWidth(text, testScale * Scene.CanvasScale.X);
                if (estimatedWidth <= maxPixelWidth)
                    return testScale;
            }
            return 0.5f;
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

                DrawGlow(rectX, rectY, rectW, rectH, borderColor, 0.3f, projection,currentTime);

                _spriteBatch.DrawRect(rectX, rectY, rectW, thickness, borderColor, projection);
                _spriteBatch.DrawRect(rectX, rectY + rectH - thickness, rectW, thickness, borderColor, projection);
                _spriteBatch.DrawRect(rectX, rectY, thickness, rectH, borderColor, projection);
                _spriteBatch.DrawRect(rectX + rectW - thickness, rectY, thickness, rectH, borderColor, projection);
            }
        }

        private void DrawCurrentCharGlow(int currentCharIdx, string text, (float x, float y, float width, float height)[] charBounds,
                                 float finalScaleX, float finalScaleY, Matrix4 projection, double currentTime)
        {
            if (currentCharIdx < 0 || currentCharIdx >= text.Length || currentCharIdx >= charBounds.Length)
                return;

            var bounds = charBounds[currentCharIdx];
            if (bounds.width <= 0 || bounds.height <= 0) return;

            char currentChar = text[currentCharIdx];
            if (!_textRenderer.TryGetGlyph(currentChar, out var glyph)) return;

            float baseX = bounds.x - glyph.XOffset * finalScaleX;
            float baseY = bounds.y - glyph.YOffset * finalScaleY;

            Color4 glowColor = GetGlowColor(_mapData.needR, _mapData.needG, _mapData.needB);
            DrawTextGlow(currentChar.ToString(), baseX, baseY, finalScaleX, finalScaleY, glowColor, projection, currentTime);
        }

        private Color4 GetCurrentCharGlowColor()
        {
            float r = _mapData.needR;
            float g = _mapData.needG;
            float b = _mapData.needB;

            // Исключение для чёрного цвета карты — свечение становится белым
            if (r < 0.1f && g < 0.1f && b < 0.1f)
                return Color4.White;

            return new Color4(
                MathHelper.Clamp(r * 1.5f, 0f, 3f),
                MathHelper.Clamp(g * 1.5f, 0f, 3f),
                MathHelper.Clamp(b * 1.5f, 0f, 3f),
                1f
            );
        }

        private void DrawSliderHoldBar(GameSession session, Matrix4 projection)
        {
            int sliderIndex = session.CurrentSliderCharIndex;
            if (session.CurrentSliders == null || !session.CurrentSliders.TryGetValue(sliderIndex, out var activeSlider))
                return;

            double currentAudioTime = _audio?.GetCurrentTime() ?? 0.0;

            float barWidth = _game.ClientSize.X * 0.8f;
            float barHeight = 24f;
            float barX = (_game.ClientSize.X - barWidth) / 2f;
            float barY = _game.ClientSize.Y - barHeight - 30f;

            float duration = Math.Max(activeSlider.endTime - activeSlider.startTime, 0.01f);
            float progress = 0f;
            if (currentAudioTime >= activeSlider.startTime)
                progress = (float)((currentAudioTime - activeSlider.startTime) / duration);
            progress = MathHelper.Clamp(progress, 0f, 1f);

            bool isSuccess = session.SuccessfullyHeldSliders.Contains(sliderIndex);

            Color4 fillColor = isSuccess
                ? SliderBarSuccess
                : (currentAudioTime >= activeSlider.startTime - 0.3f ? SliderBarHolding : SliderBarReady);

            // Фон бара
            _spriteBatch.DrawRect(barX, barY, barWidth, barHeight, SliderBarBackground, projection);

            // Заполнение прогресса
            float fillWidth = barWidth * progress;
            if (fillWidth > 0f)
                _spriteBatch.DrawRect(barX, barY, fillWidth, barHeight, fillColor, projection);

            // Обводка
            _spriteBatch.DrawRect(barX, barY, barWidth, 1f, SliderBarOutline, projection);
            _spriteBatch.DrawRect(barX, barY + barHeight - 1f, barWidth, 1f, SliderBarOutline, projection);
            _spriteBatch.DrawRect(barX, barY, 1f, barHeight, SliderBarOutline, projection);
            _spriteBatch.DrawRect(barX + barWidth - 1f, barY, 1f, barHeight, SliderBarOutline, projection);
        }

        private void DrawCompletedPhraseGlow(string text, float centerX, float centerY,
                                     float bestScale, Matrix4 projection, double currentTime)
        {
            if (string.IsNullOrEmpty(text)) return;

            Color4 baseGlow = GetGlowColor(_mapData.completeR, _mapData.completeG, _mapData.completeB);
            float pulse = 0.8f + 0.2f * (float)Math.Sin(currentTime * 6.0);

            // Желаемая толщина свечения в реальных пикселях экрана (константы, не зависят от разрешения)
            const float outerThicknessPx = 12f;   // внешнее свечение 12 пикселей
            const float innerThicknessPx = 6f;    // внутреннее свечение 6 пикселей

            // Переводим пиксели в виртуальные координаты (делим на CanvasScale)
            float outerOffsetVirtual = outerThicknessPx / Scene.CanvasScale.X;
            float innerOffsetVirtual = innerThicknessPx / Scene.CanvasScale.X;

            // Используем те же параметры, что и для основного текста:
            // baseScale = bestScale, finalScale = 1.0f
            for (int i = 0; i < 16; i++)
            {
                float angle = i * MathHelper.TwoPi / 16f;
                float dx = (float)Math.Cos(angle) * outerOffsetVirtual;
                float dy = (float)Math.Sin(angle) * outerOffsetVirtual;

                Color4 col = new Color4(baseGlow.R * 0.7f, baseGlow.G * 0.7f, baseGlow.B * 0.7f, 0.08f * pulse);
                _textRenderer.DrawStringWithCharColorsScaled(
                    text, centerX + dx, centerY + dy,
                    Scene.CanvasScale, bestScale, 1.0f,
                    Enumerable.Repeat(col, text.Length).ToArray(),
                    projection, TextAlign.Center);
            }

            for (int i = 0; i < 12; i++)
            {
                float angle = i * MathHelper.TwoPi / 12f;
                float dx = (float)Math.Cos(angle) * innerOffsetVirtual;
                float dy = (float)Math.Sin(angle) * innerOffsetVirtual;

                Color4 col = new Color4(baseGlow.R, baseGlow.G, baseGlow.B, 0.25f * pulse);
                _textRenderer.DrawStringWithCharColorsScaled(
                    text, centerX + dx, centerY + dy,
                    Scene.CanvasScale, bestScale, 1.0f,
                    Enumerable.Repeat(col, text.Length).ToArray(),
                    projection, TextAlign.Center);
            }
        }

        private Color4 GetGlowColor(float r, float g, float b)
        {
            // Если цвет почти чёрный — делаем свечение белым
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
                          Color4 baseGlowColor, Matrix4 projection, double currentTime)
        {
            float pulse = 0.7f + 0.3f * (float)Math.Sin(currentTime * 10.0); // пульсация 0.4..1.0
            float outerAlpha = 0.12f * pulse;
            float midAlpha = 0.35f * pulse;
            float innerAlpha = 0.7f * pulse;

            // Внешнее свечение – холодный оттенок (добавляем синевы)
            Color4 outerColor = new Color4(
                MathHelper.Clamp(baseGlowColor.R * 0.6f, 0f, 1f),
                MathHelper.Clamp(baseGlowColor.G * 0.8f, 0f, 1f),
                MathHelper.Clamp(baseGlowColor.B * 1.2f, 0f, 1f),
                outerAlpha
            );

            // Среднее свечение – нейтральное
            Color4 midColor = new Color4(baseGlowColor.R, baseGlowColor.G, baseGlowColor.B, midAlpha);

            // Внутреннее свечение – тёплое / яркое
            Color4 innerColor = new Color4(
                MathHelper.Clamp(baseGlowColor.R * 1.4f, 0f, 1f),
                MathHelper.Clamp(baseGlowColor.G * 1.2f, 0f, 1f),
                MathHelper.Clamp(baseGlowColor.B * 0.9f, 0f, 1f),
                innerAlpha
            );

            // Внешний слой (радиус 6–8 px)
            for (int dir = 0; dir < 24; dir++)
            {
                float angle = dir * MathHelper.TwoPi / 24f;
                float dx = (float)Math.Cos(angle) * 7.5f;
                float dy = (float)Math.Sin(angle) * 7.5f;
                _textRenderer.DrawString(text, baseX + dx, baseY + dy,
                    scaleX, scaleY,
                    outerColor.R, outerColor.G, outerColor.B, outerColor.A,
                    projection);
            }

            // Средний слой (радиус 3–4 px)
            for (int dir = 0; dir < 18; dir++)
            {
                float angle = dir * MathHelper.TwoPi / 18f;
                float dx = (float)Math.Cos(angle) * 4.2f;
                float dy = (float)Math.Sin(angle) * 4.2f;
                _textRenderer.DrawString(text, baseX + dx, baseY + dy,
                    scaleX, scaleY,
                    midColor.R, midColor.G, midColor.B, midColor.A,
                    projection);
            }

            // Внутренний слой (радиус 1.5–2 px)
            for (int dir = 0; dir < 12; dir++)
            {
                float angle = dir * MathHelper.TwoPi / 12f;
                float dx = (float)Math.Cos(angle) * 2.2f;
                float dy = (float)Math.Sin(angle) * 2.2f;
                _textRenderer.DrawString(text, baseX + dx, baseY + dy,
                    scaleX, scaleY,
                    innerColor.R, innerColor.G, innerColor.B, innerColor.A,
                    projection);
            }
        }
        #endregion

        private void DrawGlow(float x, float y, float w, float h, Color4 color, float strength, Matrix4 projection, double currentTime)
        {
            float pulse = 0.6f + 0.4f * (float)Math.Sin(currentTime * 5.0);
            // Внешнее свечение – 5 слоёв с затуханием
            for (int i = 1; i <= 5; i++)
            {
                float offset = i * 1.8f;
                float alpha = strength * (0.18f / i) * pulse;
                Color4 glowCol = new Color4(color.R, color.G, color.B, alpha);
                _spriteBatch.DrawRect(x - offset, y - offset, w + offset * 2, h + offset * 2, glowCol, projection);
            }
            // Ближний слой – чуть ярче
            Color4 nearCol = new Color4(color.R, color.G, color.B, strength * 0.4f * pulse);
            _spriteBatch.DrawRect(x - 1, y - 1, w + 2, h + 2, nearCol, projection);
        }

        #endregion
    }
}