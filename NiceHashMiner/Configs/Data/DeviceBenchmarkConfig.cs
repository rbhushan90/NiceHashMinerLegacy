/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
using System;
using System.Collections.Generic;

namespace NiceHashMiner.Configs.Data
{
    [Serializable]
    public class DeviceBenchmarkConfig
    {
        public string DeviceUUID = "";
        public string DeviceName = "";
        //public int TimeLimit { get; set; }
        public List<AlgorithmConfig> AlgorithmSettings = new List<AlgorithmConfig>();
        public List<DualAlgorithmConfig> DualAlgorithmSettings = new List<DualAlgorithmConfig>();
    }
}
