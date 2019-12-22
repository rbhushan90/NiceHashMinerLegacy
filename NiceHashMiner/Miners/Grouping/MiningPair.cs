/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
using NiceHashMiner.Algorithms;
using NiceHashMiner.Devices;

namespace NiceHashMiner.Miners.Grouping
{
    public class MiningPair
    {
        public readonly ComputeDevice Device;
        public readonly Algorithm Algorithm;
        public string CurrentExtraLaunchParameters;

        public MiningPair(ComputeDevice d, Algorithm a)
        {
            Device = d;
            Algorithm = a;
            CurrentExtraLaunchParameters = Algorithm.ExtraLaunchParameters;
        }
    }
}
