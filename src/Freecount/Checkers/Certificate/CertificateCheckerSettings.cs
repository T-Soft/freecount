using System;
using System.Security.Cryptography.X509Certificates;

namespace Freecount.Checkers.Certificate
{
	internal class CertificateCheckerSettings : ResourceCheckerSettings
	{
		/*<Obj type="cert" daysBeforeAlert="30">
			<!--<Store></Store>-->
			<Thumbprint>02 fa f3 e2 91 43 54 68 60 78 57 69 4d f5 e4 5b 68 85 18 68</Thumbprint>
			<Exec>cmd.exe ..\..\a.html type=cert subject=%cert_subject% expires=%expires_in%</Exec>
		</Obj>*/

		public int DaysBeforeAlert { set; get; }
		public string Thumbprint { set; get; }
		public string CertificateStore { set; get; }
		public X509Certificate2 Certificate { set; get; }
		public override string GetString => $"{Certificate.SubjectName.Name}{Environment.NewLine}"
			+ $"\tDays before alert {DaysBeforeAlert}.{Environment.NewLine}"
			+ $"\tScript string {CommandLineToExecute}";
	}
}
