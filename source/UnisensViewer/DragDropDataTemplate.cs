using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UnisensViewer
{
	public class DragDropDataTemplate : Decorator
	{
		// privat, könnte auch ein ReadOnly attached attribut sein
		public static readonly DependencyProperty AllowDragProperty = DependencyProperty.RegisterAttached("AllowDrag", typeof(bool), typeof(DragDropDataTemplate), new PropertyMetadata(false, new PropertyChangedCallback(AllowDragPropertyChangedCallback)));
		
		private static readonly DependencyProperty DragDropDataTemplateProperty = DependencyProperty.RegisterAttached("DragDropDataTemplate", typeof(DragDropDataTemplate), typeof(DragDropDataTemplate), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
				
		private Point dragpoint;
		private FrameworkElement dragfe;
		private bool dragwatcherinstalled;

		public DragDropDataTemplate()
		{
			SetValue(DragDropDataTemplateProperty, this);
		}
	
		// braucht der xaml parser für attached property
		public static void SetAllowDrag(UIElement uie, bool value)
		{
			uie.SetValue(AllowDragProperty, value);
		}

		private static void AllowDragPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			FrameworkElement fe = (FrameworkElement)d;

			if ((bool)e.NewValue)
			{
				fe.PreviewMouseLeftButtonDown += DragDropDataTemplate.LeftDown;
			}
		}

		private static void LeftDown(object sender, MouseButtonEventArgs e)
		{
			FrameworkElement fe = sender as FrameworkElement;

			if (fe != null && fe.DataContext != null)
			{
				DragDropDataTemplate current = fe.GetValue(DragDropDataTemplateProperty) as DragDropDataTemplate;

				if (current != null)
				{
					current.SetupDragWatcher(fe, e);
				}
			}
		}

		private void SetupDragWatcher(FrameworkElement fe, MouseButtonEventArgs e)
		{
			if (!this.dragwatcherinstalled)
			{
				this.dragwatcherinstalled = true;
				this.dragpoint = e.GetPosition(null);
				this.PreviewMouseMove += this.PreviewMouseMoveHandler;
			}

			this.dragfe = fe;	// dragfe: auf jeden fall überschreiben, das letzte AllowDrag="true" bestimmt das drag-objekt
		}

		private void ClearDragWatcher()
		{
			this.PreviewMouseMove -= this.PreviewMouseMoveHandler;
			this.dragwatcherinstalled = false;
		}

		// drag-watcher
		private void PreviewMouseMoveHandler(object sender, MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				Point p = e.GetPosition(null);

				if (Math.Abs(this.dragpoint.X - p.X) >= SystemParameters.MinimumHorizontalDragDistance ||
					Math.Abs(this.dragpoint.Y - p.Y) >= SystemParameters.MinimumVerticalDragDistance)
				{
					this.ClearDragWatcher();

					DragDrop.DoDragDrop(this.dragfe, this.dragfe.DataContext, DragDropEffects.Move);
				}
			}
			else
			{
				this.ClearDragWatcher();
			}
		}
	}
}
