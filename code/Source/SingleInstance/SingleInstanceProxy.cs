using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sfn.UnpackQueue.SingleInstance
{
    public class SingleInstanceProxy : MarshalByRefObject
    {
        public SingleInstanceProxy(ISingleInstance singleInstance)
        {
            this.SingleInstance = singleInstance;
        }

        public ISingleInstance SingleInstance { get; private set; }

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
