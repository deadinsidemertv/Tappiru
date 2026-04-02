using OpenTK;
using OpenTK.Windowing.Desktop;
using System.Text;
using TappiruCS.Render;
using OpenTK.Graphics.OpenGL4;


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


            TextureLoader.fontTexture = TextureManager.GetTexture("mainFont");
            



            gamewindow.Run();





        }
    }
}
