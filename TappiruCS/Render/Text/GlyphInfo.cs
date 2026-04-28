using OpenTK.Mathematics;

namespace TappiruCS.Render.Text
{
    public struct GlyphInfo
    {
        public int Page;
        public float TexX, TexY, Width, Height;
        public float XOffset, YOffset, XAdvance;

        // Дополнительные поля для FreeType (пока не используются в BMFont)
        public float BearingX;
        public float BearingY;

        // Для совместимости со старым кодом
        public readonly static GlyphInfo Empty = new GlyphInfo();
    }
}