using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using TappiruCS.Render.Text;
using TappiruCS.Server.Player;
using TappiruCS.UI.TextAbstract;
using static TappiruCS.Render.Text.Font;

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

        public ScoreButton(float x, float y, PlayerScore score)
            : base(x, y, 700, 100, "", "")
        {
            _scoreData = score;
            Tag = "scorebutton";
            Text = "";
            NormalColor = new Color4(1f, 1f, 1f, 0.5f);
            HoverColor = new Color4(1.5f, 1.5f, 1.5f, 0.8f);
            Opacity = 0.5f;
            AllowHover = true;

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
                Avatar = new SpriteObject(PlayerProfile.Instance.AvatarTextureId, Position.X - 130, Position.Y, 80, 80)
                {
                    Pivot = new Vector2(0.5f, 0.5f),
                    Parent = this
                };
            }
            else
            {
                Avatar = new SpriteObject(TextureManager.GetTexture("defaultprofile"), Position.X - 130, Position.Y, 80, 80)
                {
                    Pivot = new Vector2(0.5f, 0.5f),
                    Parent = this
                };
            }

            // === Грейд — сразу справа от аватарки ===
            Grade = new SpriteObject(grade, Position.X - 58, Position.Y, 37, 44)
            {
                Pivot = new Vector2(0.5f, 0.5f),
                ScaleMultiply = 2f,
                Parent = this
            };

            // === Тексты ===
            PlayerNameText = new TextObject("", Position.X-30, Position.Y-50, 36f)
            {
                ScaleMultiply = 0.29f,
                Align = TextAlign.Left,
                Color = Color4.White,
                Parent = this
            };

            ScoreComboText = new TextObject("", Position.X - 30, Position.Y, 36f)
            {
                ScaleMultiply = 0.245f,
                Align = TextAlign.Left,
                Color = new Color4(0.95f, 0.95f, 0.95f, 1f),
                Parent = this
            };

            AccuracyText = new TextObject("", Position.X+324, Position.Y+19, 32f)
            {
                ScaleMultiply = 0.22f,
                Align = TextAlign.Right,
                Parent = this
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
        public override void SetHover(bool hover)
        {
            base.SetHover(hover);


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