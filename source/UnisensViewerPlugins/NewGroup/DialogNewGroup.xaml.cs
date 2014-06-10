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
    public partial class DialogNewGroup : Window
    {
        public static bool newgroup;
        public static string idOfTheGroup;
        public DialogNewGroup()
        {
            InitializeComponent();
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            idOfTheGroup = textBox_IdOfTheGroup.Text.ToString();
            newgroup = true;
            Close();
        }
    }
}