using System.Linq;

namespace Freecount.Checkers
{
	internal abstract class ResourceCheckerSettings
	{
		public string CommandLineToExecute { set; get; }
		public abstract string GetString { get; }
	}
}
