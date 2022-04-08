using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushToPlay
{
	interface ILogger
	{
		TraceLevel LogLevel { get; set; }
		void WriteLine(string text, TraceLevel level);
	}

	static class ILoggerExtension
	{
		public static void Verbose(this ILogger logger, string text)
		{
			logger?.WriteLine(text, TraceLevel.Verbose);
		}
		public static void Info(this ILogger logger, string text)
		{
			logger?.WriteLine(text, TraceLevel.Info);
		}
		public static void Warning(this ILogger logger, string text)
		{
			logger?.WriteLine(text, TraceLevel.Warning);
		}
		public static void Error(this ILogger logger, string text)
		{
			logger?.WriteLine(text, TraceLevel.Error);
		}
	}
}
