using Newtonsoft.Json;
using NiceHashMiner.Configs;
using NiceHashMiner.Miners.Grouping;
using NiceHashMiner.Miners.Parsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NiceHashMiner.Algorithms;
using NiceHashMinerLegacy.Common.Enums;

namespace NiceHashMiner.Miners
{
    public class trex : Miner
    {
        private int _benchmarkTimeWait = 180;

        private const int TotalDelim = 2;
        public trex() : base("trex_NVIDIA")
        {
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


       

        // benchmark stuff
        protected void KillMinerBase(string exeName)
        {
            foreach (var process in Process.GetProcessesByName(exeName))
            {
                try { process.Kill(); }
                catch (Exception e) { Helpers.ConsolePrint(MinerDeviceName, e.ToString()); }
            }
        }

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

            return commandLine;
        }

        protected override void BenchmarkThreadRoutine(object commandLine)
        {
            BenchmarkSignalQuit = false;
            BenchmarkSignalHanged = false;
            BenchmarkSignalFinnished = false;
            BenchmarkException = null;
            
            Thread.Sleep(ConfigManager.GeneralConfig.MinerRestartDelayMS);

            try
            {
                try
                {
                    var latestLogFile = "t-rex.log";
                    var dirInfo = new DirectoryInfo(WorkingDirectory);
                    if (File.Exists(WorkingDirectory + latestLogFile))
                    {
                        var loglines = File.ReadAllLines(WorkingDirectory + latestLogFile);
                    }
                }
                catch
                {
                    Helpers.ConsolePrint("BENCHMARK", "Benchmark skip");
                    return;
                }
                Helpers.ConsolePrint("BENCHMARK", "Benchmark starts");
                Helpers.ConsolePrint(MinerTag(), "Benchmark should end in : " + _benchmarkTimeWait + " seconds");
                BenchmarkHandle = BenchmarkStartProcess((string) commandLine);
                BenchmarkHandle.WaitForExit(_benchmarkTimeWait + 2);
                var benchmarkTimer = new Stopwatch();
                benchmarkTimer.Reset();
                benchmarkTimer.Start();
                //BenchmarkThreadRoutineStartSettup();
                // wait a little longer then the benchmark routine if exit false throw
                //var timeoutTime = BenchmarkTimeoutInSeconds(BenchmarkTimeInSeconds);
                //var exitSucces = BenchmarkHandle.WaitForExit(timeoutTime * 1000);
                // don't use wait for it breaks everything
                BenchmarkProcessStatus = BenchmarkProcessStatus.Running;
                var keepRunning = true;
                while (keepRunning && IsActiveProcess(BenchmarkHandle.Id))
                {
                    //string outdata = BenchmarkHandle.StandardOutput.ReadLine();
                    //BenchmarkOutputErrorDataReceivedImpl(outdata);
                    // terminate process situations
                    if (benchmarkTimer.Elapsed.TotalSeconds >= (_benchmarkTimeWait + 2)
                        || BenchmarkSignalQuit
                        || BenchmarkSignalFinnished
                        || BenchmarkSignalHanged
                        || BenchmarkSignalTimedout
                        || BenchmarkException != null)
                    {
                        var imageName = MinerExeName.Replace(".exe", "");
                        // maybe will have to KILL process
                        KillMinerBase(imageName);
                        if (BenchmarkSignalTimedout)
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

                        if (BenchmarkSignalFinnished)
                        {
                            break;
                        }

                        keepRunning = false;
                        break;
                    }

                    // wait a second reduce CPU load
                    Thread.Sleep(1000);
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
                            if (line.Contains("3/3") || line.Contains("2/3"))
                            {

                                var st = line.IndexOf("- ");
                                var e = line.IndexOf("H/s");
                                var parse = line.Substring(st+2, e - st - 4).Trim();
                                double tmp = Double.Parse(parse, CultureInfo.InvariantCulture);
                                // save speed

                                if (line.Contains("kH/s"))
                                    tmp *= 1000;
                                else if (line.Contains("Mh/s"))
                                    tmp *= 1000000;

                                BenchmarkAlgorithm.BenchmarkSpeed = tmp;
                                BenchmarkSignalFinnished = true;

                            }


                        }
                    }
                }

                BenchmarkThreadRoutineFinish();
            }
        }

        // stub benchmarks read from file
        protected override void BenchmarkOutputErrorDataReceivedImpl(string outdata)
        {
            CheckOutdata(outdata);
        }

        protected override bool BenchmarkParseLine(string outdata)
        {
            Helpers.ConsolePrint("BENCHMARK", outdata);
            return false;
        }


     
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


        protected override void _Stop(MinerStopType willswitch)
        {
            Stop_cpu_ccminer_sgminer_nheqminer(willswitch);
        }

        protected override int GetMaxCooldownTimeInMilliseconds()
        {
            return 60 * 1000 * 5; // 5 minute max, whole waiting time 75seconds
        }
    }
}
