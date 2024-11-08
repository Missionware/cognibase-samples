using System.Net.NetworkInformation;

using Missionware.Cognibase.Library;
using PingerDomain.Entities;

namespace PingerAgent
{
    /// <summary>
    ///     This class represents the asynchronous task to perform the actual ping
    /// </summary>
    public class DevicePingTask : IDisposable
    {
        private readonly IClient _com; // the client objet manager

        private readonly Action<Device, PingReply, DateTime> _onPing; // the ping callback

        // Data
        private readonly Ping _pingSender = new(); // the system pinger class 
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


        #region IDisposable

        // Dispose pattern
        private volatile bool _isDisposed;

        public bool IsDisposed
        {
            get => _isDisposed;
            private set => _isDisposed = value;
        }

        // Called by either cleaner (:true) or class destructor (:false)
        protected virtual void Dispose(bool disposing)
        {
            // Check
            if (!IsDisposed)
            {
                // Only dispose once!
                IsDisposed = true;

                // Cleanup Managed Resources
                if (disposing)
                    disposingManaged();
            }
        }

        // Public Cleaner Method
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Everything needed to run for cleanup goes here
        protected virtual void disposingManaged()
        {
            // Check & Dispose Blinker
            if (_pingSender != null) _pingSender.Dispose();
        }

        #endregion
    }

    public enum TaskState
    {
        Idle,
        InProcess
    }
}