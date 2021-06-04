using System;
using System.Collections.Generic;
using System.Text;

namespace OpenVPN_action
{
    /// <summary>
    /// base openvpn  contain events/modulers/props 
    /// </summary>
    public abstract class OpenVPN:IDisposable
    {

        /// <summary>
        /// Set change inforamtions of connnection 
        /// </summary>
        public OpenVPNInfo OpenVPNInfo { get; set; } = new OpenVPNInfo();


        private event OpenVPNEvent<OpenVPNInfo> _OnConnected;
        /// <summary>
        /// Notify when completely connect
        /// <para> trigger after <see cref="OnConnecting"/> if successed </para>
        /// </summary>
        public event OpenVPNEvent<OpenVPNInfo> OnConnected
        {
            add { _OnConnected += value; }
            remove { _OnConnected -= value; }
        }

        protected virtual void OnConnectedChanged()
        {
            _OnConnected?.Invoke(OpenVPNInfo);
        }



        private event OpenVPNEvent<OpenVPNInfo> _OnDisconnected;

        /// <summary>
        /// Notify when completely disconnect
        /// <para> trigger normaly by agent if disconnect  or automatically by openvpn service </para>
        /// </summary>
        public event OpenVPNEvent<OpenVPNInfo> OnDisconnected
        {
            add { _OnDisconnected += value; }
            remove { _OnDisconnected -= value; }
        }

        protected virtual void OnDisConnectedChanged()
        {
            _OnDisconnected?.Invoke(OpenVPNInfo);
        }



        private event OpenVPNEvent<OpenVPNInfo> _OnReconnecting;
        /// <summary>
        /// Notify when auto reconnect 
        /// <para> trigger in multi way cuse reenter auth or <see cref="OnConnecting"/> / <see cref="OnConnecting"/> faild </para>
        /// </summary>
        public event OpenVPNEvent<OpenVPNInfo> OnReconnecting
        {
            add { _OnReconnecting += value; }
            remove { _OnReconnecting -= value; }
        }
     
        protected virtual void OnReconnectedChanged()
        {
            _OnReconnecting?.Invoke(OpenVPNInfo);
        }



        private event OpenVPNEvent<OpenVPNInfo> _OnConnecting;
        /// <summary>
        /// Notify when try connect
        /// <para> trigger by agent or <see cref="OnReconnecting" /> call </para>
        /// </summary>
        public event OpenVPNEvent<OpenVPNInfo> OnConnecting
        {
            add { _OnConnecting += value; }
            remove { _OnConnecting -= value; }
        }

        protected virtual void OnConnectingChanged()
        {
            _OnConnecting?.Invoke(OpenVPNInfo);
        }



        private event OpenVPNEvent<OpenVPNInfo> _OnEndCycle;
        /// <summary>
        /// Notify when num of cycle reconneting done
        /// <para> trigger by agent or <see cref="OnReconnecting" /> call </para>
        /// </summary>
        public event OpenVPNEvent<OpenVPNInfo> OnEndCycle
        {
            add { _OnEndCycle += value; }
            remove { _OnEndCycle -= value; }
        }

        protected virtual void OnEndCycleChanged()
        {
            _OnEndCycle?.Invoke(OpenVPNInfo);
        }



        private event OpenVPNEvent<OpenVPNInfo> _OnCorrupted;
        /// <summary>
        /// Notify when occurred <see cref="Exception"/> happen
        /// </summary>
        public event OpenVPNEvent<OpenVPNInfo> OnCorrupted
        {
            add { _OnCorrupted += value; }
            remove { _OnCorrupted -= value; }
        }

        protected virtual void OnCorruptedChanged()
        {
            _OnCorrupted?.Invoke(OpenVPNInfo);
        }




        private bool disposed = false;
        ~OpenVPN()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    OnConnected -= _OnConnected;
                    OnDisconnected -= _OnDisconnected;
                    OnReconnecting -= _OnReconnecting;
                    OnConnecting -= _OnConnecting;
                    OnCorrupted -= _OnCorrupted;
                }
                disposed = true;
            }
        }

    }
}
