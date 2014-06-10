using System;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace UnisensViewer
{
    public class NLogLogger
    {
        public static void ConfigureLogger()
        {
            // Step 1. Create configuration object 
            LoggingConfiguration config = new LoggingConfiguration();

            FileTarget fileTarget = new FileTarget();
            config.AddTarget("file", fileTarget);

            // Step 3. Set target properties 
            string logPath = Folders.UnisensViewerAppData;
			
            fileTarget.FileName = Path.Combine(logPath, "UnisensViewer.log");
            fileTarget.ArchiveFileName = Path.Combine(logPath, "UnisensViewer.{#####}.txt");
            fileTarget.ArchiveAboveSize = 102400; // 100kb
            fileTarget.ArchiveNumbering = ArchiveNumberingMode.Sequence;
            fileTarget.MaxArchiveFiles = 10;
            fileTarget.ConcurrentWrites = true;
            fileTarget.KeepFileOpen = false;

            fileTarget.Layout = "${longdate} | ${level} | ${message} ${exception:format=tostring}| (in  ${callsite})";

            LoggingRule rule2 = new LoggingRule("*", LogLevel.Debug, fileTarget);
            config.LoggingRules.Add(rule2);

            // Step 5. Activate the configuration
            LogManager.Configuration = config;
        }
    }
}
