﻿/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
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
using System.Windows.Forms;
using System.Net;
using System.Management;

namespace NiceHashMiner.Miners
{
    public class Bminer : Miner
    {
        private class Devices
        {
            public uint gpu_id { get; set; }
            public uint cuda_id { get; set; }
            public string bus_id { get; set; }
            public string name { get; set; }
            public uint speed { get; set; }
            public int accepted_shares { get; set; }
            public int rejected_shares { get; set; }
            public int temperature { get; set; }
            public int temperature_limit { get; set; }
            public uint power_usage { get; set; }
        }

        private class JsonApiResponse
        {
            public uint uptime { get; set; }
            public string server { get; set; }
            public string user { get; set; }
            public string algorithm { get; set; }
            public uint electricity { get; set; }
            public Devices[] devices { get; set; }
        }

        private int _benchmarkTimeWait = 2 * 45;
        private int _benchmarkReadCount;
        private double _benchmarkSum;
        private const string LookForStart = "total speed: ";
        private const string LookForEnd = "sol/s";
        private const double DevFee = 2.0;

        public Bminer() : base("Bminer")
        {
            ConectionType = NhmConectionType.NONE;
            IsNeverHideMiningWindow = true;
        }

        public override void Start(string url, string btcAdress, string worker)
        {
            LastCommandLine = GetStartCommand(url, btcAdress, worker);

            ProcessHandle = _Start();
        }

        protected override void _Stop(MinerStopType willswitch)
        {
            Helpers.ConsolePrint("Bminer Stop", "");
            Stop_cpu_ccminer_sgminer_nheqminer(willswitch);
            Thread.Sleep(200);
            KillBminer();
            //KillMinerBase("miner");

        }
        private string GetStartCommand(string url, string btcAddress, string worker)
        {
            var algo ="";
            var algoName = "";
            string username = GetUsername(btcAddress, worker);
            if (MiningSetup.CurrentAlgorithmType == AlgorithmType.ZHash)
                    {
                        algo = "144_5";
                        algoName = "zhash";
                    }
            if (MiningSetup.CurrentAlgorithmType == AlgorithmType.Beam)
                    {
                        algo = "150_5";
                        algoName = "beam";
                    }
            if (MiningSetup.CurrentAlgorithmType == AlgorithmType.GrinCuckaroo29)
            {
                algo = "cuckaroo29";
                algoName = "grin";
            }

            if (MiningSetup.CurrentAlgorithmType == AlgorithmType.GrinCuckatoo31)
            {
                algo = "cuckatoo31";
                algoName = "grin";
            }
            string nhsuff = "";
            if (Configs.ConfigManager.GeneralConfig.NewPlatform)
            {
                nhsuff = Configs.ConfigManager.GeneralConfig.StratumSuff;
            } 
                var ret = GetDevicesCommandString()
                      + " --pers auto --algo " + algo + " --server " + url.Split(':')[0]
                      + " --user " + username + " --pass x --port " + url.Split(':')[1]
                      + " --server " + algoName + ".hk" + nhsuff + ".nicehash.com"
                      + " --user " + username + " --pass x --port " + url.Split(':')[1]
                      + " --server " + algoName + ".in" + nhsuff + ".nicehash.com"
                      + " --user " + username + " --pass x --port " + url.Split(':')[1]
                      + " --server " + algoName + ".usa" + nhsuff + ".nicehash.com"
                      + " --user " + username + " --pass x --port " + url.Split(':')[1]
                      + " --server " + algoName + ".jp" + nhsuff + ".nicehash.com"
                      + " --user " + username + " --pass x --port " + url.Split(':')[1]
                      + " --server " + algoName + ".br" + nhsuff + ".nicehash.com"
                      + " --user " + username + " --pass x --port " + url.Split(':')[1]
                      + " --api " + ApiPort;


            return ret;
        }
        protected override string GetDevicesCommandString()
        {
            var deviceStringCommand = " --devices ";

            var ids = MiningSetup.MiningPairs.Select(mPair => mPair.Device.IDByBus.ToString()).ToList();
            deviceStringCommand += string.Join(" ", ids);
            deviceStringCommand +=
                " " + ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.NVIDIA);

