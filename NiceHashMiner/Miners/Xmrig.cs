﻿/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
using NiceHashMiner.Configs;
using NiceHashMiner.Miners.Parsing;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NiceHashMiner.Algorithms;
using NiceHashMinerLegacy.Common.Enums;
using System.Windows.Forms;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace NiceHashMiner.Miners
{
    public class Xmrig : Miner
    {
        private int benchmarkTimeWait = 180;
        private const string LookForStart = "speed 10s/60s/15m";
        private const string LookForEnd = "h/s max";
        private System.Diagnostics.Process CMDconfigHandle;
        private string platform = "";
        string platform_prefix = "";
        public Xmrig() : base("Xmrig")
        { }
        public override void Start(string url, string btcAdress, string worker)
        {
            LastCommandLine = GetStartCommand(url, btcAdress, worker);

            ProcessHandle = _Start();
        }
        /*
        private string GetStartCommand(string url, string btcAdress, string worker)
        {
            var extras = ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.CPU);
            return $" -o {url} -u {btcAdress}.{worker}:x --nicehash {extras} --api-port {ApiPort}";
        }
        */
        private string GetStartCommand(string url, string btcAdress, string worker)
        {
            var extras = ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.CPU);
            var algo = "cryptonightv7";
            var port = "3363";
            var variant = " --variant 1 ";
            //cn/r cryptonight/r
            string nhsuff = "";
            string username = GetUsername(btcAdress, worker);

            foreach (var pair in MiningSetup.MiningPairs)
            {
                if (pair.Device.DeviceType == DeviceType.NVIDIA)
                {
                    platform = " --no-cpu --cuda-devices=";
                }
                else if (pair.Device.DeviceType == DeviceType.AMD)
                {
                    platform = " --no-cpu --opencl-devices=";
                }
                else if (pair.Device.DeviceType == DeviceType.CPU)
                {
                    platform = "";
                }
            }

            if (Configs.ConfigManager.GeneralConfig.NewPlatform)
            {
                nhsuff = Configs.ConfigManager.GeneralConfig.StratumSuff;
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.CryptoNightV8))
            {
                algo = "cryptonightv8";
                port = "3367";
                variant = " --variant 2 ";
                return $" -o {url} {variant} -u {username}:x --nicehash {extras} --http-port {ApiPort} --donate-level=1 "
               + $" -o stratum+tcp://{algo}.{myServers[1, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x "
               + $" -o stratum+tcp://{algo}.{myServers[2, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x "
               + $" -o stratum+tcp://{algo}.{myServers[3, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x "
               + $" -o stratum+tcp://{algo}.{myServers[4, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x "
               + $" -o stratum+tcp://{algo}.{myServers[5, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x "
               + $" -o stratum+tcp://{algo}.{myServers[0, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x {platform}"
               + GetDevicesCommandString().TrimStart();
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.CryptoNightHeavy))
            {
                algo = "cryptonightheavy";
                port = "3364";
                variant = "";
                return $" --algo=cryptonight-heavy -o {url} {variant} -u {username}:x --nicehash {extras} --http-port {ApiPort} --donate-level=1 "
               + $" -o stratum+tcp://{algo}.{myServers[1, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x "
               + $" -o stratum+tcp://{algo}.{myServers[2, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x "
               + $" -o stratum+tcp://{algo}.{myServers[3, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x "
               + $" -o stratum+tcp://{algo}.{myServers[4, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x "
               + $" -o stratum+tcp://{algo}.{myServers[5, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x "
               + $" -o stratum+tcp://{algo}.{myServers[0, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x {platform}"
               + GetDevicesCommandString().TrimStart();
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.CryptoNightR))
            {
                algo = "cryptonightr";
                port = "3375";
                variant = "";
                return $" --algo=cryptonight/r -o {url} {variant} -u {username}:x --nicehash {extras} --http-port {ApiPort} --donate-level=1 "
               + $" -o stratum+tcp://{algo}.{myServers[1, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x "
               + $" -o stratum+tcp://{algo}.{myServers[2, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x "
               + $" -o stratum+tcp://{algo}.{myServers[3, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x "
               + $" -o stratum+tcp://{algo}.{myServers[4, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x "
               + $" -o stratum+tcp://{algo}.{myServers[5, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x "
               + $" -o stratum+tcp://{algo}.{myServers[0, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x {platform}"
               + GetDevicesCommandString().TrimStart();
            }

            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.RandomX))
            {
                algo = "randomxmonero";
                port = "3380";
                variant = "";
                return $" --algo=rx/0 -o stratum+tcp://{algo}.{myServers[0, 0]}{nhsuff}.nicehash.com:{port} {variant} -u {username}:x --nicehash {extras} --http-port {ApiPort} --donate-level=1 "
               + $" -o stratum+tcp://{algo}.{myServers[1, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x "
               + $" -o stratum+tcp://{algo}.{myServers[2, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x "
               + $" -o stratum+tcp://{algo}.{myServers[3, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x "
               + $" -o stratum+tcp://{algo}.{myServers[4, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x "
               + $" -o stratum+tcp://{algo}.{myServers[5, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x "
               + $" -o stratum+tcp://{algo}.{myServers[0, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x {platform}"
               + GetDevicesCommandString().TrimStart();
            }
            return "unsupported algo";
        }
        private string GetStartBenchmarkCommand(string url, string btcAdress, string worker)
        {
            string nhsuff = "";
            if (Configs.ConfigManager.GeneralConfig.NewPlatform)
            {
                nhsuff = Configs.ConfigManager.GeneralConfig.StratumSuff;
            }
            if (url.Contains("Auto"))
            {
                url = url.Replace("Auto", "eu" + nhsuff);
            }

            foreach (var pair in MiningSetup.MiningPairs)
            {
                if (pair.Device.DeviceType == DeviceType.NVIDIA)
                {
                    platform = " --no-cpu --cuda-devices=";
                    platform_prefix = "nvidia_";
                }
                else if (pair.Device.DeviceType == DeviceType.AMD)
                {
                    platform = " --no-cpu --opencl-devices=";
                    platform_prefix = "amd_"; ;
                }
                else if (pair.Device.DeviceType == DeviceType.CPU)
                {
                    platform = "";
                    platform_prefix = "cpu_";
                }
            }

            var extras = ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.CPU);
            var algo = "cryptonightv7";
            var port = "3363";
            var variant = " --variant 1 ";
            string username = GetUsername(btcAdress, worker);
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.CryptoNightV8))
            {
                algo = "cryptonightv8";
                port = "3367";
                variant = " --variant 2 ";
                //return $" -o stratum+tcp://xmr-eu.dwarfpool.com:8005 {variant} -u 42fV4v2EC4EALhKWKNCEJsErcdJygynt7RJvFZk8HSeYA9srXdJt58D9fQSwZLqGHbijCSMqSP4mU7inEEWNyer6F7PiqeX.{worker} -p x {extras} --api-port {ApiPort} --donate-level=1 "
                return $" -o stratum+tcp://{algo}.{myServers[0, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x "
                + $" -o stratum+tcp://{algo}.{myServers[1, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x {platform}"
               + GetDevicesCommandString().TrimStart();
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.CryptoNightHeavy))
            {
                algo = "cryptonightheavy";
                port = "3364";
                variant = "";
                return $" --algo=cryptonight-heavy -o stratum+tcp://{algo}.{myServers[0, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x {extras} --http-port {ApiPort} --donate-level=1 "
                + $" -o stratum+tcp://{algo}.{myServers[1, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x {platform}"
               + GetDevicesCommandString().TrimStart();
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.CryptoNightR))
            {
                algo = "cryptonightr";
                port = "3375";
                variant = "";
                return $" --algo=cryptonight/r -o stratum+tcp://xmr-eu1.nanopool.org:14444 -u 42fV4v2EC4EALhKWKNCEJsErcdJygynt7RJvFZk8HSeYA9srXdJt58D9fQSwZLqGHbijCSMqSP4mU7inEEWNyer6F7PiqeX.{worker} -p x {extras} --http-port {ApiPort} --donate-level=1 "
                + $" -o stratum+tcp://{algo}.{myServers[0, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x {platform}"
               + GetDevicesCommandString().TrimStart();
            }

            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.RandomX))
            {
                algo = "randomxmonero";
                port = "3380";
                variant = "";
                return $" --algo=rx/0 -o stratum+tcp://xmr-eu1.nanopool.org:14444 -u 42fV4v2EC4EALhKWKNCEJsErcdJygynt7RJvFZk8HSeYA9srXdJt58D9fQSwZLqGHbijCSMqSP4mU7inEEWNyer6F7PiqeX.{worker} -p x {extras} --api-port {ApiPort} --donate-level=1 "
                //return $" --algo=rx/0 {extras} --http-port {ApiPort} --donate-level=1 "
                + $" -o stratum+tcp://{algo}.{myServers[0, 0]}{nhsuff}.nicehash.com:{port} -u {username}:x {platform}"
               + GetDevicesCommandString().TrimStart();
            }
            return "unsupported algo";
        }

        protected override void _Stop(MinerStopType willswitch)
        {
            Helpers.ConsolePrint("XMRIG", "_Stop");
            Stop_cpu_ccminer_sgminer_nheqminer(willswitch);
        }

        protected override int GetMaxCooldownTimeInMilliseconds()
        {
            return 60 * 1000 * 5; // 5 min
        }

        protected async Task<ApiData> GetSummaryCpuAsyncXMRig(string method = "", bool overrideLoop = false)
        {
            var ad = new ApiData(MiningSetup.CurrentAlgorithmType);

            try
            {
                HttpWebRequest WR = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:" + ApiPort.ToString() + "/1/summary");
                WR.UserAgent = "GET / HTTP/1.1\r\n\r\n";
                WR.Timeout = 30 * 1000;
                WR.Credentials = CredentialCache.DefaultCredentials;
                WebResponse Response = WR.GetResponse();
                Stream SS = Response.GetResponseStream();
                SS.ReadTimeout = 20 * 1000;
                StreamReader Reader = new StreamReader(SS);
                var respStr = await Reader.ReadToEndAsync();

                Reader.Close();
                Response.Close();
                //Helpers.ConsolePrint(MinerTag(), respStr);

                if (string.IsNullOrEmpty(respStr))
                {
                    CurrentMinerReadStatus = MinerApiReadStatus.NETWORK_EXCEPTION;
                    throw new Exception("Response is empty!");
                }

                dynamic resp = JsonConvert.DeserializeObject(respStr);

                if (resp != null)
                {
                    JArray totals = resp.hashrate.total;
                    foreach (var total in totals)
                    {
                        if (total.Value<string>() == null) continue;
                        ad.Speed = total.Value<double>();
                        break;
                    }

                    if (ad.Speed == 0)
                    {
                        CurrentMinerReadStatus = MinerApiReadStatus.READ_SPEED_ZERO;
                    }
                    else
                    {
                        CurrentMinerReadStatus = MinerApiReadStatus.GOT_READ;
                    }
                }
                else
                {
                    throw new Exception($"Response does not contain speed data: {respStr.Trim()}");
                }
            }
            catch (Exception ex)
            {
                Helpers.ConsolePrint(MinerTag(), ex.Message);
            }

            return ad;
        }


        public override async Task<ApiData> GetSummaryAsync()
        {
            return await GetSummaryCpuAsyncXMRig();
        }

        protected override bool IsApiEof(byte third, byte second, byte last)
        {
            return third == 0x7d && second == 0xa && last == 0x7d;
        }

        #region Benchmark

        protected override string BenchmarkCreateCommandLine(Algorithm algorithm, int time)
        {
            var server = Globals.GetLocationUrl(algorithm.NiceHashID,
                Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation],
                ConectionType);
            benchmarkTimeWait = time;
            return GetStartBenchmarkCommand(server, Globals.GetBitcoinUser(), ConfigManager.GeneralConfig.WorkerName.Trim())
                + $" -l {platform_prefix}{GetLogFileName()} --print-time=2 --nicehash";
        }

        protected override void BenchmarkThreadRoutine(object commandLine)
        {
            BenchmarkThreadRoutineAlternateXmRig(commandLine, benchmarkTimeWait);
        }

        protected void BenchmarkThreadRoutineAlternateXmRig(object commandLine, int benchmarkTimeWait)
        {
            //CleanOldLogs();

            BenchmarkSignalQuit = false;
            BenchmarkSignalHanged = false;
            BenchmarkSignalFinnished = false;
            BenchmarkException = null;

            Thread.Sleep(ConfigManager.GeneralConfig.MinerRestartDelayMS);

            if (File.Exists("bin\\xmrig\\" + platform_prefix + GetLogFileName()))
                File.Delete("bin\\xmrig\\" + platform_prefix + GetLogFileName());


            try
            {
                Helpers.ConsolePrint("BENCHMARK-routineAlt", "Benchmark starts");
                Helpers.ConsolePrint(MinerTag(), "Benchmark should end in : " + benchmarkTimeWait + " seconds");
                BenchmarkHandle = BenchmarkStartProcess((string)commandLine);
                BenchmarkHandle.WaitForExit(benchmarkTimeWait + 2);
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
                    if (benchmarkTimer.Elapsed.TotalSeconds >= (benchmarkTimeWait + 2)
                        || BenchmarkSignalQuit
                        || BenchmarkSignalFinnished
                        || BenchmarkSignalHanged
                        || BenchmarkSignalTimedout
                        || BenchmarkException != null)
                    {
                        var imageName = MinerExeName.Replace(".exe", "");
                        // maybe will have to KILL process
                        KillProspectorClaymoreMinerBase(imageName);
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
                // find latest log file
                string latestLogFile = "";
                var dirInfo = new DirectoryInfo(WorkingDirectory);
                foreach (var file in dirInfo.GetFiles(platform_prefix + GetLogFileName()))
                {
                    latestLogFile = file.Name;
               //     Helpers.ConsolePrint("BENCHMARK-routineAlt", latestLogFile);
                    break;
                }

                BenchmarkHandle?.WaitForExit(10000);
                // read file log
               // Helpers.ConsolePrint("BENCHMARK-routineAlt", WorkingDirectory + latestLogFile);
                if (File.Exists(WorkingDirectory + latestLogFile))
                {
                    var lines = File.ReadAllLines(WorkingDirectory + latestLogFile);
                //    Helpers.ConsolePrint("BENCHMARK-routineAlt", lines.ToString());
                    ProcessBenchLinesAlternate(lines);
                }

                BenchmarkThreadRoutineFinish();
            }
        }


        
        protected override void ProcessBenchLinesAlternate(string[] lines)
        {
            // Xmrig reports 2.5s and 60s averages, so prefer to use 60s values for benchmark
            // but fall back on 2.5s values if 60s time isn't hit
            var twoSecTotal = 0d;
            var sixtySecTotal = 0d;
            var twoSecCount = 0;
            var sixtySecCount = 0;
            foreach (var line in lines)
            {
                BenchLines.Add(line);
                var lineLowered = line.ToLower();
                if (!lineLowered.Contains(LookForStart)) continue;
                var speeds = Regex.Match(lineLowered, $"{LookForStart} (.+?) {LookForEnd}").Groups[1].Value.Split();

                try {
                if (double.TryParse(speeds[1], out var sixtySecSpeed))
                    {
                    sixtySecTotal += sixtySecSpeed;
                    ++sixtySecCount;
                    }
                else if (double.TryParse(speeds[0], out var twoSecSpeed))
                    {
                    // Store 10s data in case 60s is never reached
                    twoSecTotal += twoSecSpeed;
                    ++twoSecCount;
                    }
                }
                catch
                {
                    MessageBox.Show("Unsupported miner version - " + MiningSetup.MinerPath,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    BenchmarkSignalFinnished = true;
                    return;
                }
            }

            if (sixtySecCount > 0 && sixtySecTotal > 0)
            {
                // Run iff 60s averages are reported
                BenchmarkAlgorithm.BenchmarkSpeed = sixtySecTotal / sixtySecCount;
            }
            else if (twoSecCount > 0)
            {
                // Run iff no 60s averages are reported but 2.5s are
                BenchmarkAlgorithm.BenchmarkSpeed = twoSecTotal / twoSecCount;
            }
        }
        
        protected override void BenchmarkOutputErrorDataReceivedImpl(string outdata)
        {
            CheckOutdata(outdata);
        }

        protected override bool BenchmarkParseLine(string outdata)
        {
            Helpers.ConsolePrint(MinerTag(), outdata);
            return false;
        }

        #endregion
    }
}
