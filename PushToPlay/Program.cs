using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PushToPlay
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Console.WriteLine("===> Push to Play <===");
			Console.WriteLine();
			var opts = Nito.OptionParsing.CommandLineOptionsParser.Parse<ProgramOptions>(args);
			var source = opts.Source;
			while (string.IsNullOrWhiteSpace(source))
			{
				Console.WriteLine("Enter source path: ");
				source = Console.ReadLine();
			}
			var ports = System.IO.Ports.SerialPort.GetPortNames();
			var port = opts.Port;
			while (string.IsNullOrWhiteSpace(port) || !ports.Contains(port))
			{
				Console.WriteLine("Available ports are:");
				for (int i = 0; i < ports.Length; i++)
				{
					Console.WriteLine($"{i + 1}) {ports[i]}");
				}
				Console.WriteLine($"Select a port (1-{ports.Length}): ");
				port = Console.ReadLine();
				if (!ports.Contains(port) && int.TryParse(port, out var idx) && idx > 0 && idx <= ports.Length)
				{
					port = ports[idx - 1];
				}
			}
			await ptp(source, port, CancellationToken.None);
		}

		static async Task ptp(string source, string port, CancellationToken token)
		{
			var serial = new SerialPort(port, 9600, Parity.None, 8, StopBits.One);
			var writeSemaphore = new SemaphoreSlim(1);
			try
			{
				serial.Open();
				var writeTask = hb(serial, writeSemaphore, token);
				while (true)
				{
					try
					{
						if (!serial.IsOpen)
						{
							serial.Open();
						}
						Console.WriteLine("Ready to go, push to play...");
						var line = serial.ReadLine();
						if (line.Trim() == "PLAY")
						{
							await play(source, serial, writeSemaphore, token);
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.Message);
					}
					await Task.Delay(1000);
				}
			}
			finally
			{
				serial.Close();
			}
		}

		static async Task hb(SerialPort serial, SemaphoreSlim writeSemaphore, CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				await Task.Delay(1000);
				await writeSemaphore.WaitAsync();
				try
				{
					serial.Write("H");
				}
				finally
				{
					writeSemaphore.Release();
				}
			}
		}

		static async Task play(string source, SerialPort serial, SemaphoreSlim writeSemaphore, CancellationToken token)
		{
			const string vlc64Path = @"C:\Program Files\VideoLAN\VLC\vlc.exe";
			const string vlc32Path = @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe";
			var vlcPath = vlc64Path;
			if (!System.IO.File.Exists(vlcPath))
				vlcPath = vlc32Path;
			if (!System.IO.File.Exists(vlcPath))
				Console.WriteLine("Could not find vlc.");
			var proc = new Process
			{
				StartInfo = new ProcessStartInfo()
				{
					FileName = vlcPath,
					Arguments = "-f --play-and-exit --no-video-title-show --qt-continue=0 " + source,
				},
				EnableRaisingEvents = true,
			};
			try
			{
				token.ThrowIfCancellationRequested();
				var tcs = new TaskCompletionSource<bool>();
				token.Register(tcsObj => ((TaskCompletionSource<bool>)tcsObj).TrySetCanceled(), tcs);
				proc.Exited += (s, e) =>
				{
					tcs.TrySetResult(true);
				};
				Console.WriteLine($"Playing at {DateTime.Now}");
				proc.Start();
				await writeSemaphore.WaitAsync(token);
				try
				{
					serial.WriteLine("1");
				}
				finally
				{
					writeSemaphore.Release();
				}
				await Task.Delay(100, token);

				await tcs.Task;
				Console.WriteLine($"Done at {DateTime.Now}");

				await writeSemaphore.WaitAsync(token);
				try
				{
					serial.WriteLine("100");
				}
				finally
				{
					writeSemaphore.Release();
				}
				await Task.Delay(100, token);
			}
			catch (OperationCanceledException)
			{
				proc.WaitForExit(100);
			}
			finally
			{
				proc.Dispose();
			}
		}
	}
}