            return deviceStringCommand;
        }

        /*
        protected override string GetDevicesCommandString()
        {
            var deviceStringCommand = MiningSetup.MiningPairs.Aggregate(" --devices ",
                (current, nvidiaPair) => current + (nvidiaPair.Device.IDByBus + " "));

            deviceStringCommand +=
                " " + ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.NVIDIA);

            return deviceStringCommand;
        }
        */
        // benchmark stuff
        protected void KillMinerBase(string exeName)
        {
            foreach (var process in Process.GetProcessesByName(exeName))
            {
                try { process.Kill(); }
                catch (Exception e) { Helpers.ConsolePrint(MinerDeviceName, e.ToString()); }
            }
        }
        private static void KillProcessAndChildren(int pid)
        {
            // Cannot close 'system idle process'.
            if (pid == 0)
            {
                return;
            }
            ManagementObjectSearcher searcher = new ManagementObjectSearcher
                    ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }
        public override void EndBenchmarkProcces()
        {
            if (BenchmarkProcessStatus != BenchmarkProcessStatus.Killing && BenchmarkProcessStatus != BenchmarkProcessStatus.DoneKilling)
            {
                BenchmarkProcessStatus = BenchmarkProcessStatus.Killing;
                try
                {
                    Helpers.ConsolePrint("BENCHMARK",
                        $"Trying to kill benchmark process {BenchmarkProcessPath} algorithm {BenchmarkAlgorithm.AlgorithmName}");

                    int k = ProcessTag().IndexOf("pid(");
                    int i = ProcessTag().IndexOf(")|bin");
                    var cpid = ProcessTag().Substring(k + 4, i - k - 4).Trim();

                    int pid = int.Parse(cpid, CultureInfo.InvariantCulture);

                    KillProcessAndChildren(pid);
                    BenchmarkHandle.Kill();
                    BenchmarkHandle.Close();
                }
                catch { }
                finally
                {
                    BenchmarkProcessStatus = BenchmarkProcessStatus.DoneKilling;
                    Helpers.ConsolePrint("BENCHMARK",
                        $"Benchmark process {BenchmarkProcessPath} algorithm {BenchmarkAlgorithm.AlgorithmName} KILLED");
                    //BenchmarkHandle = null;
                }
            }
        }
        public void KillBminer()
        {
            if (ProcessHandle != null)
            {
                try { ProcessHandle.Kill(); }
                catch { }

                try { ProcessHandle.SendCtrlC((uint)Process.GetCurrentProcess().Id); } catch { }
                ProcessHandle.Close();
                ProcessHandle = null;

                if (IsKillAllUsedMinerProcs) KillAllUsedMinerProcesses();
            }
            KillMinerBase("miner");
            //foreach (Process process in Process.GetProcessesByName("miner")) {
            //     try { process.Kill(); } catch (Exception e) { Helpers.ConsolePrint(MinerDeviceName, e.ToString()); }
            //}
        }
        protected override string BenchmarkCreateCommandLine(Algorithm algorithm, int time)
        {
            CleanOldLogs();
            var ret = "";
            var server = Globals.GetLocationUrl(algorithm.NiceHashID,
               Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], ConectionType);
            var btcAddress = Globals.GetBitcoinUser();
            var worker = ConfigManager.GeneralConfig.WorkerName.Trim();
            string nhsuff = "";
            string username = GetUsername(btcAddress, worker);
            if (Configs.ConfigManager.GeneralConfig.NewPlatform)
            {
                nhsuff = nhsuff = Configs.ConfigManager.GeneralConfig.StratumSuff;
            }
            if (MiningSetup.CurrentAlgorithmType == AlgorithmType.ZHash)
            {
                ret = " -logfile " + GetLogFileName() + " --color 0 --pec --pers BgoldPoW --algo 144_5" +
                " --server europe.equihash-hub.miningpoolhub.com --user angelbbs.FBench11 --pass x --port 20595 " +
                " --server zhash.eu" + nhsuff + ".nicehash.com --user " + username + " --pass x --port 3369" +
                " --server zhash.hk" + nhsuff + ".nicehash.com --user " + username + " --pass x --port 3369" +
                GetDevicesCommandString();
            }
            if (MiningSetup.CurrentAlgorithmType == AlgorithmType.Beam)
            {
                //START bminer.exe -uri cuckaroo29://angelbbs@mail.ru:Farm1@eu-west-stratum.grinmint.com:3416
                ret = " -logfile " + GetLogFileName() +
                " -uri beam://2c20485d95e81037ec2d0312b000b922f444c650496d600d64b256bdafa362bafc9:bench_b@beam-eu.sparkpool.com:2222 " +
                " -uri beam://2c20485d95e81037ec2d0312b000b922f444c650496d600d64b256bdafa362bafc9:bench_b@beam-asia.sparkpool.com:2222 " +
                " -uri beam://" + username + "@beam.eu" + nhsuff + ".nicehash.com:3370 " +
                " -uri beam://" + username + "@beam.hk" + nhsuff + ".nicehash.com:3370 " +
                GetDevicesCommandString();
            }
            if (MiningSetup.CurrentAlgorithmType == AlgorithmType.GrinCuckaroo29)
            {
                //START bminer.exe - uri cuckaroo29://angelbbs@mail.ru:Farm1@eu-west-stratum.grinmint.com:3416
                ret = " -logfile " + GetLogFileName() + " --color 0 --pec --algo cuckaroo29" +
                " --server grin.sparkpool.com --user angelbbs@mail.ru/bench_g --pass x --port 6666 --ssl 0" +
                " --server grincuckaroo29.eu" + nhsuff + ".nicehash.com --user " + username + " --pass x --port 3369 --ssl 0" +
                " --server grincuckaroo29.hk" + nhsuff + ".nicehash.com --user " + username + " --pass x --port 3369 --ssl 0" +
                GetDevicesCommandString();
            }
            if (MiningSetup.CurrentAlgorithmType == AlgorithmType.GrinCuckatoo31)
            {
                //START bminer.exe - uri cuckaroo29://angelbbs@mail.ru:Farm1@eu-west-stratum.grinmint.com:3416
                ret = " -logfile " + GetLogFileName() + " --color 0 --pec --algo cuckatoo31" +
                " --server grin.sparkpool.com --user angelbbs@mail.ru/bench_g --pass x --port 6666 --ssl 0" +
                " --server grincuckatoo31.eu" + nhsuff + ".nicehash.com --user " + username + " --pass x --port 3369 --ssl 0" +
                " --server grincuckatoo31.hk" + nhsuff + ".nicehash.com --user " + username + " --pass x --port 3369 --ssl 0" +
                GetDevicesCommandString();
            }

