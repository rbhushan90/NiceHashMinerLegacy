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
            return 60*1000;
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
            //IsApiReadException = MiningSetup.MinerPath == MinerPaths.Data.teamredminer;
            IsApiReadException = false;

            //add failover
            string alg = url.Substring(url.IndexOf("://") + 3, url.IndexOf(".") - url.IndexOf("://") - 3);
            string port = url.Substring(url.IndexOf(".com:") + 5, url.Length - url.IndexOf(".com:") - 5);

            var algo = "";
            var apiBind = " --api_listen=127.0.0.1:" + ApiPort;

            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.CryptoNightV8))
            {
                algo = " -a cnv8";
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.Lyra2z))
            {
                algo = " -a lyra2z";
            }

              LastCommandLine = algo + " -o " + url +
                              " -u " + username +
                              " -p x " + apiBind +
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

            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.CryptoNightV8))
            {
                CommandLine = "-a cnv8" +
                " --url stratum+tcp://cryptonightv8.eu.nicehash.com:3367" + " --user " + username + " - p x " +
                " --url stratum+tcp://xmr-eu.dwarfpool.com:8005" + " --user 42fV4v2EC4EALhKWKNCEJsErcdJygynt7RJvFZk8HSeYA9srXdJt58D9fQSwZLqGHbijCSMqSP4mU7inEEWNyer6F7PiqeX" + " -p x -d ";
            }

            //return $" -o stratum+tcp://xmr-eu.dwarfpool.com:8005 {variant} -u 42fV4v2EC4EALhKWKNCEJsErcdJygynt7RJvFZk8HSeYA9srXdJt58D9fQSwZLqGHbijCSMqSP4mU7inEEWNyer6F7PiqeX.{worker} -p x {extras} --api-port {ApiPort} --donate-level=1"
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
            double speed = 0;
            //GPU 0 - cnv8: 893.9 h/s, avg 885.2 h/s, pool 1.363kh/s a:2 r:0 hw:0
            //GPU 0 - cnv8: 2.022kh/s, avg 2.024kh/s, pool 1.863kh/s a:2017 r:69 hw:40
            if (outdata.Contains("- cnv8: "))
            {
                int i = outdata.IndexOf("cnv8: ");
                int k = outdata.IndexOf("h/s, avg");
                hashSpeed = outdata.Substring(i + 5, k - i - 6).Trim();
                Helpers.ConsolePrint(hashSpeed, "");
                if (outdata.ToUpper().Contains("H/S"))
                {
                    kspeed = 1;
                }
                if (outdata.Substring(0,45).ToUpper().Contains("KH/S"))
                {
                    kspeed = 1000;
                }
                if (outdata.Substring(0, 45).ToUpper().Contains("MH/S"))
                {
                    kspeed = 1000000;
                }
                count++;
                if (count >= 3) 
                {
                    try
                    {
                        speed = Double.Parse(hashSpeed, CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        MessageBox.Show("Unsupported miner version - " + MiningSetup.MinerPath,
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        BenchmarkSignalFinnished = true;
                        return false;
                    }
                    BenchmarkAlgorithm.BenchmarkSpeed = Math.Max(BenchmarkAlgorithm.BenchmarkSpeed, speed * kspeed);
                    //Killteamredminer();
                    BenchmarkSignalFinnished = true;
                    //BenchmarkSignalHanged = true;
                    return true;
                }
            }
            //GPU 0 - lyra2z: 2.530Mh/s, avg 2.582Mh/s, pool 2.859Mh/s a:5 r:0 hw:0
            //GPU 0 - lyra2z: 596.0kh/s, avg 607.3kh/s, pool 571.6kh/s a:1 r:0 hw:0
            //[2018-11-10 17:02:42] GPU 0 - lyra2z: 2.282Mh/s, avg 2.331Mh/s, pool 571.6kh/s a:1 r:0 hw:0

            if (outdata.Contains("- lyra2z: ") )
            {
                int i = outdata.IndexOf("lyra2z: ");
                int k = outdata.IndexOf("h/s, avg");
                hashSpeed = outdata.Substring(i+8 , k - i-9).Trim();
                Helpers.ConsolePrint(hashSpeed, "");
                if (outdata.ToUpper().Contains("H/S"))
                {
                    kspeed = 1;
                }
                if (outdata.Substring(0, 50).ToUpper().Contains("KH/S"))
                {
                    kspeed = 1000;
                }
                if (outdata.Substring(0, 50).ToUpper().Contains("MH/S"))
                {
                    kspeed = 1000000;
                }
                count++;
                if (count >= 3)
                {
                    try
                    {
                        speed = Double.Parse(hashSpeed, CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        MessageBox.Show("Unsupported miner version - " + MiningSetup.MinerPath,
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        BenchmarkSignalFinnished = true;
                        return false;
                    }

                    BenchmarkAlgorithm.BenchmarkSpeed = Math.Max(BenchmarkAlgorithm.BenchmarkSpeed, speed * kspeed);
                    //Killteamredminer();
                    BenchmarkSignalFinnished = true;
                    //BenchmarkSignalHanged = true;
                    return true;
                }
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
            var ad = new ApiData(MiningSetup.CurrentAlgorithmType);

            var resp = await GetApiDataAsync(ApiPort, "summary");
            
            //Helpers.ConsolePrint("trm-DEBUG_resp", resp.Trim());

            if (resp == null)
            {
                CurrentMinerReadStatus = MinerApiReadStatus.NONE;
                return null;
            }

            try
            {
                // Checks if all the GPUs are Alive first
                var resp2 = await GetApiDataAsync(ApiPort, "devs");
                if (resp2 == null)
                {
                    CurrentMinerReadStatus = MinerApiReadStatus.NONE;
                    return null;
                }

                var checkGpuStatus = resp2.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                /*
                for (var i = 1; i < checkGpuStatus.Length - 1; i++)
                {
                    if (checkGpuStatus[i].Contains("Enabled=Y") && !checkGpuStatus[i].Contains("Status=Alive"))
                    {
                        Helpers.ConsolePrint(MinerTag(),
                            ProcessTag() + " GPU " + i + ": Sick/Dead/NoStart/Initialising/Disabled/Rejecting/Unknown");
                        CurrentMinerReadStatus = MinerApiReadStatus.WAIT;
                        return null;
                    }
                }
                */
                var resps = resp.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                if (resps[1].Contains("SUMMARY"))
                {
                    var data = resps[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    // Get miner's current total speed
                    var speed = data[4].Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    // Get miner's current total MH
                    var totalMH = double.Parse(data[18].Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries)[1],
                        new CultureInfo("en-US"));

                    ad.Speed = double.Parse(speed[1]) * 1000;

                    if (totalMH <= PreviousTotalMH)
                    {
                        Helpers.ConsolePrint(MinerTag(), ProcessTag() + " teamredminer might be stuck as no new hashes are being produced");
                        Helpers.ConsolePrint(MinerTag(),
                            ProcessTag() + " Prev Total MH: " + PreviousTotalMH + " .. Current Total MH: " + totalMH);
                        CurrentMinerReadStatus = MinerApiReadStatus.NONE;
                        return null;
                    }

                    PreviousTotalMH = totalMH;
                }
                else
                {
                    ad.Speed = 0;
                }
            }
            catch
            {
                CurrentMinerReadStatus = MinerApiReadStatus.NONE;
                return null;
            }

            CurrentMinerReadStatus = MinerApiReadStatus.GOT_READ;
            // check if speed zero
            if (ad.Speed == 0) CurrentMinerReadStatus = MinerApiReadStatus.READ_SPEED_ZERO;

            return ad;
        }
    }
}
