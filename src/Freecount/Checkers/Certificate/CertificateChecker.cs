using System;

namespace Freecount.Checkers.Certificate
{
	internal class CertificateChecker : ResourceChecker
	{
		private readonly CertificateCheckerSettings _settings;

		public CertificateChecker(CertificateCheckerSettings settings)
		{
			_settings = settings;
		}

		public override ResourceCheckResult Check()
		{
			throw new NotImplementedException();
		}

		public override string ReportConfiguration() => _settings.GetString;

		public override string Name => $"Certificate {_settings.Certificate.SubjectName.Name} checker";
	}
}
