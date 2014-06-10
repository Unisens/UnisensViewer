using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace UnisensViewer
{
	public struct HotkeyHashkey
	{
		public Key				Key;
		public ModifierKeys		Modifiers;

		public HotkeyHashkey(Key k, ModifierKeys m)
		{
			this.Key = k;
			this.Modifiers = m;
		}

		public override int GetHashCode()
		{
            return ((int)this.Modifiers << 8) ^ (int)this.Key;
		}
	}
}