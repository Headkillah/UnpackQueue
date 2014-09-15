using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Sfn.UnpackQueue.SingleInstance
{
    public interface ISingleInstance
    {
        void MessageReceived(string message);
    }
}
