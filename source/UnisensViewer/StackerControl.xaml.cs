using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Xml.Linq;
using UnisensViewerLibrary;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NLog;

namespace UnisensViewer
{
	public partial class StackerControl : UserControl
	{
		public static readonly RoutedEvent HoverStackEvent = EventManager.RegisterRoutedEvent("HoverStack", RoutingStrategy.Bubble, typeof(HoverStackEventHandler), typeof(StackerControl));

		public ObservableCollection<ObservableCollection<RenderSlice>> renderSliceLists;

		private double						pluginContextMenuTimeCursor;
		private ObservableCollection<RenderSlice>	pluginContextMenuSelectedRsList;

		private int							zindex;

		private Point						dragpoint;

		private bool						dragwatcherinstalled;
		private List<DragScaleOffsetInfo>	dragselectedrenderslices;
		private IEnumerable<Renderer>		dragInvolvedRenderers;

		private EventHandler				killHandler;

        private ItemsControl                itemscontrol_RenderSliceImages;

		public StackerControl()
		{
			InitializeComponent();

			DataContext = this;

			this.renderSliceLists = new ObservableCollection<ObservableCollection<RenderSlice>>();
			this.killHandler = new EventHandler(this.RenderSlice_Kill);

			Focus();
            itemscontrol_renderslicestacks.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(this.ItemsControl_MouseLeftButtonUp);
        }

		public delegate void HoverStackEventHandler(object sender, HoverStackEventArgs e);

		public event HoverStackEventHandler HoverStack
		{
			add { AddHandler(HoverStackEvent, value); }
			remove { RemoveHandler(HoverStackEvent, value); }
		}
		
		public IEnumerable<IDspPlugin1> DspPlugins { get; set; }
				
		public ObservableCollection<ObservableCollection<RenderSlice>> RenderSliceLists
		{
			get { return this.renderSliceLists; }
		}

		public double PluginContextMenuTimeCursor
		{
			get { return this.pluginContextMenuTimeCursor; }
		}

		public ObservableCollection<RenderSlice> PluginContextMenuSelectedRsList
		{
			get { return this.pluginContextMenuSelectedRsList; }
		}

		public static bool IsSignalEventValueEntry(XElement xe)
		{
			return xe != null && (xe.Name.LocalName == "signalEntry" || xe.Name.LocalName == "eventEntry" || xe.Name.LocalName == "valuesEntry");
		}
		
		/// <summary>
		/// Closes all stacks
		/// </summary>
		public void CloseAllSignals()
		{
			// hier mal aufpassen, die listen werden durch die eventhandler manipuliert
			while (this.renderSliceLists.Count != 0)
			{
				ObservableCollection<RenderSlice> l = this.renderSliceLists[0];

				while (l.Count != 0)
				{
					l[0].RaiseKill();
				}
			}
		}

		#region drag and drop
		public void Dropped(DragEventArgs e, XElement xe)
		{
			if (string.Compare(xe.Name.Namespace.ToString(), "http://www.unisens.org/unisens2.0", true) != 0)
			{
				throw new Exception("das war aber mal kein xml-element aus dem unisens2.0 namespace...\n" + xe.ToString());
			}

			ObservableCollection<RenderSlice> l = this.GetRenderSliceListFromDropPoint(e);

			if (IsSignalEventValueEntry(xe))
			{
				this.DropSignalEventValueEntry(xe, l);
			}
			else if (xe.Name.LocalName == "channel" && IsSignalEventValueEntry(xe.Parent))
			{
				this.DropChannel(xe, l);
			}
			else if (xe.Name.LocalName == "group")
			{
				XElement unisensroot = xe.Parent;

				IEnumerable<string> refs = from ge in xe.Elements("{http://www.unisens.org/unisens2.0}groupEntry")
										   select ge.Attribute("ref").Value;

				IEnumerable<XElement> sigs = from s in unisensroot.Elements("{http://www.unisens.org/unisens2.0}signalEntry")
											 let id = s.Attribute("id")
											 where id != null && refs.Contains(id.Value)
											 select s;

				foreach (XElement s in sigs)
				{
					this.DropSignalEventValueEntry(s, l);
				}
			}
			else if (xe.Name.LocalName == "unisens")
			{
				foreach (XElement item in xe.Elements())
				{
					if (item.Name.LocalName == "signalEntry" || item.Name.LocalName == "valuesEntry" || item.Name.LocalName == "eventEntry")
					{
						this.Dropped(e, item);
					}
				}
			}

			// ** beim erzeugen des renderers trat ein fehler auf, die eventuell oben erzeugte rsliste löschen
			if (l.Count == 0)
			{
				this.renderSliceLists.Remove(l);
			}
		}

