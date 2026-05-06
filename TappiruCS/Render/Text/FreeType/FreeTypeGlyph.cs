using OpenTK.Mathematics;

namespace TappiruCS.Render.Text.FreeType
{
    public class FreeTypeGlyph
    {
        public GlyphInfo Info { get; }
        public int TextureId { get; }
        public Vector2 UVMin { get; }
        public Vector2 UVMax { get; }

        public FreeTypeGlyph(GlyphInfo info, int textureId, Vector2 uvMin, Vector2 uvMax)
        {
            Info = info;
            TextureId = textureId;
            UVMin = uvMin;
            UVMax = uvMax;
        }
    }
}