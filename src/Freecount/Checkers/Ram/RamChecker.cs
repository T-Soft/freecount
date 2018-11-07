using System;

namespace Freecount.Checkers.Ram
{
	internal class RamChecker : ResourceChecker
	{
		private readonly RamCheckerSettings _settings;

		public RamChecker(RamCheckerSettings settings)
		{
			_settings = settings;
		}

		public override ResourceCheckResult Check()
		{
			throw new NotImplementedException();
		}

		public override string ReportConfiguration() => _settings.GetString;

		public override string Name => "Virtual memory checker";
	}
}
