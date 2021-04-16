﻿namespace Microsoft.InnerEye.Listener.Wix.Actions
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using Microsoft.InnerEye.Gateway.Models;

    public partial class LicenseKeyForm : Form
    {
        /// <summary>
        /// The temporary settings.
        /// </summary>
        private readonly ProcessorSettings _processorSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="LicenseKeyForm"/> class.
        /// </summary>
        /// <param name="processorSettings">The temporary settings.</param>
        public LicenseKeyForm(ProcessorSettings processorSettings)
        {
            _processorSettings = processorSettings;

            InitializeComponent();

            Application.EnableVisualStyles();
        }

        /// <summary>
        /// Handles the Load event of the LicenseKeyForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void LicenseKeyForm_Load(object sender, EventArgs e)
        {
            licenseKeyTextBox.Text = Environment.GetEnvironmentVariable(_processorSettings.LicenseKeyEnvVar) ?? string.Empty;
        }

        /// <summary>
        /// Handles the Click event of the NextButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void NextButton_Click(object sender, EventArgs e)
        {
            NextButton.Enabled = false;

            await ValidateLicenseKeyAsync();

            NextButton.Enabled = true;
        }

        /// <summary>
        /// Validates the license key using the segmentation client and sets the dialog result.
        /// </summary>
        /// <returns></returns>
        private async Task ValidateLicenseKeyAsync()
        {
            var licenseKey = licenseKeyTextBox.Text;
            var (result, validationText) = await CustomActions.ValidateLicenseKeyAsync(_processorSettings, licenseKey);

            invalidKeyLabel.Text = validationText;

            if (!result)
            {
                invalidKeyLabel.Visible = true;
                return;
            }

            DialogResult = DialogResult.Yes;
        }

        /// <summary>
        /// Handles the Click event of the CancelButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        /// <summary>
        /// Handles the Click event of the BackButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void BackButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.No;
        }
    }
}