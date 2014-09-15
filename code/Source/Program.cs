using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Sfn.UnpackQueue.SingleInstance;
using System.Configuration;
using Sfn.UnpackQueue.Gui;
using System.Text;
using System.IO;

namespace Sfn.UnpackQueue
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length == 0)
            {
                InstallerForm installerForm = new InstallerForm();
                Application.Run(installerForm);
                return;
            }

            MainForm form = new MainForm();
            SingleInstanceHandler handler = null;

            string extractTo = ConfigurationManager.AppSettings[AppConfig.ExtractionPath];
            string selectFolder = ConfigurationManager.AppSettings[AppConfig.SelectExtractionFolder];

            try
            {
                if (selectFolder.ToLower() == "true" || !Directory.Exists(extractTo))
                {
                    FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
                    folderBrowser.Description = string.Format("UnpackQueue {0} - Select extraction folder:", GetVersion());
                    folderBrowser.SelectedPath = extractTo;
                    folderBrowser.ShowNewFolderButton = true;
                    DialogResult folderBrowserResult = folderBrowser.ShowDialog();
                    if (folderBrowserResult == DialogResult.OK)
                    {
                        extractTo = folderBrowser.SelectedPath;
                    }
                    else
                    {
                        return;
                    }
                }

                handler = new SingleInstanceHandler(form);
                handler.SendMessage(string.Format("{0}|{1}", 
                    string.Join("|", args.Where(a => !string.IsNullOrEmpty(a.Trim()))),
                    extractTo));
                if (handler.IsMaster)
                {
                    Application.Run(form);
                }
            }
            finally
            {
                if (handler != null)
                {
                    handler.Dispose();
                }
            }
        }

        internal static string GetVersion()
        {
            List<int> versionList = new List<int>();
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            versionList.Add(version.Major);
            versionList.Add(version.Minor);
            if (version.Build != 0)
            {
                versionList.Add(version.Build);
            }
            if (version.MinorRevision != 0)
            {
                versionList.Add(0);
                versionList.Add(version.MinorRevision);
            }
            return string.Join(".", versionList);
        }
    }
}
