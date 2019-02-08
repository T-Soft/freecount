using System;
using System.Collections.Generic;
using Freecount.Helpers;

namespace Freecount.Checkers.Ram
{
	internal class RamCheckResult : ResourceCheckResult
	{
		private readonly RamCheckerSettings _settings;
		private readonly double _freeMemoryGb;
		private readonly double _usedMemoryGb;

		public RamCheckResult(
			string checkName,
			RamCheckerSettings settings,
			double freeMemoryGb,
			double usedMemoryGb,
			bool isOk,
			bool wasErrorOnLastCheck) : base(
			checkName,
			isOk,
			wasErrorOnLastCheck)
		{
			_settings = settings;
			_freeMemoryGb = freeMemoryGb;
			_usedMemoryGb = usedMemoryGb;
		}

		public override IEnumerable<string> GetStatusReport()
		{
			var phrase = _settings.ThresholdType == ThresholdType.Free
				? $"Virtual memory free : {_freeMemoryGb} / {_settings.CriticalThreshold} GB"
				: $"Virtual memory used : {_usedMemoryGb} / {_settings.CriticalThreshold} GB";
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
				.Replace(
					"%value%",
					_settings.ThresholdType == ThresholdType.Free
						? _freeMemoryGb.ToString("####.#").Replace(",", ".")
						: _usedMemoryGb.ToString("####.#").Replace(",", "."))
				.Replace("%limit%", _settings.CriticalThreshold.ToString("####.#").Replace(",", "."))
				.Replace("%nick%", CheckName);
		}

		public override bool GetCommandLineParts(out string executableName, out string arguments)
		{
			//<Exec>cmd.exe ..\..\a.html type=ram value=%value% limit=%limit%</Exec>
			(bool success, string excutableNameDefault, string argumentsDefault) =
				GetCommandLinePartsDefault(_settings);

			executableName = excutableNameDefault;
			arguments = argumentsDefault;

			if (!success)
			{
				return false;
			}

			arguments = argumentsDefault
				.Replace(
					"%value%",
					_settings.ThresholdType == ThresholdType.Free
						? _freeMemoryGb.ToString("####.#").Replace(",", ".")
						: _usedMemoryGb.ToString("####.#").Replace(",", ".")
				)
				.Replace("%limit%", _settings.CriticalThreshold.ToString("####.#").Replace(",", "."))
				.Replace("%type%", _settings.ThresholdType.ToString().ToLower());

			return true;
		}
	}
}
