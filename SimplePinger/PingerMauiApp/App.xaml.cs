using Missionware.Cognibase.Client;
using Missionware.Cognibase.Config;
using Missionware.Cognibase.Library;
using Missionware.Cognibase.Security.Identity.Domain.System;
using Missionware.Cognibase.UI.Common;
using Missionware.Cognibase.UI.Common.ViewModels;
using Missionware.Cognibase.UI.Maui;
using Missionware.Cognibase.UI.Maui.Dialogs;
using Missionware.ConfigLib;
using Missionware.SharedLib;
using Missionware.SharedLib.Cryptography;
using Missionware.SharedLib.Licensing;

using PingerDomain.System;

using PingerUiCommon.ViewModels;

using Device = PingerDomain.Entities.Device;

namespace PingerMauiApp
{
    public partial class App : Application
    {
        private static volatile bool _isLoadingPerformed;
        private readonly bool _isAborted;

        private SimpleAuthenticationPage _authenticateWindow;
        private SimpleAuthenticationManager _authManager;
        private SimpleAuthDialogVm _authVm;
        private MauiDialog _dialog;
        private LoaderPage _loader;
        private MainViewModel _vm;

        public App()
        {
           InitializeComponent();

            MainPage = new AppShell();
        }

        public static MauiCognibaseApplication MauiCognibaseApplication { get; set; }

        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);
            ApplicationManager.MainAppWindow = window;

            window.Created += (s, e) =>
            {
                // set sync context
                MauiCognibaseApplication.RegisterMainSynchronizationContext();

                ApplicationManager.MainAppWindow = this;

                _dialog = new MauiDialog();
                _vm = new MainViewModel(MauiCognibaseApplication.Client, _dialog);
                _authManager = new SimpleAuthenticationManager(MauiCognibaseApplication.Client);
                _authenticateWindow = new SimpleAuthenticationPage();
                _authVm = new SimpleAuthDialogVm { DomainFullName = "Basic", Username = "user1", Password = "user1" };
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
            };

            return window;
        }


        private void _authManager_StateChanged(object sender, LoginStateEventArgs e)
        {
            if (e.IsConnected)
                if (!_isLoadingPerformed)
                {
                    _isLoadingPerformed = true;
                    _ = MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        _ = await MainPage.Navigation.PopModalAsync().ConfigureAwait(true);

                        _loader = new LoaderPage();
                        await MainPage.Navigation.PushModalAsync(_loader).ConfigureAwait(true);

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
                    _ = await MainPage.Navigation.PopModalAsync().ConfigureAwait(true);
                }).ConfigureAwait(false);

                break;
            }
        }
    }
}
