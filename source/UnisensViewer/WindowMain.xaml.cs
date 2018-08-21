using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using Microsoft.Windows.Controls.Ribbon;
using NLog;
using UnisensViewerLibrary;
using UnisensViewer.Helpers;
using WPFLocalizeExtension.Engine;
using System.Globalization;
using Microsoft.Win32;
using WPFLocalizeExtension.Extensions;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Linq;

namespace UnisensViewer
{
    public partial class WindowMain : RibbonWindow
    {
        public static readonly RoutedEvent UpdateStatusEvent = EventManager.RegisterRoutedEvent("UpdateStatus", RoutingStrategy.Bubble, typeof(UpdateStatusEventHandler), typeof(SignalViewerControl));
        public static readonly RoutedCommand ExecPluginRoutedCommand = new RoutedCommand();

		public static readonly DependencyProperty CurrentFileNameProperty = DependencyProperty.Register("CurrentFileName", typeof(string), typeof(WindowMain));

		private static UnisensXmlFileManager unisensFileManager = UnisensXmlFileManager.CurrentUnisensInstance;
        private static Logger logger = LogManager.GetCurrentClassLogger();

		private Hotkeys hotkeys;
        private HoverStackEventArgs hoverStackEventArgs;
        private XDocument settings = null;
        private StringCollection lastOpendFiles;

