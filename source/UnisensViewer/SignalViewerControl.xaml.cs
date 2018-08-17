using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;

namespace UnisensViewer
{
	public partial class SignalViewerControl : UserControl
	{
		public static readonly DependencyProperty	SelectionStartProperty	= DependencyProperty.Register("SelectionStart", typeof(double), typeof(SignalViewerControl));
		public static readonly DependencyProperty	SelectionEndProperty	= DependencyProperty.Register("SelectionEnd", typeof(double), typeof(SignalViewerControl));
		public static readonly RoutedEvent			BeginSelectTimeEvent	= EventManager.RegisterRoutedEvent("BeginSelectTime", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SignalViewerControl));
		public static readonly RoutedEvent			HoverRenderSliceEvent	= EventManager.RegisterRoutedEvent("HoverRenderSlice", RoutingStrategy.Bubble, typeof(HoverRenderSliceEventHandler), typeof(SignalViewerControl));
		
		public static readonly RoutedCommand CmdAutoZoomAllIndividual = new RoutedCommand();
		public static readonly RoutedCommand CmdAutoZoomAllGroupedByFiles = new RoutedCommand();
		public static readonly RoutedCommand CmdAutoZoomAllGroupedByUnits = new RoutedCommand();
		public static readonly RoutedCommand CmdChangeTimeZoom = new RoutedCommand();
        public static readonly RoutedCommand CmdChangeTimeTest = new RoutedCommand();

		private Point dragpoint;
		private bool dragwatcherinstalled;
		private double dragtime;
		private double dragtimestretch;

		private AdornerLayer adornerlayer;
		private SelectionAdorner selectionadorner;
		private SelectionMarkerAdorner	selectionmarkeradorner;

		public SignalViewerControl()
		{	
			InitializeComponent();

			////statusbar.DataContext = this;
			this.scrollbar.Maximum = RendererManager.TimeMax - (RendererManager.TimeStretch * 0.5);	// 0.5 = noch bis zur hälfte am ende weiterscrollbar
			this.scrollbar.ViewportSize = RendererManager.TimeStretch;

			RendererManager.TimeChanged += new EventHandler(this.RendererManager_TimeChanged);
			RendererManager.TimeMaxChanged += new EventHandler(this.RendererManager_TimeMaxChanged);
			RendererManager.TimeStretchChanged += new EventHandler(this.RendererManager_TimeStretchChanged);

			this.axiscontrol_time.Offset = RendererManager.Time;
			this.axiscontrol_time.Range = RendererManager.TimeStretch;

			this.BeginSelectTime += new RoutedEventHandler(this.SignalViewerControl_BeginSelectTime);
			this.HoverRenderSlice += new HoverRenderSliceEventHandler(this.SignalViewerControl_HoverRenderSlice);

			ControlTemplate ct = (ControlTemplate)FindResource("controltemplate_selectionmarker");

			this.selectionadorner = new SelectionAdorner(axiscontrol_time);
			this.selectionmarkeradorner = new SelectionMarkerAdorner(axiscontrol_time, ct);

			this.RendererManager_TimeChanged(null, null);
			this.RendererManager_TimeStretchChanged(null, null);

			Binding b1 = new Binding("ActualHeight");
			b1.Source = stackercontrol;
			this.selectionmarkeradorner.SetBinding(SelectionMarkerAdorner.SelectionHeightProperty, b1);

			Binding b2 = new Binding("ActualHeight");
			b2.Source = stackercontrol;
			this.selectionadorner.SetBinding(SelectionAdorner.SelectionHeightProperty, b2);

			Binding bs = new Binding("SelectionStart");
			bs.Mode = BindingMode.TwoWay;
			bs.Source = this;
			this.selectionmarkeradorner.SetBinding(SelectionMarkerAdorner.SelectionStartProperty, bs);

			Binding be = new Binding("SelectionEnd");
			be.Mode = BindingMode.TwoWay;
			be.Source = this;
			this.selectionmarkeradorner.SetBinding(SelectionMarkerAdorner.SelectionEndProperty, be);

			Binding b3 = new Binding("SelectionStart");
			b3.Source = this;
			this.selectionadorner.SetBinding(SelectionAdorner.SelectionStartProperty, b3);

			Binding b4 = new Binding("SelectionEnd");
			b4.Source = this;
			this.selectionadorner.SetBinding(SelectionAdorner.SelectionEndProperty, b4);
			////SizeChanged += new SizeChangedEventHandler(SignalViewerControl_SizeChanged);
		}

		public delegate void HoverRenderSliceEventHandler(object sender, HoverRenderSliceEventArgs e);

		public event RoutedEventHandler BeginSelectTime
		{
			add { AddHandler(BeginSelectTimeEvent, value); }
			remove { RemoveHandler(BeginSelectTimeEvent, value); }
		}

