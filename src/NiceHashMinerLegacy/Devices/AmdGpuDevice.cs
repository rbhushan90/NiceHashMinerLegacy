﻿using NiceHashMiner.Devices.Querying.Amd;
using System;
using NiceHashMiner.Devices.Querying.Amd.OpenCL;
using NiceHashMinerLegacy.Common;

namespace NiceHashMiner.Devices
{
    [Serializable]
    public class AmdGpuDevice
    {
        //public const string TemperatureParam = " --gpu-fan 30-95 --temp-cutoff 95 --temp-overheat 90 " + " --temp-target 75 --auto-fan --auto-gpu ";

        public int DeviceID => (int) _openClSubset.DeviceID;
        public int BusID => (int) _openClSubset.AMD_BUS_ID;
        public string DeviceName; // init this with the ADL
        public string Uuid; // init this with the ADL, use PCI_VEN & DEV IDs
        public ulong DeviceGlobalMemory => _openClSubset._CL_DEVICE_GLOBAL_MEM_SIZE;

        //public bool UseOptimizedVersion { get; private set; }
        private readonly OpenCLDevice _openClSubset = new OpenCLDevice();

        public string InfSection { get; } // has arhitecture string

        public int Adl1Index { get; } // init this with the ADL
        public int Adl2Index { get; }

        public string Codename => _openClSubset._CL_DEVICE_NAME;

        internal AmdGpuDevice(OpenCLDevice openClSubset, string infSection, bool driverDisableAlgo, string name, string uuid)
        {
            InfSection = infSection;

            _openClSubset = openClSubset ?? new OpenCLDevice();

            DeviceName = name;
            Uuid = uuid;

            // Check for optimized version
            // first if not optimized
            Logger.Info("AmdGpuDevice", "List: " + _openClSubset._CL_DEVICE_NAME);
            Helpers.ConsolePrint("AmdGpuDevice", "List: " + _openClSubset._CL_DEVICE_NAME);
        }

        internal AmdGpuDevice(OpenCLDevice openClSubset, bool driverDisableAlgo, string name, AmdBusIDInfo busIdInfo)
            : this(openClSubset, busIdInfo.InfSection, driverDisableAlgo, name, busIdInfo.Uuid)
        {
            Adl1Index = busIdInfo.Adl1Index;
            Adl2Index = busIdInfo.Adl2Index;
        }
    }
}