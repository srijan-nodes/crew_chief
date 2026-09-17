using System;
namespace CrewChiefV4
{
    public static class ConsoleLogger
    {
        public static class Log
        {
            public static void Verbose(string msg) { Console.WriteLine("[Verbose] " + msg); }
            public static void Error(string msg) { Console.WriteLine("[Error] " + msg); }
        }
    }
}
