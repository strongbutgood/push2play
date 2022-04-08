using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PushToPlay
{
	class Coordinator
	{
		private readonly ISerialDevice _serialDevice;
		private readonly IVideoPlayer _videoPlayer;
		private readonly ILogger _logger;
		private readonly string _videoSource;

		private CancellationTokenSource _cts;

		private readonly IDictionary<string, Action<string>> _serialCommands;

		internal PlayState State { get; set; }

		public Coordinator(ISerialDevice serialDevice, IVideoPlayer videoPlayer, ILogger logger, string videoSource)
		{
			this._serialDevice = serialDevice;
			this._videoPlayer = videoPlayer;
			this._logger = logger;
			this._videoPlayer.Started += this._videoPlayer_StartedAsync;
			this._videoPlayer.Finished += this._videoPlayer_FinishedAsync;
			this._videoSource = videoSource;
			this._serialCommands = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase)
			{
				["."] = this._serialDevice_ReadReady_HB,
				["!"] = this._serialDevice_ReadReady_HB,
				["PLAY"] = this._serialDevice_ReadReady_Play,
				["START"] = this._serialDevice_ReadReady_Start,
				["END"] = this._serialDevice_ReadReady_End,
				[""] = (c) => { /* no-op, ignore empty lines */ },
			};
		}

		private void _serialDevice_ReadReady_HB(string command)
		{
			if (this.State == PlayState.Idle)
			{
				this.State = PlayState.Connected;
				this._logger.Info("Ready to go, push to play...");
			}
		}
		private void _serialDevice_ReadReady_Play(string command)
		{
			if (this.State == PlayState.Idle ||
				this.State == PlayState.Connected)
			{
				this.State = PlayState.Starting;
				this._logger.Info("Start playing");
			}
		}
		private void _serialDevice_ReadReady_Start(string command)
		{
			if (this.State == PlayState.Playing)
			{
				this.State = PlayState.PlayingAcknowledged;
			}
		}
		private void _serialDevice_ReadReady_End(string command)
		{
			if (this.State != PlayState.Idle &&
				this.State != PlayState.Connected)
			{
				this.State = PlayState.Idle;
			}
		}
		private void _serialDevice_ReadReady(object sender, EventArgs args)
		{
			var line = ((ISerialDevice)sender).ReadLine();
			if (this._serialCommands.TryGetValue(line.Trim(), out var action))
			{
				action(line);
			}
			else
			{
				this._logger.Warning($"Unknown command: {line}");
			}
		}

		private async void _videoPlayer_StartedAsync(object sender, EventArgs args)
		{
			this._logger.Verbose($"Playing started");
			try
			{
				while (!await this._serialDevice.WriteLineAsync("1", this._cts?.Token ?? CancellationToken.None) && this.State == PlayState.Starting)
				{
					// repeat until the start is received
					await Task.Delay(1500, this._cts?.Token ?? CancellationToken.None);
				}
			}
			catch (OperationCanceledException)
			{
			}
		}

		private async void _videoPlayer_FinishedAsync(object sender, EventArgs args)
		{
			this._logger.Info($"Finished playing");
			try
			{
				while (!await this._serialDevice.WriteLineAsync("100", this._cts?.Token ?? CancellationToken.None) || this.State == PlayState.Starting)
				{
					// repeat until the finish is received
					await Task.Delay(1500, this._cts?.Token ?? CancellationToken.None);
				}
				this.State = PlayState.Ending;
			}
			catch (OperationCanceledException)
			{
			}
		}

		private async Task HeartBeatAsync(CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					await Task.Delay(1500, token);
					this._logger.Verbose($"Send heartbeat...");
					await this._serialDevice.WriteLineAsync("H", token);
				}
				catch (OperationCanceledException)
				{
					// no-op
					break;
				}
			}
		}

		public async Task ActivateAsync(CancellationToken token)
		{
			this._serialDevice.ReadReady += this._serialDevice_ReadReady;
			this._cts = CancellationTokenSource.CreateLinkedTokenSource(token);
			try
			{
				this._serialDevice.Close();
				this._serialDevice.Open();
				var heartbeatTask = this.HeartBeatAsync(token);
				while (true)
				{
					token.ThrowIfCancellationRequested();
					try
					{
						if (!this._serialDevice.IsOpen)
						{
							this._serialDevice.Open();
						}
						if (this.State == PlayState.Idle)
						{
							this._logger.Info("Waiting for heartbeat");
						}
						if (this.State == PlayState.Starting)
						{
							State = PlayState.Playing;
							_ = this._videoPlayer.PlayAsync(this._videoSource, token);
						}
					}
					catch (OperationCanceledException)
					{
						throw;
					}
					catch (Exception ex)
					{
						this._logger.Error(ex.ToString());
						this._serialDevice.Close();
						await Task.Delay(5000, token);
					}
					await Task.Delay(1000, token);
				}
			}
			catch (OperationCanceledException)
			{
				// exit quietly
			}
			catch (Exception ex)
			{
				this._logger.Error(ex.ToString());
			}
			finally
			{
				this._serialDevice.ReadReady -= this._serialDevice_ReadReady;
				this._serialDevice.Close();
				this._cts = null;
			}
		}
	}
}
