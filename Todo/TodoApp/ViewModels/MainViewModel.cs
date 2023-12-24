using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;
using System.Threading.Tasks;
using Missionware.Cognibase.Client;
using Missionware.Cognibase.Library;
using Missionware.Cognibase.Security.Identity.Domain;
using Missionware.Cognibase.UI.ReactiveUI.ViewModels;
using Missionware.SharedLib.UI;

using MsBox.Avalonia.ViewModels.Commands;

using ReactiveUI;
using TodoApp.Views;

using TodoDomain.Entities;

namespace TodoApp.ViewModels
{
    public class MainViewModel : ReactiveObject
    {

        IClient _client;
        private readonly IAsyncDialogService _dialogService;
        public ToDoItem SelectedItem { get; set; }

        public DataItemCollection<ToDoItem> ListItems { get; set; }
        //public ReactiveCommand<Unit, Unit> RemoveDeviceCommand { get; }
        public ReactiveCommand<ToDoItem, Unit> WriteItemCheckCommand { get; }
        public MainViewModel() { }

        public MainViewModel(IClient client, IAsyncDialogService dialogService)
        {
            _client = client;
            _dialogService = dialogService;

            Func<ToDoItem, Task> writeItemCheckFunc = item => WriteItemCheck(item);
            WriteItemCheckCommand = ReactiveCommand.CreateFromTask(writeItemCheckFunc);
           
            //RemoveDeviceCommand = ReactiveCommand.CreateFromTask(o => RemoveDevice());
        }

        private async Task WriteItemCheck(ToDoItem item)
        {
            if(item != null)
            {
                var SaveResult = await _client.SaveAsync(item);

                // if success close form
                if (!SaveResult.WasSuccessfull)
                {
                    _client.ResetAllMonitoredItems();
                    await _dialogService.ShowError("Error", "Could not save data. Try again or cancel edit.");
                }
            }
        }
    }
}