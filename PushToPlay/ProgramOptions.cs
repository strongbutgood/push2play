using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushToPlay
{
	class ProgramOptions : Nito.OptionParsing.CommandLineOptionsBase
	{
		[Nito.OptionParsing.Option("source", 's', Nito.OptionParsing.OptionArgument.Required)]
		public string Source { get; set; }

		[Nito.OptionParsing.Option("port", 'p', Nito.OptionParsing.OptionArgument.Required)]
		public string Port { get; set; } = "COM6";
	}
}
