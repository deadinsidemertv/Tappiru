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
using TappiruCS.State;



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
            this.WindowState = WindowState.Normal;
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
            textRenderer = new TextRender(spriteBatch,TextureLoader.fontTexture,TextureLoader.textureWidth,TextureLoader.textureHeight, 8,12); // 8 6

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
