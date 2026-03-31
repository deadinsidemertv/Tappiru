using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using TappiruCS.GameLogic;
using TappiruCS.Render;



namespace TappiruCS
{
    public class Game : GameWindow
    {
        public static float WindowWidth;
        public static float WindowHeight;

        private IGameState currentState;

        private SpriteBatch spriteBatch;
        private TextRender textRenderer;
        private AudioManager audio;

        private Matrix4 projection;
     


        public Game(GameWindowSettings gwSettings, NativeWindowSettings nwSetting) : base(gwSettings,nwSetting)
        {
            
            this.ClientSize = new Vector2i(1280, 720);
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        }

        GameSessionState gameRD;

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            UpdateProjection();
        }
        protected override void OnLoad()
        {
            base.OnLoad();
            UpdateProjection();


            WindowWidth = ClientSize.X;
            WindowHeight = ClientSize.Y;

            GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);

            TextureLoader.SetupGraphics();

            spriteBatch = new SpriteBatch(TextureLoader.shaderProgram);
            textRenderer = new TextRender(spriteBatch,TextureLoader.fontTexture,TextureLoader.textureWidth,TextureLoader.textureHeight, 8,6); // 8 6

            audio = new AudioManager();


            currentState = new MenuState(this, spriteBatch, textRenderer, audio);
            currentState.OnEnter();
            
        }



        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            
            currentState?.Update(args.Time);
        }






        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            currentState.Render(projection);
            SwapBuffers();
        }






        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            currentState?.HandleKeyDown(e);
        }

        public static MapData MapLoad(string mapFolderPath)
        {
            
            MapData mapdata = new MapData();
            string[] bgP = Directory.GetFiles(mapFolderPath,"*.jpg");
            mapdata.backGroundPath = bgP[0];
            string[] audioP = Directory.GetFiles(mapFolderPath, "*.mp3");
            mapdata.audioPath = audioP[0];
            string[] dataP = Directory.GetFiles(mapFolderPath, "*.tapp");
            mapdata.dataPath = dataP[0];

            string json = File.ReadAllText(mapdata.dataPath);
            JsonMap tmp = JsonSerializer.Deserialize<JsonMap>(json);
            mapdata.Events = tmp.events;
            return mapdata;
        }

        public char KeyToChar(KeyboardKeyEventArgs e)
        {
            char ch = '\0';
            switch (e.Key)
            {
                case Keys.A: return 'ф';
                case Keys.B: return 'и';
                case Keys.C: return 'с';
                case Keys.D: return 'в';
                case Keys.E: return 'у';
                case Keys.F: return 'а';
                case Keys.G: return 'п';
                case Keys.H: return 'р';
                case Keys.I: return 'ш';
                case Keys.J: return 'о';
                case Keys.K: return 'л';
                case Keys.L: return 'д';
                case Keys.M: return 'ь';
                case Keys.N: return 'т';
                case Keys.O: return 'щ';
                case Keys.P: return 'з';
                case Keys.Q: return 'й';
                case Keys.R: return 'к';
                case Keys.S: return 'ы';
                case Keys.T: return 'е';
                case Keys.U: return 'г';
                case Keys.V: return 'м';
                case Keys.W: return 'ц';
                case Keys.X: return 'ч';
                case Keys.Y: return 'н';
                case Keys.Z: return 'я';

                // Дополнительные буквы
                case Keys.LeftBracket: return 'х';      // [
                case Keys.RightBracket: return 'ъ';     // ]
                case Keys.Semicolon: return 'ж';        // ;
                case Keys.Apostrophe: return 'э';            // '
                case Keys.Comma: return 'б';            // ,
                case Keys.Period: return 'ю';           // .
                case Keys.GraveAccent: return 'ё';      // ` (клавиша с ё)



                case Keys.Space: return ' ';

                default: return '\0';

            }
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

        public void ChangeState(IGameState newState)
        {
            currentState?.OnExit();
            currentState = newState;
            currentState.OnEnter();
        }
       

    }
}
