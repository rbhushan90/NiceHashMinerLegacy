using NiceHashMiner.Miners.Grouping;
using NiceHashMiner.Miners.Parsing;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NiceHashMiner.Algorithms;
using NiceHashMinerLegacy.Common.Enums;
using NiceHashMiner.Configs;
using System.Threading;
using System.Windows.Forms;
using System.Text;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace NiceHashMiner.Miners
{
    public class TTMiner : Miner
    {
        public TTMiner() : base("TTMiner_NVIDIA")
        { }

        private int TotalCount = 12;
        private double speed = 0;

        private double Total = 0;
        private const int TotalDelim = 2;
        private bool fapi = true;

        private bool _benchmarkException => MiningSetup.MinerPath == MinerPaths.Data.TTMiner;

        protected override int GetMaxCooldownTimeInMilliseconds()
        {
            return 60 * 1000 * 8;
        }

        public override void Start(string url, string btcAdress, string worker)
        {
            if (!IsInit)
            {
                Helpers.ConsolePrint(MinerTag(), "MiningSetup is not initialized exiting Start()");
                return;
            }
            var username = GetUsername(btcAdress, worker);

            //IsApiReadException = MiningSetup.MinerPath == MinerPaths.Data.TTMiner;
            IsApiReadException = true;

            var algo = "";
            var apiBind = "";
            string alg = url.Substring(url.IndexOf("://") + 3, url.IndexOf(".") - url.IndexOf("://") - 3);
            string port = url.Substring(url.IndexOf(".com:") + 5, url.Length - url.IndexOf(".com:") - 5);
            algo = "-a " + MiningSetup.MinerName;
            apiBind = " --api-bind 127.0.0.1:" + ApiPort;
            //apiBind = "";
            url = url.Replace("stratum+tcp://", "");
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.Lyra2REv3))
            {
                algo = "-a LYRA2V3";
            }

            LastCommandLine = algo +
                " -P " + username + ":x@" + url +
                " -P " + username + ":x@" + alg + ".hk.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".jp.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".in.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".usa.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".br.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".eu.nicehash.com:" + port +

                apiBind +
                " -devices " + GetDevicesCommandString() + " " +
                ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.NVIDIA) + " ";
            ProcessHandle = _Start();
        }

        protected override void _Stop(MinerStopType willswitch)
        {
            Stop_cpu_ccminer_sgminer_nheqminer(willswitch);
        }

        // new decoupled benchmarking routines

        #region Decoupled benchmarking routines

        protected override string BenchmarkCreateCommandLine(Algorithm algorithm, int time)
        {
            string url = Globals.GetLocationUrl(algorithm.NiceHashID, Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], this.ConectionType);
            string alg = url.Substring(url.IndexOf("://") + 3, url.IndexOf(".") - url.IndexOf("://") - 3);
            string port = url.Substring(url.IndexOf(".com:") + 5, url.Length - url.IndexOf(".com:") - 5);
            var username = GetUsername(Globals.GetBitcoinUser(), ConfigManager.GeneralConfig.WorkerName.Trim());
            var commandLine = "";

            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.MTP))
            {
                commandLine = " -a MTP" +
                " -P aMGfYX8ARy4wKE57fPxkEBcnNuHegDBweE." + ConfigManager.GeneralConfig.WorkerName.Trim() + ":x@xzc.2miners.com:8080" +
                " -P " + username + ":x@" + alg + ".eu.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".hk.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".jp.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".in.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".usa.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".br.nicehash.com:" + port +
                              ExtraLaunchParametersParser.ParseForMiningSetup(
                                  MiningSetup,
                                  DeviceType.NVIDIA) +
                              " -nocolor -devices ";
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.Lyra2REv3))
            {
                commandLine = " -a LYRA2V3" +
                " -P 1JqFnUR3nDFCbNUmWiQ4jX6HRugGzX55L2." + ConfigManager.GeneralConfig.WorkerName.Trim() + ":x@lyra2v3.eu.mine.zpool.ca:4550" +
                " -P " + username + ":x@" + alg + ".eu.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".hk.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".jp.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".in.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".usa.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".br.nicehash.com:" + port +
                              ExtraLaunchParametersParser.ParseForMiningSetup(
                                  MiningSetup,
                                  DeviceType.NVIDIA) +
                              " -nocolor -devices ";
            }

            commandLine += GetDevicesCommandString();

            TotalCount = 14;

            Total = 0.0d;

            return commandLine;
        }

        protected override bool BenchmarkParseLine(string outdata)
        {
            int count = 0;
            double tmp = 0;

            if (_benchmarkException)
            {
                //Helpers.ConsolePrint("BENCHMARK:", outdata);
                if ( outdata.Contains(variables.TTMiner_bench1) && outdata.Contains(variables.TTMiner_bench2)) 
                {

                    var st = outdata.IndexOf(variables.TTMiner_bench3);
                    var e = outdata.IndexOf(variables.TTMiner_bench4);
                    try
                    {
                        var parse = outdata.Substring(st + 6, e - st - 8).Trim();
                        //Helpers.ConsolePrint("BENCHMARK!:", parse);
                        tmp = Double.Parse(parse, CultureInfo.InvariantCulture);
                    } catch
                    {
                        MessageBox.Show("Unsupported miner version - " + MiningSetup.MinerPath,
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        BenchmarkSignalFinnished = true;
                        return false;
                    }
                    // save speed

                    if (outdata.ToUpper().Contains("KH/S"))
                        tmp *= 1000;
                    else if (outdata.ToUpper().Contains("MH/S"))
                        tmp *= 1000000;
                    else if (outdata.ToUpper().Contains("GH/S"))
                        tmp *= 1000000000;

                    speed = Math.Max(speed, tmp);
                    count++;
                    TotalCount--;
                }

                if (TotalCount <= 0)
                {
                    BenchmarkAlgorithm.BenchmarkSpeed = speed;
                    BenchmarkSignalFinnished = true;
                    return true;
                }

               // return false;
            }
            /*
            if (speed > 0.0d)
            {
                BenchmarkAlgorithm.BenchmarkSpeed = speed/2;
                return true;
            }
            */
            return false;
        }

               protected override void BenchmarkOutputErrorDataReceivedImpl(string outdata)
        {
            CheckOutdata(outdata);
        }
        protected override void BenchmarkThreadRoutine(object CommandLine)
        {
            BenchmarkSignalQuit = false;
            BenchmarkSignalHanged = false;
            BenchmarkSignalFinnished = false;
            BenchmarkException = null;

            Thread.Sleep(ConfigManager.GeneralConfig.MinerRestartDelayMS);

            try
            {
                Helpers.ConsolePrint("BENCHMARK", "Benchmark starts");
                BenchmarkHandle = BenchmarkStartProcess((string)CommandLine);

                BenchmarkThreadRoutineStartSettup();
                BenchmarkTimeInSeconds = 300;
                BenchmarkProcessStatus = BenchmarkProcessStatus.Running;
                var exited = BenchmarkHandle.WaitForExit((BenchmarkTimeoutInSeconds(BenchmarkTimeInSeconds) + 20) * 1000);
                if (BenchmarkSignalTimedout && !TimeoutStandard)
                {
                    throw new Exception("Benchmark timedout");
                }

                if (BenchmarkException != null)
                {
                    throw BenchmarkException;
                }

                if (BenchmarkSignalQuit)
                {
                    throw new Exception("Termined by user request");
                }

                if (BenchmarkSignalHanged || !exited)
                {
                    throw new Exception("Miner is not responding");
                }

                if (BenchmarkSignalFinnished)
                {
                    //break;
                }
            }
            catch (Exception ex)
            {
                BenchmarkThreadRoutineCatch(ex);
            }
            finally
            {
                BenchmarkThreadRoutineFinish();
            }
        }


        #endregion // Decoupled benchmarking routines
        
        public override async Task<ApiData> GetSummaryAsync()
        {
            if (!IsApiReadException) return await GetSummaryCpuCcminerAsync();
            // check if running
            if (ProcessHandle == null)
            {
                CurrentMinerReadStatus = MinerApiReadStatus.RESTART;
                Helpers.ConsolePrint(MinerTag(), ProcessTag() + " Could not read data from CryptoNight Proccess is null");
                return null;
            }
            try
            {
                Process.GetProcessById(ProcessHandle.Id);
            }
            catch (ArgumentException ex)
            {
                CurrentMinerReadStatus = MinerApiReadStatus.RESTART;
                Helpers.ConsolePrint(MinerTag(), ProcessTag() + " Could not read data from CryptoNight reason: " + ex.Message);
                return null; // will restart outside
            }
            catch (InvalidOperationException ex)
            {
                CurrentMinerReadStatus = MinerApiReadStatus.RESTART;
                Helpers.ConsolePrint(MinerTag(), ProcessTag() + " Could not read data from CryptoNight reason: " + ex.Message);
                return null; // will restart outside
            }

            var totalSpeed = MiningSetup.MiningPairs
                .Select(miningPair =>
                    miningPair.Device.GetAlgorithm(MinerBaseType.TTMiner, MiningSetup.CurrentAlgorithmType, AlgorithmType.NONE))
                .Where(algo => algo != null).Sum(algo => algo.BenchmarkSpeed);

            var ttminerData = new ApiData(MiningSetup.CurrentAlgorithmType)
            {
                Speed = totalSpeed
            };
            CurrentMinerReadStatus = MinerApiReadStatus.GOT_READ;
            // check if speed zero
            if (ttminerData.Speed == 0) CurrentMinerReadStatus = MinerApiReadStatus.READ_SPEED_ZERO;
            return ttminerData;
        }
        
        private class JsonApiResponse
        {
            public List<string> result { get; set; }
            public int id { get; set; }
            public object error { get; set; }
        }
        /*
        public override async Task<ApiData> GetSummaryAsync()
        {
            CurrentMinerReadStatus = MinerApiReadStatus.NONE;
            var ad = new ApiData(MiningSetup.CurrentAlgorithmType, MiningSetup.CurrentSecondaryAlgorithmType);
            var ApiReadMult = 0;
            JsonApiResponse resp = null;
            Helpers.ConsolePrint("try API...........", "");
            //{"id":0,"jsonrpc":"2.0","method":"miner_getstat1"}
            
            //{"id":0,"jsonrpc":"2.0","method":"miner_getstat1"}
 //{"id":0,"jsonrpc":"2.0","result":["TT-Miner/2.1.11-beta","3","4003571;13;0","1959526;2044045","0;0;0","off;off","68;52;58;39","mtp.eu.nicehash.com:3374","0;0;0;0"]}
 
            try
            {
                //var bytesToSend0 = Encoding.ASCII.GetBytes("\n");
                //var bytesToSend = Encoding.ASCII.GetBytes("\n{\"id\":0,\"jsonrpc\":\"2.0\",\"method\":\"miner_getstat1\"}\n");
                byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes("{\n\"id\":0,\n\"jsonrpc\":\"2.0\",\n\"method\":\"miner_getstat1\"\n}\n");
                Helpers.ConsolePrint("try API2:", bytesToSend.ToString());
                var client = new TcpClient("127.0.0.1", ApiPort);
                Helpers.ConsolePrint("try API...........", "3");
                NetworkStream nwStream = client.GetStream();
                Helpers.ConsolePrint("try API...........", "4");
                //Thread.Sleep(1000);
                //await nwStream.WriteAsync(bytesToSend0, 0, bytesToSend0.Length);
                //Thread.Sleep(100);
                await nwStream.WriteAsync(bytesToSend, 0, bytesToSend.Length);
                //Thread.Sleep(100);
                //await nwStream.WriteAsync(bytesToSend0, 0, bytesToSend0.Length);
                
                Helpers.ConsolePrint("try API...........", "5");
                var bytesToRead = new byte[client.ReceiveBufferSize];
                Helpers.ConsolePrint("try API...........", "6");

                var bytesRead = await nwStream.ReadAsync(bytesToRead, 0, client.ReceiveBufferSize);
                Helpers.ConsolePrint("try API...........", "7");
                var respStr = Encoding.ASCII.GetString(bytesToRead, 0, bytesRead);
                Helpers.ConsolePrint("try API...........", "8");
                resp = JsonConvert.DeserializeObject<JsonApiResponse>(respStr, Globals.JsonSettings);
                Helpers.ConsolePrint("try API...........", "9");
                client.Close();
                Helpers.ConsolePrint("ClaymoreZcashMiner API back:", respStr);
            }
            catch (Exception ex)
            {
                Helpers.ConsolePrint(MinerTag(), "GetSummary exception: " + ex.Message);
            }

            if (resp != null && resp.error == null)
            {
                //Helpers.ConsolePrint("ClaymoreZcashMiner API back:", "resp != null && resp.error == null");
                if (resp.result != null && resp.result.Count > 4)
                {
                    //Helpers.ConsolePrint("ClaymoreZcashMiner API back:", "resp.result != null && resp.result.Count > 4");
                    var speeds = resp.result[3].Split(';');
                   // var secondarySpeeds = (IsDual()) ? resp.result[5].Split(';') : new string[0];
                    ad.Speed = 0;
                    ad.SecondarySpeed = 0;
                    foreach (var speed in speeds)
                    {
                        //Helpers.ConsolePrint("ClaymoreZcashMiner API back:", "foreach (var speed in speeds) {");
                        double tmpSpeed;
                        try
                        {
                            tmpSpeed = double.Parse(speed, CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            tmpSpeed = 0;
                        }

                        ad.Speed += tmpSpeed;
                    }

                    
                    if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.NeoScrypt))
                    {
                        ApiReadMult = 1000;
                    }
                    ad.Speed *= ApiReadMult;
                    ad.SecondarySpeed *= ApiReadMult;
                    CurrentMinerReadStatus = MinerApiReadStatus.GOT_READ;
                }

                if (ad.Speed == 0)
                {
                    CurrentMinerReadStatus = MinerApiReadStatus.READ_SPEED_ZERO;
                }

                // some clayomre miners have this issue reporting negative speeds in that case restart miner
                if (ad.Speed < 0)
                {
                    Helpers.ConsolePrint(MinerTag(), "Reporting negative speeds will restart...");
                    Restart();
                }
            }

            return ad;
        }
        */
    }
}
