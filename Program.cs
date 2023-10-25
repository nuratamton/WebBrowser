using Gtk;
using Browser;

namespace Browser
{
    class Program
    {
        static void Main(string[] args)
        {
            Application.Init();
            var app = new Application("com.example.simplebrowser", GLib.ApplicationFlags.None);
            

            var win = new SimpleBrowser.Browser();
            app.AddWindow(win);
            win.ShowAll();

            Application.Run();
        }
    }
}