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
				ports = System.IO.Ports.SerialPort.GetPortNames();
			}
			await ptp(source, port, CancellationToken.None);
		}

		static async Task ptp(string source, string port, CancellationToken token)
		{
			ISerialDevice serial = new PicoSerial(port);
			try
			{
				int state = 0; // state 0 = idle, 1 = connected, 2 = starting, 3, 4 = playing, 5 = ending
				serial.ReadReady += (s, e) =>
				{
					var line = ((ISerialDevice)s).ReadLine();
					switch (line.Trim())
					{
						case ".":
						case "!":
							if (state == 0)
							{
								state = 1;
								Console.WriteLine("Ready to go, push to play...");
							}
							break;
						case "PLAY":
							if (state < 2)
							{
								state = 2;
								Console.WriteLine("Start playing");
							}
							break;
						case "START":
							if (state == 3)
								state = 4;
							break;
						case "END":
							if (state >= 2)
								state = 0;
							break;
						case "":
							break;
						default:
							Console.WriteLine($"Unknown command: {line}");
							break;
					}
				};
				serial.Close();
				serial.Open();
				var writeTask = hb(serial, token);
				while (true)
				{
					try
					{
						if (!serial.IsOpen)
						{
							serial.Open();
						}
						if (state == 0)
							Console.WriteLine("Waiting for heartbeat");
						if (state == 2)
						{
							state = 3;
							var playTask = play(source, serial, () => state, (s) => state = s, token);
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.Message);
						serial.Close();
						await Task.Delay(5000);
					}
					await Task.Delay(1000);
				}
			}
			finally
			{
				serial.Close();
			}
		}

		static async Task hb(ISerialDevice serial, CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					await Task.Delay(1500);
					await serial.WriteLine("H", token);
				}
				catch (OperationCanceledException)
				{
					// no-op
				}
			}
		}

		static async Task play(string source, ISerialDevice serial, Func<int> getState, Action<int> setState, CancellationToken token)
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
				await serial.WriteLine("1", token);
				await Task.Delay(100, token);

				await tcs.Task;
				Console.WriteLine($"Done at {DateTime.Now}");

				while (!await serial.WriteLine("100", token) || getState() == 2)
				{
					// repeat until the finish is received
					await Task.Delay(1500, token);
				}
				setState(5);
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
