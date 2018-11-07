using System.Xml.Linq;

namespace Freecount.Email
{
	class EmailTemplate
	{
		public string Subject;
		public string Body;

		public EmailTemplate(XElement templateNode)
		{
			Subject = templateNode.Element("Subject").Value;
			Body = templateNode.Element("Body").Value;
		}
	}
}