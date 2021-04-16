namespace Microsoft.InnerEye.Listener.Wix.Actions
{
    partial class LicenseKeyForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LicenseKeyForm));
            this.MainCancelButton = new System.Windows.Forms.Button();
            this.NextButton = new System.Windows.Forms.Button();
            this.BackButton = new System.Windows.Forms.Button();
            this.titlePanel = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.licenseKeyTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.invalidKeyLabel = new System.Windows.Forms.Label();
            this.titlePanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainCancelButton
            // 
            this.MainCancelButton.Cursor = System.Windows.Forms.Cursors.Default;
            this.MainCancelButton.Location = new System.Drawing.Point(407, 311);
            this.MainCancelButton.Name = "MainCancelButton";
            this.MainCancelButton.Size = new System.Drawing.Size(75, 23);
            this.MainCancelButton.TabIndex = 0;
            this.MainCancelButton.Text = "Cancel";
            this.MainCancelButton.UseVisualStyleBackColor = true;
            this.MainCancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // NextButton
            // 
            this.NextButton.Location = new System.Drawing.Point(310, 311);
            this.NextButton.Name = "NextButton";
            this.NextButton.Size = new System.Drawing.Size(75, 23);
            this.NextButton.TabIndex = 1;
            this.NextButton.Text = "Next";
            this.NextButton.UseVisualStyleBackColor = true;
            this.NextButton.Click += new System.EventHandler(this.NextButton_Click);
            // 
            // BackButton
            // 
            this.BackButton.Enabled = false;
            this.BackButton.Location = new System.Drawing.Point(229, 311);
            this.BackButton.Name = "BackButton";
            this.BackButton.Size = new System.Drawing.Size(75, 23);
            this.BackButton.TabIndex = 2;
            this.BackButton.Text = "Back";
            this.BackButton.UseVisualStyleBackColor = true;
            this.BackButton.Click += new System.EventHandler(this.BackButton_Click);
            // 
            // titlePanel
            // 
            this.titlePanel.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.titlePanel.Controls.Add(this.panel2);
            this.titlePanel.Controls.Add(this.label2);
            this.titlePanel.Controls.Add(this.label1);
            this.titlePanel.Location = new System.Drawing.Point(-5, -4);
            this.titlePanel.Name = "titlePanel";
            this.titlePanel.Size = new System.Drawing.Size(506, 73);
            this.titlePanel.TabIndex = 3;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.panel2.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.panel2.Location = new System.Drawing.Point(3, 72);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(502, 1);
            this.panel2.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(32, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(191, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Click Next to validate the product key.";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(18, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(82, 14);
            this.label1.TabIndex = 0;
            this.label1.Text = "Product Key";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.panel1.Location = new System.Drawing.Point(-5, 300);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(507, 1);
            this.panel1.TabIndex = 4;
            // 
            // licenseKeyTextBox
            // 
            this.licenseKeyTextBox.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.licenseKeyTextBox.Location = new System.Drawing.Point(16, 152);
            this.licenseKeyTextBox.Name = "licenseKeyTextBox";
            this.licenseKeyTextBox.Size = new System.Drawing.Size(466, 21);
            this.licenseKeyTextBox.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(16, 93);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(153, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Use the following product key:";
            // 
            // invalidKeyLabel
            // 
            this.invalidKeyLabel.AutoSize = true;
            this.invalidKeyLabel.ForeColor = System.Drawing.Color.Red;
            this.invalidKeyLabel.Location = new System.Drawing.Point(385, 187);
            this.invalidKeyLabel.Name = "invalidKeyLabel";
            this.invalidKeyLabel.Size = new System.Drawing.Size(97, 13);
            this.invalidKeyLabel.TabIndex = 7;
            this.invalidKeyLabel.Text = "Invalid product key";
            this.invalidKeyLabel.Visible = false;
            // 
            // LicenseKeyForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(494, 346);
            this.Controls.Add(this.invalidKeyLabel);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.licenseKeyTextBox);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.titlePanel);
            this.Controls.Add(this.BackButton);
            this.Controls.Add(this.NextButton);
            this.Controls.Add(this.MainCancelButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "LicenseKeyForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Microsoft InnerEye Gateway Setup";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.LicenseKeyForm_Load);
            this.titlePanel.ResumeLayout(false);
            this.titlePanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button MainCancelButton;
        private System.Windows.Forms.Button NextButton;
        private System.Windows.Forms.Button BackButton;
        private System.Windows.Forms.Panel titlePanel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TextBox licenseKeyTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label invalidKeyLabel;
        private System.Windows.Forms.Panel panel2;
    }
}