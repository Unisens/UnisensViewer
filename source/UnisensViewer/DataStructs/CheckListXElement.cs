using System.ComponentModel;
using System.Xml.Linq;

namespace UnisensViewer
{
	public class CheckListXElement : INotifyPropertyChanged
	{
		private XElement xe;
		private bool ischecked;
		
		public CheckListXElement(XElement xe)
		{
			this.xe = xe;
			this.ischecked = false;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public XElement Xe
		{
			get
			{
				return this.xe;
			}

			set 
			{
				if (this.xe != value)
				{
					this.xe = value;
					if (this.PropertyChanged != null)
                    {
						this.PropertyChanged(this, new PropertyChangedEventArgs("Xe"));
                    }
				}
			}
		}

		public bool IsChecked
		{
			get
			{
				return this.ischecked;
			}

			set
			{
				if (this.ischecked != value)
				{
					this.ischecked = value;
					if (this.PropertyChanged != null)
					{
						this.PropertyChanged(this, new PropertyChangedEventArgs("IsChecked"));
					}
				}
			}
		}
	}
}
