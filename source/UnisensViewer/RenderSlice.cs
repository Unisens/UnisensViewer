using System;
using System.ComponentModel;
using System.Windows.Media;
using System.Xml.Linq;

namespace UnisensViewer
{
    /// <summary>
    /// Every signal channel, value channel or event entry has its own RenderSlice. Every RenderSlice has its own bitmap.
    /// </summary>
	public abstract class RenderSlice : INotifyPropertyChanged
	{
		public readonly Renderer	Renderer;

		public ZoomInfo Zoominfo;

		// kann auch negativ sein, zum umpolen des signals
		protected float scale;
		protected float offset;
		protected ImageSource imagesource;
		public uint color;
		
		private readonly int channel;

		private string name;
		private double range;
		private string unit;
		private XElement unisensnode;


        /*public int CompareTo(object obj)
        {
            return PosY < ((RenderSlice)obj).PosY ? -1 : 1;
        }*/

		public RenderSlice(Renderer renderer, int channel, string name, uint color, string unit, XElement unisensnode)
		{
			this.Renderer = renderer;
			this.channel = channel;
			this.name = name;
			this.color = color;
			this.unit = unit;
			this.unisensnode = unisensnode;
		}

		public event EventHandler Kill;

		public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public abstract int ImageWidth { get; set; }

		public XElement UnisensNode
		{
			get { return this.unisensnode; }
		}

		public string Name
		{
			get { return this.name; }
		}

		public uint Color
		{
			get { return this.color; }
		}

		public ImageSource ImageSource
		{
			get { return this.imagesource; }
            set
            {
                if (this.imagesource != value)
                {
                    this.imagesource = value;
                    OnPropertyChanged("ImageSource"); 
                }
            }
		}

		public string Unit
		{
			get { return this.unit; }
		}

		public float Offset
		{
			get
			{
                return this.offset;
			}

			set
			{
                if (this.offset != value)
                {
                    this.offset = value;
                    OnPropertyChanged("Offset");
                }
			}
		}

		public float Scale
		{
			get 
			{
                return this.scale; 
			}

			set
			{
                if (this.scale != value)
				{
                    this.scale = value;
					this.UpdateRange(value);
				}
			}
		}
		    
		public double Range
		{
			get
			{
                return this.range;
			}

			set
			{
                if (this.range != value)
				{
                    this.range = value;
					OnPropertyChanged("Range");
				}
			}
		}		

		public void RaiseKill()
		{
			this.Kill(this, null);
            //this.Renderer.Close(); // Free Memory (Removed because of a bug when closing one channel of e.g. ACC)
		}

		public abstract void ZoomInto(float min, float max);

		public SampleInfo GetSampleInfo(double time, double time_end)
		{
			return this.Renderer.GetSampleInfo(this.channel, time, time_end);
		}

		protected abstract void UpdateRange(double scale);
	

    }
}
