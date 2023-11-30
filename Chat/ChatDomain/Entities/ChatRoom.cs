using Missionware.Cognibase.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatDomain.Entities
{
    [PersistedClass]
    public class ChatRoom : DataItem
    {
        [PersistedProperty(IdOrder = 1, AutoValue = AutoValue.Identity)]
        public long Id { get => getter<long>(); set => setter(value); }

        [PersistedProperty(IsMandatory = true)]
        public string Name { get => getter<string>(); set => setter(value); }

        [PersistedProperty]
        public DataItemRefList<ChatMessage> Messages => getList<ChatMessage>();

        [PersistedProperty(ReverseRef = nameof(User.ChatRooms))]
        public DataItemRefList<User> Users => getList<User>();
    }
}
