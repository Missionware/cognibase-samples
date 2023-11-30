using ChatDomain.Entities;
using Missionware.Cognibase.Client;
using Missionware.Cognibase.Library;
using Missionware.SharedLib;
using Missionware.SharedLib.ConsoleMgmt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatConsoleApp
{
    public class ChatController
    {
        // data
        private volatile bool _isAborted;       // to check if it is aborted
        private volatile bool _isInitialized;   // to check if the data are initialized
        private User _myUser;                   // the user object that represents the current user
        private ChatRoom _currentRoom;          // the current chat room joined
        private ChatMessage _lastMessageSent;   // the last message sent
        private string _prompt;                 // the console prompt
        ClientApplication App { get; set; }     // the app object

        public ChatController(ClientApplication app)
        {
            // store
            App = app;

            // attach to connection event
            App.Client.ServerConnectionChange += _Client_ServerConnectionChange;
        }

        private void _Client_ServerConnectionChange(object? sender, ServerConnectionChangedEventArgs e)
        {
            // check is connected 
            if (e.IsConnected && e.RegistrationState == Missionware.SharedLib.ConnectionRegistrationState.Registered && !_isInitialized)
            {
                // mark and log
                LogManager.Log(LogLevel.Information, "Connected to server. Starting ...");

                // run init loop
                Task.Run(() => InitLoop());
            }
            else
            {
                // log
                LogManager.Log(LogLevel.Information, "Disconnected from server");
            }
        }

        private async Task InitLoop()
        {
            // first loop until we get a live collection of the devices
            while (!_isAborted)
            {
                try
                {
                    // get username
                    var myusername = App.Client.PrimaryServerMgr.ClientConnectionInfo.ClientIdentity.UserName;

                    // get or create user
                    _myUser = DataItem.GetOrCreateDataItem<User>(App.Client, new object[] { myusername });
                    _myUser.LastLoginTime = DateTime.Now;

                    // if just created
                    if (_myUser.IsNew)
                    {
                        // try save or else retry
                        if (!App.Client.Save(_myUser).WasSuccessfull)
                        {
                            // log
                            ConsoleManager.WriteLine($"{DateTime.Now:T} | Error logging to chat app. User {App.Client.PrimaryServerMgr.ClientConnectionInfo.ClientIdentity.UserName} is not registered!");
                            _myUser.Dispose();
                            _myUser = null;
                            continue;
                        }
                        else
                        {
                            // log created
                            ConsoleManager.WriteLine($"{_myUser.LastLoginTime} | Welcome {App.Client.PrimaryServerMgr.ClientConnectionInfo.ClientIdentity.UserName}");
                        }
                    }
                    else
                    {
                        if (App.Client.Save(_myUser).WasSuccessfull)
                        {
                            // log
                            ConsoleManager.WriteLine($"{_myUser.LastLoginTime:T} | Welcome {App.Client.PrimaryServerMgr.ClientConnectionInfo.ClientIdentity.UserName}");
                        }
                    }

                    break;
                }
                catch (Exception ex)
                {
                    // log error
                    ConsoleManager.WriteLine($"{DateTime.Now:T} | Error logging to chat app for user {App.Client.PrimaryServerMgr.ClientConnectionInfo.ClientIdentity.UserName}");
                    await Task.Delay(1000);
                }
            }

            // set the prompt
            _prompt = $"<{_myUser.Username}> ";

            // run
            await Task.Run(() => MainLoop());
        }

        private async Task MainLoop()
        {
            // set console in read mode
            ConsoleManager.SetConsoleReadMode("");

            // loop to perfor the actual pinging
            while (!_isAborted)
            {
                // read current line
                var line = ConsoleManager.ReadConsoleLine(_prompt, false, true);

                // check if it is a join command
                if (line.StartsWith("/join ", StringComparison.OrdinalIgnoreCase))
                {
                    // get the room name
                    var roomName = line.Replace("/join", "", StringComparison.OrdinalIgnoreCase).Trim();

                    // search for this room 
                    SearchArg args = new SearchArg(nameof(ChatRoom.Name), roomName);
                    var curRoom = App.Client.FindDataItem<ChatRoom>(args);

                    // if room no found
                    if (curRoom == null)
                    {
                        // create and save the room
                        curRoom = App.Client.CreateDataItem<ChatRoom>();
                        curRoom.Name = roomName;
                        curRoom.Users.Add(_myUser); // the reverse reference will be automtically created

                        // save
                        var response = await App.Client.SaveAsync(false, TxnAutoInclusion.References, curRoom);

                        // log
                        ConsoleManager.WriteLine($"{DateTime.Now:T} | Room {roomName} created");
                    }
                    else if (!curRoom.Users.Contains(_myUser)) // if room not contains user add the user to the room
                    {
                        // add
                        curRoom.Users.Add(_myUser);

                        // save
                        var response = await App.Client.SaveAsync(false, TxnAutoInclusion.References, curRoom);
                    }

                    // cleanup previous event handler if existed
                    if (_currentRoom != null)
                        _currentRoom.Messages.DataItemListSaved -= Messages_DataItemListSaved;

                    // set current room
                    _currentRoom = curRoom;

                    // hook to message changes
                    _currentRoom.Messages.DataItemListSaved += Messages_DataItemListSaved;

                    // log
                    ConsoleManager.WriteLine($"{DateTime.Now:T} | - {_myUser.Username} - joined room {roomName}");
                }
                else // input is a standard message
                {
                    // if current room is null then 
                    if (_currentRoom == null)
                    {
                        // log
                        ConsoleManager.WriteLine($"{DateTime.Now:T} | Error: Please join a room using the command '/join <room name>' where <room name> is the name of the room, eg '/join athens_basketball'");
                        continue;
                    }
                    else
                    {
                        // create a message and save it
                        var newMsg = App.Client.CreateDataItem<ChatMessage>();
                        newMsg.Text = line;
                        newMsg.CreatedTime = DateTime.Now;
                        _currentRoom.Messages.Add(newMsg);
                        _myUser.Messages.Add(newMsg); // set the author - reverse reference is automatically added
                        _lastMessageSent = newMsg; // cache it

                        // save
                        var response = await App.Client.SaveAsync(false, TxnAutoInclusion.References, newMsg, _myUser, _currentRoom);
                    }
                }
            }
        }

        private void Messages_DataItemListSaved(object? sender, DataItemListSavedEventArgs e)
        {
            // loop new messages
            foreach (ChatMessage item in e.AddedMembers)
            {
                // if it is a local message move one row up
                if (item == _lastMessageSent)
                    Console.SetCursorPosition(0, Console.CursorTop - 1);

                // cleanup current console line
                ConsoleManager.Write("\r" + new string(' ', Console.WindowWidth) + "\r");

                // print the message
                ConsoleManager.WriteLine($"{item.CreatedTime} | <{item.Author}> {item.Text}");

                // if it is peer message print the prompt string
                if (item != _lastMessageSent)
                    ConsoleManager.Write(_prompt);
            }
        }

    }
}
