using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace UnisensViewer
{
    public class Commands
    {
        public static readonly RoutedCommand CloseFile = new RoutedCommand();

		public static readonly RoutedCommand CropSelection = new RoutedCommand();
		
		public static readonly RoutedCommand Trim = new RoutedCommand();

		public static readonly RoutedCommand SetMarker = new RoutedCommand();
		public static readonly RoutedCommand DeleteMarker = new RoutedCommand();
		public static readonly RoutedCommand TruncateMarker = new RoutedCommand();
        public static readonly RoutedCommand SetArtifacts = new RoutedCommand();
        public static readonly RoutedCommand SetMarkerList = new RoutedCommand();
		
		static Commands()
        {
            //CloseFile.InputGestures.Add(new KeyGesture(Key.X, ModifierKeys.Control));
			SetMarker.InputGestures.Add(new KeyGesture(Key.M, ModifierKeys.Control));
			DeleteMarker.InputGestures.Add(new KeyGesture(Key.D, ModifierKeys.Control));
			TruncateMarker.InputGestures.Add(new KeyGesture(Key.T, ModifierKeys.Control));
        }
    }
}
