namespace Microsoft.InnerEye.Listener.Receiver
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Configuration.Install;
    using System.Diagnostics;
    using System.Globalization;
    using System.ServiceProcess;

    /// <summary>
    /// The project installer class.
    /// </summary>
    /// <seealso cref="Installer" />
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectInstaller"/> class.
        /// </summary>
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Configuration.Install.Installer.BeforeUninstall" /> event.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Collections.IDictionary" /> that contains the state of the computer before the installers in the <see cref="P:System.Configuration.Install.Installer.Installers" /> property uninstall their installations.</param>
        protected override void OnBeforeUninstall(IDictionary savedState)
        {
            using (var controller = new ServiceController(Program.ServiceName))
            {
                if (controller.Status == ServiceControllerStatus.Running | controller.Status == ServiceControllerStatus.Paused)
                {
                    controller.Stop();
                    controller.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 0, 15));
                }
            }

            base.OnBeforeUninstall(savedState);
        }

        /// <summary>
        /// Raises the <see cref="E:System.Configuration.Install.Installer.AfterInstall" /> event.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Collections.IDictionary" /> that contains the state of the computer after all the installers contained in the <see cref="P:System.Configuration.Install.Installer.Installers" /> property have completed their installations.</param>
        protected override void OnAfterInstall(IDictionary savedState)
        {
            try
            {
                using (var serviceController = new ServiceController(Program.ServiceName))
                {
                    serviceController.Start();
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "Failed to start service {0} with exception {1}", Program.ServiceName, e));
            }

            base.OnAfterInstall(savedState);
        }
    }
}