using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Freecount.Checkers;

namespace Freecount.Email
{
	internal class EmailNotifier
	{

		#region [PROPERTIES]

		public string Address { set; get; }
		public string Port { set; get; }
		public string Login { set; get; }
		public string Password { set; get; }
		public string SenderName { set; get; }
		public string SenderEmail { set; get; }
		public bool IsConfigured { set; get; }

		private readonly Dictionary<string, string> _admins;

		private readonly Dictionary<EventType, EmailTemplate> _emailTemplates;

		#endregion

		#region [CONSTRUCTOR]

		public EmailNotifier(XElement smtpServer, XElement adminsList, XElement templateList)
		{
			if (smtpServer == null)
			{
				throw new ArgumentNullException(nameof(smtpServer));
			}

			if (adminsList == null)
			{
				throw new ArgumentNullException(nameof(adminsList));
			}

			if (templateList == null)
			{
				throw new ArgumentNullException(nameof(templateList));
			}

			try
			{
				if (smtpServer.Attribute("isActive")?.Value != "1")
				{
					Console.WriteLine("Email notification offline!");
					return;
				}

				Address = smtpServer.Element("Address")?.Value;
				Port = smtpServer.Element("Port")?.Value;
				Login = smtpServer.Element("Login")?.Value;
				Password = smtpServer.Element("Password")?.Value;
				SenderName = smtpServer.Element("Sender")?.Element("Name")?.Value;
				SenderEmail = smtpServer.Element("Sender")?.Element("Email")?.Value;

				_admins = adminsList.Elements("Admin")
					.ToDictionary(
						elt => elt.Attribute("nick")?.Value,
						elt => elt.Value
					);
				_emailTemplates = new Dictionary<EventType, EmailTemplate>();

				_emailTemplates = templateList.Elements().ToDictionary(
					elt =>
					{
						if (Enum.TryParse(elt.Attribute("event")?.Value, true, out EventType et))
						{
							return et;
						}
						throw new Exception("Unknown event type!");
					},
					elt => new EmailTemplate(elt)
				);

				IsConfigured = true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Exception happened during EmailNotifier configuring. {Environment.NewLine}{ex}");
			}
		}

		#endregion

		#region [SEND MAIL]

		private string BuildMessage(
			string currentCounterName,
			decimal currentCounterValue,
			decimal thresholdValue,
			EventType et)
		{
			return _emailTemplates[et].Body
				.Replace("%nick%", currentCounterName)
				.Replace("%value%", currentCounterValue.ToString("####.#"))
				.Replace("%limit%", thresholdValue.ToString("####.#"));
		}

		private string BuildMessage(X509Certificate2 cert, int expiresIn, EventType et)
		{
			return _emailTemplates[et].Body
				.Replace("%cert_subject%", cert.SubjectName.Name)
				.Replace(
					"%expires_in%",
					expiresIn > 0
						? expiresIn.ToString()
						: (-expiresIn).ToString());
		}

		private string BuildSubject(string counterName, EventType et)
		{
			return _emailTemplates[et].Subject.Replace("%nick%", counterName).Replace("%cert_subject%", counterName);
		}


		public void SendMail(string counterName, decimal counterValue, decimal thresholdValue, EventType et)
		{
			//build message from template
			SendMail(
				_admins.Values.ToArray(),
				BuildSubject(counterName, et),
				BuildMessage(counterName, counterValue, thresholdValue, et)
			);
		}

		public void SendMail(X509Certificate2 cert, int expiresIn, EventType et)
		{
			//build message from template
			SendMail(
				_admins.Values.ToArray(),
				BuildSubject($"Certificate {cert.SubjectName.Name}", et),
				BuildMessage(cert, expiresIn, et)
			);
		}

		public void SendMail(ResourceCheckResult checkResult)
		{
			var subjectTmplate = _emailTemplates[checkResult.EventType].Subject;
			var bodytemplate = _emailTemplates[checkResult.EventType].Body;

			var subject = checkResult.GetEmailSubject(subjectTmplate);
			var body = checkResult.GetEmailBody(bodytemplate);

			try
			{
				using (MailMessage mail = new MailMessage
					{ From = new MailAddress(SenderEmail), Subject = subject, Body = body })
				{
					foreach (string email in _admins.Values.ToArray())
					{
						mail.To.Add(new MailAddress(email));
					}

					SmtpClient client = new SmtpClient
					{
						Host = Address,
						Port = int.Parse(Port),
						DeliveryMethod = SmtpDeliveryMethod.Network,
						Credentials = new NetworkCredential(Login, Password)
					};
					client.Send(mail);
				}
			}
			catch (Exception e)
			{
				throw new Exception($"[MAIL_SEND_FAILED] Message: {e.Message}");
			}
		}

		private void SendMail(string[] mailto, string subject, string message)
		{
			try
			{
				using (MailMessage mail = new MailMessage
					{From = new MailAddress(SenderEmail), Subject = subject, Body = message})
				{
					foreach (string email in mailto)
					{
						mail.To.Add(new MailAddress(email));
					}

					SmtpClient client = new SmtpClient
					{
						Host = Address,
						Port = Int32.Parse(Port),
						DeliveryMethod = SmtpDeliveryMethod.Network,
						Credentials = new NetworkCredential(Login, Password)
					};
					client.Send(mail);
				}
			}
			catch (Exception e)
			{
				throw new Exception($"[MAIL_SEND_FAILED] Message: {e.Message}");
			}
		}

		#endregion
	}
}
