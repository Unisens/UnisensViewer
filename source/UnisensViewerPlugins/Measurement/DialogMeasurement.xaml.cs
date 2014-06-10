//--------------------------------------------------------------------------------------------
// <copyright file="DialogMeasurement.xaml.cs" company="FZI Forschungszentrum Informatik">
// Copyright 2011 FZI Forschungszentrum Informatik, movisens GmbH
// Giau Pham (pham@fzi.de)
// </copyright>
//--------------------------------------------------------------------------------------------
namespace UnisensViewerPack1
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Xml.Linq;
    using UnisensViewerClrCppLibrary;
    using UnisensViewerLibrary;
    using org.unisens;
    using System.Windows.Forms;
    using System.Data;
    using System.Windows.Forms.VisualStyles;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Text;
    using System.Collections;
    using System.Collections.ObjectModel;
    /// <summary>
    /// Dialog of Measurement
    /// </summary>
    public partial class DialogMeasurement : Window
    {
        string [] stringtoclipboard;
        TabItem ti;
        /// <summary>
        /// the object Measurement
        /// </summary>
        private Measurement measurement = new Measurement();

        /// <summary>
        /// time_start of seleted signal
        /// </summary>
        private double timeStart = Measurement.PluginTimeStart;

        /// <summary>
        /// time_end of selected signal
        /// </summary>
        private double timeEnd = Measurement.PluginTimeEnd;

        /// <summary>
        /// lenght of selected signal
        /// </summary>
        private double timeLength = Measurement.PluginTimeLength;

        /// <summary>
        /// the unisens XML
        /// </summary>
        private XDocument unisensxml = Measurement.UnisensXml;

        /// <summary>
        /// the selected signals
        /// </summary>
        //private IEnumerable<XElement> selectedsignals = Measurement.SelectedSignals;

        private org.unisens.SignalEntry entry = Measurement.Entry;

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogMeasurement"/> class.
        /// </summary>
        public DialogMeasurement()
        {
            InitializeComponent();
            stringtoclipboard = new string[entry.getChannelCount()];
            foreach (XElement xe in Measurement.SelectedSignals)
            {
                switch (xe.Name.LocalName)
                {
                    case "signalEntry":
                    case "eventEntry":
                    case "valuesEntry":
                        switch (UnisensViewerLibrary.MeasurementEntry.GetFileFormat(xe))
                        {
                            case UnisensViewerLibrary.FileFormat.Bin:
                                this.timeLength = this.timeEnd - this.timeStart;
                                this.SampleValueCalculation(this.unisensxml, this.entry, xe, this.timeStart, this.timeEnd);
                                break;
                        }
                        break;
                }
                break;
            }
        }

        /// <summary>
        /// Show the time of selected signal.
        /// </summary>
        /// <param name="time">The time: time_start, time_end or time_length</param>
        /// <returns> string of DateTime</returns>
        private string Time_Anzeige(double time)
        {
            string timestampStart = unisensxml.Root.Attribute("timestampStart").Value;
            DateTime TimeStampStart = Convert.ToDateTime(timestampStart);
            DateTime dt = TimeStampStart.AddSeconds(time);
            string a = String.Format("{0:mm}", dt);
            string b = String.Format("{0:ss}", dt);
            string c = String.Format("{0:fff}", dt);
            String dateStr = dt.Hour + ":" + a + ":" + b + "." + c + " Uhr";
            return dateStr;
        }

        /// <summary>
        /// the Samples_value calculation.
        /// </summary>
        /// <param name="unisensxml">The unisensxml.</param>
        /// <param name="selectedsignals">The selectedsignals.</param>
        /// <param name="signalentry">The signalentry.</param>
        /// <param name="time_start">The time_start.</param>
        /// <param name="time_end">The time_end.</param>
        private void SampleValueCalculation(XDocument unisensxml, org.unisens.SignalEntry entry, XElement signalentry, double time_start, double time_end)
        {
            long nChannels = entry.getChannelCount();
            double sampleRate = entry.getSampleRate();
            long sampleStart = (long)(time_start * sampleRate);
            long sampleEnd = (long)(time_end * sampleRate);
            DataType dataType = entry.getDataType();
            // wenn es kein Auswahl gibt, wird es ganzes Signal aufgenommen
            long dataLength = 0;
            if (time_start == 0 & time_end == 0)
            {
                dataLength = entry.getCount();
                sampleStart = 0;
            }
            else
            {
                dataLength = (sampleEnd - sampleStart) + 1;
            }

            double[][] temp = new double[dataLength][];
            double[,] data = new double[dataLength, nChannels];

            switch (dataType)
            {
                case DataType.INT8:
                    sbyte[][] dataINT8 = (sbyte[][])entry.read(sampleStart, (int)dataLength);
                    for (int j = 0; j < dataLength; j++)
                    {
                        temp[j] = new double[dataINT8[j].Length];
                        for (int k = 0; k < nChannels; k++)
                            temp[j][k] = Convert.ToDouble(dataINT8[j][k]);
                    }
                    break;
                case DataType.UINT8:
                    byte[][] dataUINT8 = (byte[][])entry.read(sampleStart, (int)dataLength);
                    for (int j = 0; j < dataLength; j++)
                    {
                        temp[j] = new double[dataUINT8[j].Length];
                        for (int k = 0; k < nChannels; k++)
                            temp[j][k] = Convert.ToDouble(dataUINT8[j][k]);
                    }
                    break;
                case DataType.INT16:
                    short[][] dataINT16 = (short[][])entry.read(sampleStart, (int)dataLength);
                    for (int j = 0; j < dataLength; j++)
                    {
                        temp[j] = new double[dataINT16[j].Length];
                        for (int k = 0; k < nChannels; k++)
                            temp[j][k] = Convert.ToDouble(dataINT16[j][k]);
                    }
                    break;
                case DataType.UINT16:
                    UInt16[][] dataUINT16 = (UInt16[][])entry.read(sampleStart, (int)dataLength);
                    for (int j = 0; j < dataLength; j++)
                    {
                        temp[j] = new double[dataUINT16[j].Length];
                        for (int k = 0; k < nChannels; k++)
                            temp[j][k] = Convert.ToDouble(dataUINT16[j][k]);
                    }
                    break;
                case DataType.INT32:
                    int[][] dataINT32 = (int[][])entry.read(sampleStart, (int)dataLength);
                    for (int j = 0; j < dataLength; j++)
                    {
                        temp[j] = new double[dataINT32[j].Length];
                        for (int k = 0; k < nChannels; k++)
                            temp[j][k] = Convert.ToDouble(dataINT32[j][k]);
                    }
                    break;
                case DataType.UINT32:
                    UInt32[][] dataUINT32 = (UInt32[][])entry.read(sampleStart, (int)dataLength);
                    for (int j = 0; j < dataLength; j++)
                    {
                        temp[j] = new double[dataUINT32[j].Length];
                        for (int k = 0; k < nChannels; k++)
                            temp[j][k] = Convert.ToDouble(dataUINT32[j][k]);
                    }
                    break;
                case DataType.FLOAT:
                    float[][] dataFLOAT = (float[][])entry.read(sampleStart, (int)dataLength);
                    for (int j = 0; j < dataLength; j++)
                    {
                        temp[j] = new double[dataFLOAT[j].Length];
                        for (int k = 0; k < nChannels; k++)
                            temp[j][k] = Convert.ToDouble(dataFLOAT[j][k]);
                    }
                    break;
                case DataType.DOUBLE:
                    temp = (double[][])entry.read(sampleStart, (int)dataLength);
                    break;
            }
            data = convertToMultiArray(temp, (int)dataLength, nChannels);

            // berechne max. samplevalue und min. samplevalue
            double[] maxData = new double[nChannels];
            double[] minData = new double[nChannels];
            double[] diffData = new double[nChannels];
            double[] startData = new double[nChannels];
            double[] endData = new double[nChannels];
            double[] s_e_diffData = new double[nChannels];

            int[] maxIndex = new int[nChannels];
            int[] minIndex = new int[nChannels];
            int[] diffIndex = new int[nChannels];
            int[] startIndex = new int[nChannels];
            int[] endIndex = new int[nChannels];
            int[] s_e_diffIndex = new int[nChannels];

            double[] minPhysical = new double[nChannels];
            double[] maxPhysical = new double[nChannels];
            double[] diffPhysical = new double[nChannels];
            double[] startPhysical = new double[nChannels];
            double[] endPhysical = new double[nChannels];
            double[] s_e_diffPhysical = new double[nChannels];

            int[] minPos = new int[nChannels];
            int[] maxPos = new int[nChannels];

            double[] minPosition = new double[nChannels];
            double[] maxPosition = new double[nChannels];
            double[] diffPosition = new double[nChannels];
            double[] startPosition = new double[nChannels];
            double[] endPosition = new double[nChannels];
            double[] s_e_diffPosition = new double[nChannels];

            string[] minTime = new string[nChannels];
            string[] maxTime = new string[nChannels];
            double[] diffTime = new double[nChannels];
            string[] startTime = new string[nChannels];
            string[] endTime = new string[nChannels];
            double[] s_e_diffTime = new double[nChannels];

            double[] meanSample = new double[nChannels];
            double[] meanPhysical = new double[nChannels];
            double[] medianSample = new double[nChannels];
            double[] medianPhysical = new double[nChannels];
            double[] standard_deviation_Sample = new double[nChannels];
            double[] standard_deviation_Physical = new double[nChannels];

            for (int i = 0; i < nChannels; i++)
            {
                maxData[i] = data[0, i];
                minData[i] = data[0, i];
                startData[i] = data[0, i]; ;
                endData[i] = data[dataLength - 1, i];
                s_e_diffData[i] = endData[i] - startData[i];
                meanSample[i] = 0;
                meanPhysical[i] = 0;

                standard_deviation_Sample[i] = 0;

                //median berechnen
                if (dataLength % 2 == 0)
                { // gerade
                    medianSample[i] = (data[(dataLength / 2) - 1, i] + data[(dataLength / 2), i]) / 2;                   
                }
                else
                { // ungerade
                    medianSample[i] = data[(dataLength / 2), i];
                }
                medianPhysical[i] = (medianSample[i] - entry.getBaseline()) * entry.getLsbValue();

                for (int k = 0; k < dataLength; k++)
                {
                    if (maxData[i] < data[k, i])
                    {
                        maxData[i] = data[k, i];
                        maxPos[i] = k;
                    }
                    if (minData[i] > data[k, i])
                    {
                        minData[i] = data[k, i];
                        minPos[i] = k;
                    }
                    meanSample[i] += data[k, i];
                }
                // Mean berechnen
                meanSample[i] = Math.Round((meanSample[i] / dataLength), 3);
                meanPhysical[i] = (meanSample[i] - entry.getBaseline()) * entry.getLsbValue();

                // Standaradabweichung berechnen
                for (int k = 0; k < dataLength; k++)
                {
                    standard_deviation_Sample[i] += Math.Pow((data[k, i] - meanSample[i]), 2);
                }
                standard_deviation_Sample[i] = Math.Round((Math.Sqrt(standard_deviation_Sample[i] / (dataLength - 1))), 3);
                standard_deviation_Physical[i] = (standard_deviation_Sample[i] - entry.getBaseline()) * entry.getLsbValue();

                diffData[i] = maxData[i] - minData[i];

                maxIndex[i] = maxPos[i] + (int)sampleStart;
                minIndex[i] = minPos[i] + (int)sampleStart;
                diffIndex[i] = maxIndex[i] - minIndex[i];
                startIndex[i] = (int)sampleStart;
                endIndex[i] = (int)sampleEnd;
                s_e_diffIndex[i] = endIndex[i] - startIndex[i];

                minPhysical[i] = (minData[i] - entry.getBaseline()) * entry.getLsbValue();
                maxPhysical[i] = (maxData[i] - entry.getBaseline()) * entry.getLsbValue();
                diffPhysical[i] = maxPhysical[i] - minPhysical[i];
                startPhysical[i] = (startData[i] - entry.getBaseline()) * entry.getLsbValue();
                endPhysical[i] = (endData[i] - entry.getBaseline()) * entry.getLsbValue();
                s_e_diffPhysical[i] = endPhysical[i] - startPhysical[i];

                // Berechne die Positionen der Max. und Min. Sample Value
                minPosition[i] = (minPos[i] + sampleStart) / sampleRate;
                maxPosition[i] = (maxPos[i] + sampleStart) / sampleRate;
                diffPosition[i] = maxPosition[i] - minPosition[i];
                startPosition[i] = (sampleStart) / sampleRate;
                endPosition[i] = (sampleEnd) / sampleRate;
                s_e_diffPosition[i] = endPosition[i] - startPosition[i];

                if (unisensxml.Root.Attribute("timestampStart") != null)
                {
                    minTime[i] = Time_Anzeige(minPosition[i]);
                    maxTime[i] = Time_Anzeige(maxPosition[i]);
                    diffTime[i] = diffPosition[i];
                    startTime[i] = Time_Anzeige(startPosition[i]);
                    endTime[i] = Time_Anzeige(endPosition[i]);
                    s_e_diffTime[i] = s_e_diffPosition[i];
                }
                else
                {
                    minTime[i] = "No entry";
                    maxTime[i] = "No entry";
                    startTime[i] = "No entry";
                    endTime[i] = "No entry";
                }
            }

            // Zeichnen die Tabelle
            for (int i = 0; i < nChannels; i++)
            {
                ti = new TabItem();
                ti.Header = entry.getChannelNames()[i];
                ti.Name = entry.getChannelNames()[i];
                //Grid grid = new Grid(); 
                System.Windows.Controls.DataGrid datagrid = new System.Windows.Controls.DataGrid();
                DataTable dataTable = new DataTable();
                DataColumn column;

                column = new DataColumn();
                column.DataType = Type.GetType("System.String");
                column.ColumnName = "Parameter";
                column.Caption = "abc";
                column.Unique = false;
                dataTable.Columns.Add(column);

                column = new DataColumn();
                column.DataType = Type.GetType("System.String");
                column.ColumnName = "Minimum";
                dataTable.Columns.Add(column);

                column = new DataColumn();
                column.DataType = Type.GetType("System.String");
                column.ColumnName = "Maximum";
                dataTable.Columns.Add(column);

                column = new DataColumn();
                column.DataType = Type.GetType("System.String");
                column.ColumnName = "Difference";
                dataTable.Columns.Add(column);

                column = new DataColumn();
                column.DataType = Type.GetType("System.String");
                column.ColumnName = "First Sample";
                dataTable.Columns.Add(column);

                column = new DataColumn();
                column.DataType = Type.GetType("System.String");
                column.ColumnName = "Last Sample";
                dataTable.Columns.Add(column);

                column = new DataColumn();
                column.DataType = Type.GetType("System.String");
                column.ColumnName = "Range";
                dataTable.Columns.Add(column);

                //Rows befüllen
                DataRow row1 = dataTable.NewRow();
                row1["Parameter"] = "INDEX";
                row1["Minimum"] = minIndex[i];
                row1["Maximum"] = maxIndex[i];
                row1["Difference"] = diffIndex[i];
                row1["First Sample"] = startIndex[i];
                row1["Last Sample"] = endIndex[i];
                row1["Range"] = s_e_diffIndex[i];
                dataTable.Rows.Add(row1);

                DataRow row2 = dataTable.NewRow();
                row2["Parameter"] = "POSITION";
                row2["Minimum"] = String.Format("{0:HH:mm:ss.fff} hrs", new DateTime().Add(new TimeSpan(0, 0, 0, (int)minPosition[i], (int)(Math.Round((minPosition[i] - (int)minPosition[i]) * 1000)))));
                row2["Maximum"] = String.Format("{0:HH:mm:ss.fff} hrs", new DateTime().Add(new TimeSpan(0, 0, 0, (int)maxPosition[i], (int)(Math.Round((maxPosition[i] - (int)maxPosition[i]) * 1000)))));
                if (diffPosition[i] < 0)
                {
                    row2["Difference"] = "-" + String.Format("{0:HH:mm:ss.fff} hrs", new DateTime().Add(new TimeSpan(0, 0, 0, Math.Abs((int)diffPosition[i]), (int)(Math.Round((Math.Abs(diffPosition[i]) - Math.Abs((int)diffPosition[i])) * 1000)))));
                }
                else
                {
                    row2["Difference"] = String.Format("{0:HH:mm:ss.fff} hrs", new DateTime().Add(new TimeSpan(0, 0, 0, (int)diffPosition[i], (int)((diffPosition[i] - (int)diffPosition[i]) * 1000))));
                }
                row2["First Sample"] = String.Format("{0:HH:mm:ss.fff} hrs", new DateTime().Add(new TimeSpan(0, 0, 0, (int)startPosition[i], (int)(Math.Round((startPosition[i] - (int)startPosition[i]) * 1000)))));
                row2["Last Sample"] = String.Format("{0:HH:mm:ss.fff} hrs", new DateTime().Add(new TimeSpan(0, 0, 0, (int)endPosition[i], (int)(Math.Round((endPosition[i] - (int)endPosition[i]) * 1000)))));
                row2["Range"] = String.Format("{0:HH:mm:ss.fff} hrs", new DateTime().Add(new TimeSpan(0, 0, 0, (int)s_e_diffPosition[i], (int)(Math.Round((s_e_diffPosition[i] - (int)s_e_diffPosition[i]) * 1000)))));
                dataTable.Rows.Add(row2);

                DataRow row3 = dataTable.NewRow();
                row3["Parameter"] = "TIME";
                row3["Minimum"] = minTime[i];
                row3["Maximum"] = maxTime[i];
                if (diffTime[i] < 0)
                {
                    row3["Difference"] = "-" + String.Format("{0:HH:mm:ss.fff}", new DateTime().Add(new TimeSpan(0, 0, 0, Math.Abs((int)diffTime[i]), (int)(Math.Round((Math.Abs(diffTime[i]) - Math.Abs((int)diffTime[i])) * 1000)))));
                }
                else
                {
                    row3["Difference"] = String.Format("{0:HH:mm:ss.fff}", new DateTime().Add(new TimeSpan(0, 0, 0, (int)diffTime[i], (int)(Math.Round((diffTime[i] - (int)diffTime[i]) * 1000)))));
                }
                row3["First Sample"] = startTime[i];
                row3["Last Sample"] = endTime[i];
                row3["Range"] = String.Format("{0:HH:mm:ss.fff}", new DateTime().Add(new TimeSpan(0, 0, 0, (int)s_e_diffTime[i], (int)(Math.Round((s_e_diffTime[i] - (int)s_e_diffTime[i]) * 1000)))));
                dataTable.Rows.Add(row3);

                DataRow row4 = dataTable.NewRow();
                row4["Parameter"] = "PHYSICAL VALUE";
                row4["Minimum"] = minPhysical[i] + " " + entry.getUnit();
                row4["Maximum"] = maxPhysical[i] + " " + entry.getUnit();
                row4["Difference"] = diffPhysical[i] + " " + entry.getUnit();
                row4["First Sample"] = startPhysical[i] + " " + entry.getUnit();
                row4["Last Sample"] = endPhysical[i] + " " + entry.getUnit();
                row4["Range"] = s_e_diffPhysical[i] + " " + entry.getUnit();
                dataTable.Rows.Add(row4);

                DataRow row5 = dataTable.NewRow();
                row5["Parameter"] = "UNSCALED VALUE";
                row5["Minimum"] = minData[i];
                row5["Maximum"] = maxData[i];
                row5["Difference"] = diffData[i];
                row5["First Sample"] = startData[i];
                row5["Last Sample"] = endData[i];
                row5["Range"] = s_e_diffData[i];
                dataTable.Rows.Add(row5);

                //DataGrid füllen
                datagrid.ItemsSource = dataTable.DefaultView;
                datagrid.IsReadOnly = true;
                datagrid.SelectionUnit = DataGridSelectionUnit.Cell;
                datagrid.SelectAll();
                datagrid.SelectionMode = DataGridSelectionMode.Extended;

                // Groupbox 1
                char c = (char)177;
                System.Windows.Controls.GroupBox groupbox1 = new System.Windows.Controls.GroupBox();
                groupbox1.Header = "Statistical parameters";
                groupbox1.Margin = new Thickness(0, 0, 50, 0);
                TextBlock TextMean = new TextBlock(); TextMean.Text = "Mean: ";
                TextMean.Margin = new Thickness(0, 0, 0, 2);
                TextBlock TextMedian = new TextBlock(); TextMedian.Text = "Median: ";
                TextMedian.Margin = new Thickness(0, 0, 0, 2);
                TextBlock Text_Standard_Deviation = new TextBlock(); Text_Standard_Deviation.Text = "Standard Deviation: ";
                TextMean.Margin = new Thickness(0, 0, 0, 2);
                System.Windows.Controls.TextBox Mean = new System.Windows.Controls.TextBox();
                Mean.Text = meanPhysical[i].ToString() + " " + entry.getUnit() + "  (" + meanSample[i].ToString() + ")";
                Mean.BorderThickness = new Thickness(0);
                Mean.IsReadOnly = true;

                System.Windows.Controls.TextBox Median = new System.Windows.Controls.TextBox();
                Median.Text = medianPhysical[i].ToString() + " " + entry.getUnit() + "  (" + medianSample[i].ToString() + ")";
                Median.BorderThickness = new Thickness(0);
                Median.IsReadOnly = true;

                System.Windows.Controls.TextBox Standard_Deviation = new System.Windows.Controls.TextBox();
                Standard_Deviation.Text = standard_deviation_Sample[i].ToString() + " " + entry.getUnit();
                Standard_Deviation.Text = c.ToString() + standard_deviation_Physical[i].ToString() + " " + entry.getUnit() + "  (" + c.ToString() + standard_deviation_Sample[i].ToString() + ")";
                Standard_Deviation.BorderThickness = new Thickness(0);
                Standard_Deviation.IsReadOnly = true;

                StackPanel stackpanel1 = new StackPanel();
                stackpanel1.Children.Add(TextMean);
                stackpanel1.Children.Add(TextMedian);
                stackpanel1.Children.Add(Text_Standard_Deviation);

                StackPanel stackpanel2 = new StackPanel();
                stackpanel2.Children.Add(Mean);
                stackpanel2.Children.Add(Median);
                stackpanel2.Children.Add(Standard_Deviation);

                StackPanel stackpanel3 = new StackPanel();
                stackpanel3.Orientation = System.Windows.Controls.Orientation.Horizontal;
                stackpanel3.Children.Add(stackpanel1);
                stackpanel3.Children.Add(stackpanel2);
                groupbox1.Content = stackpanel3;

                // Groupbox 2
                System.Windows.Controls.GroupBox groupbox2 = new System.Windows.Controls.GroupBox();
                groupbox2.Header = "Entry Information";
                TextBlock textEntryId = new TextBlock(); textEntryId.Text = "Entry ID: ";
                textEntryId.Margin = new Thickness(0, 0, 0, 2);
                TextBlock textSampleRate = new TextBlock(); textSampleRate.Text = "Sample Rate: ";
                textSampleRate.Margin = new Thickness(0, 0, 0, 2);
                TextBlock textLSB = new TextBlock(); textLSB.Text = "LSB: ";

                System.Windows.Controls.TextBox entryId = new System.Windows.Controls.TextBox();
                entryId.Text = entry.getId();
                entryId.BorderThickness = new Thickness(0);
                entryId.IsReadOnly = true;
                System.Windows.Controls.TextBox samplerate = new System.Windows.Controls.TextBox();
                samplerate.Text = entry.getSampleRate().ToString();
                samplerate.BorderThickness = new Thickness(0);
                samplerate.IsReadOnly = true;
                System.Windows.Controls.TextBox lsb = new System.Windows.Controls.TextBox();
                lsb.Text = entry.getLsbValue().ToString();
                lsb.BorderThickness = new Thickness(0);
                lsb.IsReadOnly = true;

                StackPanel stackpanel4 = new StackPanel();
                stackpanel4.Children.Add(textEntryId);
                stackpanel4.Children.Add(textSampleRate);
                stackpanel4.Children.Add(textLSB);

                StackPanel stackpanel5 = new StackPanel();
                stackpanel5.Children.Add(entryId);
                stackpanel5.Children.Add(samplerate);
                stackpanel5.Children.Add(lsb);

                StackPanel stackpanel6 = new StackPanel();
                stackpanel6.Orientation = System.Windows.Controls.Orientation.Horizontal;
                stackpanel6.Children.Add(stackpanel4);
                stackpanel6.Children.Add(stackpanel5);

                groupbox1.Content = stackpanel3;
                groupbox2.Content = stackpanel6;

                StackPanel stackpanelGroup = new StackPanel();
                stackpanelGroup.Orientation = System.Windows.Controls.Orientation.Horizontal;
                stackpanelGroup.Margin = new Thickness(0, 20, 0, 0);
                stackpanelGroup.Children.Add(groupbox1);
                stackpanelGroup.Children.Add(groupbox2);
                //stackpanel.Children.Add(groupbox1);

                //--------------------------------------
                StackPanel stackpanel = new StackPanel();
                stackpanel.Children.Add(datagrid);
                stackpanel.Children.Add(stackpanelGroup);

                ti.Content = stackpanel;
                tabcontrol1.Items.Add(ti);

                StringBuilder sb = new StringBuilder();

                for (var j = 0; j < dataTable.Columns.Count; j++)
                    sb.Append(dataTable.Columns[j].ColumnName).Append("\t");
                sb.AppendLine();

                foreach (DataRow row in dataTable.Rows)
                {
                    for (var j = 0; j < dataTable.Columns.Count; j++)
                        sb.Append(row[j] ?? string.Empty).Append("\t");
                    sb.AppendLine();
                }
                stringtoclipboard[i] = sb.ToString();
            }
        }

        /// <summary>
        /// Handles the Click event of the Button control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Button_Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static double[,] convertToMultiArray(double[][] temp, int dataBlockLength, long nChannels)
        {
            double[,] returndata = new double[dataBlockLength, nChannels];
            for (int i = 0; i < dataBlockLength; i++)
            {
                for (int j = 0; j < nChannels; j++)
                {
                    if (temp == null)
                    {
                        return null;
                    }
                    else
                    {
                        returndata[i, j] = temp[i][j];
                    }
                }
            }
            return returndata;
        }

        private void Button_Copy(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.Clear();
            string str = "";
            for (int i = 0; i < entry.getChannelCount(); i++)
            {
                str += stringtoclipboard[i];
                str += "\n \n";
            }
            System.Windows.Clipboard.SetText(str);
        }
    }
}