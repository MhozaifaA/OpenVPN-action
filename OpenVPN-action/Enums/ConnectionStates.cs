using System;
using System.Collections.Generic;
using System.Text;

namespace OpenVPN_action.Enums
{
    public enum ConnectionStates : uint
    {
        UnConfigured, 
        Configured,
        Ready,
        Connecting,
        Connected,
        Disconnect,
        Reconnect,
        Corrupted,
    }
}
