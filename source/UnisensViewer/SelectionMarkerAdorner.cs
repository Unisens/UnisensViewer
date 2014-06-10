using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace UnisensViewer
{
	public class SelectionMarkerAdorner : Adorner
	{
		public static readonly DependencyProperty	TimeProperty			= DependencyProperty.Register("Time", typeof(double), typeof(SelectionMarkerAdorner), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange));
		public static readonly DependencyProperty	TimeStretchProperty		= DependencyProperty.Register("TimeStretch", typeof(double), typeof(SelectionMarkerAdorner), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange));
		public static readonly DependencyProperty	SelectionStartProperty	= DependencyProperty.Register("SelectionStart", typeof(double), typeof(SelectionMarkerAdorner), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange));
		public static readonly DependencyProperty	SelectionEndProperty	= DependencyProperty.Register("SelectionEnd", typeof(double), typeof(SelectionMarkerAdorner), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange));
		public static readonly DependencyProperty	SelectionHeightProperty	= DependencyProperty.Register("SelectionHeight", typeof(double), typeof(SelectionMarkerAdorner), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange));

		private VisualCollection visualChildren;
		private Thumb thumbStart;
		private Thumb thumbEnd;

		public SelectionMarkerAdorner(UIElement adorneduie, ControlTemplate thumbtemplate)
			: base(adorneduie)
		{
			this.visualChildren = new VisualCollection(this);
			this.Cursor = System.Windows.Input.Cursors.SizeWE;

			this.thumbStart = new Thumb();
			this.thumbEnd = new Thumb();

			this.thumbStart.Template = thumbtemplate;
			this.thumbEnd.Template = thumbtemplate;

			this.visualChildren.Add(this.thumbStart);
			this.visualChildren.Add(this.thumbEnd);

			this.thumbStart.DragDelta += new DragDeltaEventHandler(this.ThumbStart_DragDelta);
			this.thumbEnd.DragDelta += new DragDeltaEventHandler(this.ThumbEnd_DragDelta);

			this.thumbStart.DragCompleted += new DragCompletedEventHandler(this.ThumbDragCompleted);
			this.thumbEnd.DragCompleted += new DragCompletedEventHandler(this.ThumbDragCompleted);

			this.Visibility = System.Windows.Visibility.Hidden;
		}

		public double Time
		{
			get { return (double)GetValue(TimeProperty); }
			set { SetValue(TimeProperty, value); }
		}

		public double TimeStretch
		{
			get { return (double)GetValue(TimeStretchProperty); }
			set { SetValue(TimeStretchProperty, value); }
		}

		public double SelectionStart
		{
			get { return (double)GetValue(SelectionStartProperty); }
			set { SetValue(SelectionStartProperty, value); }
		}

		public double SelectionEnd
		{
			get { return (double)GetValue(SelectionEndProperty); }
			set { SetValue(SelectionEndProperty, value); }
		}

		public double SelectionHeight
		{
			get { return (double)GetValue(SelectionHeightProperty); }
			set { SetValue(SelectionHeightProperty, value); }
		}

		protected override int VisualChildrenCount
		{
			get { return this.visualChildren.Count; }
		}

		protected override Visual GetVisualChild(int index)
		{
			return this.visualChildren[index];
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			Rect r = new Rect(0.0, this.AdornedElement.RenderSize.Height, this.AdornedElement.RenderSize.Width, this.SelectionHeight);
			Clip = new RectangleGeometry(r);

			double wt = this.AdornedElement.RenderSize.Width / this.TimeStretch;
			double xs = (this.SelectionStart - this.Time) * wt;
			double xe = (this.SelectionEnd - this.Time) * wt;

			this.thumbStart.Arrange(new Rect(xs - 1, this.AdornedElement.RenderSize.Height, 3.0, this.SelectionHeight));
			this.thumbEnd.Arrange(new Rect(xe - 1, this.AdornedElement.RenderSize.Height, 3.0, this.SelectionHeight));

			return finalSize;
		}

		private void ThumbDragCompleted(object sender, DragCompletedEventArgs e)
		{
			this.Visibility = this.SelectionStart < this.SelectionEnd ? Visibility.Visible : Visibility.Hidden;
		}

		private void ThumbStart_DragDelta(object sender, DragDeltaEventArgs e)
		{
			Point p = Mouse.GetPosition(this.AdornedElement);

			if (p.X < 0.0)
			{
				RendererManager.Scroll(RendererManager.Time - (RendererManager.TimeStretch * 0.05));
			}
			else if (p.X > AdornedElement.RenderSize.Width)
			{
				RendererManager.Scroll(RendererManager.Time + (RendererManager.TimeStretch * 0.05));
			}

			double t = this.Time + (this.TimeStretch * p.X / this.AdornedElement.RenderSize.Width);
			this.SelectionStart = t >= 0.0 ? t : 0.0;
		}

		private void ThumbEnd_DragDelta(object sender, DragDeltaEventArgs e)
		{
			Point p = Mouse.GetPosition(this.AdornedElement);

			if (p.X < 0.0)
			{
				RendererManager.Scroll(RendererManager.Time - (RendererManager.TimeStretch * 0.05));
			}
			else if (p.X > this.AdornedElement.RenderSize.Width)
			{
				RendererManager.Scroll(RendererManager.Time + (RendererManager.TimeStretch * 0.05));
			}

			double t = this.Time + (this.TimeStretch * p.X / this.AdornedElement.RenderSize.Width);
			this.SelectionEnd = t >= 0.0 ? t : 0.0;
		}
	}
}