            _benchmarkTimeWait = Math.Max(time * 3, 90); //
            return ret;
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
                        EndBenchmarkProcces();
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
                // find latest log file
                var latestLogFile = "";
                var dirInfo = new DirectoryInfo(WorkingDirectory);
                foreach (var file in dirInfo.GetFiles(GetLogFileName()))
                {
                    latestLogFile = file.Name;
                    break;
                }

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
                            BenchLines.Add(line);
                            var lineLowered = line.ToLower();
                            if (lineLowered.Contains(LookForStart))
                            {
                                _benchmarkSum += GetNumber(lineLowered);
                                ++_benchmarkReadCount;
                            }
                        }
                    }

                    if (_benchmarkReadCount > 0)
                    {
                        BenchmarkAlgorithm.BenchmarkSpeed = _benchmarkSum / _benchmarkReadCount;
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

        protected double GetNumber(string outdata)
        {
            if (MiningSetup.CurrentAlgorithmType == AlgorithmType.GrinCuckaroo29 || MiningSetup.CurrentAlgorithmType == AlgorithmType.GrinCuckatoo31)
            {
                return GetNumber(outdata, LookForStart, "g/s");
            } else
            {
                return GetNumber(outdata, LookForStart, LookForEnd);
            }
        }

        protected double GetNumber(string outdata, string lookForStart, string lookForEnd)
        {
            try
            {
                double mult = 1;
                var speedStart = outdata.IndexOf(lookForStart);
                var speed = outdata.Substring(speedStart, outdata.Length - speedStart);
                speed = speed.Replace(lookForStart, "");
                speed = speed.Substring(0, speed.IndexOf(lookForEnd));

                if (speed.Contains("k"))
                {
                    mult = 1000;
                    speed = speed.Replace("k", "");
                }
                else if (speed.Contains("m"))
                {
                    mult = 1000000;
                    speed = speed.Replace("m", "");
                }

                //Helpers.ConsolePrint("speed", speed);
                speed = speed.Trim();
                try
                {
                    return double.Parse(speed, CultureInfo.InvariantCulture) * mult;
                }
                catch
                {
                    MessageBox.Show("Unsupported miner version - " + MiningSetup.MinerPath,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    BenchmarkSignalFinnished = true;
                }
            }
            catch (Exception ex)
            {
                Helpers.ConsolePrint("GetNumber",
                    ex.Message + " | args => " + outdata + " | " + lookForEnd + " | " + lookForStart);
                MessageBox.Show("Unsupported miner version - " + MiningSetup.MinerPath,
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return 0;
        }
        protected override int GetMaxCooldownTimeInMilliseconds()
        {
            return 60 * 1000 * 5;  // 5 min
        }
        protected override bool IsApiEof(byte third, byte second, byte last)
        {
            return third == 0x7d && second == 0xa && last == 0x7d;
        }

        public class GMinerResponse
        {
            public List<GMinerGpuResult> devices { get; set; }
        }

        public class GMinerGpuResult
        {
            public double speed { get; set; } = 0;
        }

        public class DstmResponse
        {
            public List<DstmGpuResult> devices { get; set; }
        }

        public class DstmGpuResult
        {
            public double speed { get; set; } = 0;
        }

        public override async Task<ApiData> GetSummaryAsync()
        {
            //Helpers.ConsolePrint("try API...........", "");
            var ad = new ApiData(MiningSetup.CurrentAlgorithmType);
            string ResponseFromGMiner;
            double total = 0;
            try
            {
                HttpWebRequest WR = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:" + ApiPort.ToString()+"/stat");
                WR.UserAgent = "GET / HTTP/1.1\r\n\r\n";
                WR.Timeout = 30 * 1000;
                WR.Credentials = CredentialCache.DefaultCredentials;
                WebResponse Response = WR.GetResponse();
                Stream SS = Response.GetResponseStream();
                SS.ReadTimeout = 20 * 1000;
                StreamReader Reader = new StreamReader(SS);
                ResponseFromGMiner = await Reader.ReadToEndAsync();
                //Helpers.ConsolePrint("API...........", ResponseFromGMiner);
                if (ResponseFromGMiner.Length == 0 || (ResponseFromGMiner[0] != '{' && ResponseFromGMiner[0] != '['))
                    throw new Exception("Not JSON!");
                Reader.Close();
                Response.Close();
            }
            catch (Exception ex)
            {
                return null;
            }

            dynamic resp = JsonConvert.DeserializeObject<JsonApiResponse>(ResponseFromGMiner);

            //Helpers.ConsolePrint("API resp...........", resp);
            if (resp != null)
            {
                for (var i = 0; i < resp.devices.Length; i++)
                {
                    total = total + resp.devices[i].speed;
                }

                ad.Speed = total;
                    if (ad.Speed == 0)
                {
                    CurrentMinerReadStatus = MinerApiReadStatus.READ_SPEED_ZERO;
                }
                else
                {
                    CurrentMinerReadStatus = MinerApiReadStatus.GOT_READ;
                }
            } else
            {
                Helpers.ConsolePrint("GMiner:", "resp - null");
            }

            Thread.Sleep(1000);
            return ad;
        }
    }
}
