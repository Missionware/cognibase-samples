﻿using Missionware.Cognibase.Library;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// 
// Cognibase Information
//
[assembly: DisplayAs(Label = "ChatDomain")]
[assembly: RuntimeDomain(DomainName = "ChatDomain",
                         DomainShortCode = "",
                         SecurityEnabled = false,
                         NoRoleCanLogin = true,
                         IsReportProvider = true,
                         ClientPreLoadMode = DomainPreLoadMode.All,
                         ServerPreLoadMode = DomainPreLoadMode.All)]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
