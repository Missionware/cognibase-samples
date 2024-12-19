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
        // Data
        private readonly IClient _client;    // the client object manager
        private string _errorText;
        public ToDoItem Item { get; set; }                          // The item instance whose properties are bound to the view
        public Action CancelAction { get; set; }                    // The UI action that will be performed when cancel is clicked
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }     // The Save item command
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }   // The Cancel edit command
        public string ErrorText
        {
            get => _errorText;
            set
            {
                _errorText = value;
                this.RaisePropertyChanged(nameof(ErrorText));
            }
        }

        public TodoItemEditVm(IClient client, ToDoItem item)
        {
            // set
            _client = client;

            // set the command
            SaveCommand = ReactiveCommand.CreateFromTask( o => Save());
            CancelCommand = ReactiveCommand.CreateFromTask(o => Cancel()); 
            Item = item;
            ErrorText = String.Empty;

            // if item parameter is null then it means that it is a creation action so create a new item
            if (Item == null)
                Item = _client.CreateDataItem<ToDoItem>();
        }


        private async Task Cancel()
        {
            // check changed
            if (Item != null && Item.IsModified)
            {
                // if cancel reset edits
                _client.ResetAllMonitoredItems();

                // just clear
                Item = null;
            }

            // close the view (navigate back to main view)
            CancelAction();

            // clear
            ErrorText = String.Empty;
        }

        private async Task Save()
        {
            // save
            var saveResult = await _client.SaveAsync(Item);

            // if success close form
            if (saveResult.WasSuccessful)
                CancelAction();
            else // else show message
                ErrorText = "Could not save data. Try again or cancel edit.";
        }
    }
}