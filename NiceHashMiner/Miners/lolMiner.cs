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
    class lolMiner : Miner
    {
        private readonly int GPUPlatformNumber;
        Stopwatch _benchmarkTimer = new Stopwatch();
        int count = 0;
        double speed = 0;

        public lolMiner()
            : base("lolMiner_AMD")
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
        }
        
        public override void Start(string url, string btcAdress, string worker)
        {
            if (!IsInit)
            {
                Helpers.ConsolePrint(MinerTag(), "MiningSetup is not initialized exiting Start()");
                return;
            }
            string username = GetUsername(btcAdress, worker);
            //IsApiReadException = MiningSetup.MinerPath == MinerPaths.Data.lolMiner;
            IsApiReadException = false;

            //add failover
            string alg = url.Substring(url.IndexOf("://") + 3, url.IndexOf(".") - url.IndexOf("://") - 3);
            string port = url.Substring(url.IndexOf(".com:") + 5, url.Length - url.IndexOf(".com:") - 5);

            //var algo = "";
            url = url.Replace("stratum+tcp://", "");
            var apiBind = " --apiport " + ApiPort;

           LastCommandLine = "--coin AUTO144_5 --overwritePersonal BgoldPoW --pool " + url +
                              " --port " + port+  
                              " --user " + username +
                              " -p x " + apiBind +
                              " " +
                              ExtraLaunchParametersParser.ParseForMiningSetup(
                                                                MiningSetup,
                                                                DeviceType.AMD) +
                              " --devices ";

            LastCommandLine += GetDevicesCommandString();//amd карты перечисляются первыми
            ProcessHandle = _Start();
        }

        // new decoupled benchmarking routines
        #region Decoupled benchmarking routines

        protected override string BenchmarkCreateCommandLine(Algorithm algorithm, int time) {
            var CommandLine = "";

            string url = Globals.GetLocationUrl(algorithm.NiceHashID, Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], this.ConectionType);
            string port = url.Substring(url.IndexOf(".com:") + 5, url.Length - url.IndexOf(".com:") - 5);
            // demo for benchmark
            string username = Globals.GetBitcoinUser();

            if (ConfigManager.GeneralConfig.WorkerName.Length > 0)
                username += "." + ConfigManager.GeneralConfig.WorkerName.Trim();
            //start lolMiner.exe --coin AUTO144_5 --overwritePersonal BgoldPoW--pool europe.equihash-hub.miningpoolhub.com
            //--port 20595 --user angelbbs.lol--pass x --devices 0,2,5 amd
            CommandLine = "--coin AUTO144_5 --overwritePersonal BgoldPoW" +
                " --pool europe.equihash-hub.miningpoolhub.com --port 20595 --user angelbbs.lol --pass x"+
                " --devices ";

            CommandLine += GetDevicesCommandString(); //amd карты перечисляются первыми
           
            return CommandLine;

        }

        protected override bool BenchmarkParseLine(string outdata) {
            string hashSpeed = "";

            //Average speed (30s): 25.5 sol/s 
            //GPU 3: Share accepted (45 ms)
            if (outdata.Contains("Average speed (30s):"))
            {
                int i = outdata.IndexOf("Average speed (30s):");
                int k = outdata.IndexOf("sol/s");
                hashSpeed = outdata.Substring(i + 21, k - i - 22).Trim();
                try
                    {
                        speed = speed + Double.Parse(hashSpeed, CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        MessageBox.Show("Unsupported miner version - " + MiningSetup.MinerPath,
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        BenchmarkSignalFinnished = true;
                        return false;
                    }
                count++;
            }

            if (outdata.Contains("Share accepted") && speed != 0)
            {
                BenchmarkAlgorithm.BenchmarkSpeed = speed / count;
                BenchmarkSignalFinnished = true;
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
                        Helpers.ConsolePrint(MinerTag(), ProcessTag() + " lolMiner might be stuck as no new hashes are being produced");
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
