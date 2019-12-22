﻿/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiceHashMiner.Devices.Querying
{
    public class VideoControllerData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string PnpDeviceID { get; set; }
        public string DriverVersion { get; set; }
        public string Status { get; set; }
        public string InfSection { get; set; } // get arhitecture
        public ulong AdapterRam { get; set; }
    }
}
