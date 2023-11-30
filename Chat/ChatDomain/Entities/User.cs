using Missionware.Cognibase.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatDomain.Entities
{
    [PersistedClass]
    public class User : DataItem
    {
        [PersistedProperty(IdOrder = 1)]
        public string Username { get => getter<string>(); set => setter(value); }

        [PersistedProperty]
        public DateTime LastLoginTime { get => getter<DateTime>(); set => setter(value); }

        [PersistedProperty(ReverseRef = nameof(ChatMessage.Author))]
        public DataItemRefList<ChatMessage> Messages => getList<ChatMessage>();

        [PersistedProperty(ReverseRef = nameof(ChatRoom.Users))]
        public DataItemRefList<ChatRoom> ChatRooms => getList<ChatRoom>();
    }
}
