using System;
using System.Collections.Generic;
using Freecount.Email;
using Freecount.Helpers;

namespace Freecount.Checkers.Disk
{
	internal class DiskCheckResult : ResourceCheckResult
	{
		public override string CheckName { get; }
		private DiskCheckerSettings _settings;
		private double _driveSpace;
		
		public DiskCheckResult(DiskCheckerSettings settings, double driveSpace, bool isOk, bool wasErrorOnLastCheck)
		{
			_driveSpace = driveSpace;
			_settings = settings;
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

		public override IEnumerable<string> GetStatusReport()
		{
			var phrase = _settings.ThresholdType == ThresholdType.Free
				? $"Drive {_settings.DriveLetter} free space left: {_driveSpace:####.#} GB"
				: $"Drive {_settings.DriveLetter} used space: {_driveSpace:####.#} GB";
			return phrase.YieldSingle();
		}

		public override string GetEmailSubject(string template)
		{
			throw new NotImplementedException();
		}

		public override string GetEmailBody(string template)
		{
			throw new NotImplementedException();
		}

		public override void GetCommandLineParts(out string executableName, out string arguments)
		{
			throw new NotImplementedException();
		}
	}
}
