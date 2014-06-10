using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace UnisensViewer
{
	public class SettingBinding : Binding
	{
		public SettingBinding()
		{
			this.Initialize();
		}

		public SettingBinding(string path)
			: base(path)
		{
			this.Initialize();
		}

		public SettingBinding(string path, IValueConverter converter)
			: base(path)
		{
			this.Initialize();
			this.Converter = converter;
		}

		private void Initialize()
		{
			this.Source = UnisensViewer.Properties.Settings.Default;
			this.Mode = BindingMode.TwoWay;
		}
	}
}
