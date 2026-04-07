using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using System;
using System.Collections.Generic;
using System.Text;
using TappiruCS.Core;
using TappiruCS.Render;
using TappiruCS.UI;

namespace TappiruCS.State
{
    internal class EditState : IGameState
    {
        private readonly Game _game;
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRenderer;
        private readonly AudioManager _audio;

        private readonly Scene _scene = new Scene();

        public Slider slider;

        public Button createmap;
        public EditState(Game game, SpriteBatch spriteBatch, TextRender textRenderer, AudioManager audio)
        {
            _game = game;
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            _audio = audio;
        }
        public void OnEnter()
        {
            var bg = new Background(_spriteBatch, TextureManager.GetTexture("defaultBG"), _game) { ParalaxEffect = true };
            var bgBlack = new Background(_spriteBatch, 0, _game) { Opacity = 0.75f };

            slider = new Slider(_spriteBatch, _textRenderer, 0, 1000, 960, 900, 1800) { AllowHover = false, Active = false };


            createmap = new Button(_spriteBatch, _textRenderer,
                160, 30, 700, 120, "button", "create", Color4.White)   // "btn" — имя текстуры через TextureManager
            {
                Layer = 0,
                TextColor = Color4.White,
                TextOffset = new Vector2(-10f, -20f),
                TextScale = 0.7f,
                ScaleMultiply = 0.4f,
                Tag = "create"
            };

            createmap.OnClick += CreateProject;

            _scene.Add(bg);
            _scene.Add(bgBlack);

            _scene.Add(createmap);

            _scene.Add(slider);
            if (slider.Active)
                _scene.Add(slider.point);
        }


        public void OnExit()
        {
            _scene.Clear();
            Console.WriteLine("Мы вышли из edit");
        }
        public void Update(double currentTime)
        {
            var mouse = _game.MouseState;
            _scene.Update(currentTime, mouse, _game);
        }
        public void Render(Matrix4 projection)
        {
            _scene.Draw(projection);
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e) { }

        public void CreateProject()
        {
            bool mp3isdone = false;

            var moduleWindow = new SpriteObject(_spriteBatch, TextureManager.GetTexture("module"), 960, 540, 600, 800);
            var inputTitle = new InputField(_game, _spriteBatch, _textRenderer, 960, 240, 600, 60)
            {
                PlaceHolderColor = Color4.White,
                PlaceHolderText = "title..."
            };
            var mp3Upload = new Button(_spriteBatch, _textRenderer, 700, 530, 150, 150, "mp3", "", Color4.White) { Layer =1};
            mp3Upload.OnClick += LoadMp3;
            var JPGUpload = new Button(_spriteBatch, _textRenderer, 900, 530, 150, 150, "png", "", Color4.White) { Layer = 1 };
            JPGUpload.OnClick += LoadBG;

            var agree = new Button(_spriteBatch, _textRenderer,
                960, 780, 700, 120, "black", "upload mp3", Color4.DarkGray)   // "btn" — имя текстуры через TextureManager
            {
                Layer = 1,
                TextColor = Color4.DarkGray,
                TextOffset = new Vector2(-10f, -30f),
                TextScale = 0.7f,
                ScaleMultiply = 0.6f,
                Tag = "create"
            };



            _scene.Add(moduleWindow);
            _scene.Add(inputTitle);

            _scene.Add(mp3Upload);
            _scene.Add(JPGUpload);

            _scene.Add(agree);

            foreach(var obj in _scene._objects)
            {
                Console.WriteLine(obj.ToString());
            }
        }

        public void LoadMp3()
        {

        }
        public void LoadBG()
        {

        }
    }
}
