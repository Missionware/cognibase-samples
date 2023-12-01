using System.Reflection;

using Missionware.Cognibase.Client;

namespace PingerDomain.System
{
    public class PingerFactory : DataItemDomainFactory
    {
        // Return DOMAIN Assembly
        protected override Assembly getFactoryAssembly() { return GetType().Assembly; }

        // Return DOMAIN Description
        protected override string getDomainDescription() { return "This is Pinger Domain"; }
    }
}