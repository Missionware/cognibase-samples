using System;

using Missionware.Cognibase.Library;

namespace PingerDomain.Entities
{
    [PersistedClass]
    public class PingHistoryItem : DataItem
    {
        [PersistedProperty(IdOrder = 1, AutoValue = AutoValue.Identity)]
        public long Id { get => getter<long>(); set => setter(value); }

        [PersistedProperty] public long DeviceId { get => getter<long>(); set => setter(value); }

        [PersistedProperty] public int Value { get => getter<int>(); set => setter(value); }

        [PersistedProperty] public DateTime Time { get => getter<DateTime>(); set => setter(value); }
    }
}