using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PushToPlay
{
	class PicoSerial : ISerialDevice
	{
		private readonly SerialPort _serial;
		private readonly SemaphoreSlim _writeSemaphore;
		private DateTime _nextWrite;

		public bool IsOpen => this._serial.IsOpen;

		public event EventHandler ReadReady;

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
		}

		private void _serial_DataReceived(object sender, SerialDataReceivedEventArgs e) => this.OnReadReady();

		private void OnReadReady() => this.ReadReady?.Invoke(this, EventArgs.Empty);

		public void Open() => this._serial.Open();

		public void Close() => this._serial.Close();

		public string ReadLine()
		{
			if (this._serial.BytesToRead == 0)
				return null;
			return this._serial.ReadLine();
		}

		public async Task<bool> WriteLine(string text, CancellationToken token)
		{
			await this._writeSemaphore.WaitAsync(token);
			try
			{
				if (this._nextWrite > DateTime.Now)
					await Task.Delay(this._nextWrite - DateTime.Now);
				this._serial.WriteLine(text);
				this._nextWrite = DateTime.Now + TimeSpan.FromSeconds(0.5);
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
	}
}
