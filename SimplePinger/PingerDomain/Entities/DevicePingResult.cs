using System;
using System.ComponentModel;

using Missionware.Cognibase.Library;

namespace PingerDomain.Entities
{
    [PersistedClass]
    public class DevicePingResult : DataItem
    {
        public DevicePingResult() : base()
        {
            this.PropertyChanged += DevicePingResult_PropertyChanged;
        }

        private void DevicePingResult_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName == nameof(Value) || e.PropertyName == nameof(LastPingTime)) && DeviceRef != null)
                DeviceRef.raisePingResultChanges();
        }

        [PersistedProperty(IdOrder = 1, AutoValue = AutoValue.Identity)]
        public long Id { get => getter<long>(); set => setter(value); }

        [PersistedProperty(ReverseRef = nameof(Device.Result), AssociationType = AssociationType.CompositionParent)]
        public Device DeviceRef { get => getter<Device>(); set => setter(value); }

        [PersistedProperty] public int Value { get => getter<int>(); set => setter(value); }

        [PersistedProperty] public DateTime LastPingTime { get => getter<DateTime>(); set => setter(value); }
    }
}