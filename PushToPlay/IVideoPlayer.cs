using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PushToPlay
{
	interface IVideoPlayer
	{
		/// <summary>Raised when the player has started the video.</summary>
		event EventHandler Started;
		/// <summary>Raised when the player has finished the video.</summary>
		event EventHandler Finished;
		/// <summary>Plays a video according to the specified <paramref name="source"/>.</summary>
		/// <param name="source">The filename of the video to play.</param>
		/// <param name="token">A cancellation token.</param>
		Task PlayAsync(string source, CancellationToken token);
	}
}
