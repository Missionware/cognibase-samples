using System;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

using Avalonia.Controls.Shapes;

using ChatDomain.Entities;

using Missionware.Cognibase.Client;
using Missionware.Cognibase.Library;
using Missionware.SharedLib.UI;

using ReactiveUI;

namespace ChatDesktopApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        IClient _client;
        private ChatRoom selectedChatRoom;
        private string newMessageText;
        private DataItemRefList<ChatMessage> currentChatMessages;
        private DataItemRefList<ChatRoom> chatRooms;
        private User currentUser;
        private readonly IAsyncDialogService _dialogService;

        public ReactiveCommand<Unit, Unit> SendMessageCommand { get; }

        public DataItemRefList<ChatRoom> ChatRooms 
        { 
            get => chatRooms;
            set
            {
                chatRooms = value;
                this.RaisePropertyChanged(nameof(ChatRooms));
            }
        }

        public DataItemRefList<ChatMessage> CurrentChatMessages 
        { 
            get => currentChatMessages;
            set
            {
                currentChatMessages = value;
                this.RaisePropertyChanged(nameof(CurrentChatMessages));
            }
        }

        public ChatRoom SelectedChatRoom 
        { 
            get => 
                selectedChatRoom;
            set
            {
                selectedChatRoom = value;
                this.RaisePropertyChanged(nameof(SelectedChatRoom));
                if(selectedChatRoom != null)
                    CurrentChatMessages = selectedChatRoom.Messages;
            }
        }

        public string NewMessageText
        {
            get => newMessageText;
            set
            {
                newMessageText = value;
                this.RaisePropertyChanged(nameof(NewMessageText));
            }
        }

        public User CurrentUser 
        { 
            get => currentUser;
            set
            {
                currentUser = value;
                this.RaisePropertyChanged(nameof(CurrentUser));

                if (currentUser != null)
                {
                    ChatRooms = currentUser.ChatRooms;
                    if (SelectedChatRoom == null)
                        SelectedChatRoom = ChatRooms.FirstOrDefault();
                }
            }
        }

        public MainViewModel() { }

        public MainViewModel(IClient client, IAsyncDialogService dialogService)
        {
            _client = client;
            _dialogService = dialogService;

            SendMessageCommand = ReactiveCommand.CreateFromTask(()=>SendMessage());
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
                if (!SaveResult.WasSuccessfull)
                {
                    _client.ResetAllMonitoredItems();
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
