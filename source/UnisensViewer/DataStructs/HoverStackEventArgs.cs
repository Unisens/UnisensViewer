using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace UnisensViewer
{
	public class HoverStackEventArgs : RoutedEventArgs
	{
		public readonly ObservableCollection<RenderSlice> Stack;
		public readonly ItemsControl ItemsControl;
		
		public HoverStackEventArgs(ItemsControl ic, ObservableCollection<RenderSlice> stack) 
			: base(StackerControl.HoverStackEvent)
		{
			this.ItemsControl = ic;
			this.Stack = stack;
		}

        /// <summary>
        /// Returns all files of the current stack
        /// </summary>
		public IEnumerable<XElement> StackSevEntries
		{
			get
			{
				ObservableCollection<RenderSlice> rslist = (ObservableCollection<RenderSlice>)this.ItemsControl.DataContext;
				return RendererManager.GetSevEntries(rslist);
			}
		}

		public IEnumerable<XElement> StackSelectionSevEntries
		{
			get
			{
				Grid grid = (Grid)LogicalTreeHelper.GetParent(this.ItemsControl);
				ListBox lb = (ListBox)grid.FindName("listbox");
				IList selecteditems = lb.SelectedItems;

				return RendererManager.GetSevEntries(selecteditems);
			}
		}

        /// <summary>
        /// Returns all channels of the current stack
        /// </summary>
		public IEnumerable<XElement> StackChannelEntries
		{
			get
			{
				ObservableCollection<RenderSlice> rslist = (ObservableCollection<RenderSlice>)this.ItemsControl.DataContext;
				
				List<XElement> entries = new List<XElement>();
				foreach (RenderSlice rs in rslist)
				{
					entries.Add(rs.UnisensNode);
				}

				return entries;
			}
		}

		public IEnumerable<XElement> StackSelectionChannelEntries
		{
			get
			{
				Grid grid = (Grid)LogicalTreeHelper.GetParent(this.ItemsControl);
				ListBox lb = (ListBox)grid.FindName("listbox");
				IList selecteditems = lb.SelectedItems;

				List<XElement> entries = new List<XElement>();
				foreach (RenderSlice rs in selecteditems)
				{
					entries.Add(rs.UnisensNode);
				}

				return entries;
			}
		}
	}
}

