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
    public class trex : Miner
    {
        public trex() : base("trex_NVIDIA")
        { }

        private int TotalCount = 0;

        private double Total = 0;
        private const int TotalDelim = 2;
        double speed = 0;
        int count = 0;

       // private bool _benchmarkException => MiningSetup.MinerPath == MinerPaths.Data.trex;

        bool benchmarkException
        {
            get
            {
                return MiningSetup.MinerPath == MinerPaths.Data.trex;
            }
        }

        protected override int GetMaxCooldownTimeInMilliseconds()
        {
            return 60 * 1000 * 4;
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
            algo = "-a " + MiningSetup.MinerName.ToLower();
            //apiBind = " --api-bind 127.0.0.1:" + ApiPort;

            IsApiReadException = true; //no api
                                       /*
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
                                           */
            LastCommandLine = algo +
            " -o " + url + " -u " + username + " -p x " +
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

            var commandLine = " -a " + algorithm.MinerName.ToLower() +
                             " -o " + url + " -u " + username + " -p x " +
                              /*
                 " -o stratum+tcp://" + alg + ".hk.nicehash.com:" + port + " " + " -u " + username + " -p x " +
                 " -o stratum+tcp://" + alg + ".jp.nicehash.com:" + port + " " + " -u " + username + " -p x " +
                 " -o stratum+tcp://" + alg + ".in.nicehash.com:" + port + " " + " -u " + username + " -p x " +
                 " -o stratum+tcp://" + alg + ".br.nicehash.com:" + port + " " + " -u " + username + " -p x " +
                 " -o stratum+tcp://" + alg + ".usa.nicehash.com:" + port + " " + " -u " + username + " -p x " +
                 " -o stratum+tcp://" + alg + ".eu.nicehash.com:" + port + " -u " + username + " -p x " +
                 */
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
            Helpers.ConsolePrint(MinerTag(), outdata);
            /*
            [ OK ] 1/1 - 1430.84 kH/s
            [ OK ] 2/2 - 1431.48 kH/s
            [ OK ] 3/3 - 1430.49 kH/s
            [ OK ] 4/4 - 1431.24 kH/s
            */
            if (benchmarkException)
            {
                Helpers.ConsolePrint(MinerTag(), outdata);
                if (outdata.Contains("5/5") || outdata.Contains("4/5"))
                {

                    var st = outdata.IndexOf("- ");
                    var e = outdata.IndexOf("/s)");

                    var parse = outdata.Substring(st, e - st - 5).Trim();
                    double tmp = Double.Parse(parse, CultureInfo.InvariantCulture);
                    // save speed

                    if (outdata.Contains("kH/s"))
                        tmp *= 1000;
                    else if (outdata.Contains("Mh/s"))
                        tmp *= 1000000;

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
            Helpers.ConsolePrint("TREX:", outdata);
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
                while (true)
                {
                    string outdata = BenchmarkHandle.StandardOutput.ReadLine();
                    BenchmarkOutputErrorDataReceivedImpl(outdata);

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
            }
            catch (Exception ex)
            {
                BenchmarkThreadRoutineCatch(ex);
            }
            finally
            {
                BenchmarkAlgorithm.BenchmarkSpeed = 0;

                var latestLogFile = "t-rex.log";
                var dirInfo = new DirectoryInfo(WorkingDirectory);

                // read file log
                if (File.Exists(WorkingDirectory + latestLogFile))
                {
                    BenchmarkThreadRoutineFinish();
                    var lines = new string[0];
                    var read = false;
                    var iteration = 0;
                    while (!read)
                    {
                        if (iteration < 10)
                        {
                            try
                            {
                                lines = File.ReadAllLines(WorkingDirectory + latestLogFile);
                                read = true;
                                Helpers.ConsolePrint(MinerTag(),
                                    "Successfully read log after " + iteration + " iterations");
                            }
                            catch (Exception ex)
                            {
                                Helpers.ConsolePrint(MinerTag(), ex.Message);
                                Thread.Sleep(1000);
                            }

                            iteration++;
                        }
                        else
                        {
                            read = true; // Give up after 10s
                            Helpers.ConsolePrint(MinerTag(), "Gave up on iteration " + iteration);
                        }
                    }

                    var addBenchLines = BenchLines.Count == 0;
                    foreach (var line in lines)
                    {
                        if (line != null)
                        {
                            Helpers.ConsolePrint("TREX!:", line);
                            if (line.Contains("5/5") || line.Contains("4/5"))
                            {

                                var st = line.IndexOf("- ");
                                var e = line.IndexOf("/s)");

                                var parse = line.Substring(st, e - st - 5).Trim();
                                double tmp = Double.Parse(parse, CultureInfo.InvariantCulture);
                                // save speed

                                if (line.Contains("kH/s"))
                                    tmp *= 1000;
                                else if (line.Contains("Mh/s"))
                                    tmp *= 1000000;

                                speed += tmp;
                                count++;
                                TotalCount--;
                            }

                        }
                    }

                    if (TotalCount <= 0)
                    {
                        BenchmarkAlgorithm.BenchmarkSpeed = speed / count;
                        BenchmarkSignalFinnished = true;
                        // return;
                    }
                }

                BenchmarkThreadRoutineFinish();


            }
        }


        #endregion // Decoupled benchmarking routines

        public override async Task<ApiData> GetSummaryAsync()
        {
            CurrentMinerReadStatus = MinerApiReadStatus.NONE;
            var totalSpeed = 0.0d;
            foreach (var miningPair in MiningSetup.MiningPairs)
            {
                var algo = miningPair.Device.GetAlgorithm(MinerBaseType.trex, AlgorithmType.Lyra2z, AlgorithmType.NONE);
                if (algo != null)
                {
                    totalSpeed += algo.BenchmarkSpeed;
                }
            }

            var trexData = new ApiData(MiningSetup.CurrentAlgorithmType)
            {
                Speed = totalSpeed
            };
            CurrentMinerReadStatus = MinerApiReadStatus.GOT_READ;
            // check if speed zero
            if (trexData.Speed == 0) CurrentMinerReadStatus = MinerApiReadStatus.READ_SPEED_ZERO;
            return trexData;


        }
    }
}
