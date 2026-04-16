using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System.Collections.Concurrent;
using TappiruCS.Core;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using TappiruCS.Server;
using TappiruCS.State.Edit;
using System.ComponentModel;
using TappiruCS.State;
using TappiruCS.State.Session;

namespace TappiruCS
{
    public class Game : GameWindow
    {
        private readonly Queue<Action> _mainThreadActions = new Queue<Action>();

        public static float WindowWidth;
        public static float WindowHeight;

        private IGameState currentState;

        private SpriteBatch spriteBatch;
        public static TextRender japanFONT;
        private TextRender textRenderer;
        private AudioManager audio;

        private Matrix4 projection;

        // === FADE SYSTEM ===
        private float _fadeAlpha = 0f;           
        private float _fadeSpeed = 2.5f;         // скорость (чем больше — тем быстрее)
        private IGameState _pendingState = null;
        private bool _isFadingOut = false;
        private bool _isTransitioning = false;

        public RenderContext RenderContext { get; private set; }

        public Game(GameWindowSettings gwSettings, NativeWindowSettings nwSetting) : base(gwSettings,nwSetting)
        {
            
            this.ClientSize = new Vector2i(1280, 720);
            this.WindowState = WindowState.Normal;
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            
        }
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            UpdateProjection();
        }
        protected override void OnLoad()
        {
            base.OnLoad();
            DifficultyCalculator.RecalculateAllStarRatings(force: true);
            UpdateProjection();


            WindowWidth = ClientSize.X;
            WindowHeight = ClientSize.Y;

            GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);

            TextureLoader.SetupGraphics();

            spriteBatch = new SpriteBatch(TextureLoader.shaderProgram);
            textRenderer = new TextRender(spriteBatch, "Textures\\Font\\font_cyrillic.fnt");
            audio = new AudioManager();

            RenderContext = new RenderContext(this, spriteBatch, textRenderer, audio);
            //audio.LoadSoundEffect("hover", "Textures/hover.ogg");
            audio.LoadSoundEffect("matchStart", "Textures/Sound/match-start.mp3");
            audio.LoadSoundEffect("hover", "Textures/Sound/hover.mp3");
            audio.LoadSoundEffect("hit", "Textures/Sound/hit-sound.mp3");


            currentState = new MenuState(RenderContext);
            currentState.OnEnter();

            _fadeAlpha = 1f;                     // полностью чёрный
            _pendingState = currentState;        // фиктивный переход (та же сцена)
            _isFadingOut = false;                // режим «появление из черного»


            
        }

        public void InvokeOnMainThread(Action action)
        {
            if (action == null) return;

            lock (_mainThreadActions)   // потокобезопасно
            {
                _mainThreadActions.Enqueue(action);
            }
        }
        protected override void OnUpdateFrame(FrameEventArgs args)
        {
           

            base.OnUpdateFrame(args);
            float delta = (float)args.Time;
            float fadeDelta = Math.Min(delta, 0.05f);

            if (_pendingState != null)
            {
                if (_isFadingOut)
                {
                    _fadeAlpha += _fadeSpeed * fadeDelta;
                    if (_fadeAlpha >= 1f)
                    {
                        _fadeAlpha = 1f;

                        // === Смена состояния только здесь ===
                        currentState?.OnExit();
                        currentState = _pendingState;
                        currentState.OnEnter();

                        _isFadingOut = false;   // теперь будет fade-in
                    }
                }
                else
                {
                    _fadeAlpha -= _fadeSpeed * fadeDelta;
                    if (_fadeAlpha <= 0f)
                    {
                        _fadeAlpha = 0f;
                        _pendingState = null;
                        _isTransitioning = false;
                    }
                }
            }

            lock (_mainThreadActions)
            {
                while (_mainThreadActions.Count > 0)
                {
                    try
                    {
                        _mainThreadActions.Dequeue().Invoke();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка в главном потоке: {ex.Message}");
                    }
                }
            }

            currentState?.Update(args.Time);
        }
        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            currentState.Render(projection);
            // === BLACK FADE (рисуется поверх всего) ===
            if (_fadeAlpha > 0.001f)
            {
                spriteBatch.Draw(
                    0,  // у тебя уже есть текстура black
                    0, 0,
                    ClientSize.X, ClientSize.Y,
                    0, 0, 1, 1,
                    0f, 0f, 0f, _fadeAlpha,
                    projection);
            }

            SwapBuffers();
        }
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            // Передаём событие в текущий игровой стейт
            if (currentState is SongSelectState songSelect)
            {
                songSelect.OnMouseWheel(e);
            }
        }
        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            audio.PlaySoundEffect("hit",1.2f);
            currentState?.HandleKeyDown(e);
        }
        public void UpdateProjection()
        {
            int width = ClientSize.X;
            int height = ClientSize.Y;
            // Ортографическая проекция: (0,0) – левый верхний угол, (width, height) – правый нижний
            projection = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);
            // Передаём в SpriteBatch (или в шейдер напрямую)
        }

        protected override void OnUnload()
        {
            audio?.Dispose();
            base.OnUnload();
        }
        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (currentState is GameSessionState gameSessionState)
            {
                gameSessionState.HandleKeyUp(e);
            }
        }
        public void ChangeState(IGameState newState)
        {
            if (_pendingState != null || _isTransitioning) return;

            _pendingState = newState;
            _isFadingOut = true;
            _isTransitioning = true;

           
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            // Если сейчас активен EditState
            if (currentState is EditState editState)
                editState.OnExit();  
            // или вызови принудительную очистку
            base.OnClosing(e);
        }
    }
}
