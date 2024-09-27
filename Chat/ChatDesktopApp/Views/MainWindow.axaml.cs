using System;

using Avalonia.Controls;
using Avalonia.Threading;

using ChatDesktopApp.ViewModels;

using ChatDomain.Entities;

using Missionware.Cognibase.Client;
using Missionware.Cognibase.Config;
using Missionware.Cognibase.Library;
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
        private User _currentUser;                   // the user object that represents the current user

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
            _startupHelper.DataLoadAction = async () =>
            {
                // data initialization flow
                // get username
                var myusername = App.Client.PrimaryServerMgr.ClientConnectionInfo.ClientIdentity.UserName;

                // get or create user
                _currentUser = DataItem.GetOrCreateDataItem<User>(App.Client, new object[] { myusername });
                _currentUser.LastLoginTime = DateTime.Now;

                // try save or else retry
                if (!App.Client.Save(_currentUser).WasSuccessfull)
                {
                    // log
                    await _dialog.ShowError("Error", $"Error logging to chat app. User {myusername} is not registered!");
                    _currentUser.Dispose();
                    _currentUser = null;
                    return;
                }

                // set data source in main Avalonia thread
                Dispatcher.UIThread.Invoke(() =>
                {
                    // set collection in your 
                    _vm.CurrentUser = _currentUser;
                    mainView.DataContext = _vm;
                });
            };

            // show the authentication dialog
            await _startupHelper.ShowAuthDialog().ConfigureAwait(false);
        }
    }
}
