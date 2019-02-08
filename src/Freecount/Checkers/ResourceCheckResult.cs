using System.Collections.Generic;
using Freecount.Email;

namespace Freecount.Checkers
{
	internal abstract class ResourceCheckResult
	{
		public bool IsOk { get; }
		public bool ShouldNotify { get; }
		
		protected string CheckName { get; }

		public EventType EventType { set; get; }

		public bool WasErrorDuringCheckerExecution { set; get; }
		public string CheckerExecutionErrorMessage { set; get; }
		
		public abstract IEnumerable<string> GetStatusReport();
		public abstract string GetEmailSubject(string template);
		public abstract string GetEmailBody(string template);

		public abstract bool GetCommandLineParts(out string executableName, out string arguments);

		protected ResourceCheckResult(string checkName, bool isOk, bool wasErrorOnLastCheck)
		{
			CheckName = checkName;
			IsOk = isOk;
			if (!isOk && wasErrorOnLastCheck)
			{
				ShouldNotify = false;
			}
			else
			{
				ShouldNotify = true;
				EventType = isOk
					? EventType.Ok
					: EventType.Warning;
			}
		}

		protected (bool success, string executableName, string arguments) GetCommandLinePartsDefault(ResourceCheckerSettings settings)
		{
			if (settings == null)
			{
				return (false, string.Empty, string.Empty);
			}

			var commandLine = settings.CommandLineToExecute.Trim();
			if (string.IsNullOrEmpty(commandLine))
			{
				return (false, string.Empty, string.Empty);
			}

			var firstSpaceIndex = commandLine.IndexOf(' ');

			if (firstSpaceIndex == -1)
			{
				return (false, string.Empty, string.Empty);
			}

			return (true, commandLine.Substring(0, firstSpaceIndex), commandLine.Substring(firstSpaceIndex + 2));
		}
	}
}
