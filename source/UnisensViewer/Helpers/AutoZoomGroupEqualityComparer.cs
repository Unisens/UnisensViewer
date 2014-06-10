using System;
using System.Collections;

namespace UnisensViewer
{
	public class AutoZoomGroupEqualityComparer : IEqualityComparer
	{
		public new bool Equals(object x, object y)
		{
			if (x is string && y is string)
			{
				return String.CompareOrdinal((string)x, (string)y) == 0;
			}
			else if (x is object[] && y is object[])
			{
				return x == y;
			}
			else
			{
				return false;
			}
		}

		public int GetHashCode(object obj)
		{
			return obj.GetHashCode();
		}
	}
}
