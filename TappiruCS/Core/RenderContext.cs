using TappiruCS;
using TappiruCS.Render;
using TappiruCS.Render.Text.BMFont;
using TappiruCS.Render.Text.FreeType;

public class RenderContext
{
    public readonly SpriteBatch SpriteBatch;
    public readonly TextRender TextRenderer;
    public readonly Game Game;
    public readonly AudioManager Audio;
    public readonly FreeTypeRender FreeType;

    public RenderContext(Game game, SpriteBatch spriteBatch, TextRender textRenderer, AudioManager audio,FreeTypeRender freetype)
    {
        Game = game ?? throw new ArgumentNullException(nameof(game));
        SpriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
        TextRenderer = textRenderer ?? throw new ArgumentNullException(nameof(textRenderer));
        Audio = audio ?? throw new ArgumentNullException(nameof(audio));
        FreeType = freetype ?? throw new ArgumentNullException(nameof(freetype));
    }
}