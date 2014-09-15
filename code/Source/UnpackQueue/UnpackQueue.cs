using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sfn.UnpackQueue.Unpacker;
using System.Text.RegularExpressions;
using System.IO;

namespace Sfn.UnpackQueue
{
    public enum UnpackMethod
    {
        UnRAR
    }

    public class UnpackQueue
    {
        #region Public events

        public event Action<ItemProgressChanged> ItemProgressChanged;
        public event Action<QueueProgressChanged> QueueProgressChanged;
        public event Action<UnpackStatus> StatusChanged;
        public event Action<string> UnpackerOutputChanged;

        #endregion

        #region Public properties

        public bool AutoAddUnpackedFiles { get; set; }

        #endregion

        public UnpackQueue()
        {
            queue = new List<QueueItem>();
        }

        #region Public methods

        public void AddToQueue(string source, string destination)
        {
            // Detect if the file has already been added with the same destination.
            if (queue.FirstOrDefault(f => f.FileToExtract == source && f.Destination == destination) != null)
            {
                return;
            }

            // Detect rar archives named *.part01.rar, *.part02.rar etc.
            Regex regexPartRar = new Regex(@"part\d*.rar$", RegexOptions.IgnoreCase);
            Match matchPartRar = regexPartRar.Match(source);
            if (matchPartRar.Success)
            {
                string baseName = source.Substring(0, source.LastIndexOf(matchPartRar.Value));
                regexPartRar = new Regex(baseName.Replace("\\", "\\\\") + regexPartRar.ToString(), RegexOptions.IgnoreCase);
                IEnumerable<QueueItem> items = GetItems().Where(i => regexPartRar.Match(i.FileToExtract.ToLower()).Success).ToList();
                if (items.FirstOrDefault(f => f.Destination == destination) != null)
                {
                    return;
                }
            }

            QueueItem item = new QueueItem() { FileToExtract = source, Destination = destination, ArchiveSize = GetArchiveSize(source) };
            queue.Add(item);
            totalSize += item.ArchiveSize;
            OnStatusChanged(new ItemAdded(item, queue.Count));
        }

        public void ClearQueue()
        {
            queue.Clear();
            totalSize = 0;
            sizeCompleted = 0;
            currentPosition = 0;
            canceled = false;
        }

        public IEnumerable<QueueItem> GetItems()
        {
            return queue;
        }

        public IEnumerable<QueueItem> GetFailedItems()
        {
            return queue.Where(f => !string.IsNullOrEmpty(f.ErrorMessage));
        }

        public void Run(UnpackMethod unpackMethod, object options)
        {
            if (queue == null)
            {
                return;
            }

            sizeCompleted = 0;
            canceled = false;
            unpacker = null;

            switch (unpackMethod)
            {
                case UnpackMethod.UnRAR: unpacker = new UnRAR(); break;
            }

            unpacker.SetOptions(options);
            unpacker.ProgressChanged += new Action<int, DateTime>(unpacker_ProgressChanged);
            unpacker.OutputChanged += new Action<string>(unpacker_OutputChanged);

            queueStarted = DateTime.Now;
            OnStatusChanged(new QueueStarted());

            int length = queue.Count;
            for (currentPosition = 0; currentPosition < length; currentPosition++)
            {
                if (canceled)
                {
                    break;
                }

                QueueItem item = queue[currentPosition];
                string file = item.FileToExtract;
                string destination = item.Destination;

                OnStatusChanged(new ItemStarted(item));
                bool success = unpacker.Unpack(file, destination);

                if (canceled)
                {
                    break;
                }

                if (success)
                {
                    if (AutoAddUnpackedFiles)
                    {
                        GetAndAddExtractedItems(item);
                    }
                    queue[currentPosition].ErrorMessage = null;
                    OnStatusChanged(new ItemCompleted(item, CalculateItemsLeft()));
                }
                else
                {
                    OnStatusChanged(new UnpackerError(item, unpacker.ErrorMessage));
                    queue[currentPosition].ErrorMessage = unpacker.ErrorMessage;
                    CalculateAndReportProgress(0);
                }

                length = queue.Count;
            }

            if (canceled)
            {
                OnStatusChanged(new QueueCanceled());
            }
            else
            {
                OnStatusChanged(new QueueCompleted());
            }
        }

        private int CalculateItemsLeft()
        {
            int itemsLeft = queue.Count - (currentPosition + 1);
            itemsLeft += GetFailedItems().Count();
            return itemsLeft;
        }

        public void Cancel()
        {
            unpacker.Cancel();
            canceled = true;
        }

        #endregion

        #region Unpacker events

        void unpacker_OutputChanged(string obj)
        {
            if (UnpackerOutputChanged != null)
            {
                UnpackerOutputChanged(obj);
            }
        }

