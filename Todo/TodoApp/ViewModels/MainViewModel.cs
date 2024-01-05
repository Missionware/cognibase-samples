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
        // Data
        IClient _client;    // the client object manager
        private readonly IAsyncDialogService _dialogService; // the dialog service

        public DataItemCollection<ToDoItem> ListItems { get; set; }     // The live collection of the ToDo items bound to the view
        public ReactiveCommand<ToDoItem, Unit> WriteItemCheckCommand { get; }   // The Check command bound to each item
        public ReactiveCommand<ToDoItem, Unit> DeleteItemCommand { get; }   // The Delete command bound to each item
        public MainViewModel() { }  // This constructor is used only in the 

        public MainViewModel(IClient client, IAsyncDialogService dialogService)
        {
            // set
            _client = client;
            _dialogService = dialogService;

            // set check command
            Func<ToDoItem, Task> writeItemCheckFunc = item => WriteItemCheck(item);
            WriteItemCheckCommand = ReactiveCommand.CreateFromTask(writeItemCheckFunc);

            // set delete command
            Func<ToDoItem, Task> deleteItemFunc = item => DeleteItem(item);
            DeleteItemCommand = ReactiveCommand.CreateFromTask(deleteItemFunc);
        }

        private async Task WriteItemCheck(ToDoItem item)
        {
            // check the item is not null
            if(item != null)
            {
                // save
                var SaveResult = await _client.SaveAsync(item);

                // if fail reset edits and notify user for the failure
                if (!SaveResult.WasSuccessfull)
                {
                    // reset all edits
                    _client.ResetAllMonitoredItems();

                    // notify
                    await _dialogService.ShowError("Error", "Could not save data. Try again or cancel edit.");
                }
            }
        }

        private async Task DeleteItem(ToDoItem item)
        {
            // check the item is not null
            if (item != null)
            {
                // confirm deletion
                AsyncDialogResult result = await _dialogService.AskConfirmation("Delete Item?",
                    $"Do you want to Proceed?");
                if (result == AsyncDialogResult.NotConfirmed)
                    return;

                // mark for deletion
                item.MarkForDeletion();

                // save
                ClientTxnInfo saveResult = await _client.SaveAsync(item);

                // if not success unmark and notify user for the failure
                if (!saveResult.WasSuccessfull)
                {
                    // unmark (reset edit)
                    item.UnMarkForDeletion();

                    // notify
                    await _dialogService.ShowError("Error", "Could not delete Item.");
                }
            }
        }
    }
}