		/// <summary>
        /// Main function: Reads the settings from settings.xml, initializes the plug-ins and creates 
        /// the plug-in menu and starts the updater.
        /// </summary>
        public WindowMain()
        {
            logger.Debug("WindowMain constructor");

			if (Properties.Settings.Default.CallUpgrade)
			{
				Properties.Settings.Default.Upgrade();
				Properties.Settings.Default.CallUpgrade = false;
			}
            lastOpendFiles = UnisensViewer.Properties.Settings.Default.Last_Opend_Files;

			SwitchCulture(UnisensViewer.Properties.Settings.Default.Language);

            InitializeComponent();
			
            try
            {
                this.settings = XDocument.Load(Folders.UnisensViewer + "settings.xml");

                if (this.settings.Root.Name.LocalName != "UnisensViewer")
                {
                    this.settings = null;
                }
            }
            catch (Exception e)
            {
                logger.ErrorException("settings.xml could not be loaded", e);
            }

            // Load the advanced settings
            this.loadAvancedSettings();


			CommandBindings.Add(new CommandBinding(ApplicationCommands.New, this.Executed_New));
			CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, this.Executed_Open));
			CommandBindings.Add(new CommandBinding(Commands.CloseFile, this.Executed_CloseFile, this.CanExecute_CloseFile));
			CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, this.Executed_Save, this.CanExecute_Save)); 
			CommandBindings.Add(new CommandBinding(ApplicationCommands.SaveAs, this.Executed_SaveAs, this.CanExecute_SaveAs));

			CommandBindings.Add(new CommandBinding(Commands.CropSelection, this.Execute_CropSelection, this.CanExecute_CropSelection));
			CommandBindings.Add(new CommandBinding(Commands.Trim, this.Execute_Trim, this.CanExecute_Trim));
			CommandBindings.Add(new CommandBinding(Commands.SetMarker, this.Execute_setMarker, this.CanExecute_setMarker));
			CommandBindings.Add(new CommandBinding(Commands.DeleteMarker, this.Execute_deleteMarker, this.CanExecute_deleteMarker));
			CommandBindings.Add(new CommandBinding(Commands.TruncateMarker, this.Execute_truncateMarker, this.CanExecute_truncateMarker));
            CommandBindings.Add(new CommandBinding(Commands.SetArtifacts, this.Execute_setArtifacts, this.CanExecute_setArtifacts));
            CommandBindings.Add(new CommandBinding(Commands.SetMarkerList, this.Execute_setMarkerList, this.CanExecute_setMarkerList));

            this.InitPlugins();
            Gridsplitter.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(this.Gridsplitter_MouseLeftButtonUp);

            
        }


        /// <summary>
        /// Loads the advanced settings from settings.xml
        /// </summary>
        private void loadAvancedSettings()
        {
            // List of settings
            // NoAutoLoad: prohibits the automatic signal loading during data set opening

            // Default values
            UnisensViewer.Properties.Settings.Default.AutoLoad = true;

            if (this.settings != null)
            {
                XElement settingElement = settings.Root.Element("Settings");
                if (settingElement != null)
                {
                    IEnumerable<XElement> noAutoLoad = settingElement.Elements("NoAutoLoad");

                    foreach (XElement noa in noAutoLoad)
                    {
                        if (noa != null)
                        {
                            UnisensViewer.Properties.Settings.Default.AutoLoad = false;
                            break;
                        }
                    }
                }
            }
        }

        public delegate void UpdateStatusEventHandler(object sender, UpdateStatusEventArgs e);

        public event UpdateStatusEventHandler UpdateStatus
        {
            add { AddHandler(UpdateStatusEvent, value); }
            remove { RemoveHandler(UpdateStatusEvent, value); }
        }

        public static bool Rounding { get; set; }

		public string CurrentFileName
		{
			get { return (string)GetValue(CurrentFileNameProperty); }
			set { SetValue(CurrentFileNameProperty, value); }
		}

		public Visibility AnalyzerVisibility
		{
			get { return analyzerPath() != null ? Visibility.Visible : Visibility.Hidden; }
		}

        [ImportMany]
        public IEnumerable<IDspPlugin1> DspPlugins { get; set; }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
            base.OnClosing(e);
        }

        #region Updates
        private static void StartSilent()
        {
            Thread.Sleep(10000);
            CheckForUpdates("/silent");
        }

        private static void CheckForUpdates(string argument)
        {
            if (File.Exists(Folders.UnisensViewer + "\\updater.exe"))
            {
                System.Diagnostics.Process prc = Process.Start(Folders.UnisensViewer + "\\updater.exe", argument);
                prc.Close();
            }
        }
        #endregion

        #region Plugins

        private void InitPlugins()
        {
            // Initalize all plug-ins
            logger.Debug("Loading plug-ins");

            // Load Plugins
            AggregateCatalog catalog = new AggregateCatalog(new AssemblyCatalog(System.Reflection.Assembly.GetExecutingAssembly()));

            try
            {
                DirectoryCatalog dircatalog = new DirectoryCatalog("Plugins");
                catalog.Catalogs.Add(dircatalog);
            }
            catch (Exception e)
            {
                throw new Exception("Cannot load external plug-ins. Plug-in directory is not available.\n", e);
            }

            try
            {
                CompositionContainer container = new CompositionContainer(catalog);
                container.ComposeParts(this);
            }
            catch (ReflectionTypeLoadException typeLoadException)
            {
                StringBuilder loaderMessages = new StringBuilder();
                loaderMessages.AppendLine("While trying to load composable parts the follwing loader exceptions were found: ");
                foreach (var loaderException in typeLoadException.LoaderExceptions)
                {
                    loaderMessages.AppendLine(loaderException.Message);
                }

                throw new Exception(loaderMessages.ToString(), typeLoadException);
            }

            // Adding Plugins to Ribbon 
            Collection<RibbonGroup> groupArray = new Collection<RibbonGroup>();
            bool groupExists = false;
            int i;

            foreach (IDspPlugin1 plugin in this.DspPlugins)
            {
                // Check if current group exists.
                groupExists = false;
                for (i = 0; i < groupArray.Count; i++)
                {
                    if (plugin.Group == groupArray[i].Header.ToString())
                    {
                        groupExists = true;
                        break;
                    }
                }

                // Create new Group when current group does not exist.
                if (!groupExists)
                {
                    groupArray.Add(new RibbonGroup());
                    i = groupArray.Count - 1;
                    groupArray[i].Header = plugin.Group;
                    //groupArray[i].IsEnabled = false;
                    PluginTab.Items.Add(groupArray[i]);
                }

                RibbonMenuButton rmb = new RibbonMenuButton();
                rmb.Label = plugin.Name;
                rmb.LargeImageSource = plugin.LargeRibbonIcon;
                rmb.SmallImageSource = plugin.SmallRibbonIcon;
                rmb.ToolTipImageSource = plugin.LargeRibbonIcon;
                rmb.ToolTipTitle = plugin.Name;
                rmb.ToolTipDescription = plugin.Description;
                rmb.ToolTipFooterTitle = "Copyright";
                rmb.ToolTipFooterImageSource = plugin.OrganizationIcon;
                rmb.ToolTipFooterDescription = plugin.CopyrightInfo;
                rmb.DataContext = plugin;
                rmb.IsEnabled = plugin.IsEnable;
                rmb.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(this.Pluginmenu_MenuItem_Click);
                groupArray[i].Items.Add(rmb);

                logger.Debug("Loaded plug-in '" + plugin.Name + "'");
            }

            this.hotkeys = new Hotkeys(this.settings, this.DspPlugins);

            signalviewercontrol.stackercontrol.DspPlugins = this.DspPlugins;
        }

        private void Pluginmenu_MenuItem_Click(object sender, MouseButtonEventArgs e)
        {
            RibbonMenuButton mi = (RibbonMenuButton)sender;
            IDspPlugin1 p = (IDspPlugin1)mi.DataContext;

            IEnumerable<XElement> retsigs = this.ExecutePluginWithDialog(p, 0.0, null);

            // xxx neuen stapel erzeugen und da retsigs reinschmeissen
        }

        private IEnumerable<XElement> ExecutePluginWithDialog(IDspPlugin1 p, double time_cursor, string parameter)
        {
            IEnumerable<XElement> retsigs = null;

			DialogPlugin dlgplugin = new DialogPlugin();
			dlgplugin.Owner = this;
			dlgplugin.DspPlugin = p;
			dlgplugin.UnisensXml = UnisensXmlFileManager.CurrentUnisensInstance.Xdocument;

            // xxx parameter in dialog editierbar machen
            if (dlgplugin.ShowDialog() == true)
            {
                retsigs = this.ExecutePlugin(p, dlgplugin.SelectedSignals, time_cursor, parameter);
            }

            return retsigs;
        }

        /// <summary>
        /// Run a plug-in after pressing a hotkey.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">pressed key event</param>
        private void WindowMain_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            HoverStackEventArgs currentHoverStackEventArgs = this.hoverStackEventArgs;
            if (currentHoverStackEventArgs != null)
            {
                PluginHotkeyBinding hb = (PluginHotkeyBinding)this.hotkeys.Bindings[new HotkeyHashkey(e.Key, Keyboard.Modifiers)];

                if (hb != null)
                {
                    Keyboard.Focus(signalviewercontrol);
                    e.Handled = true;

                    Point point = Mouse.GetPosition(currentHoverStackEventArgs.ItemsControl);
                    double time_cursor = RendererManager.Time + (RendererManager.TimeStretch * point.X / currentHoverStackEventArgs.ItemsControl.ActualWidth);
                    IEnumerable<XElement> retsigs = null;

                    switch (hb.Signals)
                    {
                        case PluginHotkeyBinding.SelectedSignals.AllOpenFiles:
                            retsigs = this.ExecutePlugin(hb.Plugin, RendererManager.GetSevEntriesAllRenderers(), time_cursor, hb.Parameter);
                            break;

                        case PluginHotkeyBinding.SelectedSignals.StackFiles:
                            retsigs = this.ExecutePlugin(hb.Plugin, currentHoverStackEventArgs.StackSevEntries, time_cursor, hb.Parameter);
                            break;

                        case PluginHotkeyBinding.SelectedSignals.StackChannels:
                            retsigs = this.ExecutePlugin(hb.Plugin, currentHoverStackEventArgs.StackChannelEntries, time_cursor, hb.Parameter);
                            break;

                        case PluginHotkeyBinding.SelectedSignals.StackSelectedFiles:
                            retsigs = this.ExecutePlugin(hb.Plugin, currentHoverStackEventArgs.StackSelectionSevEntries, time_cursor, hb.Parameter);
                            break;

                        case PluginHotkeyBinding.SelectedSignals.StackSelectedChannels:
                            retsigs = this.ExecutePlugin(hb.Plugin, currentHoverStackEventArgs.StackSelectionChannelEntries, time_cursor, hb.Parameter);
                            break;

                        case PluginHotkeyBinding.SelectedSignals.AllSignalEntries:
							retsigs = this.ExecutePlugin(hb.Plugin, UnisensXmlFileManager.CurrentUnisensInstance.Xdocument.Root.Elements("{http://www.unisens.org/unisens2.0}signalEntry"), time_cursor, hb.Parameter);
                            break;

                        case PluginHotkeyBinding.SelectedSignals.AllEventEntries:
							retsigs = this.ExecutePlugin(hb.Plugin, UnisensXmlFileManager.CurrentUnisensInstance.Xdocument.Root.Elements("{http://www.unisens.org/unisens2.0}eventEntry"), time_cursor, hb.Parameter);
                            break;

                        case PluginHotkeyBinding.SelectedSignals.AllValuesEntries:
							retsigs = this.ExecutePlugin(hb.Plugin, UnisensXmlFileManager.CurrentUnisensInstance.Xdocument.Root.Elements("{http://www.unisens.org/unisens2.0}valuesEntry"), time_cursor, hb.Parameter);
                            break;

                        case PluginHotkeyBinding.SelectedSignals.Dialog:
                            retsigs = this.ExecutePluginWithDialog(hb.Plugin, time_cursor, hb.Parameter);
                            break;

                        case PluginHotkeyBinding.SelectedSignals.All: // TODO: All Entries from Unisens.xml
                            List<XElement> entryElements = new List<XElement>();
							entryElements.AddRange(UnisensXmlFileManager.CurrentUnisensInstance.Xdocument.Root.Elements("{http://www.unisens.org/unisens2.0}signalEntry"));
							entryElements.AddRange(UnisensXmlFileManager.CurrentUnisensInstance.Xdocument.Root.Elements("{http://www.unisens.org/unisens2.0}eventEntry"));
							entryElements.AddRange(UnisensXmlFileManager.CurrentUnisensInstance.Xdocument.Root.Elements("{http://www.unisens.org/unisens2.0}valuesEntry"));

                            retsigs = this.ExecutePlugin(hb.Plugin, entryElements, time_cursor, hb.Parameter);
                            break;
                    }

                    if (retsigs != null)
                    {
                        foreach (XElement xe in retsigs)
                        {
                            if (StackerControl.IsSignalEventValueEntry(xe))
                            {
                                this.signalviewercontrol.stackercontrol.DropSignalEventValueEntry(xe, currentHoverStackEventArgs.Stack);
                            }
                        }
                    }
                }
            }
        }

        // wird vom stapel-kontextmenü ausgelöst
        private void ExecPlugin_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ExecPluginCommandParameter exec = (ExecPluginCommandParameter)e.Parameter;

            IEnumerable<XElement> sev_entries = RendererManager.GetSevEntries(exec.Stack);

            IEnumerable<XElement> retsigs = this.ExecutePlugin(exec.Plugin, sev_entries, exec.TimeCursor, exec.Parameter);

            if (retsigs != null)
            {
                foreach (XElement xe in retsigs)
                {
                    if (StackerControl.IsSignalEventValueEntry(xe))
                    {
                        signalviewercontrol.stackercontrol.DropSignalEventValueEntry(xe, exec.Stack);
                    }
                }
            }
        }

        private IEnumerable<XElement> ExecutePlugin(IDspPlugin1 p, IEnumerable<XElement> selectedsignals, double time_cursor, string parameter)
        {
            IEnumerable<XElement> retsigs = null;

            foreach (XElement xe in selectedsignals)
            {
                RendererManager.CloseRenderer(xe);
            }

            try
            {
                string path = Environment.CurrentDirectory + "\\" + "unisens.xml";
				retsigs = p.Main(UnisensXmlFileManager.CurrentUnisensInstance.Xdocument, selectedsignals, path, time_cursor, signalviewercontrol.SelectionStart, signalviewercontrol.SelectionEnd, parameter);
			}
			catch (Exception ex)
			{
				retsigs = null;
				MessageBox.Show("Exception im Plugin: " + ex.Message);
			}

            foreach (XElement xe in selectedsignals)
            {
                try
                {
                    RendererManager.ReOpenRenderer(xe);
                }
                catch
                {
                    RendererManager.KillRenderer(xe);
                }
            }

            if (retsigs != null)
            {
                foreach (XElement xe in retsigs)
                {
                    if (StackerControl.IsSignalEventValueEntry(xe))
                    {
                        signalviewercontrol.stackercontrol.DropSignalEventValueEntry(xe, null);
                    }
                }
            }

            signalviewercontrol.Deselect();
            if (RendererManager.TimeMax < RendererManager.Time)
            {
                RendererManager.Scroll(0.0);
            }
            else
            {
                RendererManager.Render();
            }

            // Save Unisens file after Plugin execution
			Executed_Save(null, null);

            return retsigs;
        }
        #endregion

        #region RibbonActions

        #region MenuActions

        private RibbonGalleryItem CreateFileRibbionItem(string file)
        {
            RibbonGalleryItem ribbonGalleryItem = new RibbonGalleryItem();
            ribbonGalleryItem.Content = file.Substring(file.LastIndexOf('\\', file.LastIndexOf('\\') - 1) + 1);
            ribbonGalleryItem.ToolTip = file;
            ribbonGalleryItem.DataContext = file;
            ribbonGalleryItem.Selected += new RoutedEventHandler(this.OpenRecentFileClick);
            return ribbonGalleryItem;
        }

        private void OpenRecentFileClick(object sender, RoutedEventArgs e)
        {
            string p = (string)((RibbonGalleryItem)sender).DataContext;
            if (p != null)
            {
				if (!File.Exists(p))
				{
					// delets a not existing entry
					this.lastOpendFiles.Remove(p);
					RGCrecentapps.Items.Remove(sender);

					// Fehlermeldung
					string errorMsg;
					string errorTitle;
					(new LocTextExtension("UnisensViewer:Translations:FileNotFoundMsg")).ResolveLocalizedValue(out errorMsg);
					(new LocTextExtension("UnisensViewer:Translations:FileNotFoundTitle")).ResolveLocalizedValue(out errorTitle);
					MessageBox.Show(errorMsg.Replace("%filename%", (string)((RibbonGalleryItem)sender).Content), errorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
				
					return;
				}

                unisensFileManager.Load(p);
            }
        }

        private void MenuItem_Export_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            sfd.Filter = "PNG (*.png)|*.png|Alle Dateien|*.*";

            if (sfd.ShowDialog() == true)
            {
                Export.ToPNG(sfd.FileName, UnisensView);
            }
        }

        private void MenuItem_Print_Click(object sender, RoutedEventArgs e)
        {
            Export.Print(UnisensView);
        }
        #endregion

        #region ButtonActions

        private void MenuItem_Click_CloseAllSignals(object sender, RoutedEventArgs e)
        {
            signalviewercontrol.CloseAllSignals();
        }

        DispatcherTimer timer;
        Stopwatch sw;
        double startTime = 0;
        private void MenuItem_Click_Playback(object sender, RoutedEventArgs e)
        {
            //SessionSettings.Instance.Update();
            //SessionSettings.Instance.WriteObject();

            if (sw != null && sw.IsRunning)
            {
                sw.Stop();
                timer.Stop();
                ButtonPlayback.LargeImageSource = new BitmapImage(new Uri(@"Images\LargeIcon_playback_play.png", UriKind.Relative));
                ButtonPlayback.Label = "Play";

                /*if(dlgvideo.IsVisible)
                    dlgvideo.Pause();*/
            }
            else
            {
                /*if (dlgvideo.IsVisible)
                    dlgvideo.Play();*/

                sw = new Stopwatch();
                sw.Start();

                startTime = RendererManager.Time;

                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds((double)1 / 10);
                timer.Tick += new EventHandler(timer_Tick);
                timer.Start();
                ButtonPlayback.LargeImageSource = new BitmapImage(new Uri(@"Images\LargeIcon_playback_pause.png", UriKind.Relative));
                ButtonPlayback.Label = "Pause";
            }
        }

        void timer_Tick(object sender, EventArgs e)
        {
            RendererManager.Scroll(startTime + (double)sw.ElapsedMilliseconds / 1000);

            /*if (dlgvideo.IsVisible)
                dlgvideo.Seek((int)(1000 * startTime) + (int) sw.ElapsedMilliseconds);*/
        }


		private void MenuItem_Click_AnalyzeData(object sender, RoutedEventArgs e)
		{
			string path = analyzerPath();
			if (path != null)
			{
				path = path.Replace("\"", "");
				Process p = new Process();
				ProcessStartInfo psi = new ProcessStartInfo();
				psi.FileName = path;
				psi.WorkingDirectory = Path.GetDirectoryName(path);
				psi.Arguments = "\"" + Path.GetDirectoryName(unisensFileManager.XmlFilePath) + "\"";
				// Vorbereitung für Übergabe der Ausgewählten Zeit (Abstimmung: Zeiten mit oder ohne Anfürhungszeichen, Zahl mit Punkt oder Komma (bis jetz Komma)
				//if (signalviewercontrol.SelectionStart < signalviewercontrol.SelectionEnd)
				//{
				//    psi.Arguments += " \"" + signalviewercontrol.SelectionStart + "\"";
				//    psi.Arguments += " \"" + signalviewercontrol.SelectionEnd + "\"";
				//}
				p.StartInfo = psi;
				p.Start();
			}
		}

		private string analyzerPath()
		{
			RegistryKey launchers_x64 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\JavaSoft\\Prefs\\de\\movisens\\launcher");
			if (launchers_x64 != null)
			{
				return ((string)launchers_x64.GetValue("data_analyzer"));
			}
            RegistryKey launchers_x86 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\JavaSoft\\Prefs\\de\\movisens\\launcher");
            if (launchers_x86 != null)
            {
                return ((string)launchers_x86.GetValue("data_analyzer"));
            }
			
			return null;	
		}

        private void Click_UseSincInterpolation_Check(object sender, RoutedEventArgs e)
        {
            RendererManager.SetInterpolation(true);
        }

        private void Click_UseSincInterpolation_Unckeck(object sender, RoutedEventArgs e)
        {
            RendererManager.SetInterpolation(false);
        }

        private void Click_UsePeaker_Check(object sender, RoutedEventArgs e)
        {
            RendererManager.SetPeak(true);
        }

        private void Click_UsePeaker_Uncheck(object sender, RoutedEventArgs e)
        {
            RendererManager.SetPeak(false);
        }

        private void RibbonButton_Click_Help(object sender, RoutedEventArgs e)
        {
            WindowHelp wh = new WindowHelp();
            wh.Owner = this;
            wh.Show();
        }

        private void Click_PhysicalValueRounding_Check(object sender, RoutedEventArgs e)
        {
            Rounding = true;
        }

        private void Click_PhysicalValueRouding_Uncheck(object sender, RoutedEventArgs e)
        {
            Rounding = false;
        }

        private void MenuItem_About_Click(object sender, RoutedEventArgs e)
        {
            DialogAbout dlgabout = new DialogAbout();
            dlgabout.Owner = this;
            dlgabout.ShowDialog();
        }

		private void MenuItem_Settings_Click(object sender, RoutedEventArgs e)
		{
			DialogSettings dlgabout = new DialogSettings();
			dlgabout.Owner = this;
			dlgabout.ShowDialog();
		}

        private void MenuItem_CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            CheckForUpdates("/checknow");
        }

        private void MenuItem_ConfigureUpdates_Click(object sender, RoutedEventArgs e)
        {
            CheckForUpdates("/configure");
        }

        private void Click_Fadenkreuz_Check(object sender, RoutedEventArgs e)
        {
            RendererManager.SetFadenkreuz(true);
        }

        private void Click_Fadenkreuz_Uncheck(object sender, RoutedEventArgs e)
        {
            RendererManager.SetFadenkreuz(false);
        }

        private void RibbonButton_Click_Homepage(object sender, RoutedEventArgs e)
        {
            string website = "https://github.com/Unisens/UnisensViewer";
            System.Diagnostics.Process.Start(website);
        }

        #endregion

        #endregion RibbonActions

        private void WindowMain_UpdateStatus(object sender, UpdateStatusEventArgs e)
        {
            status_sampleinfo.Content = e.SampleInfo;
        }

        private void WindowMain_HoverStack(object sender, HoverStackEventArgs e)
        {
            this.hoverStackEventArgs = e.ItemsControl != null ? e : null;
            e.Handled = true;
        }

        private void RibbonWindow_Loaded(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                this.UpdateStatus += new UpdateStatusEventHandler(this.WindowMain_UpdateStatus);

                signalviewercontrol.Focusable = true;
                PreviewKeyDown += new KeyEventHandler(this.WindowMain_PreviewKeyDown);

                // check and update last opend files
                if (UnisensViewer.Properties.Settings.Default.Last_Opend_Files == null)
                {
                    UnisensViewer.Properties.Settings.Default.Last_Opend_Files = new System.Collections.Specialized.StringCollection();
                    this.lastOpendFiles = UnisensViewer.Properties.Settings.Default.Last_Opend_Files;
                }

                foreach (string file in this.lastOpendFiles)
                {
                    RGCrecentapps.Items.Add(this.CreateFileRibbionItem(file));
                }

                // Used to display all signals on Startup
                if (UnisensViewer.Properties.Settings.Default.AutoLoad == true)
                {
                    UnisensXmlFileManager.CurrentUnisensInstance.FileLoaded += new UnisensXmlFileManager.FileEventHandler(CurrentUnisensInstance_FileLoaded);
                }

                string[] commandLineArgs = Environment.GetCommandLineArgs();

                unisensFileManager.New();

                if (commandLineArgs.Length == 2)
                {
                    logger.Debug("Received command line argument");
                    string path = commandLineArgs[1];

                    if (System.IO.File.Exists(path))
                    {
                        unisensFileManager.Load(path);
                    }
                }

                // Start the thread that will launch the updater in silent mode with 10 second delay.
                Thread thread = new Thread(new ThreadStart(StartSilent));
                thread.Start();
            }
        }


		private void CurrentUnisensInstance_FileLoaded(object sender, bool successfull)
		{
            using (new WaitCursor())
            {
                if (!successfull)
                {
                    logger.Debug("New unisens-file could not been opened sucessfully!");
                }else{
                    logger.Debug("New unisens-file has been opened, displaying signals...");

                    if (unisensFileManager.XmlFilePath != null)
                        signalviewercontrol.Views = new ObservableCollection<SessionView>(Directory.GetFiles(@".", "*.view").Select(path => new SessionView(path)));

                    // Displays all Signals
                    signalviewercontrol.CloseAllSignals();
                    signalviewercontrol.stackercontrol.Dropped(null, UnisensXmlFileManager.CurrentUnisensInstance.Xdocument.Root);

                    // update the file list
                    string filepath = UnisensXmlFileManager.CurrentUnisensInstance.XmlFilePath;
                    if (filepath != null)
                    {
                        // remove file if exist and add it at the top
                        int index = this.lastOpendFiles.IndexOf(filepath);
                        if (index != -1)
                        {
                            // file exist => remove it
                            this.lastOpendFiles.RemoveAt(index);
                        }

                        // Add a new item at top
                        this.lastOpendFiles.Insert(0, filepath);

                        // enable plugins when the file loaded
                        foreach (RibbonGroup rg in PluginTab.Items)
                        {
                            for (int i = 0; i < rg.Items.Count; i++)
                            {
                                ((RibbonMenuButton)rg.Items.GetItemAt(i)).IsEnabled = true;
                            }
                        }
                    }

                    try
                    {
                        RGCrecentapps.Items.Clear();
                    }
                    catch (Exception) { }

                    try
                    {
                        foreach (string file in this.lastOpendFiles)
                        {
                            RGCrecentapps.Items.Add(this.CreateFileRibbionItem(file));
                        }
                    }
                    catch (Exception) { }

                    // Update FileName
                    if (filepath != null)
                    {
                        filepath = Path.GetDirectoryName(filepath);
                        CurrentFileName = filepath.Substring(filepath.LastIndexOf('\\') + 1);
                    }
                }
            }
		}

        private void Click_SampleAndHold_Check(object sender, RoutedEventArgs e)
        {
            RendererManager.SetSampleAndHold(true);
        }

        private void Click_Point_Check(object sender, RoutedEventArgs e)
        {
            RendererManager.SetPoint(true);
        }

        private void Click_Linear_Check(object sender, RoutedEventArgs e)
        {
            RendererManager.SetLinear(true);
        }

        //private void Click_AbsoluteDauerTime_Check(object sender, RoutedEventArgs e)
        //{
        //    CheckBox_FormatDauerTime.IsChecked = false;
        //    CheckBox_FormatAbsoluteTime.IsChecked = false;
        //    if (signalviewercontrol != null)
        //    {
        //        signalviewercontrol.axiscontrol_time.AbsolutDauerTime_Check();
        //    }
        //}

        private void Click_DurationMessung_Check(object sender, RoutedEventArgs e)
        {
            CheckBox_FormatAbsoluteTime.IsChecked = false;
            if (signalviewercontrol != null)
            {
                signalviewercontrol.axiscontrol_time.DurationMessung_Check();
            }
        }

        private void Click_AbsolutTime_Check(object sender, RoutedEventArgs e)
        {
            CheckBox_FormatDauerTime.IsChecked = false;
            if (signalviewercontrol != null)
            {
                signalviewercontrol.axiscontrol_time.AbsolutTime_Check();
            }
        }

        private DialogVideo dialogVideo;

        private void MenuItem_Video_SelectFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string formats = "All Videos Files |*.dat; *.wmv; *.3g2; *.3gp; *.3gp2; *.3gpp; *.amv; *.asf;  *.avi; *.bin; *.cue; *.divx; *.dv; *.flv; *.gxf; *.iso; *.m1v; *.m2v; *.m2t; *.m2ts; *.m4v; " +
                      " *.mkv; *.mov; *.mp2; *.mp2v; *.mp4; *.mp4v; *.mpa; *.mpe; *.mpeg; *.mpeg1; *.mpeg2; *.mpeg4; *.mpg; *.mpv2; *.mts; *.nsv; *.nuv; *.ogg; *.ogm; *.ogv; *.ogx; *.ps; *.rec; *.rm; *.rmvb; *.tod; *.ts; *.tts; *.vob; *.vro; *.webm";

            openFileDialog.Filter = formats;

            if (openFileDialog.ShowDialog() == true)
            {
                SelectFile_Button24.IsEnabled = false;

                dialogVideo = new DialogVideo();
                dialogVideo.DataContext = signalviewercontrol;
                dialogVideo.SetVideoFile(new Uri(openFileDialog.FileName));
                dialogVideo.Owner = this;
                dialogVideo.Show();
                /*dialogVideo.Play();
                dialogVideo.Pause();*/

                dialogVideo.timeSlider.Value = RendererManager.Time / RendererManager.TimeMax;

                RendererManager.TimeChanged += RendererManager_TimeChanged;
                dialogVideo.Closed += dlgvideo_Closed;
            }
        }

        private void dlgvideo_Closed(object sender, EventArgs e)
        {
            SelectFile_Button24.IsEnabled = true;

            RendererManager.TimeChanged -= RendererManager_TimeChanged;
        }

        StatusTimeConverter _statusTimeConverter = new StatusTimeConverter();

        private void RendererManager_TimeChanged(object sender, EventArgs e)
        {
            if (dialogVideo.IsVisible)
            {
                double offset = 0;
                double.TryParse(videoOffset.Text, out offset);
                double seekTo = RendererManager.Time * 1000;

                var span = dialogVideo.mePlayer.NaturalDuration;
                int position = (int)(offset * 1000 + seekTo);

                dialogVideo.Seek(span.HasTimeSpan && position <= span.TimeSpan.Milliseconds ? span.TimeSpan.Milliseconds : position);
                dialogVideo.timeSlider.Value = RendererManager.Time / RendererManager.TimeMax;

                dialogVideo.timeLabel.Content = _statusTimeConverter.Convert(RendererManager.Time, null, null, null);
            }
        }


		#region File Commands
		private void Executed_New(object sender, ExecutedRoutedEventArgs e)
		{
			unisensFileManager.New();
		}

		private void Executed_Open(object sender, ExecutedRoutedEventArgs e)
		{
			Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
			ofd.Filter = "Unisens (*.xml)|*.xml|Alle Dateien|*.*";
			ofd.FileName = "unisens.xml";

			if (ofd.ShowDialog() == true)
			{
				unisensFileManager.Load(ofd.FileName);
			}
		}

		private void Executed_CloseFile(object sender, ExecutedRoutedEventArgs e)
		{
			if (unisensFileManager.IsDirty == true)
			{
				if (MessageBox.Show("XML-Datei vor dem Schließen speichern?", "XML-Datei schließen", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
				{
					if (unisensFileManager.XmlFilePath != null)
					{
						this.Executed_Save(null, null);
					}
					else
					{
						this.Executed_SaveAs(null, null);
					}
				}
			}

			CurrentFileName = null;
			unisensFileManager.Close();

            signalviewercontrol.Views.Clear();
		}

		private void CanExecute_CloseFile(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = unisensFileManager.XmlFilePath != null;
		}

		private void Executed_Save(object sender, ExecutedRoutedEventArgs e)
		{
			try
			{
				unisensFileManager.Save();
			}
			catch (Exception exeption)
			{
				MessageBox.Show(exeption.Message, "Save", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Executed_SaveAs(object sender, ExecutedRoutedEventArgs e)
		{
			Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
			sfd.Filter = "XML (*.xml)|*.xml|Alle Dateien|*.*";
			sfd.FileName = unisensFileManager.Xdocument.Root.Name.LocalName + ".xml";

			if (sfd.ShowDialog() == true)
			{
				try
				{
					unisensFileManager.SaveAs(sfd.FileName);
				}
				catch (Exception exeption)
				{
					MessageBox.Show(exeption.Message, "Save", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void CanExecute_Save(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = unisensFileManager.IsDirty && unisensFileManager.XmlFilePath != null;
		}

		private void CanExecute_SaveAs(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = unisensFileManager.Xdocument != null;
		}

		#endregion

		private void RibbonWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Executed_CloseFile(null, null);
		}

        /// <summary>
        /// Switches the localization culture.
        /// </summary>
        /// <param name="culture">ISO code of the new culture or "system" to take the culture selected in the system.</param>
        public static void SwitchCulture(string culture)
        {
			if (culture.Equals("system"))
			{
				culture = Thread.CurrentThread.CurrentCulture.Name;
			}
			
            CultureInfo ci = CultureInfo.InvariantCulture;
            try
            {
                ci = new CultureInfo(culture);
            }
            catch (CultureNotFoundException)
            {
                try
                {
                    // Try language without region
                    ci = new CultureInfo(culture.Substring(0, 2));
                }
                catch (Exception)
                {
                    ci = CultureInfo.InvariantCulture;
                }
            }
            finally
            {
                //Uncomment next line to check english translation
                //ci = new CultureInfo("en");
                LocalizeDictionary.Instance.Culture = ci;

            }
        }

		#region old plugins
		private void Execute_CropSelection(object sender, ExecutedRoutedEventArgs e)
		{
            using (new WaitCursor())
            {
                if (Crop.CropSelection(signalviewercontrol.SelectionStart, signalviewercontrol.SelectionEnd))
                {
                    // Deselect all and Render again
                    signalviewercontrol.Deselect();

                    if (RendererManager.TimeMax < RendererManager.Time)
                    {
                        RendererManager.Scroll(0.0);
                    }
                    else
                    {
                        RendererManager.Render();
                    }

                    // Save Unisens file after Plugin execution
                    Executed_Save(null, null);
                }
            }
		}

		private void CanExecute_CropSelection(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = unisensFileManager.XmlFilePath != null
				&& signalviewercontrol.SelectionStart < signalviewercontrol.SelectionEnd
				&& signalviewercontrol.SelectionStart < RendererManager.TimeMax;
		}

		private void Execute_Trim(object sender, ExecutedRoutedEventArgs e)
		{
            using (new WaitCursor())
            {
                if (Crop.TrimSelection(signalviewercontrol.SelectionStart, signalviewercontrol.SelectionEnd))
                {
                    // Deselect all and Render again
                    signalviewercontrol.Deselect();

                    if (RendererManager.TimeMax < RendererManager.Time)
                    {
                        RendererManager.Scroll(0.0);
                    }
                    else
                    {
                        RendererManager.Render();
                    }

                    // Save Unisens file after Plugin execution
                    Executed_Save(null, null);
                }
            }
		}

		private void CanExecute_Trim(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = unisensFileManager.XmlFilePath != null
				&& signalviewercontrol.SelectionStart < signalviewercontrol.SelectionEnd
				&& signalviewercontrol.SelectionStart < RendererManager.TimeMax
				&& (signalviewercontrol.SelectionStart == 0 ^ signalviewercontrol.SelectionEnd > RendererManager.TimeMax);
		}

		private void Execute_setMarker(object sender, ExecutedRoutedEventArgs e)
		{

            double timeCursor;
            ObservableCollection<RenderSlice> selectedRsList;
            string markerSymbol = "M";

            DialogsMarkerNew inputDialog = new DialogsMarkerNew();
            if (inputDialog.ShowDialog() == true)
            {
                markerSymbol = inputDialog.MarkerName;
            }

            using (new WaitCursor())
            {
                if (e.Parameter != null && e.Parameter.Equals("ContextMenu"))
                {
                    // Informationen vom Contextmenu
                    timeCursor = this.signalviewercontrol.stackercontrol.PluginContextMenuTimeCursor;
                    selectedRsList = this.signalviewercontrol.stackercontrol.PluginContextMenuSelectedRsList;
                }
                else
                {
                    // Informationen von der aktuellen Mausposition
                    HoverStackEventArgs currentHoverStackEventArgs = hoverStackEventArgs;
                    if (currentHoverStackEventArgs == null)
                    {
                        return;
                    }

                    Point point = Mouse.GetPosition(currentHoverStackEventArgs.ItemsControl);
                    timeCursor = RendererManager.Time + (RendererManager.TimeStretch * point.X / currentHoverStackEventArgs.ItemsControl.ActualWidth);
                    markerSymbol = (e.Parameter == null || e.Parameter.Equals(string.Empty)) ? "M" : e.Parameter.ToString();
                    selectedRsList = currentHoverStackEventArgs.Stack;
                }

                IEnumerable<XElement> retsigs = Marker.setMarker(RendererManager.GetSevEntries(selectedRsList), timeCursor, markerSymbol);

                // Neues signal hinzufuegen
                if (retsigs != null)
                {
                    foreach (XElement xe in retsigs)
                    {
                        if (StackerControl.IsSignalEventValueEntry(xe))
                        {
                            signalviewercontrol.stackercontrol.DropSignalEventValueEntry(xe, selectedRsList);
                        }
                    }
                }

                // Deselect all and Render again
                signalviewercontrol.Deselect();

                if (RendererManager.TimeMax < RendererManager.Time)
                {
                    RendererManager.Scroll(0.0);
                }
                else
                {
                    RendererManager.Render();
                }

                // Save Unisens file after Plugin execution
                Executed_Save(null, null);
            }
		}

		private void CanExecute_setMarker(object sender, CanExecuteRoutedEventArgs e)
		{
			//TODO
			e.CanExecute = true;
		}

		private void Execute_deleteMarker(object sender, ExecutedRoutedEventArgs e)
		{
            using (new WaitCursor())
            {
                ObservableCollection<RenderSlice> selectedRsList;
                if (e.Parameter != null && e.Parameter.Equals("ContextMenu"))
                {
                    // Informationen vom Contextmenu
                    selectedRsList = this.signalviewercontrol.stackercontrol.PluginContextMenuSelectedRsList;
                }
                else
                {
                    // Informationen von der aktuellen Mausposition
                    HoverStackEventArgs currentHoverStackEventArgs = hoverStackEventArgs;
                    if (currentHoverStackEventArgs == null)
                    {
                        return;
                    }

                    selectedRsList = currentHoverStackEventArgs.Stack;
                }
                Marker.deleteMarker(RendererManager.GetSevEntries(selectedRsList), signalviewercontrol.SelectionStart, signalviewercontrol.SelectionEnd);

                // Deselect all and Render again
                signalviewercontrol.Deselect();

                if (RendererManager.TimeMax < RendererManager.Time)
                {
                    RendererManager.Scroll(0.0);
                }
                else
                {
                    RendererManager.Render();
                }

                // Save Unisens file after Plugin execution
                Executed_Save(null, null);
            }
		}

		private void CanExecute_deleteMarker(object sender, CanExecuteRoutedEventArgs e)
		{
			//TODO
			e.CanExecute = (signalviewercontrol.SelectionStart != signalviewercontrol.SelectionEnd);
		}

		private void Execute_truncateMarker(object sender, ExecutedRoutedEventArgs e)
		{
            using (new WaitCursor())
            {
                double timeCursor;
                ObservableCollection<RenderSlice> selectedRsList;
                if (e.Parameter != null && e.Parameter.Equals("ContextMenu"))
                {
                    // Informationen vom Contextmenu
                    timeCursor = this.signalviewercontrol.stackercontrol.PluginContextMenuTimeCursor;
                    selectedRsList = this.signalviewercontrol.stackercontrol.PluginContextMenuSelectedRsList;
                }
                else
                {
                    // Informationen von der aktuellen Mausposition
                    HoverStackEventArgs currentHoverStackEventArgs = hoverStackEventArgs;
                    if (currentHoverStackEventArgs == null)
                    {
                        return;
                    }

                    Point point = Mouse.GetPosition(currentHoverStackEventArgs.ItemsControl);
                    timeCursor = RendererManager.Time + (RendererManager.TimeStretch * point.X / currentHoverStackEventArgs.ItemsControl.ActualWidth);
                    selectedRsList = currentHoverStackEventArgs.Stack;
                }

                Marker.truncateMarker(RendererManager.GetSevEntries(selectedRsList), timeCursor);

                // Deselect all and Render again
                signalviewercontrol.Deselect();

                if (RendererManager.TimeMax < RendererManager.Time)
                {
                    RendererManager.Scroll(0.0);
                }
                else
                {
                    RendererManager.Render();
                }

                // Save Unisens file after Plugin execution
                Executed_Save(null, null);
            }
		}

		private void CanExecute_truncateMarker(object sender, CanExecuteRoutedEventArgs e)
		{
			//TODO
			e.CanExecute = (signalviewercontrol.SelectionStart == signalviewercontrol.SelectionEnd);
		}

        private void Execute_setArtifacts(object sender, ExecutedRoutedEventArgs e)
        {
            string artifactSymbol_Start = "(artifact";
            string artifactSymbol_End = "artifact)";

            DialogsArtifactsNew inputDialog = new DialogsArtifactsNew();
            if (inputDialog.ShowDialog() == true)
            {
                artifactSymbol_Start = "(" + inputDialog.ArtifactsName;
                artifactSymbol_End = inputDialog.ArtifactsName + ")";
            }

            using (new WaitCursor())
            {
                //ObservableCollection<RenderSlice> selectedRsList;
                //

                IEnumerable<XElement> retsigs = Artifacts.setArtifact(signalviewercontrol.SelectionStart, signalviewercontrol.SelectionEnd, artifactSymbol_Start, artifactSymbol_End);

                // Neues signal hinzufuegen
                if (retsigs != null)
                {
                    ObservableCollection<RenderSlice> selectedRsList = this.signalviewercontrol.stackercontrol.PluginContextMenuSelectedRsList;
                    foreach (XElement xe in retsigs)
                    {
                        if (StackerControl.IsSignalEventValueEntry(xe))
                        {
                            signalviewercontrol.stackercontrol.DropSignalEventValueEntry(xe, selectedRsList);
                        }
                    }
                }

                // Deselect all and Render again
                signalviewercontrol.Deselect();

                if (RendererManager.TimeMax < RendererManager.Time)
                {
                    RendererManager.Scroll(0.0);
                }
                else
                {
                    RendererManager.Render();
                }

                // Save Unisens file after Plugin execution
                Executed_Save(null, null);
            }
        }

        private void CanExecute_setArtifacts(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = unisensFileManager.XmlFilePath != null
                && signalviewercontrol.SelectionStart < signalviewercontrol.SelectionEnd
                && signalviewercontrol.SelectionStart < RendererManager.TimeMax;
        }
		#endregion

        private void Execute_setMarkerList(object sender, ExecutedRoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                string path = Environment.CurrentDirectory + "\\" + "unisens.xml";
                IEnumerable<XElement> retsigs = MarkerList.setMarkerList(path);

                // Deselect all and Render again
                signalviewercontrol.Deselect();

                if (RendererManager.TimeMax < RendererManager.Time)
                {
                    RendererManager.Scroll(0.0);
                }
                else
                {
                    RendererManager.Render();
                }

                // Save Unisens file after Plugin execution
                Executed_Save(null, null);
            }
        }

        private void CanExecute_setMarkerList(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = unisensFileManager.XmlFilePath != null;
        }

        public void Gridsplitter_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            GridSplitter spliter = (GridSplitter)sender;
            double x = signalviewercontrol.ActualWidth;
            double y = Math.Truncate(Scrollviewer.ActualWidth);
            double maxbreite = SystemParameters.PrimaryScreenWidth
                                - SystemParameters.VerticalScrollBarWidth
                                - 5.0
                                - y;
            RendererManager.Drag(signalviewercontrol.stackercontrol , maxbreite);
        }

	}
}
