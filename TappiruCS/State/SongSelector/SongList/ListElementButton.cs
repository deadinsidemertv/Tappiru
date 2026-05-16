using NAudio.Wave.SampleProviders;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using TappiruCS.UI;
using TappiruCS.UI.Sprite;

namespace TappiruCS.State.SongSelector.SongList
{
    public class ListElementButton : Button
    {
        public int Index { get; private set; } = -1;
        public bool IsSelected { get; private set; } = false;

        // Картинка — живёт здесь, а не в базовом Button
        private readonly SpriteObject _thumbnail;
        public int ThumbnailTexture
        {
            get => _thumbnail._textureId;
            set => _thumbnail._textureId = value;
        }

        // Параметры расположения картинки (настраиваются снаружи)
        public Vector2 ImageOffset { get; set; } = new Vector2(-570f, 0f);
        public Vector2 ImageScale { get; set; } = new Vector2(0.16f, 0.75f);

        public readonly SpriteObject Fade;
        public readonly List<SpriteObject> Stars;

        public ListElementButton(
            float x, float y, float width, float height,
            string textureName, string text, MapData mapdata)
            : base(x, y, width, height, textureName, text)
        {
            Tag = "List";

            // Затемнение (исчезает при выборе)
            Fade = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, width, height - 10)
            {
                Color = new Color4(0.212f, 0f, 0.106f, 1f)
            };

            // Картинка — только у этой кнопки
            _thumbnail = new SpriteObject(0, 0, 0, 1f, 1f)
            {
                Pivot = new Vector2(0.5f, 0.5f),
                Active = false,
                AllowHover = false
            };

            _buttonBackground.Opacity = 0.5f;

            // Звёзды сложности
            Stars = BuildStars(mapdata.StarRating, width);

            AddChild(_thumbnail);
            AddChild(Fade);

            foreach (var star in Stars)
                AddChild(star);
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);

            UpdateThumbnail();
            UpdateStars();

            Fade.Active = !IsSelected;
            Fade.Opacity = IsSelected ? 0f : 0.6f;
        }

        public override void Draw(Matrix4 projection) => base.Draw(projection);

        public void SetIndex(int index) => Index = index;

        public void SetSelected(bool selected)
        {
            if (IsSelected == selected) return;
            IsSelected = selected;
        }

        public override void SetHover(bool hover)
        {
            if (IsHovered == hover) return;

            if (Parent is ScrollList scrollList)
                scrollList.NotifyHoverChanged(this, hover);

            base.SetHover(hover);
        }

        // ── Приватные хелперы ────────────────────────────────────────────────────

        private void UpdateThumbnail()
        {
            if (_thumbnail._textureId == 0)
            {
                _thumbnail.Active = false;
                return;
            }

            _thumbnail.Active = true;

            float s = ScaleMultiply;
            _thumbnail.WorldPosition = new Vector2(
                WorldPosition.X + ImageOffset.X * s,
                WorldPosition.Y + ImageOffset.Y * s
            );
            _thumbnail.Scale = new Vector2(Scale.X * ImageScale.X, Scale.Y * ImageScale.Y);
            _thumbnail.ScaleMultiply = 1f;
        }

        private void UpdateStars()
        {
          
            for (int i = 0; i < Stars.Count; i++)
            {
                bool isLast = i == Stars.Count - 1 && Stars.Count > 1;

                if (!isLast)
                {
                    Stars[i].WorldPosition = new Vector2(
                        WorldPosition.X - 280 + i * 20,
                        WorldPosition.Y + 43
                    );
                }
                else
                {
                    // Частичная звезда — встаёт вплотную к предыдущей
                    Stars[i].WorldPosition = new Vector2(
                        Stars[i - 1].WorldPosition.X + 20,
                        Stars[i - 1].WorldPosition.Y + Stars[i].ScaleMultiply
                    );
                }
                
            }
        }

        private static List<SpriteObject> BuildStars(float starRating, float buttonWidth)
        {
            var stars = new List<SpriteObject>();
            int full = (int)Math.Floor(starRating);
            float frac = starRating - full;

            for (int i = 0; i < full; i++)
                stars.Add(new SpriteObject(TextureManager.GetTexture("starRait"), 0, 0, 30, 30));

            if (frac > 0.01f)
                stars.Add(new SpriteObject(TextureManager.GetTexture("starRait"), 0, 0, 30, 30)
                {
                    ScaleMultiply = frac,
                });

            return stars;
        }
    }
}