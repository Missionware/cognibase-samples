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
        private User _currentUser;                   // the user object that represents the current user
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
                    var myusername = App.Client.PrimaryServerMgr.RegisteredClientIdentity.UserName;

                    // get or create user
                    _currentUser = DataItem.GetOrCreateDataItem<User>(App.Client, [myusername]);
                    _currentUser.LastLoginTime = DateTime.Now;

                    // if just created
                    if (_currentUser.IsNew)
                    {
                        // try save or else retry
                        if (!App.Client.Save(_currentUser).WasSuccessful)
                        {
                            // log
                            ConsoleManager.WriteLine($"{DateTime.Now:T} | Error logging to chat app. User {App.Client.PrimaryServerMgr.RegisteredClientIdentity.UserName} is not registered!");
                            _currentUser.Dispose();
                            _currentUser = null;
                            continue;
                        }
                        else
                        {
                            // log created
                            ConsoleManager.WriteLine($"{_currentUser.LastLoginTime} | Welcome {App.Client.PrimaryServerMgr.RegisteredClientIdentity.UserName}");
                        }
                    }
                    else
                    {
                        if (App.Client.Save(_currentUser).WasSuccessful)
                        {
                            // log
                            ConsoleManager.WriteLine($"{_currentUser.LastLoginTime:T} | Welcome {App.Client.PrimaryServerMgr.RegisteredClientIdentity.UserName}");
                        }
                    }

                    break;
                }
                catch (Exception ex)
                {
                    // log error
                    ConsoleManager.WriteLine($"{DateTime.Now:T} | Error logging to chat app for user {App.Client.PrimaryServerMgr.RegisteredClientIdentity.UserName}");
                    await Task.Delay(1000);
                }
            }

            // set the prompt
            _prompt = $"<{_currentUser.Username}> ";

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
                        curRoom.Users.Add(_currentUser); // the reverse reference will be automtically created

                        // save
                        var response = await App.Client.SaveAsync(false, TxnAutoInclusion.References, curRoom);

                        if(response.WasSuccessful)
                        {
                            // log
                            ConsoleManager.WriteLine($"{DateTime.Now:T} | Room {roomName} created");
                        }
                        else
                        {
                            // reset dataitems
                            App.Client.ResetAllMonitoredItems();

                            // log
                            ConsoleManager.WriteLine($"{DateTime.Now:T} | Error: Could not create room {roomName}");
                        }
                    }
                    else if (!curRoom.Users.Contains(_currentUser)) // if room not contains user add the user to the room
                    {
                        // add
                        curRoom.Users.Add(_currentUser);

                        // save
                        var response = await App.Client.SaveAsync(false, TxnAutoInclusion.References, curRoom);

                        if (!response.WasSuccessful)
                        {
                            // log
                            ConsoleManager.WriteLine($"{DateTime.Now:T} | Error: Could not join room {roomName}");

                            // return
                            return;
                        }
                    }

                    // cleanup previous event handler if existed
                    if (_currentRoom != null)
                        _currentRoom.Messages.DataItemListSaved -= Messages_DataItemListSaved;

                    // set current room
                    _currentRoom = curRoom;

                    // hook to message changes
                    _currentRoom.Messages.DataItemListSaved += Messages_DataItemListSaved;

                    // log
                    ConsoleManager.WriteLine($"{DateTime.Now:T} | - {_currentUser.Username} - joined room {roomName}");
                }
                else if (line.StartsWith("/leave ", StringComparison.OrdinalIgnoreCase)) // check if it is a leave command
                {
                    // get the room name
                    var roomName = line.Replace("/leave", "", StringComparison.OrdinalIgnoreCase).Trim();

                    // get chatroom of user if exists
                    var curUserRoom = _currentUser.ChatRooms.ToList().FirstOrDefault(o => o.Name == roomName);

                    // check if found
                    if (curUserRoom != null)
                    {
                        // remove user
                        _currentUser.ChatRooms.Remove(curUserRoom);

                        // save
                        var response = await App.Client.SaveAsync(false, TxnAutoInclusion.References, _currentUser, curUserRoom);

                        // check
                        if (!response.WasSuccessful)
                        {
                            // log
                            ConsoleManager.WriteLine($"{DateTime.Now:T} | Error: Could not leave room {roomName}");
                        }
                        else
                        {
                            // log
                            ConsoleManager.WriteLine($"{DateTime.Now:T} | Successfully left room {roomName}");
                        }
                    }
                    else
                    {
                        // log
                        ConsoleManager.WriteLine($"{DateTime.Now:T}  | Error: Could not find room {roomName} to leave");
                    }
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
                        _currentUser.Messages.Add(newMsg); // set the author - reverse reference is automatically added
                        _lastMessageSent = newMsg; // cache it

                        // save
                        var response = await App.Client.SaveAsync(false, TxnAutoInclusion.References, newMsg, _currentUser, _currentRoom);

                        // check
                        if (!response.WasSuccessful)
                        {
                            // log
                            ConsoleManager.WriteLine($"{DateTime.Now:T} | Error: Could not send message");
                        }
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
