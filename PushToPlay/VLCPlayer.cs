using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PushToPlay
{
	class VLCPlayer : IVideoPlayer
	{
		internal const string vlc64Path = @"C:\Program Files\VideoLAN\VLC\vlc.exe";
		internal const string vlc32Path = @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe";

		internal string ExePath { get; }

		internal string Arguments { get; }

		public event EventHandler Started;

		public event EventHandler Finished;

		public VLCPlayer()
		{
			var vlcPath = vlc64Path;
			if (!System.IO.File.Exists(vlcPath))
				vlcPath = vlc32Path;
			if (!System.IO.File.Exists(vlcPath))
				Console.WriteLine("Could not find vlc.");
			this.ExePath = vlcPath;
			this.Arguments = "-f --play-and-exit --no-video-title-show --qt-continue=0 ";
		}

		/// <summary>Prepares a new <see cref="Process"/> to run the video player.</summary>
		/// <param name="source">The filename of the video to play.</param>
		/// <returns>A <see cref="Process"/> to run the video player.</returns>
		private Process PrepareProcess(string source)
		{
			return new Process
			{
				StartInfo = new ProcessStartInfo()
				{
					FileName = this.ExePath,
					Arguments = this.Arguments + source,
				},
				EnableRaisingEvents = true,
			};
		}

		public async Task PlayAsync(string source, CancellationToken token)
		{
			var proc = this.PrepareProcess(source);
			try
			{
				token.ThrowIfCancellationRequested();
				var tcs = new TaskCompletionSource<bool>();
				token.Register(tcsObj => ((TaskCompletionSource<bool>)tcsObj).TrySetCanceled(), tcs);
				proc.Exited += (s, e) =>
				{
					tcs.TrySetResult(true);
				};
				proc.Start();
				this.Started?.Invoke(this, EventArgs.Empty);
				await Task.Delay(1000, token);

				await tcs.Task;

				this.Finished?.Invoke(this, EventArgs.Empty);
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
