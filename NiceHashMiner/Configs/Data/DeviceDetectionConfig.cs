/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
using System;

namespace NiceHashMiner.Configs.Data
{
    /// <summary>
    /// DeviceDetectionConfig is used to enable/disable detection of certain GPU type devices
    /// </summary>
    ///
    [Serializable]
    public class DeviceDetectionConfig
    {
        public bool DisableDetectionCPU { get; set; }
        public bool DisableDetectionAMD { get; set; }
        public bool DisableDetectionNVIDIA { get; set; }

        public DeviceDetectionConfig()
        {
            DisableDetectionCPU = false;
            DisableDetectionAMD = false;
            DisableDetectionNVIDIA = false;
        }
    }
}
