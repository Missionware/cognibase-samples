using System;
using System.Reactive;
using System.Threading.Tasks;
using Missionware.Cognibase.Client;
using Missionware.Cognibase.Library;
using Missionware.Cognibase.UI.ReactiveUI.ViewModels;
using Missionware.SharedLib.UI;

using MsBox.Avalonia.ViewModels.Commands;

using ReactiveUI;

using TodoDomain.Entities;

namespace TodoApp.ViewModels
{
    public class TodoItemEditVm : ReactiveObject
    {
        private readonly IClient _client;
        private readonly IAsyncDialogService _dialogService;

        public ToDoItem Item { get; set; }
        public bool IsEdit { get; set; }
        public bool IsNew { get; set; }
        public ClientTxnInfo SaveResult { get; set; }
        public Action CancelAction { get; set; }
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        public TodoItemEditVm(IClient client, ToDoItem item, IAsyncDialogService dialogService)
        {
            _dialogService = dialogService;
            SaveCommand = ReactiveCommand.CreateFromTask( o => Save());
            CancelCommand = ReactiveCommand.CreateFromTask(o => Cancel()); 
            _client = client;
            Item = item;
            if (Item == null)
            {
                Item = _client.CreateDataItem<ToDoItem>();
                IsNew = true;
            }

            IsEdit = !IsNew;
        }


        private async Task Cancel()
        {
            // check changed
            if (Item != null && Item.IsChanged)
            {
                // if cancel reset and close
                _client.ResetAllMonitoredItems();
                Item = null;
                CancelAction();
            }
            else
            {
                // just close
                CancelAction();
            }
        }

        private async Task Save()
        {
            SaveResult = await _client.SaveAsync(Item);

            // if success close form
            if (SaveResult.WasSuccessfull)
                CancelAction();
            else // else show message
                await _dialogService.ShowError("Error", "Could not save data. Try again or cancel edit.");
        }
    }
}