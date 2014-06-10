using System;
using System.Collections.Generic;

namespace UnisensViewer
{
	public class FileNameEqualityComparer : IEqualityComparer<string>
	{
		public bool Equals(string x, string y)
		{
			return String.Compare(System.IO.Path.GetFileName(x), System.IO.Path.GetFileName(y), StringComparison.OrdinalIgnoreCase) == 0;
		}

		public int GetHashCode(string s)
		{
			return s.ToLowerInvariant().GetHashCode();
		}
	}
}
