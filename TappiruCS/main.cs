using OpenTK.Windowing.Desktop;
using System.Text;


namespace TappiruCS
{
    class Programm
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = UTF8Encoding.UTF8;
            GameWindowSettings gwSetting = GameWindowSettings.Default;
            NativeWindowSettings nwSetting = NativeWindowSettings.Default;
            Game gamewindow = new Game(gwSetting, nwSetting);

            gamewindow.Run();

        }
    }
}
