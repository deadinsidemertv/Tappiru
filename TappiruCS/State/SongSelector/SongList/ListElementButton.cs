using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using TappiruCS.UI;

namespace TappiruCS.State.SongSelector.SongList
{
    public class ListElementButton : Button
    {
        public int Index { get; private set; } = -1;
        public bool IsSelected { get; private set; } = false;

        public readonly SpriteObject fade;
        public readonly List<SpriteObject> StarRating;

        // ИЗМЕНЕНИЕ: Теперь принимаем MapData вместо JsonMap
        public ListElementButton(
            float x, float y, float width, float height,
            string textureName, string text, MapData mapdata)
            : base(x, y, width, height, textureName, text)
        {
            Tag = "List";

            fade = new SpriteObject(TextureManager.GetTexture("slider_line"), 0, 0, width, height - 10)
            {
                Color = new Color4(0.212f, 0, 0.106f, Opacity)
            };

            StarRating = new List<SpriteObject>();

            // Теперь используем StarRating из MapData (он уже рассчитан)
            int fullStars = (int)Math.Floor(mapdata.StarRating);

            for (int i = 0; i < fullStars; i++)
            {
                var star = new SpriteObject(TextureManager.GetTexture("starRait"), 0 + i * 15, 0, 30, 30);
                StarRating.Add(star);
                AddChild(star);
            }

            // Частичная звезда (если есть дробная часть)
            float fraction = mapdata.StarRating - (int)Math.Floor(mapdata.StarRating);
            if (fraction > 0.01f) // чтобы не рисовать пустую звезду при целом числе
            {
                var partialStar = new SpriteObject(TextureManager.GetTexture("starRait"), 0, 0, 30, 30)
                {
                    ScaleMultiply = fraction
                };
                StarRating.Add(partialStar);
                AddChild(partialStar);
            }

            AddChild(fade);
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);

            //fade.LocalPosition = new Vector2(LocalPosition.X, LocalPosition.Y);

            // Позиционируем звёзды
            for (int i = 0; i < StarRating.Count; i++)
            {
                if (i < StarRating.Count - 1 || StarRating.Count == 1)
                {
                    StarRating[i].WorldPosition = new Vector2(WorldPosition.X - 280 + i * 20, WorldPosition.Y + 43);
                }
                else
                {
                    // Последняя (частичная) звезда
                    StarRating[i].WorldPosition = new Vector2(
                        StarRating[i - 1].WorldPosition.X + 20,
                        StarRating[i - 1].WorldPosition.Y + 1 * StarRating[i].ScaleMultiply);
                }
            }

            if (IsSelected)
            {
                fade.Active = false;
                fade.Opacity = 0f;
            }
            else
            {
                fade.Active = true;
                fade.Opacity = 0.6f;
            }
        }

        public override void Draw(Matrix4 projection)
        {
            base.Draw(projection);
        }

        public void SetIndex(int index)
        {
            Index = index;
        }

        public override void SetHover(bool hover)
        {
            if (IsHovered == hover) return;

            if (Parent is ScrollList scrollList)
            {
                scrollList.NotifyHoverChanged(this, hover);
            }

            base.SetHover(hover);
        }

        public void SetSelected(bool selected)
        {
            if (IsSelected == selected) return;
            IsSelected = selected;
        }


    }
}