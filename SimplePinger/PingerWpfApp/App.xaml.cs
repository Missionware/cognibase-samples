using System;
using System.Windows;
using System.Windows.Media.Imaging;

using Missionware.Cognibase.Client;
using Missionware.Cognibase.Security.Identity.Domain.System;
using Missionware.Cognibase.UI.Wpf.Client;
using Missionware.Cognibase.UI.Wpf.Dialogs;
using Missionware.Cognibase.UI.Wpf.Extensions;
using Missionware.SharedLib;

using PingerDomain.System;

namespace PingerApp
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // On Startup
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //
            // WPF APPLICATION BUILDUP
            //
            PingerApp.MainWindow.App = WpfApplicationBuilder.CreateAsMain(this)
                .WithMainWindowType<MainWindow>()
                .WithAuthenticationWindowType<DefaultAuthenticateWindow>()
                .WithMainClient(o =>
                    ClientBuilder<WpfApplication>.CreateFor(o)
                        .WithSettingsFromConfig()
                        .WithDomainFactory<PingerFactory>()
                        .WithDomainFactory<IdentityFactory>()
                        .Build())
                .Build();

            // Start a splash screen
            PingerApp.MainWindow.App.StartSplash(new SplashControlData
            {
                ImageBytes =
                    new BitmapImage(new Uri("pack://application:,,,/Images/hello_world_new_black.png"))
                        .ToImageBytes(),
                MessageColorCode = new AppColor { A = 255, R = 255, G = 255, B = 255 },
                TitleMessage = "Initializing PINGER..."
            });

            // Start
            PingerApp.MainWindow.App.StartUpClient(StartupConnectionMode.Configuration);
        }
    }
}