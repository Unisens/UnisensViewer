//-----------------------------------------------------------------------
// <copyright file="ProgressBarDialog.cs" company="FZI Forschungszentrum Informatik">
// Copyright 2011 FZI Forschungszentrum Informatik, movisens GmbH
// </copyright>
//-----------------------------------------------------------------------
namespace UnisensViewerLibrary
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using System.Windows.Forms;
    
    /// <summary>
    /// Progress bar for plug ins.
    /// </summary>
    public partial class ProgressBarDialog : Form
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressBarDialog"/> class.
        /// </summary>
        /// <param name="min">The min. of progress bar.</param>
        /// <param name="max">The max. of progress bar.</param>
        /// <param name="step">The step of progress bar.</param>
        /// <param name="title">The title of progress bar.</param>
        public ProgressBarDialog(int min, int max, int step, string title)
        {
            InitializeComponent();

            this.progressBar.Value = min;
            this.progressBar.Minimum = min;
            this.progressBar.Maximum = max;
            this.progressBar.Step = step;
            this.Text = title;
        }

        /// <summary>
        /// Performs the step.
        /// </summary>
        public void PerformStep()
        {
            progressBar.PerformStep();
        }

        /// <summary>
        /// Handles the Load event of the ProgressBarDialog control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void ProgressBarDialog_Load(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Handles the Click event of the ProgressBar control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void ProgressBar_Click(object sender, EventArgs e)
        {
        }
    }
}