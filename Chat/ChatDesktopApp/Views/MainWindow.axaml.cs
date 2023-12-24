using System;

using Avalonia.Controls;
using Avalonia.Threading;

using ChatDesktopApp.ViewModels;

using Missionware.Cognibase.Client;
using Missionware.Cognibase.Config;
using Missionware.Cognibase.Security.Identity.Domain.System;
using Missionware.Cognibase.UI.Avalonia;
using Missionware.Cognibase.UI.Common.ViewModels;
using Missionware.ConfigLib;
using Missionware.SharedLib;
using Missionware.SharedLib.Avalonia;

namespace ChatDesktopApp.Views
{
    public partial class MainWindow : Window
    {
        public static AvaloniaApplication App { get; set; }

        private AvaloniaStartupHelper _startupHelper;
        private readonly AvaloniaDialog _dialog = new();
        private readonly MainViewModel _vm;


        public MainWindow()
        {
            InitializeComponent();

            _vm = new MainViewModel(App.Client, _dialog);
        }

        protected override void OnInitialized()
        {
            // Call base
            base.OnInitialized();

            // Read client settings
            SettingsManager settings = ConfigBuilder.Create().FromAppConfigFile();

            // Get proper SECTION
            ClientSetupSettings clientSettings = settings.GetSection<ClientSetupSettings>();

            // Set to CUSTOM Connect Workflow
            clientSettings.ProcessSecuritySetting.UseCustomWorkflowToConnectSetting = true;

            // Initialize the correct (Avalonia) COGNIBASE Application through the Application Manager 
            App = ApplicationManager.InitializeAsMainApplication(
                new AvaloniaApplication(new AvaloniaApplicationFeatures()));

            // Initializes a Client Object Manager with the settings from configuration
            var client = ClientObjMgr.Initialize(App, ref clientSettings);

            // Registers domains through Domain Factory classes
            _ = client.RegisterDomainFactory<IdentityFactory>();

            // TODO: add your domain registration here


            // Initialize Security PROFILE
            _ = App.InitializeApplicationSecurity(client, ref clientSettings);

            // set the main app window
            ApplicationManager.MainAppWindow = this;

            // Fix for Avalonia
            ApplicationManager.RegisterApplicationStartingModeProvider(() => { return ApplicationStartMode.Window; });

            // set sync context
            App.RegisterMainSynchronizationContext();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            App.Client.Close();
        }

        protected override async void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            // build the startup helper that contains the auth manager, the auth dialog and the loader
            _startupHelper = new AvaloniaStartupHelper(this, App.Client);
            _startupHelper.AuthVm = new SimpleAuthDialogVm { DomainFullName = "Basic", Username = "user1", Password = "user1" };
            _startupHelper.QuitAction = () => Close(); // set the quit action
            _startupHelper.DataLoadAction = () =>
            {
                // data initialization flow

                // read data using a live collection
                // DataItemCollection<YourDataItem> collection = App.Client.ReadDataItemCollection<YourDataItem>();

                // set data source in main Avalonia thread
                Dispatcher.UIThread.Invoke(() =>
                {
                    // set collection in your 
                    //_vm.ListItems = collection;
                    mainView.DataContext = _vm;
                });
            };

            // show the authentication dialog
            await _startupHelper.ShowAuthDialog().ConfigureAwait(false);
        }
    }
}
