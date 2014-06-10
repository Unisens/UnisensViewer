using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;
using NLog;

namespace UnisensViewer
{
    public partial class App : Application
    {
        private static Logger logger;

		private bool crashDuringStartup = true;

        public App()
        {
#if (!DEBUG)
            // UI Exceptions
            DispatcherUnhandledException += AppDispatcherUnhandledException;

            // Thread exceptions
            AppDomain.CurrentDomain.UnhandledException += AppDomainUnhandledException;
#endif
            NLogLogger.ConfigureLogger();
            logger = LogManager.GetCurrentClassLogger();
            logger.Info("Application starting");
        }

        private static void AppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException(e.ExceptionObject as Exception, e.IsTerminating);
        }

        private static void HandleException(Exception e, bool isTerminating)
        {
            logger.ErrorException("isTerminating = " + isTerminating.ToString(), e);
            if (e == null)
            {
            	return;
            }

            CrashRptNET.CrashReport crashReport = new CrashRptNET.CrashReport("UnisensViewer", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            crashReport.ErrorReportURL = "http://software.unisens.org/ErrorReport/ErrorReporter.php";
            crashReport.Send(e);

            if (isTerminating)
            {
            	Application.Current.Shutdown();
            }
        }

		private void ApplicationStartup(object sender, StartupEventArgs e)
		{
			StartupUri = new Uri("WindowMain.xaml", UriKind.Relative);
			this.crashDuringStartup = false;
		}

		private void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			HandleException(e.Exception, false);
			e.Handled = true;
			if (this.crashDuringStartup)
			{
				this.Shutdown();
			}
		}
    }
}
