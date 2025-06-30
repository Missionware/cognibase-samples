using Missionware.SharedLib.Logging.SerilogAddin;
using Missionware.SharedLib;
using Missionware.ConfigLib;
using Missionware.Cognibase.Config;
using Missionware.Cognibase.Client;
using Missionware.Cognibase.Security.Identity.Domain.System;
using ChatDomain.Entities;
using Missionware.Cognibase.Library;
using Missionware.Cognibase.UI.Common;
using AutoGen.Core;
using AutoGen.Gemini;
using System.Text;
using Google.Cloud.AIPlatform.V1;

namespace CognibaseConsoleApp
{
    internal class Program
    {
        static ClientObjMgr _client;
        static Dictionary<long, IStreamingAgent> _agents = new();
        static User _currentUser;
        private static SimpleAuthenticationManager _authManager;
        private static string envVar;

        static void Main(string[] args)
        {
            // enable serilog
            LogManager.RegisterAgentType<SerilogAgent>();

            // check for environment variable
            envVar = Environment.GetEnvironmentVariable("GOOGLE_GEMINI_API_KEY");

            // if null or empty, exit
            if (String.IsNullOrEmpty(envVar))
            {
                Console.WriteLine("Error: GOOGLE_GEMINI_API_KEY environment variable is not set. Exiting...");
                Console.ReadLine();
                Environment.Exit(1);
            }

            //
            // SETTINGS SETUP
            //

            // Read client settings
            var settings = ConfigBuilder.Create().FromAppConfigFile();

            // Get proper SECTION
            var clientSettings = settings.GetSection<ClientSetupSettings>();
            clientSettings.ProcessSecuritySetting.UseCustomWorkflowToConnectSetting = true;
            //
            // APPLICATION SETUP
            //

            // Initialize the correct (Client) ODOS Application through the Application Manager 
            var cApp = ApplicationManager.InitializeAsMainApplication(new ClientApplication());

            // Initializes a Client Object Manager with the settings from configuration
            _client = ClientObjMgr.Initialize(cApp, ref clientSettings);

            // Registers domains through Domain Factory classes that reside in Domain assembly
            _client.RegisterDomainFactory<IdentityFactory>();

            // log
            Console.WriteLine("Starting application...");

            //
            // SECURITY SETUP
            //

            // Initialize Security PROFILE
            cApp.InitializeApplicationSecurity(_client, ref clientSettings);

            _client.ServerConnectionChange += Client_ServerConnectionChange;

            //
            // RUN
            //
            _authManager = new SimpleAuthenticationManager(cApp.Client);

            cApp.ApplicationStartUp += App_ApplicationStartUp;
            cApp.StartUpClient(StartupConnectionMode.NoConnection);
        }

        private static void App_ApplicationStartUp(object? sender, EventArgs e)
        {
            _authManager.LoginAsync("Basic", "user3", "user3");
        }

        private static void Client_ServerConnectionChange(object? sender, Missionware.Cognibase.Library.ServerConnectionChangedEventArgs e)
        {
            if (e.RegistrationState == ConnectionRegistrationState.Registered && e.IsListening)
            {
                _currentUser = Missionware.Cognibase.Library.DataItem.GetOrCreateDataItem<User>(_client, new object[] { "Botaki" });

                if (!_client.Save(_currentUser).WasSuccessful)
                {
                    // log
                    Console.WriteLine("Error", $"Error logging to chat app. Bot is not registered!");
                    _currentUser.Dispose();
                    _currentUser = null;
                    return;
                }

                runBot();
            }
        }

        public static void runBot()
        {
            // setup event handler for ChatMessages changes
            Missionware.Cognibase.Library.DataItem.GetClassInfo(typeof(ChatMessage)).DataItemSaved += Program_DataItemSaved;
        }

        private static void Program_DataItemSaved(object? sender, DataItemSavedEventArgs e)
        {
            Task.Run(async () =>
            {
                var msg = e.DataItem as ChatMessage;

                // filter out my messages or deleted/archived messages
                if (msg.Author == _currentUser || e.DataItem.IsDeleted || e.DataItem.IsArchived)
                    return;

                // vet chatroom by getting the reverse referrers
                var room = msg.GetPersistedReferers(true, true).OfType<ChatRoom>().FirstOrDefault() as ChatRoom;

                // create AI agent if not exists
                var agent = GetOrCreateAgent(room);

                Console.WriteLine($"[{room.Name}] {msg.Author.Username}: {msg.Message}");

                // get reply from AI agent
                var replyText = await agent.SendAsync(msg.Text);

                // get text
                var rspTxt = replyText.GetContent();

                Console.WriteLine($"[{room.Name}] Response: {rspTxt}");

                // if the response is not empty and does not contain "__ignore__" save to room messages as Bot response
                if (!String.IsNullOrEmpty(rspTxt) && !rspTxt.Contains("__ignore__"))
                {
                    // register change set
                    var undoset = _client.RegisterChangeSet();

                    try
                    {
                        // create message and fill data
                        var reply = _client.CreateDataItem<ChatMessage>();
                        reply.Author = _currentUser;
                        reply.Text = rspTxt;
                        reply.CreatedTime = DateTime.UtcNow;
                        room.Messages.Add(reply);
                        _currentUser.Messages.Add(reply);

                        // save
                        var result = _client.Save(reply, room, _currentUser);

                        // check result
                        if (!result.WasSuccessful)
                            undoset.ResetDataItems(); // reset changes if not successful
                        else
                            Console.WriteLine($"Message saved.");

                    }
                    catch (Exception ex)
                    {
                        undoset.ResetDataItems(); // reset changes if not successful
                        // log error
                        Console.WriteLine("Error", $"Error creating reply message: {ex.Message}");
                        return;
                    }
                    finally
                    {
                        // remove changeset
                        _client.UnRegisterChangeSet(undoset);
                    }

                }
            });
        }

        static IStreamingAgent GetOrCreateAgent(ChatRoom room)
        {
            // try get agent
            if (!_agents.TryGetValue(room.Id, out var agent))
            {
                // create new agent
                agent = new GeminiChatAgent(
                    name: $"gemini",
                    model: "gemini-2.0-flash",
                    apiKey: envVar,
                    systemMessage: @$"
You are Botaki, a helpful assistant observing chatroom messages.

Only respond when:
- Your name ""Botaki"" is mentioned directly (e.g., ""Hey Botaki"", ""Botaki"", ""@Botaki"").
- A user clearly asks for help (e.g., includes words like ""help"", ""assist"", ""support"", ""tell"").
- There is a factual or technical question relevant to the conversation that has not been answered.

DO NOT respond to:
- Greetings like ""hi"", ""hello"", or small talk if not mentioning your name.
- Casual chat between users.
- Anything not requiring your input.

Keep your responses brief, informative, and in the same tone as the room. Stay silent unless you are needed.
If you decide the message does not need a response, reply with ""__ignore__""


"
                ).RegisterMessageConnector()
            .RegisterPrintMessage();

                _agents[room.Id] = agent;
            }
            return agent;
        }
    }
}