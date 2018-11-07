using System.Collections.Generic;
using Freecount.Email;

namespace Freecount.Checkers
{
	internal abstract class ResourceCheckResult
	{
		public bool IsOk { set; get; }
		public bool ShouldNotify { set; get; }
		
		public abstract string CheckName { get; }
		public EventType EventType { set; get; }

		public bool WasErrorDuringCheckerExecution { set; get; }
		public string CheckerExecutionErrorMessage { set; get; }


		public abstract IEnumerable<string> GetStatusReport();
		public abstract string GetEmailSubject(string template);
		public abstract string GetEmailBody(string template);
		public abstract void GetCommandLineParts(out string executableName, out string arguments);
	}
}
