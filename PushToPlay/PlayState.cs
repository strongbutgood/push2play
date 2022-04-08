using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushToPlay
{
	enum PlayState
	{
		Idle = 0,
		Connected,
		Starting,
		Playing,
		PlayingAcknowledged,
		Ending
	}
}
