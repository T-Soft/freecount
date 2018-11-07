using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Freecount.Helpers
{
	internal class ConsoleColorer : IDisposable
	{
		private readonly ConsoleColor _initialColor;
		public ConsoleColorer(ConsoleColor newColor)
		{
			_initialColor = Console.ForegroundColor;
			Console.ForegroundColor = newColor;
		}

		public void Dispose()
		{
			Console.ForegroundColor = _initialColor;
		}
	}
}
