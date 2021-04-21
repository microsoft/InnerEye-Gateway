namespace Microsoft.InnerEye.Listener.Wix.Actions
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using Microsoft.InnerEye.Listener.Common.Providers;

    public partial class LicenseKeyForm : Form
    {
        /// <summary>
        /// Gateway processor config provider.
        /// </summary>
        private readonly GatewayProcessorConfigProvider _gatewayProcessorConfigProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="LicenseKeyForm"/> class.
        /// </summary>
        /// <param name="processorSettings">Gateway processor config provider.</param>
        public LicenseKeyForm(GatewayProcessorConfigProvider gatewayProcessorConfigProvider)
        {
            _gatewayProcessorConfigProvider = gatewayProcessorConfigProvider;

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
            var processorSettings = _gatewayProcessorConfigProvider.ProcessorSettings();

            inferenceUriTextBox.Text = processorSettings.InferenceUri.AbsoluteUri;
            licenseKeyTextBox.Text = processorSettings.LicenseKey;
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
            var inferenceUri = new Uri(inferenceUriTextBox.Text);
            var licenseKey = licenseKeyTextBox.Text;

            invalidKeyLabel.Text = string.Empty;
            invalidKeyLabel.Visible = false;

            var (result, validationText) = await CustomActions.ValidateLicenseKeyAsync(_gatewayProcessorConfigProvider, licenseKey, inferenceUri);

            invalidKeyLabel.Text = validationText;
            invalidKeyLabel.Visible = !result;

            if (!result)
            {
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