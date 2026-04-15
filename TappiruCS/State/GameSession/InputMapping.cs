using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.GameLogic;

public static class InputMapping
{
    public static readonly Dictionary<Keys, char[]> KeyToCharsMap = new()
    {
            { Keys.A,      new char[] { 'a', 'ф' } },
            { Keys.B,      new char[] { 'b', 'и' } },
            { Keys.C,      new char[] { 'c', 'с' } },
            { Keys.D,      new char[] { 'd', 'в' } },
            { Keys.E,      new char[] { 'e', 'у' } },
            { Keys.F,      new char[] { 'f', 'а' } },
            { Keys.G,      new char[] { 'g', 'п' } },
            { Keys.H,      new char[] { 'h', 'р' } },
            { Keys.I,      new char[] { 'i', 'ш' } },
            { Keys.J,      new char[] { 'j', 'о' } },
            { Keys.K,      new char[] { 'k', 'л' } },
            { Keys.L,      new char[] { 'l', 'д' } },
            { Keys.M,      new char[] { 'm', 'ь' } },
            { Keys.N,      new char[] { 'n', 'т' } },
            { Keys.O,      new char[] { 'o', 'щ' } },
            { Keys.P,      new char[] { 'p', 'з' } },
            { Keys.Q,      new char[] { 'q', 'й' } },
            { Keys.R,      new char[] { 'r', 'к' } },
            { Keys.S,      new char[] { 's', 'ы' } },
            { Keys.T,      new char[] { 't', 'е' } },
            { Keys.U,      new char[] { 'u', 'г' } },
            { Keys.V,      new char[] { 'v', 'м' } },
            { Keys.W,      new char[] { 'w', 'ц' } },
            { Keys.X,      new char[] { 'x', 'ч' } },
            { Keys.Y,      new char[] { 'y', 'н' } },
            { Keys.Z,      new char[] { 'z', 'я' } },
            { Keys.LeftBracket,  new char[] { '[', 'х' } },
            { Keys.RightBracket, new char[] { ']', 'ъ' } },
            { Keys.Semicolon,    new char[] { ';', 'ж' } },
            { Keys.Apostrophe,   new char[] { '\'', 'э' } },
            { Keys.Comma,        new char[] { ',', 'б' } },
            { Keys.Period,       new char[] { '.', 'ю' } },
            { Keys.GraveAccent,  new char[] { '`', 'ё' } },
            { Keys.Space,        new char[] { ' ' } },
    };

    public static void Initialize() => GameSession.InitCharToKeyMap(KeyToCharsMap);
}