        void unpacker_ProgressChanged(int percent, DateTime eta)
        {
            if (ItemProgressChanged != null)
            {
               QueueItem item = queue[currentPosition];
               ItemProgressChanged(new Sfn.UnpackQueue.ItemProgressChanged(item, percent, eta));
            }

            CalculateAndReportProgress(percent);
        }

        #endregion

        #region Private methods

        private void OnStatusChanged(UnpackStatus status)
        {
            if (StatusChanged != null)
            {
                StatusChanged(status);
            }
        }

        /// <summary>
        /// Calculate and report the totalt percent complete.
        /// </summary>
        /// <param name="itemPercent">The current item's completed percent.</param>
        private void CalculateAndReportProgress(int itemPercent)
        {
            int percentCompleted = -1;

            QueueItem currentItem = queue[currentPosition];

            double itemBytesComplete = ((double)itemPercent / 100) * (double)currentItem.ArchiveSize;
            percentCompleted = (int)((itemBytesComplete + sizeCompleted) / totalSize * 100);

            TimeSpan elapsed = DateTime.Now.Subtract(queueStarted);
            int remainingPercent = 100 - percentCompleted;
            double secondsPerPercent = elapsed.TotalSeconds / percentCompleted;

            int remainingSeconds = remainingPercent * (int)Math.Ceiling(secondsPerPercent);

            if (QueueProgressChanged != null)
            {
                int itemsLeft = CalculateItemsLeft();
                if (itemsLeft == 0 && itemPercent != 100)
                {
                    itemsLeft = 1;
                }
                QueueProgressChanged(new Sfn.UnpackQueue.QueueProgressChanged(percentCompleted, DateTime.Now.AddSeconds(remainingSeconds), itemsLeft));
            }

            if (itemPercent == 100)
            {
                sizeCompleted += currentItem.ArchiveSize;
            }
        }

        /// <summary>
        /// Calculate the full archive size for a file name. 
        /// The archive can be split into several files.
        /// The size is returned in bytes.
        /// </summary>
        /// <param name="fileName">The file name of one of the archive files.</param>
        /// <returns>The archive file size in bytes.</returns>
        private long GetArchiveSize(string fileName)
        {
            long archiveSize = 0;
            FileInfo file = new FileInfo(fileName);

            IEnumerable<FileInfo> archiveFiles = null;

            // Match multipart archives.
            
            Match matchPartRar = REGEX_PART_RAR.Match(fileName);
            if (matchPartRar.Success)
            {
                string baseName = fileName.Substring(0, fileName.LastIndexOf(matchPartRar.Value));
                Regex regexPartRar = new Regex(baseName.Replace("\\", "\\\\") + REGEX_PART_RAR.ToString(), RegexOptions.IgnoreCase);
                archiveFiles = file.Directory.GetFiles().Where(i => regexPartRar.Match(i.Name.ToLower()).Success).ToList();
            }
            else
            {
                // Fetch all files with the base name with an extension of ".rar" or ".rXX" where xx is a number.
                string baseName = Path.GetFileNameWithoutExtension(file.Name);
                Regex regexRNumber = new Regex(baseName + REGEX_RAR_RNN.ToString());
                archiveFiles = file.Directory.GetFiles().Where(i => regexRNumber.IsMatch(i.Name));
            }

            if (archiveFiles != null)
            {
                foreach (var item in archiveFiles)
                {
                    archiveSize += item.Length;
                }
            }

            return archiveSize;
        }

        /// <summary>
        /// Get the content of the provided queue item and add all files that are archives to UnpackQueue.
        /// </summary>
        /// <param name="item">The item to examine for archive files.</param>
        private void GetAndAddExtractedItems(QueueItem item)
        {
            string[] content = unpacker.GetArchiveContent(item.FileToExtract);
            foreach (var file in content)
            {
                if (REGEX_PART_RAR.IsMatch(file) || REGEX_RAR_RNN.IsMatch(file))
                {
                    // File is located in the extraction folder.
                    this.AddToQueue(Path.Combine(item.Destination, file), item.Destination);
                }
            }
        }

        #endregion

        #region Private attributes

        private List<QueueItem> queue;
        private int currentPosition;
        private UnpackerBase unpacker;
        private bool canceled;
        private DateTime queueStarted;
        private long totalSize;
        private long sizeCompleted;

        private readonly Regex REGEX_PART_RAR = new Regex(@"part\d*.rar$", RegexOptions.IgnoreCase);
        private readonly Regex REGEX_RAR_RNN = new Regex(@"\.(r[\d]*|rar)$", RegexOptions.IgnoreCase);

        #endregion
    }
}
