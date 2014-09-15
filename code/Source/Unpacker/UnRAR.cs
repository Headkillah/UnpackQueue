using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace Sfn.UnpackQueue.Unpacker
{
    public class UnRAR : UnpackerBase
    {
        #region UnpackerBase implementations

        public override void SetOptions(object options)
        {
            unrarLocation = base.GetPropertyValue("UnRARLocation", options);
        }

        public override bool Unpack(string file, string extractionPath)
        {
            percentCompleted = 0;
            lastPercent = -1;
            base.ErrorMessage = null;

            file = FixArgumentString(file);
            extractionPath = FixArgumentString(extractionPath);

            proc = GetUnRarProcess(string.Format("x {0} {1}", file, extractionPath));
            proc.OutputDataReceived += new DataReceivedEventHandler(proc_OutputDataReceived);
            this.startTime = DateTime.Now;
            proc.Start();
            proc.BeginOutputReadLine();
            proc.WaitForExit();

            return string.IsNullOrEmpty(base.ErrorMessage);
        }

        public override void Cancel()
        {
            proc.Kill();
            base.OnOutputChanged("Canceled");
        }

        public override string[] GetArchiveContent(string file)
        {
            file = FixArgumentString(file);
            proc = GetUnRarProcess(string.Format("lb {0}", file));

            List<string> content = new List<string>();
            proc.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    content.Add(e.Data);
                }
            };
            proc.Start();
            proc.BeginOutputReadLine();
            proc.WaitForExit();
            return content.ToArray();
        }

        #endregion

        #region Private methods

        private void CalculatePercent(string unrarOutput)
        {
            if (unrarOutput.StartsWith("Extracting  ") ||
                unrarOutput.StartsWith("...         "))
            {
                string percent = "";
                int lastIndexOfSpace = unrarOutput.LastIndexOf(" ");
                percent = unrarOutput.Substring(lastIndexOfSpace);
                percent = percent.Replace("%", "");
                int.TryParse(percent, out percentCompleted);
            }
            else if (unrarOutput == "All OK")
            {
                percentCompleted = 100;
            }
        }

        private string FixArgumentString(string argument)
        {
            if (string.IsNullOrEmpty(argument))
            {
                return argument;
            }

            if (!argument.StartsWith("\""))
            {
                argument = "\"" + argument;
            }
            if (!argument.EndsWith("\""))
            {
                argument += "\"";
            }

            return argument;
        }

        private Process GetUnRarProcess(string args)
        {
            proc = new Process();
            proc.StartInfo.FileName = unrarLocation;
            proc.StartInfo.Arguments = args;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.CreateNoWindow = true;
            return proc;
        }

        #endregion

        #region Event handlers

        void proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                string data = e.Data as string;
                base.OnOutputChanged(data);

                if (data == "Program aborted")
                {
                    base.ErrorMessage = "Program aborted. This is most likely because the target file already exists.";
                    return;
                }
                else if (data.Trim().EndsWith("is not RAR archive"))
                {
                    base.ErrorMessage = "The file is not a valid RAR archive.";
                    return;
                }

                CalculatePercent(data);

                if (percentCompleted != lastPercent)
                {
                    lastPercent = percentCompleted;
                    TimeSpan ts = DateTime.Now.Subtract(startTime);
                    int remainingPercent = 100 - percentCompleted;
                    double secondsPerPercent = ts.TotalSeconds / percentCompleted;

                    int remainingSeconds = remainingPercent * (int)Math.Ceiling(secondsPerPercent);

                    base.OnProgressChanged(percentCompleted, DateTime.Now.AddSeconds(remainingSeconds));
                }
            }
        }

        #endregion

        #region Private attributes

        private string unrarLocation;
        Process proc;
        private int percentCompleted;
        private int lastPercent;
        private DateTime startTime;

        #endregion
    }
}
