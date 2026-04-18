using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core;
using TappiruCS.Core.GameObject;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using TappiruCS.Server;
using TappiruCS.Server.Player;
using TappiruCS.UI;
using System.IO;

namespace TappiruCS.State.Session
{
    public class GameSessionState : IGameState
    {
        // === Зависимости ===
        private readonly RenderContext _context;

        // === Данные игры ===
        public GameSession session;
        public MapData _mapData;

        // === Визуальные объекты ===
        private readonly Scene _scene = new Scene();
        public Background bg;
        public Background Fade;
        public SpriteObject scorebarBG;

        public VideoBackground videoBg;

        private readonly PhraseDisplayRenderer _phraseRenderer;     // ← вынесен
        private ScoreBarUI _scoreBarUI;                    // ← вынесен (по желанию)

        // === Состояние ввода ===
        private readonly HashSet<Keys> _pressedKeys = new HashSet<Keys>();

        public GameSessionState(RenderContext context, MapData mapdata)
        {
            _context = context;
            _mapData = mapdata;

            _phraseRenderer = new PhraseDisplayRenderer(context, mapdata);
            _scoreBarUI = new ScoreBarUI(context.TextRenderer); 
        }

        public void OnEnter()
        {
            _scene.Initialize(_context);
            InputMapping.Initialize();

            session = new GameSession(_mapData);

            int scoreBarTex = TextureManager.GetTexture("scorebg");

            // === ПРИОРИТЕТ ВИДЕО ===
            if (!string.IsNullOrEmpty(_mapData.videoPath) && File.Exists(_mapData.videoPath))
            {
                videoBg = new VideoBackground(_mapData.videoPath);
                _scene.Add(videoBg);
                videoBg.LoadVideo();

                Console.WriteLine($"[GameSession] ✅ Видео-фон активирован: {Path.GetFileName(_mapData.videoPath)}");
            }
            else
            {
                int bgTex = TextureLoader.Load(_mapData.backGroundPath);
                bg = new Background(bgTex)
                {
                    AutoBreathingParallax = true,
                    BreathingSpeed = 0.65f,
                    BreathingStrength = 12f
                };
                _scene.Add(bg);
                Console.WriteLine("[GameSession] Используется статичный фон");
            }

            Fade = new Background(0) { Opacity = 0.7f };
            scorebarBG = new SpriteObject(scoreBarTex, 960, 540, 1920, 1080) { AllowHover = false };

            _scoreBarUI = new ScoreBarUI(_context.TextRenderer);
            _scoreBarUI.AddToScene(_scene);

            _scene.Add(Fade);
            _scene.Add(scorebarBG);

            _context.Audio.LoadMusic(_mapData.audioPath);
            _context.Audio.Play();
        }

        public void OnExit()
        {
            _context.Audio.Stop();
            videoBg?.DisposeVideo();
            videoBg = null;
            _scene.Clear();
        }

        public void Update(double currentTime)
        {
            _scene.Update(currentTime, _context.Game.MouseState, _context.Game);

            if (session == null) return;

            float audioTime = _context.Audio?.GetCurrentTime() ?? 0f;
            session.Update(audioTime, _context.Game.KeyboardState);

            CheckForMapCompletion(audioTime);
            _scoreBarUI.Update(session, currentTime);
        }

        public void Render(Matrix4 projection)
        {
            _scene.Draw(projection);
            _phraseRenderer.Draw(session, projection, 960, 440);
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e)
        {
            if (session == null) return;

            double currentTime = _context.Audio?.GetCurrentTime() ?? 0.0;

            if (!session.IsInputAllowed(currentTime)) return;
            if (_pressedKeys.Contains(e.Key)) return;

            if (!InputMapping.KeyToCharsMap.TryGetValue(e.Key, out char[] possibleChars))
                return;

            int idx = session.CurrentCharIndex;
            if (idx >= session.CurrentPhaseChars.Length) return;

            char expected = session.CurrentPhaseChars[idx];

            if (Array.IndexOf(possibleChars, expected) >= 0)
                session.HandleInput(expected, currentTime);
            else
                session.HandleInput('\0', currentTime);

            _pressedKeys.Add(e.Key);
        }

        public void HandleKeyUp(KeyboardKeyEventArgs e)
        {
            _pressedKeys.Remove(e.Key);
        }

        private void CheckForMapCompletion(float currentAudioTime)
        {
            if (currentAudioTime < session.endTime) return;

            var newScore = new PlayerScore
            {
                MapName = session.CurrentMap.title,
                MapHash = session.CurrentMap.MapHash,
                _score = session.TotalScore,
                _accuraci = session.Accuracy,
                _maxCobmo = session.MaxCombo,
                _completePhase = session.CompletedPhases,
                _failPhase = session.FailedPhases,
                _completeChar = session.CorrectHits,
                _failChar = session.Misses,
                PlayedAt = DateTime.Now,
                PlayerName = PlayerProfile.Instance.IsLoggedIn ? " " + PlayerProfile.Instance.UserName : "offline-mode"
            };

            ScoreManager.AddScore(newScore);
            _context.Audio.Stop();

            if (string.IsNullOrEmpty(Auth.AuthToken))
            {
                Console.WriteLine("Нет токена — результат не отправлен (оффлайн)");
                
            }
            if (PlayerProfile.Instance.IsLoggedIn)
            {
                _ = ScoreSubmitter.SubmitScoreAsync(newScore);   // fire-and-forget
            }

            _context.Game.ChangeState(new ScoreBoardState(_context, newScore, _mapData));
        }
    }
}