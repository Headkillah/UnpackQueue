using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Windows.Forms;

namespace Sfn.UnpackQueue
{
    public static class Installer
    {
        public static void InstallFiles(string applicationPath)
        {
            Install(applicationPath, "WinRAR");
        }

        public static void InstallDirectories(string applicationPath)
        {
            Install(applicationPath, "Directory");
        }

        public static void UninstallFiles()
        {
            Uninstall("WinRAR");
        }

        public static void UninstallDirectories()
        {
            Uninstall("Directory");
        }

        private static void Install(string applicationPath, string mainSubKey)
        {
            RegistryKey winrarKey = Registry.ClassesRoot.OpenSubKey(mainSubKey);
            RegistryKey shellKey = winrarKey.OpenSubKey("shell", true);
            RegistryKey unpackQueueKey = shellKey.CreateSubKey("Unpack queue");
            RegistryKey commandKey = unpackQueueKey.CreateSubKey("command");

            StringBuilder sb = new StringBuilder();
            for (int i = 1; i < 10; i++)
            {
                sb.AppendFormat("\"%{0}\"", i);
                if (i != 9) { sb.Append(" "); }
            }

            commandKey.SetValue("", string.Format("\"{0}\" {1}", applicationPath, sb.ToString()));
            commandKey.Close();
            unpackQueueKey.Close();
            shellKey.Close();
            winrarKey.Close();
        }

        public static void Uninstall(string mainSubKey)
        {
            RegistryKey winrarKey = Registry.ClassesRoot.OpenSubKey(mainSubKey);
            RegistryKey shellKey = winrarKey.OpenSubKey("shell", true);
            shellKey.DeleteSubKeyTree("Unpack queue", false);
            shellKey.Close();
            winrarKey.Close();
        }
    }
}
