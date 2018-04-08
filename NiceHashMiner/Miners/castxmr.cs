using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using NiceHashMiner.Configs;
using NiceHashMiner.Enums;
using NiceHashMiner.Devices;
using NiceHashMiner.Miners.Grouping;
using NiceHashMiner.Miners.Parsing;
using System.Threading.Tasks;
using System.Threading;

namespace NiceHashMiner.Miners
{
    public class CastXMR : Miner
    {
        private int benchmarkTimeWait = 11 * 60;
        private int benchmarkStep = 0;
        double benchmarkSpeed = 0;
        public CastXMR() : base("CastXMR") { }

        bool benchmarkException {
            get {
                return MiningSetup.MinerPath == MinerPaths.Data.CastXMR;
            }
        }

        protected override int GET_MAX_CooldownTimeInMilliseconds() {
            if (this.MiningSetup.MinerPath == MinerPaths.Data.CastXMR) {
                return 60 * 1000 * 12; // wait for hashrate string
            }
            return 60 * 1000 * 12; // 11 minute max
        }

        public override void Start(string url, string btcAdress, string worker)
        {
            if (!IsInit) {
                Helpers.ConsolePrint(MinerTAG(), "MiningSetup is not initialized exiting Start()");
                return;
            }
            string username = GetUsername(btcAdress, worker);

            //IsAPIReadException = MiningSetup.MinerPath == MinerPaths.Data.CastXMR;
            IsAPIReadException = false; //** in miner 


            //add failover
            string alg = url.Substring(url.IndexOf("://") + 3, url.IndexOf(".") - url.IndexOf("://") - 3);
            string port = url.Substring(url.IndexOf(".com:") + 5, url.Length - url.IndexOf(".com:") - 5);

            url = alg + "." + Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation] +
                    ".nicehash.com:" + port;
            /*            
                        LastCommandLine = 
                                          " --pool " + url +
                                          " --user " + username + "--password x " +
                                          " --pool " + alg + ".hk.nicehash.com:" + port +
                                          " --user " + username + "--password x " +
                                          " --pool " + alg + ".in.nicehash.com:" + port +
                                          " --user " + username + "--password x " +
                                          " --pool " + alg + ".jp.nicehash.com:" + port +
                                          " --user " + username + "--password x " +
                                          " --pool " + alg + ".usa.nicehash.com:" + port +
                                          " --user " + username + "--password x " +
                                          " --pool " + alg + ".br.nicehash.com:" + port +
                                          " --user " + username + "--password x " +
                                          " --pool " + alg + ".eu.nicehash.com:" + port +
                                          " --user " + username + "--password x " +
                                              ExtraLaunchParametersParser.ParseForMiningSetup(
                                                                            MiningSetup,
                                                                            DeviceType.AMD) +
                                      " --gpu " +
                                      GetDevicesCommandString() +
                                                      " --remoteaccess" +
                                          " --remoteport=" + APIPort.ToString();

            */
            LastCommandLine =
                              " --pool " + url +
                              " --user " + username + "--password x " +
                                  ExtraLaunchParametersParser.ParseForMiningSetup(
                                                                MiningSetup,
                                                                DeviceType.AMD) +
                          " --gpu " +
                          GetDevicesCommandString() +
                                          " --remoteaccess" +
                              " --remoteport=" + APIPort.ToString();
            ProcessHandle = _Start();
        }

        protected override void _Stop(MinerStopType willswitch) {
            Stop_cpu_ccminer_sgminer_nheqminer(willswitch);
        }

        // new decoupled benchmarking routines
        #region Decoupled benchmarking routines

        protected override string BenchmarkCreateCommandLine(Algorithm algorithm, int time) {

            string CommandLine;

            string name = Globals.NiceHashData[algorithm.NiceHashID].name;
            int port = Globals.NiceHashData[algorithm.NiceHashID].port;
            string url = name + "." + Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation] +
                    ".nicehash.com:" +
                    port;

            // demo for benchmark
            string username = Globals.DemoUser;

            if (ConfigManager.GeneralConfig.WorkerName.Length > 0)
                username += "." + ConfigManager.GeneralConfig.WorkerName.Trim();

            // cd to the cgminer for the process bins
            CommandLine = " --pool " + url +
                          " --user " + Globals.DemoUser +
                          " -password x " +
                          ExtraLaunchParametersParser.ParseForMiningSetup(
                                                                MiningSetup,
                                                                DeviceType.AMD) +
                          " --gpu " +
                          GetDevicesCommandString() +
                                          " --remoteaccess" +
                              " --remoteport=" + APIPort.ToString();

