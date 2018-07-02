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
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace NiceHashMiner.Miners
{
    public class CryptoDredge : Miner
    {
        public CryptoDredge() : base("CryptoDredge_NVIDIA")
        { }

        private int TotalCount = 0;

        private double Total = 0;
        private const int TotalDelim = 2;
        double speed = 0;
        int count = 0;

        private bool _benchmarkException => MiningSetup.MinerPath == MinerPaths.Data.CryptoDredge;

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

             IsApiReadException = MiningSetup.MinerPath == MinerPaths.Data.CryptoDredge;

            var algo = "";
            var apiBind = "";
            string alg = url.Substring(url.IndexOf("://") + 3, url.IndexOf(".") - url.IndexOf("://") - 3);
            string port = url.Substring(url.IndexOf(".com:") + 5, url.Length - url.IndexOf(".com:") - 5);
            algo = "--algo " + MiningSetup.MinerName;
            apiBind = " --api-bind 127.0.0.1:" + ApiPort;

            IsApiReadException = false;
            LastCommandLine = algo +
                " -o " + url + " -u " + username + " -p x " +
                " --url=stratum+tcp://" + alg + ".hk.nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o " + alg + ".jp.nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o " + alg + ".in.nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o " + alg + ".br.nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o " + alg + ".usa.nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o " + alg + ".eu.nicehash.com:" + port + " -u " + username + " -p x " +
                apiBind + 
                " -d " + GetDevicesCommandString() + " " +
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
            var username = GetUsername(Globals.DemoUser, ConfigManager.GeneralConfig.WorkerName.Trim());

            var commandLine = " --algo " + algorithm.MinerName +
                             " -o" + url + " -u " + username + " -p x " +
                " -o stratum+tcp://" + alg + ".hk.nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o stratum+tcp://" + alg + ".jp.nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o stratum+tcp://" + alg + ".in.nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o stratum+tcp://" + alg + ".br.nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o stratum+tcp://" + alg + ".usa.nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o stratum+tcp://" + alg + ".eu.nicehash.com:" + port + " -u " + username + " -p x " +
                              ExtraLaunchParametersParser.ParseForMiningSetup(
                                  MiningSetup,
                                  DeviceType.NVIDIA) +
                              " -d ";
            commandLine += GetDevicesCommandString();

            TotalCount = 2;

            Total = 0.0d;

            return commandLine;
        }

        protected override bool BenchmarkParseLine(string outdata)
        {


            if (_benchmarkException)
            {
                if (outdata.Contains("GPU") && outdata.Contains("/s"))
                {

                    var st = outdata.IndexOf("Avr ");
                    var e = outdata.IndexOf("/s)");

                    var parse = outdata.Substring(st + 4, e - st - 6).Trim();
                    double tmp = Double.Parse(parse, CultureInfo.InvariantCulture);
                    // save speed

                    if (outdata.Contains("kH/s"))
                        tmp *= 10;
                    else if (outdata.Contains("Mh/s"))
                        tmp *= 10000;
                    else if (outdata.Contains("GH/s"))
                        tmp *= 10000000;


                    speed += tmp;
                    count++;
                    TotalCount--;
                }
                if (TotalCount <= 0)
                {
                    BenchmarkAlgorithm.BenchmarkSpeed = speed / count;
                    BenchmarkSignalFinnished = true;
                    return true;
                }

                return false;
            }

            if (speed > 0.0d)
            {
                BenchmarkAlgorithm.BenchmarkSpeed = speed / count;

                return true;
            }

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
            CurrentMinerReadStatus = MinerApiReadStatus.NONE;
            var ad = new ApiData(MiningSetup.CurrentAlgorithmType, MiningSetup.CurrentSecondaryAlgorithmType);

            string resp = null;
            try
            {
                var bytesToSend = Encoding.ASCII.GetBytes("summary");
                var client = new TcpClient("127.0.0.1", ApiPort);
                var nwStream = client.GetStream();
                await nwStream.WriteAsync(bytesToSend, 0, bytesToSend.Length);
                var bytesToRead = new byte[client.ReceiveBufferSize];
                var bytesRead = await nwStream.ReadAsync(bytesToRead, 0, client.ReceiveBufferSize);
                var respStr = Encoding.ASCII.GetString(bytesToRead, 0, bytesRead);

                client.Close();
                resp = respStr;
            }
            catch (Exception ex)
            {
                Helpers.ConsolePrint(MinerTag(), "GetSummary exception: " + ex.Message);
            }

            if (resp != null )
            {
                    var st = resp.IndexOf(";KHS=");
                    var e = resp.IndexOf(";SOLV=");
                    var parse = resp.Substring(st + 5, e - st - 5).Trim();
                    double tmp = Double.Parse(parse, CultureInfo.InvariantCulture);
                ad.Speed = tmp*1000;
                  
                    
                

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


    }

}
