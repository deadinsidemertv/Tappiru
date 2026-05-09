using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;

namespace TappiruCS.UI
{
    public class ClippingMask : GameObject
    {
        public readonly SpriteObject MaskSprite;
        private SpriteObject _debugOverlay;

        public ClippingMask(float x, float y, float width, float height)
        {
            LocalPosition = new Vector2(x, y);
            Scale = new Vector2(width, height);
            AllowHover = false;

            MaskSprite = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, width, height)
            {
                Color = new Color4(1f, 1f, 1f, 1f),
                Opacity = 1f,
                AllowHover = false,
            };
            AddChild(MaskSprite);
        }

        /// <summary>
        /// Включить отображение маски для отладки.
        /// Вызывай после создания объекта, не в конструкторе.
        /// </summary>
        public void EnableDebug(float opacity = 0.3f, Color4? color = null)
        {
            if (_debugOverlay != null) return;

            _debugOverlay = new SpriteObject(TextureManager.GetTexture("white"), 0, 0,
                                              MaskSprite.Scale.X, MaskSprite.Scale.Y)
            {
                Color = color ?? new Color4(0f, 1f, 0.4f, 1f), // зелёный по умолчанию
                Opacity = opacity,
                AllowHover = false,
            };
            AddChild(_debugOverlay);
        }

        public void DisableDebug()
        {
            if (_debugOverlay == null) return;
            RemoveChild(_debugOverlay);
            _debugOverlay = null;
        }

        internal void BeginClip(Matrix4 projection)
        {
            if (Context == null) return;

            GL.ColorMask(false, false, false, false);
            GL.DepthMask(false);
            GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
            GL.StencilMask(0xFF);

            MaskSprite.Draw(projection);

            GL.ColorMask(true, true, true, true);
            GL.DepthMask(true);
            GL.StencilFunc(StencilFunction.Equal, 1, 0xFF);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
            GL.StencilMask(0x00);
        }

        internal void EndClip(Matrix4 projection)
        {
            if (Context == null) return;

            GL.ColorMask(false, false, false, false);
            GL.DepthMask(false);
            GL.StencilFunc(StencilFunction.Always, 0, 0xFF);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
            GL.StencilMask(0xFF);

            MaskSprite.Draw(projection);

            GL.ColorMask(true, true, true, true);
            GL.DepthMask(true);
            GL.StencilFunc(StencilFunction.Always, 0, 0xFF);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
            GL.StencilMask(0x00);
        }

        public override void Draw(Matrix4 projection)
        {
            if (!Active || Context == null) return;

            // MaskSprite пропускаем — он рисуется только внутри BeginClip/EndClip
            // иначе он рисовался бы как обычный белый непрозрачный спрайт
            foreach (var child in _children)
            {
                if (child.Active && child != MaskSprite)
                    child.Draw(projection);
            }
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);
        }
    }
}