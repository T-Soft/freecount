using System;
using System.Collections.Generic;

namespace Freecount.Checkers.Certificate
{
	internal class CertificateCheckResult : ResourceCheckResult
	{
		private readonly CertificateCheckerSettings _settings;

		public CertificateCheckResult(
			string checkName,
			CertificateCheckerSettings settings,
			bool isOk,
			bool wasErrorOnLastCheck) : base(
			checkName,
			isOk,
			wasErrorOnLastCheck)
		{
			_settings = settings;
		}

		public override IEnumerable<string> GetStatusReport()
		{
			throw new NotImplementedException();
		}

		public override string GetEmailSubject(string template)
		{
			//<Subject>WARNING! Crtificate %cert_subject% expired!</Subject>
			return template.Replace("%cert_subject%", _settings.Certificate.Subject);
		}

		public override string GetEmailBody(string template)
		{
			//<Body>WARNING! Certificate %cert_subject% expired %expires_in% days ago</Body>
			return template
				.Replace("%cert_subject%", _settings.Certificate.Subject)
				.Replace(
					"%expires_in%",
					(DateTime.Now - _settings.Certificate.NotAfter).TotalDays.ToString("####.#").Replace(",", "."));

		}

		public override bool GetCommandLineParts(out string executableName, out string arguments)
		{
			// <Exec>cmd.exe ..\..\a.html type=cert subject=%cert_subject% expires=%expires_in%</Exec>
			(bool success, string excutableNameDefault, string argumentsDefault) =
				GetCommandLinePartsDefault(_settings);

			executableName = excutableNameDefault;
			arguments = argumentsDefault;

			if (!success)
			{
				return false;
			}

			arguments = argumentsDefault
				.Replace("%cert_subject%", _settings.Certificate.Subject)
				.Replace(
					"%expires_in%",
					(DateTime.Now - _settings.Certificate.NotAfter).TotalDays.ToString("####.#").Replace(",", "."));

			return true;
		}

	}
}
