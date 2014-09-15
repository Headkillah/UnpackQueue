using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Sfn.UnpackQueue.Unpacker
{
    public abstract class UnpackerBase
    {

        #region Public events

        public event Action<int, DateTime> ProgressChanged;
        public event Action<string> OutputChanged;

        #endregion

        #region Public properties

        public string ErrorMessage { get; protected set; }

        #endregion

        #region Public abstract methods

        public abstract void SetOptions(object options);
        public abstract bool Unpack(string file, string extractionPath);
        public abstract void Cancel();
        public abstract string[] GetArchiveContent(string fileName);

        #endregion

        #region Protected methods

        protected string GetPropertyValue(string propertyName, object options)
        {
            Type type = options.GetType();
            PropertyInfo property = type.GetProperties().FirstOrDefault(p => p.Name == propertyName);
            return property.GetValue(options, null) as string;
        }

        protected void OnProgressChanged(int percent, DateTime eta)
        {
            if (ProgressChanged != null)
            {
                ProgressChanged(percent, eta);
            }
        }

        protected void OnOutputChanged(string message)
        {
            if (OutputChanged != null)
            {
                OutputChanged(message);
            }
        }

        #endregion
    }
}
