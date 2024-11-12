using System.Net.NetworkInformation;

using Missionware.Cognibase.Library;
using Missionware.SharedLib;

using PingerDomain.Entities;

namespace PingerAgent
{
    /// <summary>
    ///     This class represents the asynchronous task to perform the actual ping
    /// </summary>
    public class DevicePingTask : IManagedDisposable
    {
        private readonly IClient _com; // the client objet manager

        private readonly Action<Device, PingReply, DateTime> _onPing; // the ping callback

        // Data
        private Ping _pingSender = new(); // the system pinger class 
        private volatile TaskState _state; // the state of the ping

        public DevicePingTask(Device device, IClient com,
            Action<Device, PingReply, DateTime> OnPing) // ref object lockObject)
        {
            // init
            Device = device;
            _com = com;
            _onPing = OnPing;
        }

        public DateTime PingStartedOn { get; private set; } // time when ping requested
        public DateTime PingFinishedOn { get; private set; } // time when ping returned
        public Device Device { get; } // the device dataitem
        public TaskState State { get => _state; private set => _state = value; } // the state

        /// <summary>
        ///     The async ping method
        /// </summary>
        /// <returns></returns>
        public async Task Ping()
        {
            // init 
            State = TaskState.InProcess;
            DateTime now = DateTime.Now;
            PingReply? reply = null;

            try
            {
                // mark ping started
                PingStartedOn = now;
                Console.WriteLine($"{now} : Pinging {Device.Host}");

                // perform the ping
                reply = await _pingSender.SendPingAsync(Device.Host, 2000).ConfigureAwait(false);

                // mark end ping
                now = DateTime.Now;
            }
            catch (PingException ex)
            {
                // in case of error log
                now = DateTime.Now;
                Console.WriteLine($"ERROR: {now} : Ping error for host {Device.Host}. Message: {ex.Message}");
            }
            catch (Exception ex)
            {
                // in case of error log
                now = DateTime.Now;
                Console.WriteLine($"ERROR: {now} : Unknown error for host {Device.Host}");
                Console.WriteLine(ex.ToString());
            }

            // fire callback
            _onPing(Device, reply, now);

            // set time and state
            PingFinishedOn = Device.Result.LastPingTime;
            State = TaskState.Idle;
        }


        #region IManagedDisposable

        //
        // Dispose pattern
        //

        // Data
        private volatile int _isDisposed;
        private bool _isDisposing;

        // Properties
        public bool IsDisposed => _isDisposed == 1;
        public bool IsDisposing => _isDisposing;

        // Called by either cleaner (:true) or class destructor (:false)
        private void dispose(bool isDisposingExplicitly)
        {
            // Only dispose once!
            if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
                DisposeManager.SafeDispose(isDisposingExplicitly, ref _isDisposing, onExplicitlyDisposing, onFinalizationDisposing);
        }

        // Public Cleaner Method
        public virtual void Dispose()
        {
            dispose(true);
            GC.SuppressFinalize(this);
        }

        // Everything needed to run for cleanup of UNMANAGED resources goes here
        protected virtual void onFinalizationDisposing()
        {
            // Do nothing
        }

        // Everything needed to run for cleanup of MANAGED resources goes here
        protected virtual void onExplicitlyDisposing()
        {
            _pingSender.Dispose();
        }

        #endregion
    }

    public enum TaskState
    {
        Idle,
        InProcess
    }
}