using System;
using System.Reactive;
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
using Missionware.SharedLib;
using Missionware.SharedLib.Avalonia;
using Missionware.SharedLib.UI;
using Missionware.SharedLib.UI.Avalonia;

using ReactiveUI;

using TodoApp.Views;
using TodoApp.ViewModels;

using TodoDomain.Entities;
using TodoDomain.System;

namespace TodoApp
{
    public partial class MainWindow : Window
    {
        public static AvaloniaApplication App { get; set; }
        private AvaloniaStartupHelper _startupHelper;
        public ReactiveCommand<ToDoItem, Unit> AddEditItemCommand { get; }

        public MainWindow()
        {
            Func<ToDoItem, Task> addEditItemFunc = item => OpenAddEditItemView(item);
            AddEditItemCommand = ReactiveCommand.CreateFromTask(addEditItemFunc);

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
            _ = client.RegisterDomainFactory<TodoFactory>();
            _ = client.RegisterDomainFactory<IdentityFactory>();

            // Initialize Security PROFILE
            _ = App.InitializeApplicationSecurity(client, ref clientSettings);

            // set the main app window
            ApplicationManager.MainAppWindow = this;

            // Fix for Avalonia
            ApplicationManager.RegisterProcessInteractionModeProvider(() => ProcessInteractionMode.Window);

            // set sync context
            App.RegisterMainSynchronizationContext();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            App.Client?.Dispose();
        }

        protected override async void OnOpened(EventArgs e)
        {
            // call base
            base.OnOpened(e);

            // create startup helper
            _startupHelper = new AvaloniaStartupHelper(this, App.Client);

            // create a new Auth dialog view model that will be used by the standard Avalonia login window and
            // setting the domain and hardcoded username/password (only for simplicity)
            _startupHelper.AuthVm = new SimpleAuthDialogVm { DomainFullName = "Basic", Username = "user1", Password = "user1" };

            // set the quit and data load delegates
            _startupHelper.QuitAction = () => Close();
            _startupHelper.DataLoadAction = () =>
            {
                // read devices
                DataItemCollection<ToDoItem> todoItems = App.Client.ReadDataItemCollection<ToDoItem>();

                // set data source in main thread
                Dispatcher.UIThread.Invoke(() =>
                {
                    homeView.Setup(App.Client, todoItems);
                });
            };

            // show the auth dialog window
            await _startupHelper.ShowAuthDialog().ConfigureAwait(false);
        }

        public async Task OpenAddEditItemView(ToDoItem item)
        {
            // create the edit view View Model
            var itemVm = new TodoItemEditVm(App.Client, item);

            // create the Edit View 
            var itemView = new TodoEditView();

            // set the cancel action to be the navigation back to the main view
            itemVm.CancelAction = () => Content = homeView;

            // set the Data Context to the viewmodel
            itemView.DataContext = itemVm;

            // navigate to the edit view
            Content = itemView;
        }


    }
}