# OpenVPN-action
The most powerful management to handle ui events

## Quick start

```C#

using OpenVPN_action;
using OpenVPN_action.Enum;

 private readonly OpenVPNAction openVPN;
   
   public ctor()
   {
   ...
   
   // pass your own username / password
     openVPN = new OpenVPNAction(SecureSettingApp.GetEmail(), SecureSettingApp.GetPassword());
     openVPN.NumCycleOfReconnecting = 3;
     InitVPNEvent();
    ...
   }
   


  private void InitVPNEvent()
  {
      openVPN.OnConnecting += (info) => { ... };

      openVPN.OnConnected += (info) => { ... };

      openVPN.OnReconnecting += (info) => { ... };
      
      openVPN.OnDisconnected += (info) => { ... };

      openVPN.OnEndCycle += (info) => { ... };

      openVPN.OnCorrupted +=  (info) =>
      {
        ...
          //optional
          openVPN.RemoveRefuseRun();
         ...
      };
  }
  
  
  private async ValueTask ConnectDisconnect()
  {
  ...
  
    var state = openVPN.OpenVPNInfo.GetConnectionState();

    if ((state == ConnectionStates.Disconnect ||
        state == ConnectionStates.UnConfigured))
    {
     ...
        openVPN.Configuration(SettingApp.OpenVpn_Path, SettingApp.ovpnFile_Path);
        await openVPN.Start();
        await openVPN.Connect();
        return;
    }
    
    if(state == ConnectionStates.Connecting)
    {
        IsBlockVPN = false;
        await openVPN.Disconnect();
        HasAllowVPN = true;
        return;
    }

    if (state == ConnectionStates.Connected)
    {
       ...
        await openVPN.Disconnect();
        return;
    }
  ...
  }


```
  ##### *simple scenario*
----

## Documentation
soon with many uses
