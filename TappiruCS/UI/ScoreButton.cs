using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using TappiruCS.Server.Player;

namespace TappiruCS.UI
{
    public class ScoreButton : Button
    {
        public SpriteObject Avatar { get; private set; }
        public SpriteObject Grade { get; private set; }

        public TextObject PlayerNameText { get; private set; }
        public TextObject ScoreComboText { get; private set; }
        public TextObject AccuracyText { get; private set; }

        private readonly PlayerScore _scoreData;

        public int grade;

        public ScoreButton(SpriteBatch spriteBatch, TextRender textRenderer, float x, float y, PlayerScore score)
            : base(spriteBatch, textRenderer, x, y, 700, 100, "", "", Color4.White)
        {
            _scoreData = score;
            Tag = "scorebutton";
            Text = "";
            NormalColor = new Color4(1f, 1f, 1f, 0.5f);
            HoverColor = new Color4(1.5f, 1.5f, 1.5f, 0.3f);

            if (score._accuraci == 100f && score._failChar == 0)
                grade = TextureLoader.Load("Textures/grade/grade5.png");
            else if (score._accuraci > 90.0f && score._failChar == 0)
                grade = TextureLoader.Load("Textures/grade/grade4.png");
            else if ((score._accuraci > 80.0f && score._failChar == 0) || (score._accuraci > 90.0f))
                grade = TextureLoader.Load("Textures/grade/grade3.png");
            else if ((score._accuraci > 70.0f && score._failChar == 0) || (score._accuraci > 80.0f))  
                grade = TextureLoader.Load("Textures/grade/grade2.png");                                
            else if (score._accuraci > 60.0f)
                grade = TextureLoader.Load("Textures/grade/grade1.png");
            else
                grade = TextureLoader.Load("Textures/grade/grade0.png");

            // === Аватар ===
            if (PlayerProfile.Instance.IsLoggedIn)
            {
                Avatar = new SpriteObject(spriteBatch, PlayerProfile.Instance.AvatarTextureId, Position.X - 130, Position.Y, 80, 80)
                {
                    Pivot = new Vector2(0.5f, 0.5f)
                };
            }
            else
            {
                Avatar = new SpriteObject(spriteBatch, TextureManager.GetTexture("defaultprofile"), Position.X - 130, Position.Y, 80, 80)
                {
                    Pivot = new Vector2(0.5f, 0.5f)
                };
            }

            // === Грейд — сразу справа от аватарки ===
            Grade = new SpriteObject(spriteBatch, grade, Position.X - 58, Position.Y, 37, 44)
            {
                Pivot = new Vector2(0.5f, 0.5f),
                ScaleMultiply = 2f
            };

            // === Тексты ===
            PlayerNameText = new TextObject(textRenderer, "", Position.X-10, Position.Y-50, 1f)
            {
                ScaleMultiply = 0.29f,
                Align = TextRender.TextAlign.Left,
                Color = Color4.White
            };

            ScoreComboText = new TextObject(textRenderer, "", Position.X - 10, Position.Y, 1f)
            {
                ScaleMultiply = 0.245f,
                Align = TextRender.TextAlign.Left,
                Color = new Color4(0.95f, 0.95f, 0.95f, 1f)
            };

            AccuracyText = new TextObject(textRenderer, "", Position.X+324, Position.Y+19, 1f)
            {
                ScaleMultiply = 0.22f,
                Align = TextRender.TextAlign.Right,

            };

            AddChild(Avatar);
            AddChild(Grade);
            AddChild(PlayerNameText);
            AddChild(ScoreComboText);
            AddChild(AccuracyText);

            UpdateContent();
        }

        private void UpdateContent()
        {
            PlayerNameText.Text = _scoreData.PlayerName;
            ScoreComboText.Text = $"Очки:{_scoreData._score:N0} (x{_scoreData._maxCobmo})";
            AccuracyText.Text = $"{_scoreData._accuraci:F2}%";
        }

        public void SetRank(int rank)
        {
            PlayerNameText.Text = $"{rank}. {_scoreData.PlayerName}";
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);
            Avatar.Opacity = 1f;
            Grade.Opacity = 1f;
            UpdateContent();

        }

        public override void Draw(Matrix4 projection)
        {  

            var (dLeft, dTop, _, _) = GetDesignBounds();
            base.Draw(projection);

        }
    }
}