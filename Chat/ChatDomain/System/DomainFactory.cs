using Missionware.Cognibase.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChatDomain.System
{
    public class DomainFactory : DataItemDomainFactory
    {
        // Return DOMAIN Assembly
        protected override Assembly getFactoryAssembly() { return GetType().Assembly; }

        // Return DOMAIN Description
        protected override string getDomainDescription() { return "This is ChatDomain Domain"; }
    }
}