		public event HoverRenderSliceEventHandler HoverRenderSlice
		{
			add { AddHandler(HoverRenderSliceEvent, value); }
			remove { RemoveHandler(HoverRenderSliceEvent, value); }
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

		public void CloseAllSignals()
		{
			stackercontrol.CloseAllSignals();
		}

		public void Deselect()
		{
			this.SelectionStart = 0.0;
			this.SelectionEnd = 0.0;
			this.selectionmarkeradorner.Visibility = Visibility.Hidden;
		}

		private void SignalViewerControl_HoverRenderSlice(object sender, HoverRenderSliceEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine(e.RenderSlice.Name);
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			this.adornerlayer = AdornerLayer.GetAdornerLayer(this);
			this.adornerlayer.Add(this.selectionadorner);
			this.adornerlayer.Add(this.selectionmarkeradorner);
		}

		private void RendererManager_TimeChanged(object sender, EventArgs e)
		{
			double t = RendererManager.Time;

			// dies löst kein scroll-event aus!
			this.scrollbar.Value = t;

			this.axiscontrol_time.Offset = t;

			this.selectionadorner.Time = t;
			this.selectionmarkeradorner.Time = t;
		}

		private void RendererManager_TimeMaxChanged(object sender, EventArgs e)
		{
			this.scrollbar.Maximum = RendererManager.TimeMax - (RendererManager.TimeStretch * 0.5);
		}

		private void RendererManager_TimeStretchChanged(object sender, EventArgs e)
		{
			double ts = RendererManager.TimeStretch;

			this.scrollbar.Maximum = RendererManager.TimeMax - (ts * 0.5);
			this.scrollbar.ViewportSize = ts;

			this.axiscontrol_time.Range = ts;

			this.selectionadorner.TimeStretch = ts;
			this.selectionmarkeradorner.TimeStretch = ts;
		}

		private void Scrollbar_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
		{
			////if (e.ScrollEventType != System.Windows.Controls.Primitives.ScrollEventType.ThumbTrack)
			RendererManager.Scroll(e.NewValue);
		}

		private void SignalViewerControl_BeginSelectTime(object sender, RoutedEventArgs e)
		{
			this.selectionmarkeradorner.Visibility = System.Windows.Visibility.Hidden;

			if (!this.dragwatcherinstalled)
			{
				this.dragpoint = Mouse.GetPosition(axiscontrol_time);

				this.dragtime = RendererManager.Time + (RendererManager.TimeStretch * this.dragpoint.X / this.axiscontrol_time.ActualWidth);
				if (this.dragtime < 0.0)
				{
					this.dragtime = 0.0;
				}

				this.dragwatcherinstalled = true;
				this.PreviewMouseMove += new MouseEventHandler(this.PreviewMouseMove_selection);
				e.Handled = true;
				Mouse.Capture(this);
			}
		}

		private void PreviewMouseDown_scrollstretch(object sender, MouseButtonEventArgs e)
		{
			// time-scroll/stretch
			if (!this.dragwatcherinstalled &&
				((e.LeftButton == MouseButtonState.Pressed && Keyboard.Modifiers == ModifierKeys.Alt) ||
					(e.MiddleButton == MouseButtonState.Pressed && (Keyboard.Modifiers == ModifierKeys.None || Keyboard.Modifiers == ModifierKeys.Alt))))
			{
				// koordinaten in bezug auf axiscontrol_time - das hat (sollte haben) die gleiche
				// breite wie die angezeigten signale
				this.dragpoint = e.GetPosition(axiscontrol_time);

				this.dragtime = RendererManager.Time + (RendererManager.TimeStretch * this.dragpoint.X / this.axiscontrol_time.ActualWidth);
				this.dragtimestretch = RendererManager.TimeStretch;

				this.dragwatcherinstalled = true;
				this.PreviewMouseMove += new MouseEventHandler(this.PreviewMouseMove_scrollstretch);
				e.Handled = true;
				Mouse.Capture(this);
			}
		}

		private void PreviewMouseMove_scrollstretch(object sender, MouseEventArgs e)
		{
			Point p = e.GetPosition(axiscontrol_time);
			double deltax = p.X - this.dragpoint.X;
			double deltay = p.Y - this.dragpoint.Y;

			if (e.LeftButton == MouseButtonState.Pressed || e.MiddleButton == MouseButtonState.Pressed)
			{	
				// erst stretchen, dann scrollen!
				////RendererManager.Stretch(dragtimestretch - 0.05 * deltay);
				////RendererManager.Stretch(dragtimestretch - deltay * 0.001 * RendererManager.TimeMax);
				RendererManager.Stretch(this.dragtimestretch - (deltay * 0.005 * this.dragtimestretch));

				RendererManager.Scroll(this.dragtime - (RendererManager.TimeStretch * p.X / axiscontrol_time.ActualWidth));
			}
			else
			{
				// nix mehr gedrückt, handler deinstallieren
				Mouse.Capture(null);
				this.PreviewMouseMove -= this.PreviewMouseMove_scrollstretch;
				this.dragwatcherinstalled = false;
			}

			e.Handled = true;
		}

		private void PreviewMouseMove_selection(object sender, MouseEventArgs e)
		{
			Point p = e.GetPosition(axiscontrol_time);

			if (e.LeftButton == MouseButtonState.Pressed)
			{
				if (p.X < 0.0)
				{
					RendererManager.Scroll(RendererManager.Time - (RendererManager.TimeStretch * 0.05));
				}
				else if (p.X > axiscontrol_time.RenderSize.Width)
				{
					RendererManager.Scroll(RendererManager.Time + (RendererManager.TimeStretch * 0.05));
				}

				double t = RendererManager.Time + (RendererManager.TimeStretch * p.X / axiscontrol_time.ActualWidth);
				if (t < 0.0)
				{
					t = 0.0;
				}

				if (this.dragtime <= t)
				{
					this.SelectionStart = this.dragtime;
					this.SelectionEnd = t;
				}
				else
				{
					this.SelectionStart = t;
					this.SelectionEnd = this.dragtime;
				}
			}
			else
			{
				this.selectionmarkeradorner.Visibility = this.SelectionStart < this.SelectionEnd ? Visibility.Visible : Visibility.Hidden;

				// nix mehr gedrückt, handler deinstallieren
				Mouse.Capture(null);
				this.PreviewMouseMove -= this.PreviewMouseMove_selection;
				this.dragwatcherinstalled = false;
			}

			e.Handled = true;
		}

		private void Executed_CmdAutoZoomAllIndividual(object sender, ExecutedRoutedEventArgs e)
		{
			foreach (IEnumerable<RenderSlice> rslist in stackercontrol.RenderSliceLists)
			{
				RendererManager.AutoZoomIndividual(rslist);
			}
		}

		private void Executed_CmdAutoZoomAllGroupedByFiles(object sender, ExecutedRoutedEventArgs e)
		{
			foreach (IEnumerable<RenderSlice> rslist in stackercontrol.RenderSliceLists)
			{
				RendererManager.AutoZoomGroupedByFiles(rslist);
			}
		}

		private void Executed_CmdAutoZoomAllGroupedByUnits(object sender, ExecutedRoutedEventArgs e)
		{
			foreach (IEnumerable<RenderSlice> rslist in stackercontrol.RenderSliceLists)
			{
				RendererManager.AutoZoomGroupedByUnits(rslist);
			}
		}

        private void Executed_CmdChangeTimeTest(object sender, ExecutedRoutedEventArgs e)
        {
            Trace.WriteLine("------------------------------" + e.Parameter.ToString());
        }

        private void Executed_CmdChangeTimeZoom(object sender, ExecutedRoutedEventArgs e)
        {
            string param = (string)e.Parameter;
            if (param.Equals("all"))
            {
                RendererManager.Scroll(0.0);
                RendererManager.Stretch(RendererManager.TimeMax);
            }
            else if (param.Equals("selection"))
            {
				if (this.SelectionStart != this.SelectionEnd)
                {
					RendererManager.Scroll(this.SelectionStart);
					RendererManager.Stretch(this.SelectionEnd - this.SelectionStart);
                }
            }
            else if (param.Equals("next"))
            {
                RendererManager.Scroll(RendererManager.Time + RendererManager.TimeStretch);
            }
            else if (param.Equals("prev"))
            {
                RendererManager.Scroll(RendererManager.Time - RendererManager.TimeStretch);
            }
            else
            {
                // Parameter ist ein Zeitwert in Sekunden
                RendererManager.Stretch(double.Parse(param));
            }
        }

		private void CanExecute_CmdChangeTimeZoom(object sender, CanExecuteRoutedEventArgs e)
		{
			string param = (string)e.Parameter;
			if (param.Equals("selection"))
			{
				e.CanExecute = (this.SelectionStart != this.SelectionEnd);
			}
            if (param.Equals("next"))
            {
                e.CanExecute = RendererManager.Time + RendererManager.TimeStretch < RendererManager.TimeMax;
            }
            else if (param.Equals("prev"))
            {
                e.CanExecute = RendererManager.Time - RendererManager.TimeStretch >= 0;
            }
			else
			{
				e.CanExecute = true;
			}
		}



	}
}

#region old code
/*
void SignalViewerControl_SizeChanged(object sender, SizeChangedEventArgs e)
{
	System.Diagnostics.Debug.WriteLine(++counter + " -- " + DesiredSize);
}
*/

/*
private void Executed_Zoom(object sender, ExecutedRoutedEventArgs e)
{
	RendererManager.ZoomInto();
}
*/
#endregion 