		public void DropSignalEventValueEntry(XElement seventry, ObservableCollection<RenderSlice> rslist)
		{
			if (rslist == null)
			{
				// neuen stapel am ende erzeugen
				rslist = new ObservableCollection<RenderSlice>();
				this.renderSliceLists.Add(rslist);
			}

			// alle kanäle hinzufügen
			Renderer r = RendererManager.GetRenderer(seventry);

			if (r != null)
			{
				List<RenderSlice> l = new List<RenderSlice>();

				for (int a = 0; a < r.Channels; ++a)
				{
					RenderSlice rs = r.GetRenderSlice(a);

					this.AttachKillHandler(rs);
					this.MoveRenderSlice(rs, rslist);

					l.Add(rs);
				}

				RendererManager.AutoZoomGroupedByFiles(l);
			}

			if (rslist.Count == 0)
			{
				// ** beim erzeugen des renderers trat ein fehler auf, die eventuell oben erzeugte rsliste löschen
				this.renderSliceLists.Remove(rslist);
			}
		}
		
		private void ScrollViewer_Drop(object sender, DragEventArgs e)
		{
			RenderSlice s;
			XElement xe;

			// abstrakte klasse nehmen geht ned:
			// s = e.Data.GetData(typeof(RenderSlice)) as RenderSlice;
			if ((s = e.Data.GetData(typeof(RasterRenderSlice)) as RenderSlice) != null)
			{
				this.Dropped(e, s);
			}
			else if ((s = e.Data.GetData(typeof(EventRenderSlice)) as RenderSlice) != null)
			{
				this.Dropped(e, s);
			}
			else if ((xe = e.Data.GetData(typeof(XElement)) as XElement) != null)
			{
				this.Dropped(e, xe);
			}
		}

		private ObservableCollection<RenderSlice> GetRenderSliceListFromDropPoint(DragEventArgs e)
		{
			ObservableCollection<RenderSlice> l = new ObservableCollection<RenderSlice>();

            if (e != null)
            {
				for (int a = 0, b = this.renderSliceLists.Count; a < b; ++a)
                {
                    ContentPresenter container = (ContentPresenter)itemscontrol_renderslicestacks.ItemContainerGenerator.ContainerFromIndex(a);
                    Point p = e.GetPosition(container);

					if (p.Y < container.ActualHeight * 0.05)
					{
						// zwischendrin +- 5% (davor einfügen)
						this.renderSliceLists.Insert(a, l);
						return l;
					}
					else if (p.Y < container.ActualHeight * 0.95)
					{
						// auf den stack (90% breiter streifen)
						return this.renderSliceLists[a];
					}
                }
            }

			// signalstacks war leer oder hinter den letzten stack einfügen
			this.renderSliceLists.Add(l);
			return l;
		}

		public void MoveRenderSlice(RenderSlice rs, ObservableCollection<RenderSlice> dest)
		{
			// in alter liste finden, dann entfernen
			foreach (ObservableCollection<RenderSlice> l in this.renderSliceLists)
			{
				if (l.Remove(rs))
				{
					// wurde gefunden und entfernt.

					// falls alte liste jetzt leer, löschen.
					if (l.Count == 0 && l != dest)
					{
						this.renderSliceLists.Remove(l);
					}

					break;
				}
			}

			dest.Add(rs);
		}

		private unsafe void Dropped(DragEventArgs e, RenderSlice rs)
		{
			ObservableCollection<RenderSlice> l = this.GetRenderSliceListFromDropPoint(e);
            Renderer r = rs.Renderer;
            double maxbreite = rs.ImageSource.Height;
            int imageWidth;
            if (l.Count > 0)
            {
                imageWidth = (int)l[0].ImageSource.Width;
            }
            else
            {
                imageWidth = 128;
            }

            RendererManager.Renderer_Update(rs, imageWidth, maxbreite);
            RendererManager.RenderSlice_Update(rs, imageWidth, maxbreite);

            rs.Scale = (float)(imageWidth / rs.Range);
			this.MoveRenderSlice(rs, l);          
            XElement channel = rs.UnisensNode;

            RendererManager.Render(rs.Renderer, channel);
		}

