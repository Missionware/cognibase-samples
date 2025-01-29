using ChatDomain.System;
using Missionware.Cognibase;
using Missionware.Cognibase.Client;
using Missionware.SharedLib;
using Missionware.SharedLib.Logging.SerilogAddin;
using Missionware.ConfigLib;
using Missionware.Cognibase.Config;
using Missionware.Cognibase.Security.Identity.Domain.System;

namespace ChatConsoleApp
{
    internal class Program
    {
        private static ChatController _controller;
        static void Main(string[] args)
        {
            // enable serilog
            LogManager.RegisterAgentType<SerilogAgent>();

            //
            // SETTINGS SETUP
            //

            // Read client settings
            var settings = ConfigBuilder.Create().FromAppConfigFile();

            // Get proper SECTION
            var clientSettings = settings.GetSection<ClientSetupSettings>();

            //
            // APPLICATION SETUP
            //

            // Initialize the correct (Client) Cognibase Application through the Application Manager 
            var cApp = ApplicationManager.InitializeAsMainApplication(new ClientApplication());

            // Initializes a Client Object Manager with the settings from configuration
            var client = ClientObjMgr.Initialize(cApp, ref clientSettings);

            // Registers domains through Domain Factory classes that reside in Domain assembly
            client.RegisterDomainFactory<IdentityFactory>();
            client.RegisterDomainFactory<DomainFactory>();

            // log
            Console.WriteLine("Starting application...");

            //
            // SECURITY SETUP
            //

            // Initialize Security PROFILE
            cApp.InitializeApplicationSecurity(client, ref clientSettings);

            // set the chat controller
            _controller = new ChatController(cApp);

            //
            // RUN
            //
            cApp.StartUpClient(StartupConnectionMode.ConnectAndStart);
        }
    }
}