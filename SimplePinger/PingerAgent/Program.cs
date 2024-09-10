using Missionware.Cognibase.Client;
using Missionware.Cognibase.Config;
using Missionware.Cognibase.Security.Identity.Domain.System;
using Missionware.Cognibase.UI.Common;
using Missionware.ConfigLib;
using Missionware.SharedLib;

using PingerDomain.System;

namespace PingerAgent
{
    internal class Program
    {
        private static SimpleAuthenticationManager _authManager;
        private static readonly PingController controller = new();

        private static void Main(string[] args)
        {
            //
            // SETTINGS SETUP
            //

            // Get SETTINGS Manager
            SettingsManager settingsManager = ConfigBuilder.Create().FromAppConfigFile();

            // Get proper SECTION
            ClientSetupSettings clientSetupSettings = settingsManager.GetSection<ClientSetupSettings>();

            // set to custom
            clientSetupSettings.ProcessSecuritySetting.UseCustomWorkflowToConnectSetting = true;

            //
            // APPLICATION SETUP
            //

            // Initialize the correct (Client) COGNIBASE Application through the Application Manager 
            ClientApplication? cApp = ApplicationManager.InitializeAsMainApplication(new ClientApplication());

            // Initializes a Client Object Manager with the settings from configuration
            var client = ClientObjMgr.Initialize(cApp, ref clientSetupSettings);

            // Registers domains through Domain Factory classes that reside in Domain assembly
            client.RegisterDomainFactory<PingerFactory>();
            client.RegisterDomainFactory<IdentityFactory>();

            // log
            Console.WriteLine("Starting application...");

            //
            // SECURITY SETUP
            //

            // Initialize Security PROFILE
            cApp.InitializeApplicationSecurity(client, ref clientSetupSettings);

            //
            // RUN
            //

            // attach to startup to start the app
            cApp.ApplicationStartUp += App_ApplicationStartUp;

            // Link APPLICATION to STATIC Instance
            PingController.App = cApp;
            _authManager = new SimpleAuthenticationManager(cApp.Client);

            // Start
            cApp.StartUpClient(StartupConnectionMode.NoConnection);
        }

        private static void App_ApplicationStartUp(object? sender, EventArgs e)
        {
            // call controller start
            controller.Start();

            _authManager.LoginAsync("Basic", "user1", "user1");
        }
    }
}