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

using TodoApp.ViewModels;

using TodoDomain.Entities;
using TodoDomain.System;

namespace TodoApp.Views
{
    public partial class MainWindow : Window
    {
        public static AvaloniaApplication App { get; set; }

        private AvaloniaStartupHelper _startupHelper;
        private readonly AvaloniaDialog _dialog = new();
        private readonly MainViewModel _vm;

        public ReactiveCommand<ToDoItem, Unit> AddEditItemCommand { get; }
        public ReactiveCommand<ToDoItem, Unit> DeleteItemCommand { get; }

        public MainWindow()
        {
            Func<ToDoItem, Task> addEditItemFunc = item => AddEditItem(item);
            AddEditItemCommand = ReactiveCommand.CreateFromTask(addEditItemFunc);

            Func<ToDoItem, Task> deleteItemFunc = item => DeleteItem(item);
            DeleteItemCommand = ReactiveCommand.CreateFromTask(deleteItemFunc);

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
            _ = client.RegisterDomainFactory<TodoFactory>();
            _ = client.RegisterDomainFactory<IdentityFactory>();

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

            _startupHelper = new AvaloniaStartupHelper(this, App.Client);
            _startupHelper.AuthVm = new SimpleAuthDialogVm { DomainFullName = "Basic", Username = "user1", Password = "user1" };
            _startupHelper.AuthVm.DomainSelectionVisible = false;
            _startupHelper.AuthVm.SaveCredentialsSelectionVisible = false;
            _startupHelper.QuitAction = () => Close();
            _startupHelper.DataLoadAction = () =>
            {
                // read devices
                DataItemCollection<ToDoItem> collection = App.Client.ReadDataItemCollection<ToDoItem>();

                // set data source in main thread
                Dispatcher.UIThread.Invoke(() =>
                {
                    _vm.ListItems = App.Client.ReadDataItemCollection<ToDoItem>();
                    mainView.DataContext = _vm;
                });
            };

            await _startupHelper.ShowAuthDialog().ConfigureAwait(false);
        }

        public async Task AddEditItem(ToDoItem item)
        {
            var itemVm = new TodoItemEditVm(App.Client, item, _dialog);
            var itemView = new TodoEditView();
            itemVm.CancelAction = () => Content = mainView;
            itemView.DataContext = itemVm;
            Content = itemView;
        }

        private async Task DeleteItem(ToDoItem item)
        {
            if (item != null)
            {
                // confirm deletion
                AsyncDialogResult result = await _dialog.AskConfirmation("Delete Item?",
                    $"Do you want to Proceed?");
                if (result == AsyncDialogResult.NotConfirmed)
                    return;

                // mark for deletion
                item.MarkForDeletion();

                // save
                ClientTxnInfo saveResult = await App.Client.SaveAsync();

                // if not success unmark 
                if (!saveResult.WasSuccessfull)
                {
                    await _dialog.ShowError("Error", "Could not delete Item.");
                    item.UnMarkForDeletion();
                }
            }
            else
            {
                // log
                await _dialog.ShowMessage("Info", "Nothing is selected");
            }
        }
    }
}