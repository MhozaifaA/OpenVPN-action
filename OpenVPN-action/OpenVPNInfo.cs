using OpenVPN_action.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenVPN_action
{
    /// <summary>
    /// Configuration information   
    /// </summary>
    public class OpenVPNInfo
    {
        public OpenVPNInfo()
        {}

        public OpenVPNInfo(string userName, string password, string openVPNServicePath, string oVPNFilePath)
        {
            SetUserName(userName);
            SetPassword(password);
            SetOpenVPNServicePath(openVPNServicePath);
            SetOVPNFilePath(oVPNFilePath);
        }

        private string UserName { get; set; }
        private string Password { get; set; }
        private string OpenVPNServicePath { get; set; }
        private string OVPNFilePath { get; set; }
        private ConnectionStates ConnectionState { get; set; }
        /// <summary>
        /// Public IP Address set by .ovpn active
        /// </summary>
        private string Public_IP_Address { get; set; }



        public void SetUserName(string value) => UserName = value;
        public string GetUserName() => UserName;

        public void SetPassword(string value) => Password = value;
        public string GetPassword() => Password;

        public void SetOpenVPNServicePath(string value) => OpenVPNServicePath = value;
        public string GetOpenVPNServicePath() => OpenVPNServicePath;
        
        public void SetOVPNFilePath(string value) => OVPNFilePath = value;
        public string GetOVPNFilePath() => OVPNFilePath;

        public void SetConnectionState(ConnectionStates value) => ConnectionState = value;
        public ConnectionStates GetConnectionState() => ConnectionState;

        public void SetPublic_IP_Address(string value) => Public_IP_Address = value;
        public string GetPublic_IP_Address() => Public_IP_Address;

    }
}
