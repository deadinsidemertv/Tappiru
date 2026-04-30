using TappiruCS;
using TappiruCS.Render;
using TappiruCS.Render.Text.FreeType;

public class RenderContext
{
    public readonly SpriteBatch SpriteBatch;
    public readonly Game Game;
    public readonly AudioManager Audio;
    public readonly FreeTypeRender FreeType;

    public RenderContext(Game game, SpriteBatch spriteBatch, AudioManager audio)
    {
        Game = game ?? throw new ArgumentNullException(nameof(game));
        SpriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
        Audio = audio ?? throw new ArgumentNullException(nameof(audio));
    }
}