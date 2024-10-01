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

            // Set to CUSTOM Connect Workflow
            clientSetupSettings.ProcessSecuritySetting.UseCustomWorkflowToConnectSetting = true;

            //
            // APPLICATION SETUP
            //

            // Initialize the correct (Wpf) COGNIBASE Application through the Application Manager 
            PingerApp.MainWindow.App = ApplicationManager.InitializeAsMainApplication(
                new WpfApplication(new WpfApplicationFeatures()));

            // Initializes a Client Object Manager with the settings from configuration
            var client = ClientObjMgr.Initialize(PingerApp.MainWindow.App, ref clientSetupSettings);

            // Registers domains through Domain Factory classes that reside in Domain assembly
            _ = client.RegisterDomainFactory<PingerFactory>();
            _ = client.RegisterDomainFactory<IdentityFactory>();


            // set sync context
            PingerApp.MainWindow.App.RegisterMainWindowFactory(PingerApp.MainWindow.MainWindowFactory);

            //
            // SECURITY SETUP
            //

            // Initialize Security PROFILE
            _ = PingerApp.MainWindow.App.InitializeApplicationSecurity(client, ref clientSetupSettings);

            //
            // RUN
            //

            ApplicationManager.IsUserInterActive = true;
            ApplicationManager.IsDialogInterActive = true;
            ApplicationManager.RequiresDelegatedAuthentication = false;
   
            // Register
            ApplicationManager.RegisterApplicationStartingModeProvider(() => { return ApplicationStartMode.Window; });


            // Start
            PingerApp.MainWindow.App.StartUpClient(StartupConnectionMode.NoConnection);
        }




    }
}