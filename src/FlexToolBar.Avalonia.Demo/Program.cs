using Avalonia;
using System;

namespace FlexToolBar.Avalonia.Demo;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
        public static void Main(string[] args)
        {
            // HARDWARE SHIELD: Disables the unstable Linux DBus IME system natively before engine initialization
            Environment.SetEnvironmentVariable("AVALONIA_IM_MODULE", "none");

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
