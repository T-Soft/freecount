using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Freecount.Checkers
{
	internal abstract class ResourceChecker
	{
		public abstract ResourceCheckResult Check();
		public abstract string ReportConfiguration();
		public abstract string Name { get; }
	}
}
