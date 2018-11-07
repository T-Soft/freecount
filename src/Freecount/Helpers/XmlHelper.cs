using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.SqlServer.Server;

namespace Freecount.Helpers
{
	public static class XmlHelper
	{
		public static IEnumerable<XElement> GetAllElements(this XDocument doc)
		{
			return doc?.Root?.Descendants();
		}

		public static XElement GetChildElementByName(this XDocument target, string name)
		{
			return target.GetAllElements().ToList().FirstOrDefault(elt => elt.Name == name);
		}

		public static XElement GetChildElementByName(this XElement target, string name)
		{
			return target.Descendants().ToList().FirstOrDefault(elt => elt.Name == name);
		}

		public static IEnumerable<XElement> GetChildElementsByAttributeValue(this XDocument target, string attributeName, string value)
		{
			return target.GetAllElements().ToList()
				.Where(elt => elt.Attributes(attributeName).Any(a => a.Value == value));
		}

		public static string GetChildElementValue(this XDocument target, string name, string defaultValue = null)
		{
			return GetChildElementByName(target, name)?.Value ?? defaultValue;
		}

		public static string GetAttributeValue(this XElement target, string name, string defaultValue = null)
		{
			return target.Attributes(name).FirstOrDefault()?.Value ?? defaultValue;
		}

		public static string GetChildElementValue(this XElement target, string name, string defaultValue = null)
		{
			return GetChildElementByName(target, name)?.Value ?? defaultValue;
		}
	}
}
