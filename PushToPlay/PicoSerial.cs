using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PushToPlay
{
	/// <summary>
	/// Serial device for communications with a Raspberri Pi Pico board via the onboard USB.
	/// </summary>
	class PicoSerial : ISerialDevice
	{
		private readonly SerialPort _serial;
		private readonly SemaphoreSlim _writeSemaphore;
		private DateTime _nextWrite;
		private TimeSpan _nextWriteDelay;

		public bool IsOpen => this._serial.IsOpen;

		public event EventHandler ReadReady;

		/// <summary>Creates a new serial port for a Raspberri Pi Pico board communication via USB.</summary>
		/// <param name="port">The port number.</param>
		public PicoSerial(string port)
		{
			this._serial = new SerialPort(port, 115200, Parity.None, 8, StopBits.One)
			{
				DtrEnable = true,
				WriteTimeout = 5000
			};
			this._writeSemaphore = new SemaphoreSlim(1);
			this._serial.DataReceived += this._serial_DataReceived;
			this._nextWrite = DateTime.Now;
			this._nextWriteDelay = TimeSpan.FromSeconds(0.5);
		}

		/// <summary>Handles when data is received from the serial device.</summary>
		/// <param name="sender">Serial port.</param>
		/// <param name="e">Event arguments.</param>
		private void _serial_DataReceived(object sender, SerialDataReceivedEventArgs e) => this.OnReadReady();

		/// <summary>Raises the <see cref="ReadReady"/> event.</summary>
		private void OnReadReady() => this.ReadReady?.Invoke(this, EventArgs.Empty);

		public void Open() => this._serial.Open();

		public void Close() => this._serial.Close();

		public string ReadLine()
		{
			if (this._serial.BytesToRead == 0)
				return null;
			return this._serial.ReadLine();
		}

		public async Task<bool> WriteLineAsync(string text, CancellationToken token)
		{
			await this._writeSemaphore.WaitAsync(token);
			try
			{
				await this.WaitUntilNextWrite(token);
				this._serial.WriteLine(text);
				return true;
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				if (this._serial.IsOpen)
				{
					this._serial.Close();
					this._serial.Open();
				}
				return false;
			}
			finally
			{
				this._writeSemaphore.Release();
			}
		}

		/// <summary>Waits until the delay between writes is approximately observed.</summary>
		/// <param name="token">A cancellation token.</param>
		private async Task WaitUntilNextWrite(CancellationToken token)
		{
			var now = DateTime.Now;
			if (this._nextWrite > now)
				await Task.Delay(this._nextWrite - now);
			this._nextWrite = DateTime.Now + this._nextWriteDelay;
		}
	}
}
