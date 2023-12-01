using System.Runtime.InteropServices;

using Missionware.Cognibase.Library;

// 
// COGNIBASE Information
//
[assembly: DisplayAs(Label = "Pinger Domain")]
[assembly: RuntimeDomain(DomainName = "PingerDomain",
    DomainShortCode = "PING",
    SecurityEnabled = false,
    NoRoleCanLogin = true,
    IsReportProvider = true)]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]