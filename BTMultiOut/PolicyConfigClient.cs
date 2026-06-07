using System;
using System.Runtime.InteropServices;

namespace BTMultiOut
{
    /// <summary>
    /// Uses the undocumented Windows IPolicyConfig COM interface to set
    /// the default audio endpoint — the only reliable way to do this
    /// programmatically without a third-party library.
    /// </summary>
    public static class PolicyConfigClient
    {
        [ComImport]
        [Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPolicyConfig
        {
            void GetMixFormat();
            void GetDeviceFormat();
            void ResetDeviceFormat();
            void SetDeviceFormat();
            void GetProcessingPeriod();
            void SetProcessingPeriod();
            void GetShareMode();
            void SetShareMode();
            void GetPropertyValue();
            void SetPropertyValue();
            void SetDefaultEndpoint(
                [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
                ERole role);
            void SetEndpointVisibility();
        }

        [ComImport]
        [Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
        [ClassInterface(ClassInterfaceType.None)]
        private class PolicyConfigClientCom { }

        private enum ERole { eConsole = 0, eMultimedia = 1, eCommunications = 2 }

        /// <summary>
        /// Sets the Windows default playback device by device ID.
        /// Device ID comes from MMDevice.ID (NAudio).
        /// </summary>
        public static void SetDefaultDevice(string deviceId)
        {
            var policyConfig = (IPolicyConfig)new PolicyConfigClientCom();
            policyConfig.SetDefaultEndpoint(deviceId, ERole.eConsole);
            policyConfig.SetDefaultEndpoint(deviceId, ERole.eMultimedia);
            policyConfig.SetDefaultEndpoint(deviceId, ERole.eCommunications);
        }
    }
}