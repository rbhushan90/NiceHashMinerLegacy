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
using NiceHashMiner.Devices;
using NiceHashMiner.Miners.Grouping;
using NiceHashMiner.Miners.Parsing;
using System.Threading.Tasks;
using System.Threading;
using NiceHashMiner.Algorithms;
using NiceHashMiner.Switching;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NiceHashMinerLegacy.Common.Enums;

namespace NiceHashMiner.Miners
{
    public class CastXMR : Miner
    {
        private int benchmarkStep = 0;
        private int st = 0;
        private double speed = 0.0d;
        private string hashspeed = "";
        public CastXMR() : base("CastXMR") { }

        bool benchmarkException {
            get {
                return MiningSetup.MinerPath == MinerPaths.Data.CastXMR;
            }
        }
        
        protected override int GetMaxCooldownTimeInMilliseconds() {
            if (this.MiningSetup.MinerPath == MinerPaths.Data.CastXMR) {
                return 60 * 1000 * 12; // wait for hashrate string
            }
            _maxCooldownTimeInMilliseconds = 60 * 1000 * 12;
            return 60 * 1000 * 12;
        }
        

        public override void Start(string url, string btcAdress, string worker)
        {
            if (!IsInit) {
                Helpers.ConsolePrint(MinerTag(), "MiningSetup is not initialized exiting Start()");
                return;
            }
            string username = GetUsername(btcAdress, worker);

            IsApiReadException = false; //** in miner 

            string alg = url.Substring(url.IndexOf("://") + 3, url.IndexOf(".") - url.IndexOf("://") - 3);
            string port = url.Substring(url.IndexOf(".com:") + 5, url.Length - url.IndexOf(".com:") - 5);

            url = alg + "." + Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation] +
                    ".nicehash.com:" + port;
                        /* WTF? No failover?
                        LastCommandLine = 
                                          " --pool " + url +
                                          " --user " + username + " --password x " +
                                          " --pool " + alg + ".hk.nicehash.com:" + port +
                                          " --user " + username + " --password x " +
                                          " --pool " + alg + ".in.nicehash.com:" + port +
                                          " --user " + username + " --password x " +
                                          " --pool " + alg + ".jp.nicehash.com:" + port +
                                          " --user " + username + " --password x " +
                                          " --pool " + alg + ".usa.nicehash.com:" + port +
                                          " --user " + username + " --password x " +
                                          " --pool " + alg + ".br.nicehash.com:" + port +
                                          " --user " + username + " --password x " +
                                          " --pool " + alg + ".eu.nicehash.com:" + port +
                                          " --user " + username + " --password x " +
                                              ExtraLaunchParametersParser.ParseForMiningSetup(
                                                                            MiningSetup,
                                                                            DeviceType.AMD) +
                                      " --gpu " +
                                      GetDevicesCommandString() +
                                                      " --remoteaccess" +
                                          " --remoteport=" + ApiPort.ToString();
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
                              " --remoteport=" + ApiPort.ToString() + "  --forcecompute ";
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.CryptoNightV7))
            {
                LastCommandLine = LastCommandLine + " --algo=1";
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.CryptoNightHeavy))
            {
                LastCommandLine = LastCommandLine + " --algo=2";
            }
            ProcessHandle = _Start();
        }

        protected override void _Stop(MinerStopType willswitch) {
            Stop_cpu_ccminer_sgminer_nheqminer(willswitch);
        }

        // new decoupled benchmarking routines
        #region Decoupled benchmarking routines

        protected override string BenchmarkCreateCommandLine(Algorithm algorithm, int time) {

            string CommandLine;
            string url = "";
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.CryptoNightV7))
            {
                url = Globals.GetLocationUrl(AlgorithmType.CryptoNightV7, Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], NhmConectionType.STRATUM_TCP).Replace("stratum+tcp://", "");
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.CryptoNightHeavy))
            {
                url = Globals.GetLocationUrl(AlgorithmType.CryptoNightHeavy, Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], NhmConectionType.STRATUM_TCP).Replace("stratum+tcp://", "");
            }
            

            string username = Globals.DemoUser;

            if (ConfigManager.GeneralConfig.WorkerName.Length > 0)
                username += "." + ConfigManager.GeneralConfig.WorkerName.Trim();

            CommandLine = " --pool " + url +
                          " --user " + Globals.DemoUser +
                          " -password x " +
                          ExtraLaunchParametersParser.ParseForMiningSetup(
                                                                MiningSetup,
                                                                DeviceType.AMD) +
                          " --gpu " +
                          GetDevicesCommandString() +
                                          " --remoteaccess" +
                              " --remoteport=" + ApiPort.ToString() + "  --forcecompute ";

            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.CryptoNightV7))
            {
                CommandLine = CommandLine + " --algo=1";
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.CryptoNightHeavy))
            {
                CommandLine = CommandLine + " --algo=2";
            }
            return CommandLine;

        }

        protected override bool BenchmarkParseLine(string outdata) {
            Helpers.ConsolePrint(MinerTag(), outdata);

            if (benchmarkException)
            {
                //NiceHashMiner.Forms.Form_Benchmark.BenchmarkStringAdd = " " + (benchmarkStep*3).ToString() + "%"; 
                //NiceHashMiner.Miner.BenchmarkStringAdd = " " + (benchmarkStep * 3).ToString() + "%";
                if (outdata.Contains("RPM | "))
                {
                    benchmarkStep++;
                    int end = outdata.IndexOf("H/s");
                    st = outdata.IndexOf("RPM | ");
                    hashspeed = outdata.Substring(st + 6, end - st - 6);
                    speed = speed + Double.Parse(hashspeed, CultureInfo.InvariantCulture);
                    //if (outdata.Contains("Hash Rate Avg: ")) //не находит шару за 5 минут на 570...
                    if (benchmarkStep >=33)
                    {
                        BenchmarkAlgorithm.BenchmarkSpeed = (speed / benchmarkStep);
                        BenchmarkSignalFinnished = true;
                    }
                }

            }
            return false;
        }

        protected override void BenchmarkOutputErrorDataReceivedImpl(string outdata) {
            CheckOutdata(outdata);
        }

        #endregion // Decoupled benchmarking routines

        public override async Task<ApiData> GetSummaryAsync() {
            
            var ad = new ApiData(MiningSetup.CurrentAlgorithmType);
            string ResponseFromCastxmr;
            try
            {
                HttpWebRequest WR = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:"+ ApiPort.ToString());
                WR.UserAgent = "GET / HTTP/1.1\r\n\r\n";
                WR.Timeout = 30 * 1000;
                WR.Credentials = CredentialCache.DefaultCredentials;
                WebResponse Response = WR.GetResponse();
                Stream SS = Response.GetResponseStream();
                SS.ReadTimeout = 20 * 1000;
                StreamReader Reader = new StreamReader(SS);
                ResponseFromCastxmr = Reader.ReadToEnd();
                //Helpers.ConsolePrint("API...........", ResponseFromCastxmr);
                if (ResponseFromCastxmr.Length == 0 || (ResponseFromCastxmr[0] != '{' && ResponseFromCastxmr[0] != '['))
                    throw new Exception("Not JSON!");
                Reader.Close();
                Response.Close();
            }
            catch (Exception ex)
            {
                //Helpers.ConsolePrint("API", ex.Message);
                return null;
            }

            dynamic resp = JsonConvert.DeserializeObject(ResponseFromCastxmr);

            if (resp != null)
            {
                int totals = resp.total_hash_rate_avg/1000;
                //Helpers.ConsolePrint("API hashrate...........", totals.ToString());

                ad.Speed = totals;
                if (ad.Speed == 0)
                {
                    CurrentMinerReadStatus = MinerApiReadStatus.READ_SPEED_ZERO;
                }
                else
                {
                    CurrentMinerReadStatus = MinerApiReadStatus.GOT_READ;
                }
            }

            Thread.Sleep(1000);
            return ad;
        }
    }
}
