using Missionware.Cognibase.Library;

namespace TodoDomain.Entities
{
    [PersistedClass]
    public class ToDoItem : DataItem
    {
        [PersistedProperty(IdOrder = 1, AutoValue = AutoValue.Identity)]
        public long Id { get => getter<long>(); set => setter(value); }

        [PersistedProperty] 
        public string Description { get => getter<string>(); set => setter(value); }

        [PersistedProperty]
        public bool IsChecked { get => getter<bool>(); set => setter(value); }
    }
}