using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace Freecount.Helpers
{
	internal static class CertificateHelper
	{
		public static X509Certificate2 SearchCertificateByThumbprint(string certificateThumbprint)
		{
			certificateThumbprint = Regex.Replace(certificateThumbprint, @"[^\da-zA-z]", string.Empty).ToUpper();
			X509Store compStore =
				new X509Store("My", StoreLocation.LocalMachine);
			compStore.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);

			X509Store store =
				new X509Store("My", StoreLocation.CurrentUser);
			store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);

			X509Certificate2Collection found =
				compStore.Certificates.Find(
					X509FindType.FindByThumbprint,
					certificateThumbprint,
					false
				);

			if (found.Count == 0)
			{
				found = store.Certificates.Find(
					X509FindType.FindByThumbprint,
					certificateThumbprint,
					false
				);
				if (found.Count != 0)
				{
					// means found in Current User store
				}
				else
				{
					Console.WriteLine($"Certificate with thumbprint {certificateThumbprint} not found!");
					return null;
				}
			}
			else
			{
				// means found in LocalMachine store
			}

			if (found.Count == 1)
			{
				return found[0];
			}
			else
			{
				Console.WriteLine($"More than one certificate with thumbprint {certificateThumbprint} found!");
				return null;
			}
		}
	}
}
