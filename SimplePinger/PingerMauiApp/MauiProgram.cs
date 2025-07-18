using Microsoft.Extensions.Logging;

using Missionware.Cognibase.Client;
using Missionware.Cognibase.Config;
using Missionware.Cognibase.Security.Identity.Domain.System;
using Missionware.Cognibase.UI.Maui;
using Missionware.ConfigLib;
using Missionware.SharedLib;
using PingerDomain.System;

using SkiaSharp.Views.Maui.Controls.Hosting;

namespace PingerMauiApp
{
    public static class MauiProgram
    {
        private static volatile bool _isClientInitialized;

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            builder.UseSkiaSharp();

            initialize();
            

            return builder.Build();
        }

        private static void initialize()
        {
            if (_isClientInitialized)
                return;

            //
            // SETTINGS SETUP
            //

            // Read client settings
            string configText;
            using Stream fileStream = FileSystem.Current.OpenAppPackageFileAsync("app.config").Result;
            using (var reader = new StreamReader(fileStream))
            {
                configText = reader.ReadToEnd();
                Configuration.SetupMainSettingsFromText("", "", configText);
            }

            SettingsManager settings = ConfigBuilder.Create().FromXmlConfigText(configText);

            // Get proper SECTION
            ClientSetupSettings clientSettings = settings.GetSection<ClientSetupSettings>();

            // Set to CUSTOM Connect Workflow
            clientSettings.ProcessSecuritySetting.UseCustomWorkflowToConnectSetting = true;

            //
            // APPLICATION SETUP
            //

            // Initialize the correct (Avalonia) COGNIBASE Application through the Application Manager 
            App.MauiCognibaseApplication =
                ApplicationManager.InitializeAsMainApplication(
                    new MauiCognibaseApplication(new MauiCognibaseApplicationFeatures()));

            // Initializes a Client Object Manager with the settings from configuration
            var client = ClientObjMgr.Initialize(App.MauiCognibaseApplication, ref clientSettings);

            // Registers domains through Domain Factory classes that reside in Domain assembly
            _ = client.RegisterDomainFactory<PingerFactory>();
            _ = client.RegisterDomainFactory<IdentityFactory>();

            //
            // SECURITY SETUP
            //

            // Initialize Security PROFILE
            _ = App.MauiCognibaseApplication.InitializeApplicationSecurity(client, ref clientSettings);

            //
            // RUN
            //

            ApplicationManager.IsUserInterActive = true;
            ApplicationManager.IsDialogInterActive = true;
            ApplicationManager.RequiresDelegatedAuthentication = false;

            // Register
            ApplicationManager.RegisterProcessInteractionModeProvider(() => ProcessInteractionMode.Window);


            // set
            _isClientInitialized = true;
        }

    }
}
