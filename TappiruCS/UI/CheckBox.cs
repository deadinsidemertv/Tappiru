using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;

namespace TappiruCS.UI
{
    public class CheckBox : GameObject
    {
        public bool IsSelected=false;
        public SpriteObject sprite;

        public Color4 idleColor = Color4.White;
        public Color4 SelectedColor = new Color4(1.5f, 0, 1.5f, 1f);

        private const float HoverBrightness = 0.5f;

        public event Action<bool> OnSelectedChanged;


        public CheckBox(float x,float y,float scaleX,float scaleY) 
        {
            LocalPosition = new Vector2 (x,y);
            Scale = new Vector2(scaleX,scaleY);
            sprite = new SpriteObject(TextureManager.GetTexture("noFail"), 0, 0, scaleX, scaleY);
            AddChild(sprite);
        }
        public override void Update(double deltaTime,MouseState mouse)
        {
            base.Update(deltaTime, mouse);

            if (IsHovered && mouse.IsButtonPressed(MouseButton.Left))
            {
                IsSelected = !IsSelected;
                OnSelectedChanged?.Invoke(IsSelected);
            }



            sprite.Color = GetCurrentColor();
        }

        private Color4 GetCurrentColor()
        {
            Color4 baseColor = IsSelected ? SelectedColor : idleColor;

            if (IsHovered)
            {
                // Умножаем RGB на HoverBrightness, альфу оставляем как есть (или тоже умножаем — по желанию)
                return new Color4(
                    MathHelper.Clamp(baseColor.R * HoverBrightness, 0, 1),
                    MathHelper.Clamp(baseColor.G * HoverBrightness, 0, 1),
                    MathHelper.Clamp(baseColor.B * HoverBrightness, 0, 1),
                    baseColor.A
                );
            }

            return baseColor;
        }

        public override void SetHover(bool hover)
        {
            base.SetHover(hover);

            if (sprite != null)
                sprite.Color = GetCurrentColor();

        }

        public override void Draw(Matrix4 projection)
        {

            sprite.Color = GetCurrentColor();
            base.Draw(projection);  
        }
    }
}
