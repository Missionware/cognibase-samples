using Missionware.Cognibase.Client;
using Missionware.Cognibase.Security.Identity.Domain.System;
using Missionware.Cognibase.UI.WinForms.Client;
using Missionware.SharedLib;
using Missionware.SharedLib.Drawing.Extensions;

using PingerDomain.System;

using PingerWinFormsApp.Properties;

namespace PingerWinFormsApp
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // WIN FORMS standard calls
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            //
            // WIN FORMS APPLICATION BUILDUP
            //
            MainForm.App = WinFormsApplicationBuilder.CreateAsMain()
                .WithMainWindowType<MainForm>()
                .WithMainClient(o =>
                    ClientBuilder.CreateFor(o)
                        .WithSettingsFromConfig()
                        .WithDomainFactory<PingerFactory>()
                        .WithDomainFactory<IdentityFactory>()
                        .Build())
                .Build();

            // Start a splash screen
            MainForm.App.StartSplash(new SplashControlData
            {
                ImageBytes = Resources.hello_world_new_black.ToImageBytes(),
                MessageColorCode = new AppColor
                {
                    A = 255, R = 255, G = 255, B = 255
                },
                TitleMessage = "Initializing PINGER..."
            });

            // Start
            MainForm.App.StartUpClient(StartupConnectionMode.StartAndConnect);
        }
    }
}