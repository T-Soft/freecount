using System;

namespace Freecount.Checkers.Certificate
{
	internal class CertificateChecker : ResourceChecker
	{
		private readonly CertificateCheckerSettings _settings;

		public override string Name => $"Certificate {_settings.Certificate.SubjectName.Name} checker";

		public CertificateChecker(CertificateCheckerSettings settings)
		{
			_settings = settings;
		}

		public override ResourceCheckResult Check()
		{
			if (_settings.Certificate.NotAfter.AddDays(-_settings.DaysBeforeAlert)
				< DateTime.Now)
			{
				_wasWarning = true;
				return new CertificateCheckResult(Name, _settings, false, _wasWarning);
			}

			if (_wasWarning)
			{
				_wasWarning = false;
			}

			return new CertificateCheckResult(Name, _settings, true, _wasWarning);
		}

		public override string ReportConfiguration() => _settings.GetString;
	}
}
