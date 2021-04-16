namespace Microsoft.InnerEye.Listener.Processor
{
    partial class ProjectInstaller
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ProcessorServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.ProcessorServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // ProcessorServiceProcessInstaller
            // 
            this.ProcessorServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.ProcessorServiceProcessInstaller.Password = null;
            this.ProcessorServiceProcessInstaller.Username = null;
            // 
            // ProcessorServiceInstaller
            // 
            this.ProcessorServiceInstaller.DisplayName = "InnerEye Gateway Processor Service";
            this.ProcessorServiceInstaller.ServiceName = Program.ServiceName;
            this.ProcessorServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            this.ProcessorServiceInstaller.DelayedAutoStart = true;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.ProcessorServiceProcessInstaller,
            this.ProcessorServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller ProcessorServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller ProcessorServiceInstaller;
    }
}