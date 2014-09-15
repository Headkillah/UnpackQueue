using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sfn.UnpackQueue
{
    public abstract class UnpackStatus
    {
        public QueueItem Item { get; protected set; }
    }

    public class QueueStarted : UnpackStatus { }
    public class QueueCompleted : UnpackStatus { }
    public class QueueCanceled : UnpackStatus { }
    public class QueueProgressChanged : UnpackStatus
    {
        public QueueProgressChanged(int percentCompleted, DateTime eta, int itemsLeft)
        {
            this.PercentCompleted = percentCompleted;
            this.Eta = eta;
            this.ItemsLeft = itemsLeft;
        }
        public int PercentCompleted { get; private set; }
        public DateTime Eta { get; private set; }
        public int ItemsLeft { get; private set; }
    }

    public class ItemAdded : UnpackStatus
    {
        public int ItemsInQueue { get; private set; }
        public ItemAdded(QueueItem item, int itemsInQueue)
        {
            Item = item;
            ItemsInQueue = itemsInQueue;
        }
    }

    public class ItemStarted : UnpackStatus
    {
        public ItemStarted(QueueItem item)
        {
            Item = item;
        }
    }

    public class ItemCompleted : UnpackStatus
    {
        public int ItemsLeftInQueue { get; private set; }
        public ItemCompleted(QueueItem item, int itemsLeftInQueue)
        {
            Item = item;
            ItemsLeftInQueue = itemsLeftInQueue;
        }
    }

    public class ItemProgressChanged : UnpackStatus
    {
        public ItemProgressChanged(QueueItem item, int percentCompleted, DateTime eta)
        {
            this.Item = item;
            this.PercentCompleted = percentCompleted;
            this.Eta = eta;
        }
        public int PercentCompleted { get; private set; }
        public DateTime Eta { get; private set; }
    }

    public class UnpackerError : UnpackStatus
    {
        public string ErrorMessage { get; private set; }
        public UnpackerError(QueueItem item, string message)
        {
            Item = item;
            ErrorMessage = message;
        }
    }
}
