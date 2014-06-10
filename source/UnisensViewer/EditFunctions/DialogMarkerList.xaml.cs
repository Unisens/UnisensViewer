using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Text.RegularExpressions;
using UnisensViewer.Translations;
using WPFLocalizeExtension.Extensions;

namespace UnisensViewer
{    
    /// <summary>
    /// Interaktionslogik für DialogMarkerList.xaml
    /// </summary>
    public partial class DialogMarkerList : Window
    {
        public static bool markerlist;
        public static double sampleRate;
        public static string entryId;
        public static string textfeld;
        public static string comment;

        private Regex entryID_Regex = new Regex(@"^([0-9]|[a-z]|_|-|[A-Z]){1,254}\.csv$");
        //private Regex entryID_Regex = new Regex(@"([0-9]|[a-z]|_|-|[A-Z]){1,254}(\.csv){0,1}");
        private Regex csv_Regex = new Regex(@"(.csv)");
        private Regex marker_Regex = new Regex(@"^([0-9]*)(;)(.*)(;)(.*)$");
        private Regex abstasrate_Regex = new Regex(@"^[0-9]{1,254}(,|.){0,125}");

        public DialogMarkerList()
        {
            InitializeComponent();          
        }

        public bool ID
        {          
            get 
            { 
                bool match = entryID_Regex.IsMatch(tb_EntryId.Text);
                // Ausdruck ist richtig mit .csv
                if (match == true)
                {
                    return true;
                }
                // Ausdruck enthält die .csv nicht.
                else if (csv_Regex.IsMatch(tb_EntryId.Text) == false)
                {
                    tb_EntryId.Text = tb_EntryId.Text + ".csv";
                    //return true;
                }
                return entryID_Regex.IsMatch(tb_EntryId.Text);
            }
        }

        public bool Marker
        {
            get
            {
                for (int i = 0; i < tb_textfeld.LineCount; i++)
                {
                    if (!marker_Regex.IsMatch(tb_textfeld.GetLineText(i)))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public bool Sample
        {
            get { return abstasrate_Regex.IsMatch(tb_SampleRate.Text); }
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            CultureInfo cultureInfo = CultureInfo.CurrentCulture;
            if (cultureInfo.ToString() == "de-DE")
            {
                tb_SampleRate.Text = tb_SampleRate.Text.Replace('.', ',');
            }
            else if (cultureInfo.ToString() == "en-US")
            {
                tb_SampleRate.Text = tb_SampleRate.Text.Replace(',', '.');
            }
            sampleRate = Convert.ToDouble(tb_SampleRate.Text);
            entryId = tb_EntryId.Text;
            textfeld = tb_textfeld.Text;
            comment = tb_Comment.Text;
            markerlist = true;          
            Close();
        }

        private void EntryID_Changed(object sender, RoutedEventArgs e)
        {
            if (!ID)
            {
                MessageBox.Show(GetUIString("UnisensViewer:Translations:EntryIdWarning"), GetUIString("UnisensViewer:Translations:InputErrors"));
                button_OK.IsEnabled = false;
            }
            else if (Marker && Sample)
            {
                button_OK.IsEnabled = true;
            }
            else
            {
                button_OK.IsEnabled = false;
            }
        }

        private void Marker_Changed(object sender, RoutedEventArgs e)
        {
            if (!Marker)
            {
                MessageBox.Show(GetUIString("UnisensViewer:Translations:MarkerListWarning"), GetUIString("UnisensViewer:Translations:InputErrors"));
                button_OK.IsEnabled = false;
            }
            else if (ID && Sample)
            {
                button_OK.IsEnabled = true;
            }
            else
            {
                button_OK.IsEnabled = false;
            }
        }

        private void Abstasrate_Changed(object sender, RoutedEventArgs e)
        {
            if (!Sample)
            {
                MessageBox.Show(GetUIString("UnisensViewer:Translations:SamplingWarning"), GetUIString("UnisensViewer:Translations:InputErrors"));
                button_OK.IsEnabled = false;
            }
            else if (ID && Marker)
            {
                button_OK.IsEnabled = true;
            }
            else
            {
                button_OK.IsEnabled = false;
            }
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
            markerlist = false;
        }

        public string GetUIString(string key)
        {
            string uiString;
            LocTextExtension locEx = new LocTextExtension(key);
            locEx.ResolveLocalizedValue(out uiString);
            return uiString;
        }
    }
}
