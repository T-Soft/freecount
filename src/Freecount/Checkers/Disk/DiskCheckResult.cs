using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using Freecount.Email;
using Freecount.Helpers;

namespace Freecount.Checkers.Disk
{
	internal class DiskCheckResult : ResourceCheckResult
	{
		private readonly DiskCheckerSettings _settings;
		private readonly double _driveSpace;

		public DiskCheckResult(
			string checkerName,
			DiskCheckerSettings settings,
			double driveSpace,
			bool isOk,
			bool wasErrorOnLastCheck) : base(checkerName, isOk, wasErrorOnLastCheck)
		{
			_driveSpace = driveSpace;
			_settings = settings;
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
			//<Subject>WARNING! %nick% state critical!</Subject>
			return template.Replace("%nick%", CheckName);
		}

		public override string GetEmailBody(string template)
		{
			//<Body>WARNING! Counter %nick% value [%value% GB] is below threshold of [%limit% GB]</Body>
			return template
				.Replace("%value%", _driveSpace.ToString("####.#").Replace(",", "."))
				.Replace("%limit%", _settings.CriticalThreshold.ToString("####.#").Replace(",", "."))
				.Replace("%nick%", CheckName);
		}

		public override bool GetCommandLineParts(out string executableName, out string arguments)
		{
			//cmd.exe ..\..\a.html type=dskD value=%value% limit=%limit%
			(bool success, string excutableNameDefault, string argumentsDefault) =
				GetCommandLinePartsDefault(_settings);

			executableName = excutableNameDefault;
			arguments = argumentsDefault;

			if (!success)
			{
				return false;
			}

			arguments = argumentsDefault
				.Replace("%value%", _driveSpace.ToString("####.#").Replace(",", "."))
				.Replace("%limit%", _settings.CriticalThreshold.ToString("####.#").Replace(",", "."))
				.Replace("%type%", _settings.ThresholdType.ToString().ToLower());

			return true;
		}
	}
}
