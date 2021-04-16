namespace Microsoft.InnerEye.Listener.Receiver
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
            this.ReceiveServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.ReceiverInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // ReceiveServiceProcessInstaller
            // 
            this.ReceiveServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.ReceiveServiceProcessInstaller.Password = null;
            this.ReceiveServiceProcessInstaller.Username = null;
            // 
            // ReceiverInstaller
            // 
            this.ReceiverInstaller.DisplayName = "InnerEye Gateway Receive Service";
            this.ReceiverInstaller.ServiceName = Program.ServiceName;
            this.ReceiverInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            this.ReceiverInstaller.DelayedAutoStart = true;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.ReceiveServiceProcessInstaller,
            this.ReceiverInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller ReceiveServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller ReceiverInstaller;
    }
}