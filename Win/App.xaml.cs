using System;
using System.Linq;
using System.Windows;

namespace NOR
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new MainWindow();

            // If started with --minimized flag (e.g. from Windows Startup registry), keep window hidden
            if (e.Args.Contains("--minimized"))
            {
                // App starts directly in the system tray
            }
            else
            {
                mainWindow.Show();
            }
        }
    }
}

