using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Freecount.Helpers
{
	public static class EnumerableHelper
	{
		public static IEnumerable<T> YieldSingle<T>(this T target)
		{
			yield return target;
		}

		public static List<T> YieldSingleList<T>(this T target)
		{
			if (target == null)
			{
				throw new ArgumentNullException(nameof(target));
			}

			return target.YieldSingle().ToList();
		}
	}
}
