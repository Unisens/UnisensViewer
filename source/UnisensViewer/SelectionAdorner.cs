using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace UnisensViewer
{
	public class SelectionAdorner : Adorner
	{
		public static readonly DependencyProperty TimeProperty				= DependencyProperty.Register("Time", typeof(double), typeof(SelectionAdorner), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
		public static readonly DependencyProperty TimeStretchProperty		= DependencyProperty.Register("TimeStretch", typeof(double), typeof(SelectionAdorner), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
		public static readonly DependencyProperty SelectionStartProperty	= DependencyProperty.Register("SelectionStart", typeof(double), typeof(SelectionAdorner), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
		public static readonly DependencyProperty SelectionEndProperty		= DependencyProperty.Register("SelectionEnd", typeof(double), typeof(SelectionAdorner), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
		public static readonly DependencyProperty SelectionHeightProperty	= DependencyProperty.Register("SelectionHeight", typeof(double), typeof(SelectionAdorner), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

		public SelectionAdorner(UIElement adorneduie)
			: base(adorneduie)
		{
			IsHitTestVisible = false;
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

		protected override void OnRender(DrawingContext dc)
		{
			// irgendwas markiert?
			if (this.SelectionStart < this.SelectionEnd)
			{
				Rect c = new Rect(0.0, AdornedElement.RenderSize.Height, AdornedElement.RenderSize.Width, this.SelectionHeight);
				dc.PushClip(new RectangleGeometry(c));

				double w = this.AdornedElement.RenderSize.Width;
				double wt = w / RendererManager.TimeStretch;

				double xStart = (this.SelectionStart - RendererManager.Time) * wt;
				double xEnd = (this.SelectionEnd - RendererManager.Time) * wt;

				SolidColorBrush renderBrush = new SolidColorBrush(Colors.DeepSkyBlue);
				renderBrush.Opacity = 0.3;

				Rect r = new Rect(xStart, AdornedElement.RenderSize.Height, xEnd - xStart, this.SelectionHeight);
				dc.DrawRectangle(renderBrush, null, r);
			}
		}
	}
}