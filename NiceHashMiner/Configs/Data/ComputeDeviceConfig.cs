/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
using System;

namespace NiceHashMiner.Configs.Data
{
    [Serializable]
    public class ComputeDeviceConfig
    {
        public string Name = "";
        public bool Enabled = true;
        public string UUID = "";
    }
}
