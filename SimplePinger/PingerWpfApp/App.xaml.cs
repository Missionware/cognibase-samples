using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Imaging;

using Missionware.Cognibase.Client;
using Missionware.Cognibase.Config;
using Missionware.Cognibase.Security.Identity.Domain.System;
using Missionware.Cognibase.UI.Common.ViewModels;
using Missionware.Cognibase.UI.Wpf.Client;
using Missionware.Cognibase.UI.Wpf.Dialogs;
using Missionware.Cognibase.UI.Wpf.Extensions;
using Missionware.ConfigLib;
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
            // SETTINGS SETUP
            //

            // Get SETTINGS Manager
            SettingsManager settingsManager = ConfigBuilder.Create().FromAppConfigFile();

            // Get proper SECTION
            ClientSetupSettings clientSetupSettings = settingsManager.GetSection<ClientSetupSettings>();

            //
            // APPLICATION SETUP
            //

            // Initialize the correct (Wpf) COGNIBASE Application through the Application Manager 
            PingerWpfApp.MainWindow.App = ApplicationManager.InitializeAsMainApplication(
                new WpfApplication(new WpfApplicationFeatures()));

            // Initializes a Client Object Manager with the settings from configuration
            var client = ClientObjMgr.Initialize(PingerWpfApp.MainWindow.App, ref clientSetupSettings);

            // Registers domains through Domain Factory classes that reside in Domain assembly
            _ = client.RegisterDomainFactory<PingerFactory>();
            _ = client.RegisterDomainFactory<IdentityFactory>();


            // set sync context
            PingerWpfApp.MainWindow.App.RegisterMainWindowFactory(PingerWpfApp.MainWindow.MainWindowFactory);

            //
            // SECURITY SETUP
            //

            // Initialize Security PROFILE
            _ = PingerWpfApp.MainWindow.App.InitializeApplicationSecurity(client, ref clientSetupSettings);

            //
            // RUN
            //


            // Start
            if (clientSetupSettings.ProcessSecuritySetting.UseCustomWorkflowToConnectSetting)
                PingerWpfApp.MainWindow.App.StartUpClient(StartupConnectionMode.NoConnection);
            else
                PingerWpfApp.MainWindow.App.StartUpClient(StartupConnectionMode.ConnectAndStart);
        }




    }
}