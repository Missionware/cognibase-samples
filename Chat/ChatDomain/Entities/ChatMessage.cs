using Missionware.Cognibase.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatDomain.Entities
{
    [PersistedClass]
    public class ChatMessage : DataItem
    {
        [PersistedProperty(IdOrder = 1, AutoValue = AutoValue.Identity)]
        public long Id { get => getter<long>(); set => setter(value); }

        [PersistedProperty]
        public string Text { get => getter<string>(); set => setter(value); }

        [PersistedProperty]
        public DateTime CreatedTime { get => getter<DateTime>(); set => setter(value); }

        [PersistedProperty(ReverseRef = nameof(User.Messages))]
        public User Author { get => getter<User>(); set => setter(value); }
    }
}
