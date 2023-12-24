using System;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

using Missionware.Cognibase.Client;
using Missionware.Cognibase.Config;
using Missionware.Cognibase.Security.Identity.Domain.System;
using Missionware.Cognibase.UI.Avalonia;
using Missionware.Cognibase.UI.Avalonia.Dialogs;
using Missionware.Cognibase.UI.Common;
using Missionware.Cognibase.UI.Common.ViewModels;
using Missionware.ConfigLib;
using PingerDomain.Entities;
using Missionware.SharedLib;

using PingerUiCommon.ViewModels;
using PingerDomain.System;
using Missionware.SharedLib.Avalonia;

namespace PingerAvaloniaApp
{
    public partial class MainWindow : Window
    {
        private AvaloniaStartupHelper _startupHelper;
        private readonly AvaloniaDialog _dialog = new();
        private readonly MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();
            Type dgType = typeof(DataGrid);
            menuAdd.Click += MenuAdd_Click;
            menuEdit.Click += MenuEdit_Click;
            _vm = new MainViewModel(App.Client, _dialog);
            DataContext = _vm;
        }

        public static AvaloniaApplication App { get; set; }


        protected override void OnInitialized()
        {
            // Call base
            base.OnInitialized();

            //
            // SETTINGS SETUP
            //

            // Read client settings
            SettingsManager settings = ConfigBuilder.Create().FromAppConfigFile();

            // Get proper SECTION
            ClientSetupSettings clientSettings = settings.GetSection<ClientSetupSettings>();

            // Set to CUSTOM Connect Workflow
            clientSettings.ProcessSecuritySetting.UseCustomWorkflowToConnectSetting = true;

            //
            // APPLICATION SETUP
            //

            // Initialize the correct (Avalonia) COGNIBASE Application through the Application Manager 
            App = ApplicationManager.InitializeAsMainApplication(
                new AvaloniaApplication(new AvaloniaApplicationFeatures()));

            // Initializes a Client Object Manager with the settings from configuration
            var client = ClientObjMgr.Initialize(App, ref clientSettings);

            // Registers domains through Domain Factory classes that reside in Domain assembly
            _ = client.RegisterDomainFactory<PingerFactory>();
            _ = client.RegisterDomainFactory<IdentityFactory>();

            //
            // SECURITY SETUP
            //

            // Initialize Security PROFILE
            _ = App.InitializeApplicationSecurity(client, ref clientSettings);

            //
            // RUN
            //

            ApplicationManager.MainAppWindow = this;

            ApplicationManager.IsUserInterActive = true;
            ApplicationManager.IsDialogInterActive = true;
            ApplicationManager.RequiresDelegatedAuthentication = false;

            // Register
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

            _startupHelper = new AvaloniaStartupHelper(this, App.Client);
            _startupHelper.AuthVm  = new SimpleAuthDialogVm { DomainFullName = "TestDev", Username = "user1", Password = "user1" };
            _startupHelper.QuitAction = () => Close();
            _startupHelper.DataLoadAction = () =>
            {
                // read devices
                DataItemCollection<Device> collection = App.Client.ReadDataItemCollection<Device>();

                // set data source in main thread
                Dispatcher.UIThread.Invoke(() =>
                {
                    _vm.Devices = collection;
                });
            };

            await _startupHelper.ShowAuthDialog().ConfigureAwait(false);
        }

        private async void MenuEdit_Click(object? sender, RoutedEventArgs e)
        {
            // call edit form
            if (_vm.SelectedDevice != null)
            {
                var devVm = new DeviceEditVm(App.Client, _vm.SelectedDevice, _dialog);
                var devView = new DeviceEditWindow();
                devVm.CloseAction = devView.Close;
                devView.SetupView(devVm);
                await devView.ShowDialog(this).ConfigureAwait(false);
            }
            else
            {
                await _dialog.ShowMessage("Info", "Nothing is selected").ConfigureAwait(false);
            }
        }

        private async void MenuAdd_Click(object? sender, RoutedEventArgs e)
        {
            var devVm = new DeviceEditVm(App.Client, null, _dialog);
            var devView = new DeviceEditWindow();
            devVm.CloseAction = devView.Close;
            devView.SetupView(devVm);
            await devView.ShowDialog(this).ConfigureAwait(false);
        }

        
        
    }
}