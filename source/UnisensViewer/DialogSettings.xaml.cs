using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text.RegularExpressions;


namespace UnisensViewer
{
	/// <summary>
	/// Interaction logic for DialogSettings.xaml
	/// </summary>
	public partial class DialogSettings : Window
	{
		public DialogSettings()
		{
			InitializeComponent();
		}

		public string Round
		{
			get
			{
				return UnisensViewer.Properties.Settings.Default.Round.ToString();
			}
			set
			{
				string inputString = value as string;
				string numberString = "0";

				for (int i = 0; i < inputString.Length; i++)
				{
					if (Regex.IsMatch(inputString[i].ToString(), "^[0-9]$"))
					{
						numberString += inputString[i];
					}
				}

				UnisensViewer.Properties.Settings.Default.Round = int.Parse(numberString);
			}
		}

		private void ComboBox_Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			WindowMain.SwitchCulture((string)ComboBox_Language.SelectedValue);
		}

		private void Close_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void TextBox_Round_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			foreach (char c in e.Text)
			{
				if (!char.IsDigit(c))
				{
					e.Handled = true;
					return;
				}
			}
		}

	}
}
