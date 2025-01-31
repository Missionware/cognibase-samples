﻿using System;
using System.Collections.ObjectModel;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

using Missionware.Cognibase.Client;
using Missionware.Cognibase.Config;
using Missionware.Cognibase.Security.Identity.Domain.System;
using Missionware.Cognibase.UI.Avalonia;
using Missionware.Cognibase.UI.Common.ViewModels;
using Missionware.ConfigLib;
using Missionware.SharedLib;
using Missionware.SharedLib.Avalonia;

using TodoDomain.Entities;
using TodoDomain.System;

namespace TodoAppMinimal
{
    public partial class MainWindow : Window
    {
        public static AvaloniaApplication App { get; set; }

        private AvaloniaStartupHelper _startupHelper;

       

        public MainWindow()
        {
            InitializeComponent();
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
            _ = client.RegisterDomainFactory<TodoFactory>();

            // Initialize Security PROFILE
            _ = App.InitializeApplicationSecurity(client, ref clientSettings);

            // set the main app window
            ApplicationManager.MainAppWindow = this;

            // Fix for Avalonia
            ApplicationManager.RegisterProcessInteractionModeProvider(() => { return ProcessInteractionMode.Window; });

            // set sync context
            App.RegisterMainSynchronizationContext();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            App.Client.CloseConnection();
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
                DataItemCollection<ToDoItem> items = App.Client.ReadDataItemCollection<ToDoItem>();

                // set data source in main Avalonia thread
                Dispatcher.UIThread.Invoke(() =>
                {
                    homeView.SetupView(App.Client, items);
                });
            };

            // show the authentication dialog
            await _startupHelper.ShowAuthDialog().ConfigureAwait(false);
        }

    }
}
