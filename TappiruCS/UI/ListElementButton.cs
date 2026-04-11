using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core;
using TappiruCS.GameLogic;
using TappiruCS.Render;

namespace TappiruCS.UI
{
    public class ListElementButton : Button
    {
        public int Index { get; private set; } = -1;
        public bool IsSelected { get; private set; } = false;

        public readonly SpriteObject fade;
        public readonly List<SpriteObject> StarRating;

        public ListElementButton(SpriteBatch spriteBatch, TextRender textRenderer,
                          float x, float y, float width, float height,
                          string textureName, string text, Color4 color,JsonMap mapdata)
            : base(spriteBatch, textRenderer, x, y, width, height, textureName, text, color)
        {
            Tag = "List";
            fade = new SpriteObject(spriteBatch, TextureManager.GetTexture("slider_line"), x, y, width, height - 10)
            {
                Color = new Color4(0.212f, 0, 0.106f, Opacity)
            };
            StarRating = new List<SpriteObject>();
            int fullStars = (int)Math.Floor(mapdata.StarRating);
            for (int i = 0; i < fullStars; i++)
            {
                StarRating.Add(new SpriteObject(spriteBatch, TextureManager.GetTexture("starRait"), x+i*15, y, 30, 30));
                AddChild(StarRating[i]);
            }
            float fraction = mapdata.StarRating - (int)Math.Floor(mapdata.StarRating);
            StarRating.Add(new SpriteObject(spriteBatch, TextureManager.GetTexture("starRait"), x, y, 30, 30) { ScaleMultiply = fraction});


            AddChild(StarRating[StarRating.Count-1]);
            AddChild(fade);
            
        }
        public override void Update(double deltaTime,MouseState mouse)
        {
            base.Update(deltaTime, mouse);
            fade.Position = new Vector2(Position.X, Position.Y);

            for(int i = 0; i < StarRating.Count-1; i++)
            {
                StarRating[i].Position = new Vector2(Position.X - 280 + i * 20, Position.Y + 43);   
            }
            StarRating[StarRating.Count - 1].Position = new Vector2(StarRating[StarRating.Count - 2].Position.X + 20, StarRating[StarRating.Count - 2].Position.Y+1* StarRating[StarRating.Count - 1].ScaleMultiply);

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
            if (IsHovered == hover)
                return;

            bool wasHovered = IsHovered;

            // Специфика списка
            if (Parent is ScrollList scrollList)
            {
                scrollList.NotifyHoverChanged(this, hover);
                Console.WriteLine($"{Index} - index button");
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