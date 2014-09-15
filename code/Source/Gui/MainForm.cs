using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Sfn.UnpackQueue.SingleInstance;
using Sfn.UnpackQueue.Unpacker;
using System.Configuration;

namespace Sfn.UnpackQueue.Gui
{
    public partial class MainForm : Form, ISingleInstance
    {
        public MainForm()
        {
            InitializeComponent();

            this.Text = string.Format("UnpackQueue {0}", Program.GetVersion());

            this.VisibleChanged += new EventHandler(MainForm_VisibleChanged);
            this.FormClosing += new FormClosingEventHandler(MainForm_FormClosing);

            unpackQueue = new UnpackQueue();
            unpackQueue.ItemProgressChanged += new Action<ItemProgressChanged>(unpackQueue_statusChanged);
            unpackQueue.UnpackerOutputChanged += new Action<string>(unpackQueue_UnpackerOutputChanged);
            unpackQueue.StatusChanged += new Action<UnpackStatus>(unpackQueue_statusChanged);
            unpackQueue.QueueProgressChanged += new Action<QueueProgressChanged>(unpackQueue_statusChanged);
            unpackQueue.AutoAddUnpackedFiles = ConfigurationManager.AppSettings["AutoAddUnpackedFiles"] == "true";

            CreateWorker();
        }

        #region Private methods

        private void CreateWorker()
        {
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.WorkerReportsProgress = true;
        }

        private void UpdateStatus(string status)
        {
            if (string.IsNullOrEmpty(status)) { return; }
            try
            {
                statusStrip.Invoke((MethodInvoker)delegate
                {
                    statusStrip.Items.Clear();
                    statusStrip.Items.Add(status);
                });
            }
            catch { /* left blank */ }
        }

        private void UpdateUnpackerOutput(string output)
        {
            try
            {
                richTextBoxUnpackerOutput.Invoke((MethodInvoker)delegate
                {
                    if (richTextBoxUnpackerOutput.Text.Length != 0)
                    {
                        richTextBoxUnpackerOutput.Text += Environment.NewLine;
                    }

                    richTextBoxUnpackerOutput.Text += output;
                    richTextBoxUnpackerOutput.SelectionStart = richTextBoxUnpackerOutput.Text.Length;
                    richTextBoxUnpackerOutput.SelectionLength = 0;
                    richTextBoxUnpackerOutput.ScrollToCaret();
                });
            }
            catch { /* left blank */ }
        }

        #endregion

        #region Private events

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            unpackQueue.Cancel();
            buttonCancel.Enabled = false;
            buttonRerun.Enabled = true;
        }

        private void buttonRerun_Click(object sender, EventArgs e)
        {
            var failedItems = new List<QueueItem>(unpackQueue.GetFailedItems());
            unpackQueue.ClearQueue();
            foreach (var item in failedItems)
            {
                unpackQueue.AddToQueue(item.FileToExtract, item.Destination);
            }

            if (!worker.IsBusy)
            {
                worker.RunWorkerAsync();
            }
        }

        void MainForm_VisibleChanged(object sender, EventArgs e)
        {
            IEnumerable<QueueItem> items = unpackQueue.GetItems();
            if (items != null)
            {
                foreach (var item in items)
                {
                    EditItemQueueListBox(item, "Idle");
                }
            }

            SetItemsLeft(items.Count());
        }