		private void DropChannel(XElement channel, ObservableCollection<RenderSlice> rslist)
		{
			Renderer r = RendererManager.GetRenderer(channel.Parent);

			if (r != null)
			{
				List<RenderSlice> renderSliceList = new List<RenderSlice>();

				int channelNum = MeasurementEntry.GetChannelNum(channel);

				if (channelNum != -1)
				{
					RenderSlice renderSlice = r.GetRenderSlice(channelNum);

					this.AttachKillHandler(renderSlice);
					this.MoveRenderSlice(renderSlice, rslist);

					renderSliceList.Add(renderSlice);
				}
				
				RendererManager.AutoZoomIndividual(renderSliceList);
			}
		}
		#endregion

		private void ListBox_renderSliceStack_SelectionChanged(object sender, SelectionChangedEventArgs args)
		{
			// ausgewähltes signal in den vordergrund bringen
			ListBox l = (ListBox)sender;
			RenderSlice rs = l.SelectedItem as RenderSlice;

			if (rs != null)
			{
				Grid grid = (Grid)LogicalTreeHelper.GetParent(l);
				ItemsControl itemscontrol_rendersliceimages = (ItemsControl)grid.FindName("itemscontrol_rendersliceimages");

				ContentPresenter cp = (ContentPresenter)itemscontrol_rendersliceimages.ItemContainerGenerator.ContainerFromItem(rs);
				Panel.SetZIndex(cp, ++this.zindex);
			}
			else
			{
				l.SelectedIndex = 0;
			}
		}

		//////////////////////////////////////////////////////////////////////////////////////
		//
		// scale + offset dragging
		//
		//////////////////////////////////////////////////////////////////////////////////////

		private void ItemsControl_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (!this.dragwatcherinstalled &&
				((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) != 0 &&
					(e.MiddleButton == MouseButtonState.Pressed || e.LeftButton == MouseButtonState.Pressed)))
			{
				ItemsControl ic = (ItemsControl)sender;
				Mouse.Capture(ic);

				this.dragpoint = e.GetPosition(ic);		// position relativ zum itemscontrol!!	

				// die listbox aus der datatemplate-instanz muss erst noch ermittelt werden
				Grid grid = (Grid)LogicalTreeHelper.GetParent(ic);
				ListBox listbox = (ListBox)grid.FindName("listbox");

				if (listbox.SelectedItem == null)
				{
					listbox.SelectedIndex = 0;
				}

				/*
				dragselectedrenderslice = (RenderSlice) listbox.SelectedItem;

				dragscale = dragselectedrenderslice.Scale;
				dragoffset = dragselectedrenderslice.Offset;
				*/
				
				// die folgende liste NICHT mit linq zusammenbauen!!
				// das ist dann saulahm!! ich glaub er re-evaluiert die abfrage, sobald offset/scale
				// verändert wird => changed-event (INotifyPropertyChanged in RenderSlice)
				this.dragselectedrenderslices = new List<DragScaleOffsetInfo>();
				foreach (RenderSlice rs in listbox.SelectedItems)
				{
					this.dragselectedrenderslices.Add(new DragScaleOffsetInfo(rs));
				}

				this.dragInvolvedRenderers = RendererManager.GetInvolvedRenderers(from RenderSlice rs in listbox.SelectedItems select rs);
				
				// Keyboard.Modifiers == None ist für das time-scroll&stretch in signalviewercontrol
				if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0)
				{
					ic.PreviewMouseMove += this.ItemsControl_PreviewMouseMove_ScaleAndOffset;
					this.dragwatcherinstalled = true;
				}

