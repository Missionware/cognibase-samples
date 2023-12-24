using System;
using Missionware.Cognibase.Server;

namespace TodoServer
{
    internal class Program
    {
        [MTAThread]
        private static void Main(string[] args)
        {
            ServerManager.StartInConsole();
        }
    }
}