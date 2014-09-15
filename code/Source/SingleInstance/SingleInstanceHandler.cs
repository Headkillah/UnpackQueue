using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Runtime.Serialization;

namespace Sfn.UnpackQueue.SingleInstance
{
    public class SingleInstanceHandler : IDisposable
    {
        #region Constructor/destructor

        public SingleInstanceHandler(ISingleInstance singleInstance)
        {
            string mutexName = "Sfn.UnpackQueue.SingleInstance.Mutex";
            string proxyObjectName = "SingleInstanceProxy";            

            bool firstInstance = false;
            singleInstanceMutex = new Mutex(true, mutexName, out firstInstance);

            // Register Ipc server
            if (firstInstance)
            {    
                // Create a new Ipc server channel
                ipcChannel = new IpcServerChannel(mutexName);
                // Register the server channel with Windows
                ChannelServices.RegisterChannel(ipcChannel, false);
                // Register the proxy type as a singleton
                RemotingConfiguration.RegisterWellKnownServiceType(
                    typeof(SingleInstanceProxy),
                    proxyObjectName,
                    WellKnownObjectMode.Singleton);
                // Create the one and only proxy object to be used.
                proxy = new SingleInstanceProxy(singleInstance);
                // Publish the proxy object so that clients can access it. 
                RemotingServices.Marshal(proxy, proxyObjectName);

                // Flag this instance as the master
                IsMaster = true;
            }
            // Register Ipc client
            else
            {
                // Create the uri to the master proxy object
                string proxyUri = string.Format("ipc://{0}/{1}", mutexName, proxyObjectName);
                // Create client channel and register it
                ipcChannel = new IpcClientChannel();
                ChannelServices.RegisterChannel(ipcChannel, false);
                
                // Fetch the object using reflection
                proxy = (SingleInstanceProxy)Activator.GetObject(typeof(SingleInstanceProxy), proxyUri);
                
                // This is not the master
                IsMaster = false;
            }
        }

        ~SingleInstanceHandler()
        {
            Dispose(false);
        }

        #endregion

        #region Public propertis

        public bool IsMaster { get; private set; }

        #endregion

        #region Public methods

        public void SendMessage(string message)
        {
            proxy.SingleInstance.MessageReceived(message);

        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private methods

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (singleInstanceMutex != null)
                    {
                        singleInstanceMutex.Close();
                        singleInstanceMutex = null;
                    }
                }

                disposed = true;
            }
        }

        #endregion

        #region Private attributes

        private bool disposed;
        private Mutex singleInstanceMutex;
        private IChannel ipcChannel;
        private SingleInstanceProxy proxy;

        #endregion
    }
}
