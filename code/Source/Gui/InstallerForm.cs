using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Security.Principal;
using System.Diagnostics;

namespace Sfn.UnpackQueue.Gui
{
    public partial class InstallerForm : Form
    {
        public InstallerForm()
        {
            InitializeComponent();

            this.Text = string.Format("UnpackQueue {0} installer", Program.GetVersion());
        }

        private void CheckUAC()
        {
            WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            if (!pricipal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                buttonInstallFiles.Enabled = false;
                buttonUninstallFiles.Enabled = false;
                buttonInstallFolders.Enabled = false;
                buttonUninstallFolders.Enabled = false;

                MessageBox.Show("UAC is preventing this action to complete. Restarting as administrator.");
                ProcessStartInfo processInfo = new ProcessStartInfo();
                processInfo.Verb = "runas";
                processInfo.FileName = Application.ExecutablePath;
                try
                {
                    Process.Start(processInfo);
                    Application.Exit();
                }
                catch (Win32Exception)
                {
                    //Do nothing. Probably the user canceled the UAC window
                }
            }
        }

        private void buttonInstallFiles_Click(object sender, EventArgs e)
        {
            try
            {
                CheckUAC();
                Installer.InstallFiles(Application.ExecutablePath);
                MessageBox.Show("Installation completed");
            }
            catch (Exception exc)
            {
                MessageBox.Show("Installation failed: " + exc.Message);
            }
        }

        private void buttonUninstallFiles_Click(object sender, EventArgs e)
        {
            try
            {
                CheckUAC();
                Installer.UninstallFiles();
                MessageBox.Show("Uninstallation completed");
            }
            catch (Exception exc)
            {
                MessageBox.Show("Uninstallation failed: " + exc.Message);
            }
        }

        private void buttonInstallFolders_Click(object sender, EventArgs e)
        {
            try
            {
                CheckUAC();
                Installer.InstallDirectories(Application.ExecutablePath);
                MessageBox.Show("Installation completed");
            }
            catch (Exception exc)
            {
                MessageBox.Show("Installation failed: " + exc.Message);
            }
        }

        private void buttonUninstallFolders_Click(object sender, EventArgs e)
        {
            try
            {
                CheckUAC();
                Installer.UninstallDirectories();
                MessageBox.Show("Uninstallation completed");
            }
            catch (Exception exc)
            {
                MessageBox.Show("Uninstallation failed: " + exc.Message);
            }
        }
    }
}
