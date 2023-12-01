using Missionware.Cognibase.Client;
using Missionware.Cognibase.Config;
using Missionware.Cognibase.Security.Identity.Domain.System;
using Missionware.Cognibase.UI.Common;
using Missionware.Cognibase.UI.Common.ViewModels;
using Missionware.Cognibase.UI.Maui;
using Missionware.Cognibase.UI.Maui.Dialogs;
using Missionware.ConfigLib;
using Missionware.SharedLib;

using PingerDomain.System;

using PingerUiCommon.ViewModels;

using Device = PingerDomain.Entities.Device;

namespace PingerMauiApp
{
    public partial class MauiMobileApp : Application
    {
        private static volatile bool _isClientInitialized;
        private static volatile bool _isLoadingPerformed;
        private readonly bool _isAborted;

        private readonly MainPage _MainPage;

        private SimpleAuthenticationPage _authenticateWindow;
        private SimpleAuthenticationManager _authManager;
        private SimpleAuthDialogVm _authVm;
        private MauiDialog _dialog;
        private LoaderPage _loader;
        private MainViewModel _vm;

        public MauiMobileApp()
        {
            InitializeComponent();

            _MainPage = new MainPage();

            MainPage = _MainPage;
        }

        public static MauiCognibaseApplication MauiCognibaseApplication { get; set; }

        protected override Window CreateWindow(IActivationState activationState)
        {
            Window window = base.CreateWindow(activationState);

            window.Created += (s, e) =>
            {
                initialize();
                _dialog = new MauiDialog();
                _vm = new MainViewModel(MauiCognibaseApplication.Client, _dialog);
                _authManager = new SimpleAuthenticationManager(MauiCognibaseApplication.Client);
                _authenticateWindow = new SimpleAuthenticationPage();
                _authVm = new SimpleAuthDialogVm { DomainFullName = "TestDev", Username = "user1", Password = "user1" };
                _authenticateWindow.BindingContext = _authVm;
                _authManager.StateChanged += _authManager_StateChanged;
                _authManager.PropertyChanged +=
                    (o, e) => _authVm.CanConnect = _authManager.Command == ConnectionCommand.None;
                _authVm.OkCommand = new StandardCommand(() =>
                    _authManager.LoginAsync(_authVm.DomainFullName, _authVm.Username, _authVm.Password));
                _authVm.CancelCommand = new StandardCommand(() =>
                {
                    Current.Quit();
                });

                _ = MainPage.Navigation.PushModalAsync(_authenticateWindow);

                _ = MainThread.InvokeOnMainThreadAsync(async () =>
                    MauiCognibaseApplication.ApplicationFeatures.SynchronizationContext =
                        await Dispatcher.GetSynchronizationContextAsync().ConfigureAwait(false));
            };

            return window;
        }

        private void initialize()
        {
            if (_isClientInitialized)
                return;

            //
            // SETTINGS SETUP
            //

            // Read client settings
            using Stream fileStream = FileSystem.Current.OpenAppPackageFileAsync("app.config").Result;
            string line;
            var lines = new List<string>();

            using (var sr = new StreamReader(fileStream))
            {
                while ((line = sr.ReadLine()) != null)
                    lines.Add(line);
            }

            string configText = string.Join(Environment.NewLine, lines);

            SettingsManager settings = ConfigBuilder.Create().FromXmlConfigText(configText);

            // Get proper SECTION
            ClientSetupSettings clientSettings = settings.GetSection<ClientSetupSettings>();

            // Set to CUSTOM Connect Workflow
            clientSettings.ProcessSecuritySetting.UseCustomWorkflowToConnectSetting = true;

            //
            // APPLICATION SETUP
            //

            // Initialize the correct (Avalonia) COGNIBASE Application through the Application Manager 
            MauiCognibaseApplication =
                ApplicationManager.InitializeAsMainApplication(
                    new MauiCognibaseApplication(new MauiCognibaseApplicationFeatures()));

            // Initializes a Client Object Manager with the settings from configuration
            ClientObjMgr client = ClientBuilder<MauiCognibaseApplication>
                .CreateFor(MauiCognibaseApplication)
                .WithSettings(clientSettings)
                .WithDomainFactory<PingerFactory>()
                .WithDomainFactory<IdentityFactory>()
                .Build();

            ////
            //// SECURITY SETUP
            ////

            //// Initialize Security PROFILE
            //_App.initializeApplicationSecurity(client, ref clientSettings);

            //
            // RUN
            //

            ApplicationManager.MainAppWindow = _MainPage;
            _isClientInitialized = true;
        }

        private void _authManager_StateChanged(object sender, LoginStateEventArgs e)
        {
            if (e.IsConnected)
                if (!_isLoadingPerformed)
                {
                    _isLoadingPerformed = true;
                    _ = MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        _ = await MainPage.Navigation.PopModalAsync().ConfigureAwait(false);

                        _loader = new LoaderPage();
                        await MainPage.Navigation.PushModalAsync(_loader).ConfigureAwait(false);

                        _ = Task.Factory.StartNew(async () =>
                        {
                            await loadData().ConfigureAwait(false);
                        });
                    });
                }
        }

        private async Task<bool> isLoaderVisible()
        {
            return await MainThread.InvokeOnMainThreadAsync(() =>
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
                    DataItemCollection<Device> collection =
                        MauiCognibaseApplication.Client.ReadDataItemCollection<Device>();

                    // set data source in main thread
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        _vm.Devices = collection;
                    }).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // error 
                    await Task.Delay(1000).ConfigureAwait(false);
                    continue;
                }

                // close form (in main thread)
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    MainPage.BindingContext = _vm;
                    _ = await MainPage.Navigation.PopModalAsync().ConfigureAwait(false);
                }).ConfigureAwait(false);

                break;
            }
        }
    }
}