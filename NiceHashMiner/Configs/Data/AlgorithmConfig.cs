﻿using NiceHashMiner.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace NiceHashMiner.Configs.Data {
    [Serializable]
    public class AlgorithmConfig {
        public string Name = ""; // Used as an indicator for easier user interaction
        public AlgorithmType NiceHashID = AlgorithmType.NONE;
        public MinerBaseType MinerBaseType = MinerBaseType.NONE;
        public string MinerName = "";  // TODO probably not needed
        public double BenchmarkSpeed = 0;
        public string ExtraLaunchParameters= "";
        public bool Enabled = true;
        public int LessThreads = 0;
    }
}
