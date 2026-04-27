using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Drawing.Text;
using TappiruCS.Core;
using TappiruCS.GameLogic;
using TappiruCS.GameLogic.Mod;
using TappiruCS.Render;
using TappiruCS.Render.Text;
using TappiruCS.Server.Player;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;
using static TappiruCS.Render.Text.Font;

namespace TappiruCS.State.SongSelector.RankingPanel
{
    public class ScoreButton : Button
    {
        // ── Публичные дочерние объекты (нужны снаружи для управления видимостью) ──
        public SpriteObject Avatar { get; private set; }
        public SpriteObject Grade { get; private set; }

        // ── Приватные текстовые поля ──
        private readonly TextObject _playerNameText;
        private readonly TextObject _scoreComboText;
        private readonly TextObject _accuracyText;

        private readonly PlayerScore _score;

        // ── Константы макета ──
        private const int ButtonWidth = 700;
        private const int ButtonHeight = 100;
        private const float DefaultOpacity = 0.5f;

        public ScoreButton(float x, float y, PlayerScore score)
            : base(x, y, ButtonWidth, ButtonHeight, "white", "")
        {
            _buttonBackground.Opacity = 0.5f;
            _score = score;
            LocalPosition = new Vector2(x, y);
            NormalColor = Color4.Black;
            HoverColor = new Color4(0.2f, 0.2f, 0.2f, 1f);
            Tag = "scorebutton";
            Text = "";
            Opacity = DefaultOpacity;
            AllowHover = true;
            Layer = 10;
            Description = "player score";

            Avatar = BuildAvatar();
            Grade = BuildGrade(ResolveGradeTexture(score));

            _playerNameText = BuildPlayerNameText();
            _scoreComboText = BuildScoreComboText();
            _accuracyText = BuildAccuracyText();

            AddChild(Avatar);
            AddChild(Grade);
            AddChild(_playerNameText);
            AddChild(_scoreComboText);
            AddChild(_accuracyText);

            RefreshContent();
        }

        // ─────────────────────────────────────────────
        //  Публичный API
        // ─────────────────────────────────────────────

        /// <summary>Добавляет номер позиции перед именем игрока.</summary>
        public void SetRank(int rank)
        {
            _playerNameText.Text = $"{rank}. {_score.PlayerName}";
        }

        // ─────────────────────────────────────────────
        //  Overrides
        // ─────────────────────────────────────────────

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);

            Avatar.Opacity = 1f;
            Grade.Opacity = 1f;

            RefreshContent();
        }

        public override void Draw(Matrix4 projection)
        {
            base.Draw(projection);
        }

        // ─────────────────────────────────────────────
        //  Приватные методы построения UI
        // ─────────────────────────────────────────────

        private SpriteObject BuildAvatar()
        {
            int avatarTexture = PlayerProfile.Instance.IsLoggedIn
                ? PlayerProfile.Instance.AvatarTextureId
                : TextureManager.GetTexture("defaultprofile");

            return new SpriteObject(avatarTexture, -130, 0, 80, 80)
            {
                Pivot = new Vector2(0.5f, 0.5f),
                Parent = this,
                AllowHover = false,
            };
        }

        private SpriteObject BuildGrade(int gradeTexture)
        {
            return new SpriteObject(gradeTexture, - 58,0, 37, 44)
            {
                Pivot = new Vector2(0.5f, 0.5f),
                ScaleMultiply = 2f,
                AllowHover = false,
                Parent = this,
            };
        }

        private TextObject BuildPlayerNameText() =>
            new TextObject("",  - 30,  - 50, 36f)
            {
                ScaleMultiply = 1f,
                Align = TextAlign.Left,
                Color = Color4.White,
                AllowHover = false,
                Parent = this,
            };

        private TextObject BuildScoreComboText() =>
            new TextObject("",  - 30, 0, 36f)
            {
                ScaleMultiply = 1f,
                Align = TextAlign.Left,
                Color = new Color4(0.95f, 0.95f, 0.95f, 1f),
                AllowHover = false,
                Parent = this,
            };

        private TextObject BuildAccuracyText() =>
            new TextObject("",  + 324,  + 19, 32f)
            {
                ScaleMultiply = 1f,
                Align = TextAlign.Right,
                AllowHover = false,
                Parent = this,
            };

        private void RefreshContent()
        {
            string modsText = FormatMods(_score.mods);
            _playerNameText.Text = _score.PlayerName+"      "+modsText;
            _scoreComboText.Text = $"Очки: {_score._score:N0}  (x{_score._maxCobmo})";
            _accuracyText.Text = $"{_score._accuraci:F2}%";

            Description = $"played at: {_score.PlayedAt} | 300x: {_score._completePhase} | 100x: {_score._completeChar} | miss: {_score._failChar}";
        }
        private string FormatMods(List<GameMod> mods)
        {
            if (mods == null || mods.Count == 0)
                return "";  // или пусто, если модов нет

            // Предположим, что у GameMod есть string ShortName или переопределён ToString()
            return string.Join(", ", mods.Select(m => m.ShortName));
        }
        // ─────────────────────────────────────────────
        //  Статика: определение грейда
        // ─────────────────────────────────────────────

        private static int ResolveGradeTexture(PlayerScore score)
        {
            string gradeName = CalculateGrade(score._accuraci, score._failChar);
            return TextureLoader.Load($"Textures/grade/{gradeName}.png");
        }

        private static string CalculateGrade(float accuracy, int failCount)
        {
            bool noFail = failCount == 0;

            if (accuracy == 100f && noFail) return "grade5";
            if (accuracy > 90f && noFail) return "grade4";
            if ((accuracy > 80f && noFail) ||
                accuracy > 90f) return "grade3";
            if ((accuracy > 70f && noFail) ||
                accuracy > 80f) return "grade2";
            if (accuracy > 60f) return "grade1";

            return "grade0";
        }
    }
}