using System;
using Missionware.Cognibase.Server;
using Missionware.SharedLib;
using Missionware.SharedLib.Logging.SerilogAddin;

namespace TodoServer
{
    internal class Program
    {
        [MTAThread]
        private static void Main(string[] args)
        {
            // enable Serilog
            LogManager.RegisterAgentType<SerilogAgent>();

            // start the object server
            ServerManager.StartInConsole();
        }
    }
}