using System;
using System.ComponentModel;

using Missionware.Cognibase.Library;

namespace PingerDomain.Entities
{
    [PersistedClass]
    public class Device : DataItem
    {
        [PersistedProperty(IdOrder = 1, AutoValue = AutoValue.Identity)]
        public long Id { get => getter<long>(); set => setter(value); }

        [PersistedProperty] public string Name { get => getter<string>(); set => setter(value); }

        [PersistedProperty] public string Host { get => getter<string>(); set => setter(value); }

        [PersistedProperty(DefaultValue = 1)] public int PingInterval { get => getter<int>(); set => setter(value); }

        [PersistedProperty(ReverseRef = nameof(DevicePingResult.DeviceRef),
            AssociationType = AssociationType.CompositionChild)]
        public DevicePingResult Result { get => getter<DevicePingResult>(); set => setter(value); }

        [IndirectProperty(Target = nameof(Result))]
        public int PingResult
        {
            get
            {
                int threshold = 2 * PingInterval;
                if (threshold < 10)
                    threshold = 10;

                if (DateTime.Now - TimeSpan.FromSeconds(threshold) > Result.LastPingTime)
                    return 0;
                return Result.Value;
            }
        }

        internal void raisePingResultChanges()
        {
            OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(PingResult)));
        }
    }
}