using System;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

using Avalonia.Controls.Shapes;

using ChatDomain.Entities;

using Missionware.Cognibase.Client;
using Missionware.Cognibase.Library;
using Missionware.SharedLib.ConsoleMgmt;
using Missionware.SharedLib.UI;

using ReactiveUI;

namespace ChatDesktopApp.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        IClient _client;
        private ChatRoom _selectedChatRoom;
        private string _newMessageText;
        private string _newRoomText;
        private User _currentUser;
        private readonly IAsyncDialogService _dialogService;

        public ReactiveCommand<Unit, Unit> SendMessageCommand { get; }   // The send message command bound to send button
        public ReactiveCommand<Unit, Unit> CreateRoomCommand { get; }    // The create room command bound to create/join button
        public ReactiveCommand<ChatRoom, Unit> LeaveRoomCommand { get; }   // The leave room bound to each item

        public ChatRoom SelectedChatRoom 
        { 
            get => 
                _selectedChatRoom;
            set
            {
                _selectedChatRoom = value;
                this.RaisePropertyChanged(nameof(SelectedChatRoom));
            }
        }

        public string NewMessageText
        {
            get => _newMessageText;
            set
            {
                _newMessageText = value;
                this.RaisePropertyChanged(nameof(NewMessageText));
            }
        }


        public string NewRoomText
        {
            get => _newRoomText;
            set
            {
                _newRoomText = value;
                this.RaisePropertyChanged(nameof(NewRoomText));
            }
        }

        public User CurrentUser 
        { 
            get => _currentUser;
            set
            {
                _currentUser = value;
                this.RaisePropertyChanged(nameof(CurrentUser));
            }
        }

        public MainViewModel() { }

        public MainViewModel(IClient client, IAsyncDialogService dialogService)
        {
            _client = client;
            _dialogService = dialogService;

            SendMessageCommand = ReactiveCommand.CreateFromTask(()=>SendMessage());
            CreateRoomCommand = ReactiveCommand.CreateFromTask(() => CreateRoom());

            // set leave room command
            Func<ChatRoom, Task> leaveRoomFunc = item => LeaveRoom(item);
            LeaveRoomCommand = ReactiveCommand.CreateFromTask(leaveRoomFunc);

            // set the first room as selected if exists
            SelectedChatRoom = CurrentUser?.ChatRooms.FirstOrDefault();
        }

        private async Task CreateRoom()
        {
            if(!String.IsNullOrWhiteSpace(NewRoomText))
            {
                // response
                ClientTransactionInfo response = null;

                // search for this room 
                SearchArg args = new SearchArg(nameof(ChatRoom.Name), NewRoomText);
                var curRoom = _client.FindDataItem<ChatRoom>(args);

                // if room no found
                if (curRoom == null)
                {
                    // create and save the room
                    curRoom = _client.CreateDataItem<ChatRoom>();
                    curRoom.Name = NewRoomText;
                    curRoom.Users.Add(CurrentUser); // the reverse reference will be automtically created

                    // save
                    response = await _client.SaveAsync(false, TxnAutoInclusion.References, curRoom);

                }
                else if (!curRoom.Users.Contains(CurrentUser)) // if room not contains user add the user to the room
                {
                    // add
                    curRoom.Users.Add(CurrentUser);

                    // save
                    response = await _client.SaveAsync(false, TxnAutoInclusion.References, curRoom);
                }

                // if success close form
                if (response != null && !response.WasSuccessful)
                {
                    // reset items
                    _client.ResetAllMonitoredItems();
                    
                    // notify user
                    await _dialogService.ShowError("Error", "Could not save data. Try again or cancel edit.");
                }
                else
                {
                    // after save reset the textbox
                    NewRoomText = null;

                    // set current room
                    SelectedChatRoom = curRoom;
                }
            }
        }

        private async Task LeaveRoom(ChatRoom item)
        {
            // check the item is not null
            if (item != null)
            {
                // confirm deletion
                AsyncDialogResult result = await _dialogService.AskConfirmation($"Leave Room {item.Name}?",
                    $"Do you want to Proceed?");
                if (result == AsyncDialogResult.NotConfirmed)
                    return;

                // cache selected
                var curSelected = SelectedChatRoom;

                // remove user
                _currentUser.ChatRooms.Remove(item);

                // save
                ClientTransactionInfo saveResult = await _client.SaveAsync(item, _currentUser);

                // if not success unmark and notify user for the failure
                if (!saveResult.WasSuccessful)
                {
                    // reset items
                    _client.ResetAllMonitoredItems();

                    // notify
                    await _dialogService.ShowError("Error", $"Could not leave room {item.Name}.");
                }

                // check if it was selected and set as selected the first found
                if(curSelected == item)
                    SelectedChatRoom = _currentUser.ChatRooms.FirstOrDefault();
            }
        }

        private async Task SendMessage()
        {
            if (SelectedChatRoom != null && !String.IsNullOrEmpty(NewMessageText))
            {
                // create a message and save it
                var newMsg = _client.CreateDataItem<ChatMessage>();
                newMsg.Text = NewMessageText;
                newMsg.CreatedTime = DateTime.Now;
                SelectedChatRoom.Messages.Add(newMsg);
                CurrentUser.Messages.Add(newMsg); // set the author - reverse reference is automatically added

                // save
                var SaveResult = await _client.SaveAsync(false, TxnAutoInclusion.References, newMsg, CurrentUser, SelectedChatRoom);
                
                // if success close form
                if (!SaveResult.WasSuccessful)
                {
                    // reset items
                    _client.ResetAllMonitoredItems();

                    // notify user
                    await _dialogService.ShowError("Error", "Could not save data. Try again or cancel edit.");
                }
                else
                {
                    // after save reset the textbox
                    NewMessageText = null;
                }
            }
        }
    }
}