        void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                unpackQueue.Cancel();
                worker.CancelAsync();
            }
            catch
            {
                // Do nothing
            }
        }

        #endregion

        #region Unpack queue events

        void unpackQueue_UnpackerOutputChanged(string obj)
        {
            worker.ReportProgress(-1, obj);
        }

        void unpackQueue_statusChanged(UnpackStatus obj)
        {
            worker.ReportProgress(-1, obj);
        }

        #endregion

        #region Background worker events

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is UnpackStatus)
            {
                string message = null;
                if (e.UserState is QueueStarted)
                {
                    message = "Started unpacking items in queue.";
                }
                else if (e.UserState is QueueCompleted)
                {
                    message = "Unpacking of all queued items is completed";
                }
                else if (e.UserState is QueueCanceled)
                {
                    message = "Unpacking canceled";
                }
                else if (e.UserState is QueueProgressChanged)
                {
                    QueueProgressChanged queueProgressChanged = e.UserState as QueueProgressChanged;
                    EditQueueInfo(queueProgressChanged);
                }
                else if (e.UserState is ItemAdded)
                {
                    ItemAdded itemAdded = e.UserState as ItemAdded;
                    SetItemsLeft(itemAdded.ItemsInQueue);
                    message = string.Format("Added item \"{0}\"", GetFileName(itemAdded.Item));
                    EditItemQueueListBox(itemAdded.Item, "Idle");
                }
                else if (e.UserState is ItemStarted)
                {
                    ItemStarted itemStarted = e.UserState as ItemStarted;
                    message = string.Format("Unpacking of item \"{0}\" started", GetFileName(itemStarted.Item));
                    EditItemQueueListBox(itemStarted.Item, "Unpackning (0%)");
                }
                else if (e.UserState is ItemProgressChanged)
                {
                    ItemProgressChanged itemProgressChanged = e.UserState as ItemProgressChanged;
                    EditItemQueueListBox(itemProgressChanged.Item, string.Format("Unpacking ({0}% {1})",
                        itemProgressChanged.PercentCompleted, ConvertEta(itemProgressChanged.Eta)));
                }
                else if (e.UserState is ItemCompleted)
                {
                    ItemCompleted itemCompleted = e.UserState as ItemCompleted;
                    SetItemsLeft(itemCompleted.ItemsLeftInQueue);
                    message = string.Format("Unpacking of item \"{0}\" completed", GetFileName(itemCompleted.Item));
                    EditItemQueueListBox(itemCompleted.Item, "Completed");
                }
                else if (e.UserState is UnpackerError)
                {
                    UnpackerError unpackerError = e.UserState as UnpackerError;
                    message = string.Format("Error unpacking item: {0}", unpackerError.ErrorMessage);
                    EditItemQueueListBox(unpackerError.Item, string.Format("Error: {0}", unpackerError.ErrorMessage));
                }

                UpdateStatus(message);
            }
            else if (e.UserState is string)
            {
                UpdateUnpackerOutput(e.UserState as string);
            }
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            buttonCancel.Invoke((MethodInvoker)delegate()
            {
                buttonCancel.Text = "Cancel";
                buttonCancel.Enabled = true;
            });

            string unrarPath = ConfigurationManager.AppSettings[AppConfig.UnRARLocation];
            unpackQueue.Run(UnpackMethod.UnRAR, new { UnRARLocation = unrarPath });
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            int failedItems = unpackQueue.GetFailedItems().Count();
            if (ConfigurationManager.AppSettings[AppConfig.AutoClose].ToLower() == "true" && failedItems < 1)
            {
                Application.Exit();
            }

            buttonCancel.Invoke((MethodInvoker)delegate
            {
                buttonCancel.Enabled = false;
            });

            buttonRerun.Invoke((MethodInvoker)delegate
            {
                buttonRerun.Enabled = failedItems > 0;
            });

            CreateWorker();
        }

        #endregion

        #region Visual apperance methods

        private void EditQueueInfo(QueueProgressChanged queueProgressChanged)
        {
            try
            {
                progressBarQueue.Invoke((MethodInvoker)delegate
                {
                    progressBarQueue.Value = queueProgressChanged.PercentCompleted;
                });

                labelTimeRemainingQueue.Invoke((MethodInvoker)delegate
                {
                    labelTimeRemainingQueue.Text = ConvertEta(queueProgressChanged.Eta);
                });

                labelItemsLeftInQueue.Invoke((MethodInvoker)delegate
                {
                    labelItemsLeftInQueue.Text = queueProgressChanged.ItemsLeft.ToString();
                });

            }
            catch
            {
                /* Left blank. Will only happen during startup before the form has been rendered */
            }
        }

        private void EditItemQueueListBox(QueueItem p, string status)
        {
            try
            {
                listBoxQueue.Invoke((MethodInvoker)delegate
                {
                    int currentItem = -1;
                    string filename = GetFileName(p);
                    try
                    {
                        currentItem = listBoxQueue.FindString(filename);
                    }
                    catch { /* does not exist */ }

                    string item = string.Format("{0} - {1}", filename, status);

                    if (currentItem > -1)
                    {
                        listBoxQueue.Items[currentItem] = item;
                    }
                    else
                    {
                        listBoxQueue.Items.Add(item);
                    }


                    // Calculate with for horizontal scrollbars
                    Graphics g = listBoxQueue.CreateGraphics();
                    int hzSize = (int)g.MeasureString(item, listBoxQueue.Font).Width;
                    if (hzSize > listBoxQueue.HorizontalExtent)
                    {
                        listBoxQueue.HorizontalExtent = hzSize;
                    }

                    // Make sure the edited item is visible
                    listBoxQueue.SelectedIndex = currentItem;
                    listBoxQueue.SelectedIndex = -1;
                });
            }
            catch { }
        }

        private void SetItemsLeft(int items)
        {
            try
            {
                labelItemsLeftInQueue.Invoke((MethodInvoker)delegate
                {
                    labelItemsLeftInQueue.Text = items.ToString();
                });
            }
            catch { }
        }

        private string ConvertEta(DateTime eta)
        {
            TimeSpan ts = eta.Subtract(DateTime.Now);
            return string.Format("{0}m{1}s", ts.Minutes, ts.Seconds);
        }

        private string GetFileName(QueueItem item)
        {
            return item.FileToExtract.Substring(item.FileToExtract.LastIndexOf("\\") + 1);
        }

        #endregion

        #region ISingleInstance methods

        void ISingleInstance.MessageReceived(string message)
        {
            List<string> messageParts = new List<string>(message.Split("|".ToCharArray()));
            string extractTo = messageParts[messageParts.Count - 1];
            messageParts.RemoveAt(messageParts.Count - 1);
            string[] inputs = messageParts.ToArray();

            foreach (var source in inputs)
            {
                List<string> files = FileSystemHelper.GetFiles(source);
                foreach (var file in files)
                {
                    unpackQueue.AddToQueue(file, extractTo);
                }
            }

            if (!worker.IsBusy)
            {
                worker.RunWorkerAsync();
            }
            else
            {
                // It seems that on some occasions the auto unpacking does not start.
                // The below is a test solution to this problem.
                System.Timers.Timer timer = new System.Timers.Timer(2000);
                timer.Elapsed += delegate(object sender, System.Timers.ElapsedEventArgs e)
                {
                    if (!worker.IsBusy && unpackQueue.GetItems().Count() > 0)
                    {
                        worker.RunWorkerAsync();
                    }
                };
                timer.AutoReset = false;
                timer.Enabled = true;
            }


        }

        #endregion

        #region Private attributes

        UnpackQueue unpackQueue;
        BackgroundWorker worker;

        #endregion
    }
}