            //  CommandLine += GetDevicesCommandString();

            // CommandLine += " && del dump.txt\"";

            return CommandLine;

        }

        protected override bool BenchmarkParseLine(string outdata) {

            Helpers.ConsolePrint(MinerTAG(), outdata);
            if (benchmarkException)
            {
                if (outdata.Contains("Hash Rate Avg: "))
                {
                    benchmarkStep++;
                    int st = outdata.IndexOf("Hash Rate Avg: ");
                    int end = outdata.IndexOf("H/s");
                    //      int len = outdata.Length - speedLength - st;

                    //          string parse = outdata.Substring(st, len-1).Trim();
                    //          double tmp = 0;
                    //          Double.TryParse(parse, NumberStyles.Any, CultureInfo.InvariantCulture, out tmp);

                    // save speed
                    //       int i = outdata.IndexOf("Benchmark:");
                    //       int k = outdata.IndexOf("/s");
                    string hashspeed = outdata.Substring(st + 15, end - st - 15);
                    Helpers.ConsolePrint(MinerTAG(), hashspeed);
                    /*
                    int b = hashspeed.IndexOf(" ");
                       if (hashspeed.Contains("k"))
                           tmp *= 1000;
                       else if (hashspeed.Contains("m"))
                           tmp *= 1000000;
                       else if (hashspeed.Contains("g"))
                           tmp *= 1000000000;

                   }
                   */

                    double speed = Double.Parse(hashspeed, CultureInfo.InvariantCulture);
                    /*
                    benchmarkSpeed = (benchmarkSpeed + speed) / benchmarkStep;
                    if (benchmarkStep == 10)
                    {
                        BenchmarkAlgorithm.BenchmarkSpeed = benchmarkSpeed;
                        BenchmarkSignalFinnished = true;
                    }
                    */
                    BenchmarkAlgorithm.BenchmarkSpeed = speed;
                    BenchmarkSignalFinnished = true;
                }

            }
            return false;
        }

        protected override void BenchmarkOutputErrorDataReceivedImpl(string outdata) {
            CheckOutdata(outdata);
        }

        #endregion // Decoupled benchmarking routines

        public override async Task<APIData> GetSummaryAsync() {
            // CryptoNight does not have api bind port
            APIData hsrData = new APIData(MiningSetup.CurrentAlgorithmType);
            Helpers.ConsolePrint("API...........", hsrData.ToString());
            //string resp2 = await GetAPIDataAsync(APIPort, "GET / HTTP/1.1\r\n");
            //Helpers.ConsolePrint(MinerTAG(), "CASTXMR api!!2: " + resp2);
            hsrData.Speed = 0;
            if (IsAPIReadException) {
                // check if running
                if (ProcessHandle == null) {
                    _currentMinerReadStatus = MinerAPIReadStatus.RESTART;
                    Helpers.ConsolePrint(MinerTAG(), ProcessTag() + " Could not read data from castxmr Proccess is null");
                    return null;
                }
                try {
                    var runningProcess = Process.GetProcessById(ProcessHandle.Id);
                } catch (ArgumentException ex) {
                    _currentMinerReadStatus = MinerAPIReadStatus.RESTART;
                    Helpers.ConsolePrint(MinerTAG(), ProcessTag() + " Could not read data from castxmr reason: " + ex.Message);
                    return null; // will restart outside
                } catch (InvalidOperationException ex) {
                    _currentMinerReadStatus = MinerAPIReadStatus.RESTART;
                    Helpers.ConsolePrint(MinerTAG(), ProcessTag() + " Could not read data from castxmr reason: " + ex.Message);
                    return null; // will restart outside
                }

                var totalSpeed = 0.0d;
                foreach (var miningPair in MiningSetup.MiningPairs) {
           //         var algo = miningPair.Device.GetAlgorithm(MinerBaseType.CastXMR, AlgorithmType.CryptoNightV7, AlgorithmType.NONE);
           //         if (algo != null) {
           //             totalSpeed += algo.BenchmarkSpeed;
           //         }
                }

               // hsrData.Speed = totalSpeed;
               // return hsrData;
            }

              return await GetSummaryGPU_CastXMRAsync();
            //return hsrData;
        }
    }
}
