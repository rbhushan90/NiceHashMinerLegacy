using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Management;
using NiceHashMiner.Configs;
using NiceHashMiner.Devices;
using NiceHashMiner.Miners.Grouping;
using NiceHashMiner.Miners.Parsing;
using System.Threading;
using System.Threading.Tasks;
using NiceHashMiner.Algorithms;
using NiceHashMiner.Switching;
using NiceHashMinerLegacy.Common.Enums;

namespace NiceHashMiner.Miners
{
    class teamredminer : Miner
    {
        private readonly int GPUPlatformNumber;
        Stopwatch _benchmarkTimer = new Stopwatch();
        int count = 0;

        public teamredminer()
            : base("teamredminer_AMD")
        {
            GPUPlatformNumber = ComputeDeviceManager.Available.AmdOpenCLPlatformNum;
            IsKillAllUsedMinerProcs = true;
            IsNeverHideMiningWindow = true;

        }
       
        protected override int GetMaxCooldownTimeInMilliseconds() {
            return 240;
        }
        
        protected override void _Stop(MinerStopType willswitch) {
            Stop_cpu_ccminer_sgminer_nheqminer(willswitch);
        //    Killteamredminer();
        }
        
        public override void Start(string url, string btcAdress, string worker)
        {
            if (!IsInit)
            {
                Helpers.ConsolePrint(MinerTag(), "MiningSetup is not initialized exiting Start()");
                return;
            }
            string username = GetUsername(btcAdress, worker);
            IsApiReadException = true; //** in miner
            //add failover
            string alg = url.Substring(url.IndexOf("://") + 3, url.IndexOf(".") - url.IndexOf("://") - 3);
            string port = url.Substring(url.IndexOf(".com:") + 5, url.Length - url.IndexOf(".com:") - 5);

            LastCommandLine = " -a lyra2z -o " + url +
                              " -u " + username +
                              " -p x " +
                              " " +
                              ExtraLaunchParametersParser.ParseForMiningSetup(
                                                                MiningSetup,
                                                                DeviceType.AMD) +
                              " -d ";

            LastCommandLine += GetDevicesCommandString();
            ProcessHandle = _Start();
        }

        // new decoupled benchmarking routines
        #region Decoupled benchmarking routines

        protected override string BenchmarkCreateCommandLine(Algorithm algorithm, int time) {
            var CommandLine = "";

            string url = Globals.GetLocationUrl(algorithm.NiceHashID, Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], this.ConectionType);

            // demo for benchmark
            string username = Globals.GetBitcoinUser();

            if (ConfigManager.GeneralConfig.WorkerName.Length > 0)
                username += "." + ConfigManager.GeneralConfig.WorkerName.Trim();

            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.Lyra2z))
            {
                CommandLine = "-a lyra2z" +
                " --url stratum+tcp://lyra2z.eu.nicehash.com:3365" +  " --user " + username + " - p x " +
                " --url stratum+tcp://lyra2z.eu.mine.zpool.ca:4553" + " --user 1JqFnUR3nDFCbNUmWiQ4jX6HRugGzX55L2" + " -p c=BTC -d ";
            }
            /*
            CommandLine = " -a lyra2z "+
                          " --url " + url +
                          " --user " + username +
                          " -p x " +
                          ExtraLaunchParametersParser.ParseForMiningSetup(
                                                                MiningSetup,
                                                                DeviceType.AMD) +
                          " -d ";
*/
            CommandLine += GetDevicesCommandString();
           
            return CommandLine;

        }

        protected override bool BenchmarkParseLine(string outdata) {
            string hashSpeed = "";
            int kspeed = 1;
            //Pool lyra2z.eu.nicehash.com share accepted.
            //Stats GPU 0 - lyra2z: 3.279Mh/s (3.279Mh/s)
            if (outdata.Contains("- lyra2z: ") )
            {
                int i = outdata.IndexOf("- lyra2z: ");
                int k = outdata.IndexOf("Mh/s (");
                hashSpeed = outdata.Substring(i+10 , k - i-10).Trim();
                Helpers.ConsolePrint(hashSpeed, "");
                if (outdata.ToUpper().Contains("H/S"))
                {
                    kspeed = 1;
                }
                if (outdata.ToUpper().Contains("KH/S"))
                {
                    kspeed = 1000;
                }
                if (outdata.ToUpper().Contains("MH/S"))
                {
                    kspeed = 1000000;
                }

                double speed = Double.Parse(hashSpeed, CultureInfo.InvariantCulture);
                BenchmarkAlgorithm.BenchmarkSpeed = Math.Max(BenchmarkAlgorithm.BenchmarkSpeed, speed * kspeed);
                //Killteamredminer();
                BenchmarkSignalFinnished = true;
                //BenchmarkSignalHanged = true;
                return true;
            }

            return false;

        }

      
        protected override void BenchmarkOutputErrorDataReceivedImpl(string outdata)
        {
            CheckOutdata(outdata);
        }

      
        #endregion // Decoupled benchmarking routines

        // TODO _currentMinerReadStatus
        public override async Task<ApiData> GetSummaryAsync()
        {
            //if (!IsApiReadException) return await GetSummaryCpuCcminerAsync();
            // check if running
            if (ProcessHandle == null)
            {
                CurrentMinerReadStatus = MinerApiReadStatus.RESTART;
                Helpers.ConsolePrint(MinerTag(), ProcessTag() + " Could not read data from teamredminer Proccess is null");
                return null;
            }
            try
            {
                Process.GetProcessById(ProcessHandle.Id);
            }
            catch (ArgumentException ex)
            {
                CurrentMinerReadStatus = MinerApiReadStatus.RESTART;
                Helpers.ConsolePrint(MinerTag(), ProcessTag() + " Could not read data from teamredminer reason: " + ex.Message);
                return null; // will restart outside
            }
            catch (InvalidOperationException ex)
            {
                CurrentMinerReadStatus = MinerApiReadStatus.RESTART;
                Helpers.ConsolePrint(MinerTag(), ProcessTag() + " Could not read data from teamredminer reason: " + ex.Message);
                return null; // will restart outside
            }

            var totalSpeed = 0.0d;
            foreach (var miningPair in MiningSetup.MiningPairs)
            {
                var algo = miningPair.Device.GetAlgorithm(MinerBaseType.teamredminer, AlgorithmType.Lyra2z, AlgorithmType.NONE);
                if (algo != null)
                {
                    totalSpeed += algo.BenchmarkSpeed;
                }
            }

            var teamredminerData = new ApiData(MiningSetup.CurrentAlgorithmType)
            {
                Speed = totalSpeed
            };
            CurrentMinerReadStatus = MinerApiReadStatus.GOT_READ;
            // check if speed zero
            if (teamredminerData.Speed == 0) CurrentMinerReadStatus = MinerApiReadStatus.READ_SPEED_ZERO;
            return teamredminerData;
        }

    }
}
