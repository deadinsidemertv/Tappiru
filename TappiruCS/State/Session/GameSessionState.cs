using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.IO;
using TappiruCS.Core;
using TappiruCS.Core.GameObject;
using TappiruCS.GameLogic;
using TappiruCS.GameLogic.Logic;
using TappiruCS.GameLogic.Mod;
using TappiruCS.Render;
using TappiruCS.Server;
using TappiruCS.Server.Player;
using TappiruCS.State.SongSelector;
using TappiruCS.UI;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        public ProgressBar progressbar;

        public VideoBackground videoBg;

        private readonly PhraseDisplayRenderer _phraseRenderer;     // ← вынесен
        private ScoreBarUI _scoreBarUI;                    // ← вынесен (по желанию)

        // === Состояние ввода ===
        private readonly HashSet<Keys> _pressedKeys = new HashSet<Keys>();

        public enum gameState
        {
            GameOver,
            Pause,
            Play
        }

        public gameState currentGameState { get; private set; }
        private bool _isGameOverHandled = false;

        public GameSessionState(RenderContext context, MapData mapdata)
        {
            _context = context;
            _mapData = mapdata;

            _phraseRenderer = new PhraseDisplayRenderer(context, mapdata);
            _scoreBarUI = new ScoreBarUI();
        }

        public void OnEnter()
        {
            currentGameState = gameState.Play;
            _scene.Initialize(_context);
            InputMapping.Initialize();

            session = new GameSession(_mapData);

            int scoreBarTex = TextureManager.GetTexture("gameoverlay");

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

            progressbar = new ProgressBar(80, 53, 400, 5) { Layer = 10};
            _scene.Add(progressbar);

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


            progressbar.SetValue(session.Health);
            progressbar.MaxValue = 100f;

        
           float audioTime = _context.Audio?.GetCurrentTime() ?? 0f;
           session.Update(audioTime, _context.Game.KeyboardState);


            if (session.Health <= 0f && currentGameState != gameState.GameOver && !_isGameOverHandled)
            {
                TriggerGameOver();
                return;
            }

            CheckForMapCompletion(audioTime);
           _scoreBarUI.Update(session, currentTime);
         

        }
        Background LoseBG = null;
        Button backButton = null;
        Button retryButton = null;
        public void CreatePauseMenu()
        {

            if (currentGameState == gameState.Play)
            {
                LoseBG = new Background(TextureManager.GetTexture("pause-panel")) { Layer = 10,ParalaxEffect = true };
                backButton = new Button(960, 540, 400, 100, "button", "back")
                {
                    Layer = 10,
                    TextOffset = new Vector2(-55f, 15f)
                };
                retryButton = new Button(960, 700, 400, 100, "button", "retry") 
                { 
                    Layer = 10,
                    TextOffset = new Vector2(-55f, 15f)
                };

                retryButton.OnClick += RestartGame;
                backButton.OnClick += BackToSongSelector;

                _scene.Add(LoseBG);
                _scene.Add(backButton);
                _scene.Add(retryButton);
            }
            else if(currentGameState == gameState.Pause)
            {
                _scene.Remove(LoseBG);
                _scene.Remove(backButton);
                _scene.Remove(retryButton);
            }
        }
        private void TriggerGameOver()
        {

            _isGameOverHandled = true;
            session.IsPause = true;                    // останавливаем логику
            _context.Audio.Pause();

            currentGameState = gameState.GameOver;

            CreateGameOver();                          // показываем экран Game Over

            Console.WriteLine("[GameSession] ИГРОК УМЕР — HP <= 0");
        }

        public void CreateGameOver()
        {
            if (currentGameState == gameState.GameOver)
            {
                LoseBG = new Background(TextureManager.GetTexture("gameover-panel")) { Layer = 10, ParalaxEffect = true };
                backButton = new Button(960, 540, 400, 100, "button", "back")
                {
                    Layer = 10,
                    TextOffset = new Vector2(-55f, 15f)
                };
                retryButton = new Button(960, 700, 400, 100, "button", "retry")
                {
                    Layer = 10,
                    TextOffset = new Vector2(-55f, 15f)
                };

                retryButton.OnClick += RestartGame;
                backButton.OnClick += BackToSongSelector;

                _scene.Add(LoseBG);
                _scene.Add(backButton);
                _scene.Add(retryButton);
            }
        }

        public void BackToSongSelector()=> _context.Game.ChangeState(new SongSelectState(_context));
        public void RestartGame()=> _context.Game.ChangeState(new GameSessionState(_context, _mapData));

        public void Render(Matrix4 projection)
        {
            _scene.Draw(projection);
            _phraseRenderer.Draw(session, projection, 960, 540);
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e)
        {
            if (session == null) return;

            if (e.Key == Keys.Escape && currentGameState == gameState.Play)
            {
                session.IsPause = true;
                _context.Audio.Pause();
                CreatePauseMenu();
                currentGameState = gameState.Pause;
                Console.WriteLine("GOTO PAUSE");
            }
            else if (e.Key == Keys.Escape && currentGameState == gameState.Pause)
            {
                session.IsPause = false;
                CreatePauseMenu();
                currentGameState = gameState.Play;
                _context.Audio.Resume();
                Console.WriteLine("GOTO Play");
            }

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

        private async Task CheckForMapCompletion(float currentAudioTime)
        {
            if (currentAudioTime < session.EndTime) return;

            var newScore = new PlayerScore
            {
                MapName = session.CurrentMap.title,
                MapHash = session.CurrentMap.MapHash,

                _score = session.TotalScore,
                _accuraci = session.Accuracy ,
                _maxCobmo = session.MaxCombo,

                _completePhase = session.CompletedPhases,
                _failPhase = session.FailedPhases,
                _completeChar = session.CorrectHits,
                _failChar = session.Misses,

                _perfectSlider = session.PerfectSliders,
                _goodSlider = session.GoodSliders,

                PlayedAt = DateTime.Now,

                mods = new List<GameMod>(session.CurrentMap.mods),
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
                await ScoreSubmitter.SubmitScoreAsync(newScore);   // fire-and-forget
            }

            _context.Game.ChangeState(new ScoreBoardState(_context, newScore, _mapData));
        }
    }
}