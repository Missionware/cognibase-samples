using Missionware.Cognibase.Server;
using Missionware.SharedLib;
using Missionware.SharedLib.Logging.SerilogAddin;

namespace ChatServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // enable Serilog
            LogManager.RegisterAgentType<SerilogAgent>();

            // start the object server
            ServerManager.StartInConsole();
        }
    }
}