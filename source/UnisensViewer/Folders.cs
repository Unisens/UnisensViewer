using System;
using System.Diagnostics;

namespace UnisensViewer
{
    public static class Folders
    {
        public static string UnisensViewer
        {
            get
            {
                return System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\";
            }
        }

        public static string UnisensViewerAppData
        {
            get
            {
                return System.IO.Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FZI\\UnisensViewer");
            }
        }
    }
}
