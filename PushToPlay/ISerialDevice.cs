using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PushToPlay
{
	/// <summary>
	/// Provides communication with a remote device.
	/// </summary>
	interface ISerialDevice
	{
		/// <summary>Gets if the serial device is open for communication.</summary>
		bool IsOpen { get; }
		/// <summary>Raised when the serial device has data ready for reading.</summary>
		event EventHandler ReadReady;
		/// <summary>Opens the serial device for communication.</summary>
		void Open();
		/// <summary>Closes the serial device.</summary>
		void Close();
		/// <summary>Reads a line from the serial device.</summary>
		/// <returns>A line of text.</returns>
		string ReadLine();
		/// <summary>Writes the <paramref name="text"/> and the newline character to the serial device.</summary>
		/// <param name="text">The text to write.</param>
		/// <param name="token">A cancellation token.</param>
		/// <returns>True if the write was successful, otherwise false.</returns>
		Task<bool> WriteLineAsync(string text, CancellationToken token);
	}
}
