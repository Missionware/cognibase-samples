using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Net.NetworkInformation;

using Missionware.Cognibase.Client;
using Missionware.Cognibase.Library;
using Missionware.ConfigLib;
using PingerDomain.Entities;
using Missionware.SharedLib;

namespace PingerAgent
{
    /// <summary>
    ///     The ping controller is the manager of the ping tasks
    ///     It periodically
    /// </summary>
    public class PingController
    {
        private readonly object _comLockObject = new(); // lock for performing actions on COM
        private DataItemCollection<Device> _devicesCollection; // the device live collection
        private volatile bool _isAborted; // to check if it is aborted

        // Data
        private volatile bool _isInitialized; // flag to prevent duplicate
        private DateTime _lastRetentionCheck = DateTime.MinValue; // last check for deletion of history items

        private readonly ConcurrentDictionary<Device, DevicePingTask>
            _pingTasks = new(); // dict for keeping the state of ping task per device

        private PingerOptions _settings; // the settings
        private readonly List<PingHistoryItem> newHistoryItemsToSave = new(); // cache any new history items

        private readonly int retentionCheckInterval = 30; // Interval (seconds) to check for deleting old history items

        // keep a static App instance to also have easy access to the ClientObjectManager (_Client)
        // this could be also hosted in an IoC container as singleton
        public static ClientApplication App { get; set; }

        public void Start()
        {
            // get settings
            _settings = ConfigBuilder.Create().FromAppConfigFile().GetTopLevelSettings<PingerOptions>();

            // log
            Console.WriteLine("Connecting to server ...");
            App.Client.ServerConnectionChange += _Client_ServerConnectionChange;
        }

        public void Stop()
        {
            _isAborted = true;
        }

        private void _Client_ServerConnectionChange(object? sender, ServerConnectionChangedEventArgs e)
        {
            if (e.IsConnected && e.RegistrationState == ConnectionRegistrationState.Registered && !_isInitialized)
            {
                // mark and log
                _isInitialized = true;
                Console.WriteLine("Connected to server. Starting ping ...");

                // run
                Task.Run(() => MainLoop());
            }
            else
            {
                Console.WriteLine("Disconnected from server");
            }
        }

        private async Task MainLoop()
        {
            // first loop until we get a live collection of the devices
            while (!_isAborted)
                try
                {
                    // read
                    _devicesCollection = App.Client.ReadDataItemCollection<Device>();
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {DateTime.Now}: {ex}");
                    await Task.Delay(1000);
                }

            // attach to changes
            _devicesCollection.CollectionChanged += _devices_CollectionChanged;

            // foreach device create a ping task and link them
            foreach (Device item in _devicesCollection.GetMembersTypedList())
                _pingTasks.TryAdd(item, new DevicePingTask(item, App.Client, onPing));

            // log
            Console.WriteLine($"Found {_devicesCollection.Count} devices");

            // loop to perfor the actual pinging
            while (!_isAborted)
            {
                // mark
                DateTime now = DateTime.Now;

                // iterate all ping tasks
                foreach (KeyValuePair<Device, DevicePingTask> deviceKv in _pingTasks)
                    try
                    {
                        // if state is idle and is time to fire next ping
                        if (deviceKv.Value.State == TaskState.Idle &&
                            now - deviceKv.Value.PingFinishedOn > TimeSpan.FromSeconds(deviceKv.Key.PingInterval) &&
                            now - deviceKv.Value.PingStartedOn > TimeSpan.FromSeconds(deviceKv.Key.PingInterval))
                            // fire ping asynchronously without waiting (await)
                            // this will call the callback tha is in synchronized block
                            deviceKv.Value.Ping();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }

                try
                {
                    // init the deletion items list
                    ICollection<PingHistoryItem> toDeleteHistoryItems = null;

                    // deletion check interval passed
                    if (now - _lastRetentionCheck > TimeSpan.FromSeconds(retentionCheckInterval))
                    {
                        // mark
                        _lastRetentionCheck = now;

                        // get query for old items
                        IDataItemCollectionValidator<PingHistoryItem> deleteionValidator = getQueryForDeletion(now);

                        // read old items
                        toDeleteHistoryItems = App.Client.ReadDataItems(deleteionValidator);

                        // mark for deletion
                        foreach (PingHistoryItem item in toDeleteHistoryItems)
                            item.MarkForDeletion();
                    }

                    // lock to prevent getting "half" changes when changing
                    lock (_comLockObject)
                    {
                        // save all
                        App.Client.Save();

                        // clear new history items
                        newHistoryItemsToSave.Clear();
                    }
                }
                catch (Exception ex)
                {
                    // log
                    Console.WriteLine($"ERROR: {DateTime.Now} : Failed to save results");
                    Console.WriteLine(ex.ToString());
                }

                // wait
                await Task.Delay(250);
            }
        }

        private void onPing(Device device, PingReply reply, DateTime time)
        {
            // lock to prevent incomplete changes save
            lock (_comLockObject)
            {
                // if success log and apply value
                if (reply != null && reply.Status == IPStatus.Success)
                {
                    Console.WriteLine($"{time} : Ping success for {device.Host}");
                    device.Result.Value = 2;
                }
                else if (reply != null) // if reply with error
                {
                    Console.WriteLine($"ERROR: {time} : Ping error for host {device.Host}.");
                    device.Result.Value = 1;
                }
                else // if reply is null -> no output
                {
                    device.Result.Value = 1;
                }

                // mark
                device.Result.LastPingTime = time;

                // create new history item and add it to list for saving
                PingHistoryItem? historyItem = App.Client.CreateDataItem<PingHistoryItem>();
                historyItem.DeviceId = device.Id;
                historyItem.Time = device.Result.LastPingTime;
                historyItem.Value = device.Result.Value;
                newHistoryItemsToSave.Add(historyItem);
            }
        }

        // on device change (add, delete)
        private void _devices_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // handle removed items
            if (e.OldItems != null)
                foreach (Device item in e.OldItems)
                {
                    DevicePingTask task = null;
                    _pingTasks.Remove(item, out task);
                }

            // handle new items
            if (e.NewItems != null)
                foreach (Device item in e.NewItems)
                    _pingTasks.TryAdd(item, new DevicePingTask(item, App.Client, onPing));
        }

        // the OOQL query
        private IDataItemCollectionValidator<PingHistoryItem> getQueryForDeletion(DateTime time)
        {
            // get field
            DataItemField? timeField =
                DataItem.GetClassInfo(typeof(PingHistoryItem)).GetField(nameof(PingHistoryItem.Time));

            // create where expression
            var whereActiveExpr = new WhereComparisonExpression(timeField, WhereOperator.LesserThan,
                time - TimeSpan.FromHours(_settings.RetentionHours));

            // create and return validator
            var deletionValidator =
                new DataItemCollectionValidator<PingHistoryItem>(true) as IDataItemCollectionValidator<PingHistoryItem>;
            deletionValidator.QueryExpression = whereActiveExpr;
            deletionValidator.IsForceLoader = true;
            return deletionValidator;
        }
    }
}