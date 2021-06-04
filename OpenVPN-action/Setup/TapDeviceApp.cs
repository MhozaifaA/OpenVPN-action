using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace OpenVPN_action.Setup
{
    public static class TapDeviceApp
    {
        /// <summary>
        /// full controlled result to check and install tap for vpn
        /// <para>warning:  if return false should <see langword="throw"/> <see cref="Exception"/>  </para>
        /// </summary>
        /// <param name="tapctl_path"></param>
        /// <param name="oemvista_inf_path"></param>
        /// <param name="tabinstall_path"></param>
        /// <returns> <see cref="bool"/>  <see cref="true"/> if success add tap , other <see cref="false"/> </returns>
        public static bool CallLogical_vpn_Tab(string tapctl_path, string oemvista_inf_path,string tabinstall_path)
        {
            if (!GetIfExistTap())
            {
                CreateTap(tapctl_path);
                if (!GetIfExistTap())
                {
                    InstallTap(oemvista_inf_path, tabinstall_path);
                    CreateTap(tapctl_path);
                    if (!GetIfExistTap())
                    {
                        return false;
                    }
                    DeleteLastTap(tapctl_path); // should be InstallTap success
                }
            }

            return true;
        }

        /// <summary>
        /// check NetworkInterfaces if exist any same name
        /// </summary>
        /// <param name="name">default works woth vpn apps</param>
        /// <returns></returns>
        public static bool GetIfExistTap(string name= "vpn")
            => NetworkInterface.GetAllNetworkInterfaces().Any(x => x.Name == name);

        /// <summary>
        /// create new Tap in NetworkInterfaces
        /// <para> what's tapctl !: it's cmd work under openvpn tap/tun windows v6 and more  </para>
        /// </summary>
        /// <param name="tapctl_path">location ctl</param>
        /// <param name="name"></param>
        public static void CreateTap(string tapctl_path,string name = "vpn")
        {
            Process process = new Process();
            process.StartInfo.FileName = tapctl_path;
            process.StartInfo.Arguments = $"create --name {name}";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.Start();
            process.WaitForExit();
            process.Close();
        }

        /// <summary>
        /// delete last name created from same NetworkInterface tap
        /// </summary>
        /// <param name="tapctl_path">location ctl</param>
        public static void RemoveTap(string tapctl_path, string name = "vpn")
        {
            var lasCreated = NetworkInterface.GetAllNetworkInterfaces().Where(x => x.Name.StartsWith("vpn")).LastOrDefault();
            if(lasCreated is null) return;
            Process process = new Process();
            process.StartInfo.FileName = tapctl_path;
            process.StartInfo.Arguments = $"delete {lasCreated.Id}";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.Start();
            process.WaitForExit();
            process.Close(); //tapctl delete vpn
        }
        
        /// <summary>
        /// useful after install automatic created by openvpn
        /// </summary>
        /// <param name="tapctl_path">location ctl</param>
        public static void DeleteLastTap(string tapctl_path)
        {
            var lasCreated = NetworkInterface.GetAllNetworkInterfaces().Where(x => x.Name.StartsWith("Ethernet"))
                .Select(x => new { x.Name, x.Id }).OrderBy(x => x.Name).LastOrDefault();
            if (lasCreated is null) return;
            Process process = new Process();
            process.StartInfo.FileName = tapctl_path;
            process.StartInfo.Arguments = $"delete {lasCreated.Id}";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.Start();
            process.WaitForExit();
            process.Close(); //tapctl delete vpn
        }


        /// <summary>
        /// install INF file 
        /// </summary>
        /// <param name="oemvista_inf_path"> cmd written by openvpn </param>
        /// <param name="tabinstall_path"> cmd written by openvpn </param>
        public static void InstallTap(string oemvista_inf_path,string tabinstall_path)
        {
            //x64   
            if (Environment.Is64BitOperatingSystem)
            {
                TapDeviceManager._infPath = oemvista_inf_path;//for once application start , whill call static ctor once 
                if (TapDeviceManager.GetTapDevices().ToArray().Length == 0)
                    TapDeviceManager.SetupTapDevice();
            }
            else  //x32
            {
                string Command = $@"{tabinstall_path} install {oemvista_inf_path} tap0901";
                ProcessStartInfo ProcessInfo;
                Process Process;
                ProcessInfo = new ProcessStartInfo("cmd.exe", "/C " + Command);
                ProcessInfo.WindowStyle = ProcessWindowStyle.Hidden;
                ProcessInfo.CreateNoWindow = true;
                ProcessInfo.UseShellExecute = false;
                Process = Process.Start(ProcessInfo);
                Process.WaitForExit();
                Process.Close();
            }
        }

       
    }
}
