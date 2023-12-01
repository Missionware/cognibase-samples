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

namespace PingerAvaloniaApp
{
    public partial class MainWindow : Window
    {
        private readonly SimpleAuthenticationManager _authManager;
        private readonly AvaloniaDialog _dialog = new();
        private readonly bool _isAborted;
        private readonly MainViewModel _vm;

        private SimpleAuthenticateWindow _authenticateWindow;
        private SimpleAuthDialogVm _authVm;
        private volatile bool _isInitialized;
        private LoaderWindow _loader;

        public MainWindow()
        {
            InitializeComponent();
            Type dgType = typeof(DataGrid);
            menuAdd.Click += MenuAdd_Click;
            menuEdit.Click += MenuEdit_Click;
            _vm = new MainViewModel(App.Client, _dialog);
            DataContext = _vm;
            _authManager = new SimpleAuthenticationManager(App.Client);
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

            ApplicationManager.SetupAvaloniaApp();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            App.Client.Close();
        }

        protected override async void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            _authenticateWindow = new SimpleAuthenticateWindow();
            _authVm = new SimpleAuthDialogVm { DomainFullName = "TestDev", Username = "user1", Password = "user1" };
            _authenticateWindow.DataContext = _authVm;
            _authManager.StateChanged += _authManager_StateChanged;
            _authManager.PropertyChanged +=
                (o, e) => _authVm.CanConnect = _authManager.Command == ConnectionCommand.None;
            _authVm.OkCommand = new StandardCommand(() =>
                _authManager.LoginAsync(_authVm.DomainFullName, _authVm.Username, _authVm.Password));
            _authVm.CancelCommand = new StandardCommand(Close);

            await _authenticateWindow.ShowDialog(this).ConfigureAwait(false);
        }

        private void _authManager_StateChanged(object? sender, LoginStateEventArgs e)
        {
            if (e.IsConnected)
                if (!_isInitialized)
                {
                    _isInitialized = true;

                    _ = Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _authenticateWindow.Close();
                        _loader = new LoaderWindow();
                        _ = _loader.ShowDialog(this);
                    });

                    _ = Task.Factory.StartNew(async () =>
                    {
                        await loadData().ConfigureAwait(false);
                    });
                }
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

        private async Task<bool> isLoaderVisible()
        {
            return await Dispatcher.UIThread.InvokeAsync(() =>
            {
                return Task.FromResult(_loader.IsVisible);
            }).ConfigureAwait(false);
        }

        // load data in background thread
        private async Task loadData()
        {
            // loop
            while (!_isAborted)
            {
                // wait for the loader to be visible
                if (!await isLoaderVisible().ConfigureAwait(false))
                {
                    await Task.Delay(100).ConfigureAwait(false);
                    continue;
                }

                try
                {
                    // read devices
                    DataItemCollection<Device> collection = App.Client.ReadDataItemCollection<Device>();

                    // set data source in main thread
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _vm.Devices = collection;
                    });
                }
                catch (Exception)
                {
                    // error 
                    await Task.Delay(1000).ConfigureAwait(false);
                    continue;
                }

                // close form (in main thread)
                await Dispatcher.UIThread.InvokeAsync(_loader.Close);

                break;
            }
        }
    }
}