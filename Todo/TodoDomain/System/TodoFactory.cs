using System.Reflection;
using Missionware.Cognibase.Client;

namespace TodoDomain.System
{
    public class TodoFactory : DataItemDomainFactory
    {
        // Return DOMAIN Assembly
        protected override Assembly getFactoryAssembly() { return GetType().Assembly; }

        // Return DOMAIN Description
        protected override string getDomainDescription() { return "This is ToDo Domain"; }
    }
}