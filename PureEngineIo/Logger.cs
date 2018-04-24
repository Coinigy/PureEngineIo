using System;
using System.Runtime.CompilerServices;

namespace PureEngineIo
{
    public static class Logger
    {
		internal static void Log(string message, [CallerMemberName] string memberName = "")
		{
			if(PureEngineIoSocket.Debug)
				OutputConsole.WriteLine(memberName + " " + message, ConsoleColor.Blue);
		}

		internal static void Log(Exception ex, [CallerMemberName] string memberName = "")
		{
			if (PureEngineIoSocket.Debug)
				OutputConsole.WriteLine(memberName + " " + ex.Message, ConsoleColor.Red);
		}
	}
}
