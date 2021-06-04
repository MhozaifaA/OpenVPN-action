﻿using OpenVPN_action.Setup.System;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenVPN_action.Setup
{
    /// <summary>
    /// <see cref="https://github.com/esptl/OpenVPNUI/blob/master/Esp.Tools.OpenVPN.Configuration/SetupAPI.cs#L220"/>
    /// </summary>
    public static class TapDeviceManager
    {
        private const string ClassName = "tap0901";
        private const string HardwareID = ClassName;
        //private static readonly string _infPath;
        public static string _infPath;
        //static TapDeviceManager()
        //{
        //    _infPath = $@"{AppDomain.CurrentDomain.BaseDirectory}\openvpn\OemVista.inf";
        //}

        private static Guid GetClassGuid()
        {
            var guid = new Guid();
            if (SetupApi.SetupDiGetINFClass(_infPath, ref guid, ClassName, 32, 0))
                return guid;
            throw new IOException("Inf file could not be loaded");
        }

        public static IEnumerable<TapDevice> GetTapDevices()
        {
            var guid = GetClassGuid();
            var propertyBuffer = new StringBuilder();
            var deviceInfoData = new SetupApi.SP_DEVINFO_DATA();
            var deviceInfoSet = SetupApi.SetupDiGetClassDevsA(ref guid, 0, IntPtr.Zero,
                SetupApi.DIGCF_PRESENT);
            if (deviceInfoSet == IntPtr.Add(IntPtr.Zero, -1))

            {
            }

            var cont = true;
            for (var i = 0; cont; i++)
            {
                deviceInfoData.cbSize = (uint)Marshal.SizeOf(typeof(SetupApi.SP_DEVINFO_DATA));
                ;
                //is devices exist for class
                deviceInfoData.DevInst = 0;
                deviceInfoData.ClassGuid = guid;
                deviceInfoData.Reserved = (IntPtr)0;

                var res = SetupApi.SetupDiEnumDeviceInfo(deviceInfoSet,
                    (uint)i, ref deviceInfoData);
                if (!res && deviceInfoData.DevInst == 0)
                {
                    cont = false;
                    break;
                }


                propertyBuffer.Capacity = SetupApi.MAX_DEV_LEN;

                uint index = 0;
                uint requiredSize = 0;
                res = SetupApi.SetupDiGetDeviceRegistryProperty(deviceInfoSet,
                    ref deviceInfoData, SetupApi.SPDRP_DEVICEDESC, out index,
                    propertyBuffer, SetupApi.MAX_DEV_LEN,
                    out requiredSize);
                if (!res)
                {
                    var error = SetupApi.GetLastError();
                    SetupApi.SetupDiDestroyDeviceInfoList(deviceInfoSet);
                    throw new Win32Exception(error);
                }

                var device = new TapDevice
                {
                    Name = propertyBuffer.ToString(),
                    DevInst = deviceInfoData.DevInst
                };
                index = 0;
                requiredSize = 0;
                res = SetupApi.SetupDiGetDeviceRegistryProperty(deviceInfoSet,
                    ref deviceInfoData, SetupApi.SPDRP_HARDWAREID, out index,
                    propertyBuffer, SetupApi.MAX_DEV_LEN,
                    out requiredSize);
                if (!res)
                {
                    var error = SetupApi.GetLastError();
                    SetupApi.SetupDiDestroyDeviceInfoList(deviceInfoSet);
                    throw new Win32Exception(error);
                }
                device.HardwareId = propertyBuffer.ToString();
                if (device.HardwareId == HardwareID)
                    yield return device;
            }
        }

        public static bool SetupTapDevice()
        {
            var guid = GetClassGuid();

            var deviceInfoSet = SetupApi.SetupDiCreateDeviceInfoList(ref guid, IntPtr.Zero);
            var da = new SetupApi.SP_DEVINFO_DATA();
            da.cbSize = (uint)Marshal.SizeOf(typeof(SetupApi.SP_DEVINFO_DATA));
            var success = SetupApi.SetupDiCreateDeviceInfo(deviceInfoSet, ClassName, ref guid, null,
                IntPtr.Zero, 0x00000001, ref da);
            if (!success)
                throw new Win32Exception(SetupApi.GetLastError());

            var result = SetupApi.SetupDiSetDeviceRegistryProperty(deviceInfoSet, ref da,
                SetupApi.SPDRP_HARDWAREID,
                HardwareID, 2000);
            if (!result)
            {
                var error = SetupApi.GetLastError();
                SetupApi.SetupDiDestroyDeviceInfoList(deviceInfoSet);
                throw new Win32Exception(error, string.Format("SetupDiSetDeviceRegistryProperty {0}", error));
            }
            result = SetupApi.SetupDiCallClassInstaller(SetupApi.DIF_REGISTERDEVICE, deviceInfoSet, ref da);
            if (!result && false)
            {
                var error = SetupApi.GetLastError();
                SetupApi.SetupDiDestroyDeviceInfoList(deviceInfoSet);
                throw new Win32Exception(error, string.Format("SetupDiCallClassInstaller {0}", error));
            }
            var rest = IntPtr.Zero;
            result = SetupApi.UpdateDriverForPlugAndPlayDevices(IntPtr.Zero, HardwareID, _infPath,
                SetupApi.INSTALLFLAG.FORCE, rest);
            if (!result)
            {
                var error = SetupApi.GetLastError();
                SetupApi.SetupDiCallClassInstaller(0x00000005, deviceInfoSet, ref da);
                // throw new Win32Exception(error, string.Format("UpdateDriverForPlugAndPlayDevices {0}", error));
            }
            return true;
        }

        public class TapDevice
        {
            public string Name { get; internal set; }

            public string HardwareId { get; set; }

            public uint DevInst { get; set; }
        }
    }
}
