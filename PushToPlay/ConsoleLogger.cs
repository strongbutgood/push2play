using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PushToPlay
{
	class ConsoleLogger : ILogger
	{
		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

		public TraceLevel LogLevel { get; set; } = TraceLevel.Info;

		public void WriteLine(string text, TraceLevel level)
		{
			if (level > TraceLevel.Off && level <= this.LogLevel)
			{
				this._semaphore.Wait();
				try
				{
					Console.WriteLine($"{DateTime.Now.TimeOfDay:hh\\:mm\\:ss\\.fff}  {level.ToString().ToUpper(),-8}: {text}");
				}
				finally
				{
					this._semaphore.Release();
				}
			}
		}
	}
}
