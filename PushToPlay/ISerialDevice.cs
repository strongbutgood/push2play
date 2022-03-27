using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PushToPlay
{
	interface ISerialDevice
	{
		bool IsOpen { get; }
		event EventHandler ReadReady;
		void Open();
		void Close();
		string ReadLine();
		Task<bool> WriteLine(string text, CancellationToken token);
	}
}
