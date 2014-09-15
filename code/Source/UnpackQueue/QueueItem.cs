using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sfn.UnpackQueue
{
    public class QueueItem
    {
        public string FileToExtract { get; set; }
        public string Destination { get; set; }
        public long ArchiveSize { get; set; }
        public string ErrorMessage { get; set; }
    }
}
