using System.Diagnostics;
using System.Threading.Tasks;
using NiceHashMiner.Algorithms;
using NiceHashMiner.Miners.Parsing;
using NiceHashMinerLegacy.Common.Enums;

namespace NiceHashMiner.Miners
{
    public class CpuMiner : Miner
    {
        public CpuMiner()
            : base("cpuminer_CPU")
        {
        }

        protected override int GetMaxCooldownTimeInMilliseconds()
        {
            return 3600000; // 1hour
        }

        public override void Start(string url, string btcAdress, string worker)
        {
            if (!IsInit)
            {
                Helpers.ConsolePrint(MinerTag(), "MiningSetup is not initialized exiting Start()");
                return;
            }

            var username = GetUsername(btcAdress, worker);

            LastCommandLine = "--algo=" + MiningSetup.MinerName +
                              " --url=" + url +
                              " --userpass=" + username + ":x " +
                              ExtraLaunchParametersParser.ParseForMiningSetup(
                                  MiningSetup,
                                  DeviceType.CPU) +
                              " --api-bind=" + ApiPort;

            ProcessHandle = _Start();
        }

        public override Task<ApiData> GetSummaryAsync()
        {
            return GetSummaryCpuCcminerAsync();
        }

        protected override void _Stop(MinerStopType willswitch)
        {
            Stop_cpu_ccminer_sgminer_nheqminer(willswitch);
        }

        protected override NiceHashProcess _Start()
        {
            var p = base._Start();

            var affinityMask = MiningSetup.MiningPairs[0].Device.AffinityMask;
            if (affinityMask != 0 && p != null)
                CpuID.AdjustAffinity(p.Id, affinityMask);

            return p;
        }

        // new decoupled benchmarking routines

        #region Decoupled benchmarking routines
        [System.Runtime.InteropServices.DllImport("Kernel32")]
        static extern bool IsProcessorFeaturePresent(ProcessorFeature processorFeature);
        enum ProcessorFeature : uint
        {
            PF_FLOATING_POINT_PRECISION_ERRATA = 0,
            PF_FLOATING_POINT_EMULATED = 1,
            PF_COMPARE_EXCHANGE_DOUBLE = 2,
            PF_MMX_INSTRUCTIONS_AVAILABLE = 3,
            PF_PPC_MOVEMEM_64BIT_OK = 4,
            PF_ALPHA_BYTE_INSTRUCTIONS = 5,
            PF_XMMI_INSTRUCTIONS_AVAILABLE = 6,
            PF_3DNOW_INSTRUCTIONS_AVAILABLE = 7,
            PF_RDTSC_INSTRUCTION_AVAILABLE = 8,
            PF_PAE_ENABLED = 9,
            PF_XMMI64_INSTRUCTIONS_AVAILABLE = 10,
            PF_SSE_DAZ_MODE_AVAILABLE = 11,
            PF_NX_ENABLED = 12,
            PF_SSE3_INSTRUCTIONS_AVAILABLE = 13,
            PF_COMPARE_EXCHANGE128 = 14,
            PF_COMPARE64_EXCHANGE128 = 15,
            PF_CHANNELS_ENABLED = 16,
            PF_XSAVE_ENABLED = 17,
            PF_SECOND_LEVEL_ADDRESS_TRANSLATION = 20,
            PF_VIRT_FIRMWARE_ENABLED = 21,
        }


        protected override string BenchmarkCreateCommandLine(Algorithm algorithm, int time)
        {
            foreach (ProcessorFeature feature in System.Enum.GetValues(typeof(ProcessorFeature)))
            {
                Helpers.ConsolePrint("CPU features", feature.ToString() + "\t: " + IsProcessorFeaturePresent(feature));
            }

            return "--algo=" + algorithm.MinerName +
                   " --benchmark" +
                   ExtraLaunchParametersParser.ParseForMiningSetup(
                       MiningSetup,
                       DeviceType.CPU) +
                   " --time-limit " + time;
        }

        protected override Process BenchmarkStartProcess(string CommandLine)
        {
            var benchmarkHandle = base.BenchmarkStartProcess(CommandLine);

            var affinityMask = MiningSetup.MiningPairs[0].Device.AffinityMask;
            if (affinityMask != 0 && benchmarkHandle != null)
                CpuID.AdjustAffinity(benchmarkHandle.Id, affinityMask);

            return benchmarkHandle;
        }

        protected override bool BenchmarkParseLine(string outdata)
        {
            if (!double.TryParse(outdata, out var lastSpeed)) return false;

            BenchmarkAlgorithm.BenchmarkSpeed = lastSpeed;
            return true;

        }

        protected override void BenchmarkOutputErrorDataReceivedImpl(string outdata)
        {
            CheckOutdata(outdata);
        }

        #endregion // Decoupled benchmarking routines
    }
}
