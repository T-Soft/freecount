using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Freecount.Helpers
{
	public static class SpaceHelper
	{
		public static double BytesToGigabytes(double sizeInBytes)
		{
			return sizeInBytes / 1024.0 / 1024.0 / 1024.0;
		}
	}
}
