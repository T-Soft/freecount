using System;
using System.Collections.Generic;

namespace Freecount.Checkers.Certificate
{
	internal class CertificateCheckResult : ResourceCheckResult
	{
		public override string CheckName { get; }

		public override List<string> GetStatusReport()
		{
			throw new NotImplementedException();
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