				e.Handled = true;
			}
		}

		private void ItemsControl_PreviewMouseMove_ScaleAndOffset(object sender, MouseEventArgs e)
		{
			ItemsControl ic = (ItemsControl)sender;

			Point	p = e.GetPosition(ic);
			double deltax = p.X - this.dragpoint.X;
			double deltay = p.Y - this.dragpoint.Y;

			if (e.MiddleButton == MouseButtonState.Pressed || e.LeftButton == MouseButtonState.Pressed)
			{
				if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
				{
					foreach (DragScaleOffsetInfo dsoi in this.dragselectedrenderslices)
					{
						//dsoi.Renderslice.Offset = dsoi.Dragoffset + ((float)(deltay / (ic.ActualHeight * dsoi.Dragscale)) * 128.0f);
                        dsoi.Renderslice.Offset = dsoi.Dragoffset + ((float)(deltay / (ic.ActualHeight * dsoi.Dragscale)) * (float)(dsoi.Renderslice.ImageSource.Width));
					}

					foreach (Renderer r in this.dragInvolvedRenderers)
					{
						RendererManager.Render(r, null);
					}
				}

				if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
				{
                    float scale = 0;
					foreach (DragScaleOffsetInfo dsoi in this.dragselectedrenderslices)
					{
                        scale = dsoi.Dragscale + (float)(deltax * 0.005 * dsoi.Dragscale);
                        if (scale > 0) dsoi.Renderslice.Scale = scale; //disable inversion (scaling < 0)
                    }

					foreach (Renderer r in this.dragInvolvedRenderers)
					{
						RendererManager.Render(r, null);
					}
				}
			}
			else
			{
				// nix mehr gedrückt, handler deinstallieren
				ic.PreviewMouseMove -= this.ItemsControl_PreviewMouseMove_ScaleAndOffset;
				this.dragwatcherinstalled = false;
				Mouse.Capture(null);
			}

			e.Handled = true;
		}

		private void Button_Click_killsignal(object sender, RoutedEventArgs e)
		{
            try
            {
                Button b = (Button)sender;
                RenderSlice r = (RenderSlice)b.DataContext;

                r.RaiseKill();
            }
            catch (Exception ex)
            {
                Logger logger = LogManager.GetCurrentClassLogger();
                logger.Error("Strange error. Clicked on kill signal that is no RenderSlice: " + ex.StackTrace);
            }
		}

		private void AttachKillHandler(RenderSlice rs)
		{
			// nur bei neu erzeugten renderslices EINMAL reinhooken,
			// d.h. bei renderslices, die noch in keiner liste sind.
			// es ist nämlich möglich, dass man das xml-element nochmal reindragt!
			rs.Kill -= this.killHandler;
			rs.Kill += this.killHandler;
		}

		private void RenderSlice_Kill(object sender, EventArgs e)
		{
			RenderSlice rs = (RenderSlice)sender;

			// handler wieder löschen (sonst speicherleck)
			rs.Kill -= this.killHandler;

			foreach (ObservableCollection<RenderSlice> l in this.renderSliceLists)
			{
				if (l.Remove(rs))
				{
					if (l.Count == 0)
					{
						this.renderSliceLists.Remove(l);
					}

					break;
				}
			}
		}

        /// <summary>
        /// Closes this stack
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void MenuItem_Click_Stack_Close(object sender, RoutedEventArgs e)
		{
			// siehe kommentar in CloseAllSignals
			MenuItem mi = (MenuItem)sender;
			ObservableCollection<RenderSlice> rslist = (ObservableCollection<RenderSlice>)mi.DataContext;

			while (rslist.Count != 0)
			{
				rslist[0].RaiseKill();
			}
		}

        /// <summary>
        /// Closes all stacks, except this one.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void MenuItem_Click_Stack_CloseOthers(object sender, RoutedEventArgs e)
        {
            // siehe kommentar in CloseAllSignals
            MenuItem mi = (MenuItem)sender;
            ObservableCollection<RenderSlice> thisRsList = (ObservableCollection<RenderSlice>)mi.DataContext;

			for (int i = this.renderSliceLists.Count - 1; i >= 0; i--)
            {
				ObservableCollection<RenderSlice> rsList = this.renderSliceLists[i];

                for (int j = rsList.Count - 1; j >= 0; j--)
                {
                    if (thisRsList.IndexOf(rsList[j]) < 0)
                    {
                        // Close this render slice
                        rsList[j].RaiseKill();
                    }
                }
            }
        }

		//////////////////////////////////////////////////////////////////////////////////////
		//
		// plugin contextmenu
		//
		//////////////////////////////////////////////////////////////////////////////////////

		private void MenuItem_plugins_Initialized(object sender, EventArgs e)
		{
			MenuItem mi = (MenuItem)sender;
			mi.ItemsSource = this.DspPlugins;
		}

		private void PluginContextMenu_Opened(object sender, RoutedEventArgs e)
		{
			ContextMenu ctx = (ContextMenu)sender;

			Point point = Mouse.GetPosition(ctx.PlacementTarget);

			this.pluginContextMenuTimeCursor = RendererManager.Time + (RendererManager.TimeStretch * point.X / ctx.PlacementTarget.RenderSize.Width);
			this.pluginContextMenuSelectedRsList = (ObservableCollection<RenderSlice>)ctx.DataContext;
		}

		private void MenuItem_plugins_Click(object sender, RoutedEventArgs e)
		{
			MenuItem mi = (MenuItem)sender;
			IDspPlugin1 idsp = (IDspPlugin1)mi.DataContext;

			WindowMain.ExecPluginRoutedCommand.Execute(new ExecPluginCommandParameter(idsp, this.pluginContextMenuSelectedRsList, this.pluginContextMenuTimeCursor, null), null);
		}
	
		private void ItemsControl_renderSliceStacks_MouseDown(object sender, MouseButtonEventArgs e)
		{
			// "markieren" an signalviewercontrol weiterleiten
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				RaiseEvent(new RoutedEventArgs(SignalViewerControl.BeginSelectTimeEvent, this));
				e.Handled = true;
			}
		}

		private void Image_MouseEnter(object sender, MouseEventArgs e)
		{
			Image i = (Image)sender;
			RaiseEvent(new HoverRenderSliceEventArgs((RenderSlice)i.DataContext));
		}

		private void Image_MouseMove(object sender, MouseEventArgs e)
		{
			Image			i	= (Image)sender;
			RenderSlice		rs	= (RenderSlice)i.DataContext;
			
			// achtung image ist rotiert, also x<->y
			Point			p	= e.GetPosition(i);		

			// zeit, die der mauscursor (1 pixel) überspannt
			double time = RendererManager.Time + (RendererManager.TimeStretch * p.Y / i.ActualHeight);	// hier ebenso ActualWidth<->ActualHeight
			double time_end = time + (RendererManager.TimeStretch / i.ActualHeight);					// 1 pixel breite

			RaiseEvent(new UpdateStatusEventArgs(rs.GetSampleInfo(time, time_end)));
		}
		
		private void MenuItem_Click_AutoZoom_Stack_Separate(object sender, RoutedEventArgs e)
		{
			MenuItem mi = (MenuItem)sender;
			ObservableCollection<RenderSlice> rslist = (ObservableCollection<RenderSlice>)mi.DataContext;

			RendererManager.AutoZoomIndividual(rslist);
		}

		private void MenuItem_Click_AutoZoom_Stack_Files(object sender, RoutedEventArgs e)
		{
			MenuItem mi = (MenuItem)sender;
			ObservableCollection<RenderSlice> rslist = (ObservableCollection<RenderSlice>)mi.DataContext;

			RendererManager.AutoZoomGroupedByFiles(rslist);
		}
		
		private void MenuItem_Click_AutoZoom_Stack_Units(object sender, RoutedEventArgs e)
		{
			MenuItem mi = (MenuItem)sender;
			ObservableCollection<RenderSlice> rslist = (ObservableCollection<RenderSlice>)mi.DataContext;

			RendererManager.AutoZoomGroupedByUnits(rslist);
		}

		private IEnumerable<RenderSlice> GetSelectedRenderSlices(ObservableCollection<RenderSlice> rslist)
		{
			// die zu dem stapel gehörende listbox suchen
			ContentPresenter cp = (ContentPresenter)itemscontrol_renderslicestacks.ItemContainerGenerator.ContainerFromItem(rslist);
			DataTemplate dt = cp.ContentTemplate;
			ListBox lb = (ListBox)dt.FindName("listbox", cp);

			// SelectedItems hat IList, NICHT die generische version IList<>!
			// brauchen IList<RenderSlice>, also mit linq kurz konvertieren...
			IEnumerable<RenderSlice> selectedrs = from RenderSlice rs in lb.SelectedItems
												  select rs;
			return selectedrs;
		}

		private void MenuItem_Click_AutoZoom_Selected_Separate(object sender, RoutedEventArgs e)
		{
			// den stapel ermitteln, ist im datacontext
			MenuItem mi = (MenuItem)sender;
			ObservableCollection<RenderSlice> rslist = (ObservableCollection<RenderSlice>)mi.DataContext;

			IEnumerable<RenderSlice> selectedrs = this.GetSelectedRenderSlices(rslist);

			RendererManager.AutoZoomIndividual(selectedrs);
		}

		private void MenuItem_Click_AutoZoom_Selected_Files(object sender, RoutedEventArgs e)
		{
			MenuItem mi = (MenuItem)sender;
			ObservableCollection<RenderSlice> rslist = (ObservableCollection<RenderSlice>)mi.DataContext;
			IEnumerable<RenderSlice> selectedrs = this.GetSelectedRenderSlices(rslist);

			RendererManager.AutoZoomGroupedByFiles(selectedrs);
		}

		private void MenuItem_Click_AutoZoom_Selected_Units(object sender, RoutedEventArgs e)
		{
			MenuItem mi = (MenuItem)sender;
			ObservableCollection<RenderSlice> rslist = (ObservableCollection<RenderSlice>)mi.DataContext;
			IEnumerable<RenderSlice> selectedrs = this.GetSelectedRenderSlices(rslist);

			RendererManager.AutoZoomGroupedByUnits(selectedrs);
		}

		private void MenuItem_Click_Invert(object sender, RoutedEventArgs e)
		{
			MenuItem mi = (MenuItem)sender;
			ObservableCollection<RenderSlice> rslist = (ObservableCollection<RenderSlice>)mi.DataContext;
			IEnumerable<RenderSlice> selectedrs = this.GetSelectedRenderSlices(rslist);

			foreach (RenderSlice rs in selectedrs)
			{
                // Inverst the scaling factor and calculate new range
				rs.Scale = -rs.Scale;
                float min = -rs.Offset;
                float max = (float)rs.Range - rs.Offset;

                // Update range and offset.
                rs.Zoominfo.PhysicalMax = max > min ? max : min;
                rs.Zoominfo.PhysicalMin = max > min ? min : max;
                rs.Offset = -max;
            }

            // Draw inversted signal
            RendererManager.UpdateRenderers(RendererManager.GetInvolvedRenderers(selectedrs));
        }

		private void ItemsControl_renderSliceImages_MouseEnter(object sender, MouseEventArgs e)
		{
			ItemsControl						ic		= (ItemsControl)sender;
			ObservableCollection<RenderSlice>	rslist	= (ObservableCollection<RenderSlice>)ic.DataContext;

			RaiseEvent(new HoverStackEventArgs(ic, rslist));
		}

		private void ItemsControl_renderSliceImages_MouseLeave(object sender, MouseEventArgs e)
		{
			RaiseEvent(new HoverStackEventArgs(null, null));
		}

        private void Mousemove(object sender, MouseEventArgs e)
        {
			if (RendererManager.Fadenkreuz)
			{
				Point pt0 = Mouse.GetPosition(canvas1);

				line1.StrokeThickness = 1;
				line2.StrokeThickness = 1;

				line1.Visibility = System.Windows.Visibility.Visible;
				line2.Visibility = System.Windows.Visibility.Visible;

				// paint horizontal line
				line1.X1 = 0;
				line1.X2 = Crosshair.ActualWidth;
				line1.Y1 = pt0.Y;
				line1.Y2 = pt0.Y;
				
				// paint vertical line
				line2.X1 = pt0.X;
				line2.X2 = pt0.X;
				line2.Y1 = 0;
				line2.Y2 = Crosshair.ActualHeight;
			}
			else
			{
				line1.Visibility = System.Windows.Visibility.Hidden;
				line2.Visibility = System.Windows.Visibility.Hidden;
			}
        }

		private void listbox_Loaded(object sender, RoutedEventArgs e)
		{
			ListBox l = (ListBox)sender;
			l.SelectedIndex = 0;
		}

        public unsafe void ItemsControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {           
            int imageWidth = (int)itemscontrol_RenderSliceImages.ActualHeight;
            ObservableCollection<RenderSlice> rslist = (ObservableCollection<RenderSlice>)itemscontrol_RenderSliceImages.DataContext;
            RenderSlice rs = null;
            bool width_changed = false;
            if (imageWidth < 10)
            {
                imageWidth = 10;
            }


            if (rslist != null)
            {               
                // check if the width is changed or a slice is closed, when the mouse is released.
                if (imageWidth != ((RasterRenderSlice)rslist[0]).ImageWidth)
                {
                    width_changed = true;
                }
            }
            if (width_changed)
            {
                int i = 0;
                while (i < rslist.Count)
                {
                    // at adding a new RenderSlice in the rslist, 
                    // the current RenderSlice is moved forward.
                    rs = rslist[0];
                    int deltaWidth = (int)(imageWidth - rs.ImageSource.Width);

                    int maxbreite = ((RasterRenderSlice)rs).ImageHeight;
                    // modify all RenderSlice of the Renderers
                    RendererManager.Renderer_Update(rs, imageWidth, maxbreite);
                    RendererManager.RenderSlice_Update(rs, imageWidth, maxbreite);

                    // Update Scale
                    rs.Scale = (float)(imageWidth / rs.Range);                  
                    //rs.ZoomInto(rs.Zoominfo.PhysicalMin, rs.Zoominfo.PhysicalMax);
                  
                    Renderer r = rs.Renderer;
                    XElement channel = rs.UnisensNode;

                    // old RenderSlice replaced by new
                    this.MoveRenderSlice(rs, rslist);

                    // paint all signals again
                    RendererManager.Render(r,channel);
                    i++;                   
                }                    
            }                 
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            Grid grid = (Grid)sender;
            itemscontrol_RenderSliceImages = (ItemsControl)grid.FindName("itemscontrol_rendersliceimages");
        }
	}

	#region old code
	/*
	void DropInto(XElement xe, ObservableCollection<RenderSlice> l)
	{
		if (String.Compare(xe.Name.Namespace.ToString(), "http://www.unisens.org/unisens2.0", true) != 0)
			throw new Exception("das war aber mal kein xml-element aus dem unisens2.0 namespace...\n" + xe.ToString());


		if (IsSignalEventValueEntry(xe))
		{

			// alle kanäle hinzufügen
			Renderer r = RendererManager.GetRenderer(xe);

			if (r != null)
			{
				for (int a = 0; a < r.Channels; ++a)
				{
					RenderSlice rs = r.GetRenderSlice(a);

					AttachKillHandler(rs);
					MoveRenderSlice(rs, l);
				}

				r.ZoomInto(RendererManager.Time, RendererManager.TimeStretch);
			}
		}
	}
	
	private void plugincontextmenu_Initialized(object sender, EventArgs e)
	{
		ContextMenu ctx = (ContextMenu)sender;
		ctx.ItemsSource = ((App)App.Current).DspPlugins;
	}


	private void plugincontextmenu_MenuItem_Click(object sender, RoutedEventArgs e)
	{
		MenuItem mi = (MenuItem)sender;
		IDspPlugin idsp = (IDspPlugin)mi.DataContext;


		IEnumerable<XElement> unisensnodes = from rs in plugincontextmenu_selected_rslist
												select rs.UnisensNode;


		IEnumerable<XElement> dsp_unisensnodes = idsp.Main(unisensnodes, 0.0, 0.0);
		// rückgabe: liste von signalentries, evententries, valueentries...


		if (dsp_unisensnodes != null && dsp_unisensnodes.Count() > 0)
		{
			// neuen signalstapel erzeugen, dann die ergebnis-signale da reinschmeissen
			ObservableCollection<RenderSlice> rs_dsp = new ObservableCollection<RenderSlice>();
			foreach (XElement xe in dsp_unisensnodes)
				DropInto(xe, rs_dsp);

			int pos = renderslicelists.IndexOf(plugincontextmenu_selected_rslist);
			renderslicelists.Insert(pos + 1, rs_dsp);
		}
	}

	void MouseMove_ScaleAndOffset(ItemsControl ic, Point p, double deltax, double deltay)
	{
		if (dragselectedrenderslice != null)
		{
			//dragselectedrenderslice.MoveOffsetFromView(dragoffset, (float)(deltay / ic.ActualHeight));
			dragselectedrenderslice.Offset = dragoffset - (float)(deltay / (ic.ActualHeight * dragselectedrenderslice.Scale)) * 128.0f;

			//dragselectedrenderslice.Scale = dragscale + (float)(deltax * 0.001);
			dragselectedrenderslice.Scale = dragscale + (float)(deltax * 0.005 * dsoi.dragscale);

			RendererManager.Render(dragselectedrenderslice.renderer);
		}
	}
	*/
	#endregion
}