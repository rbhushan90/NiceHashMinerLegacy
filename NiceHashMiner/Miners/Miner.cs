﻿/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NiceHashMiner.Configs;
using NiceHashMiner.Interfaces;
using NiceHashMiner.Miners;
using NiceHashMiner.Miners.Grouping;
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
using System.Timers;
using System.Windows.Forms;
using NiceHashMiner.Algorithms;
using NiceHashMinerLegacy.Common.Enums;
using Timer = System.Timers.Timer;
using System.Net.NetworkInformation;
using System.Management;
using NiceHashMiner.Stats;

namespace NiceHashMiner
{
    public class ApiData
    {
        public AlgorithmType AlgorithmID;
        public AlgorithmType SecondaryAlgorithmID;
        public string AlgorithmName;
        public double Speed;
        public double SecondarySpeed;
        public double PowerUsage;

        public ApiData(AlgorithmType algorithmID, AlgorithmType secondaryAlgorithmID = AlgorithmType.NONE)
        {
            AlgorithmID = algorithmID;
            SecondaryAlgorithmID = secondaryAlgorithmID;
            AlgorithmName = AlgorithmNiceHashNames.GetName(Helpers.DualAlgoFromAlgos(algorithmID, secondaryAlgorithmID));
            Speed = 0.0;
            SecondarySpeed = 0.0;
            PowerUsage = 0.0;
        }
    }

    //
    public class MinerPidData
    {
        public string MinerBinPath;
        public int Pid = -1;
    }

    public abstract class Miner
    {
        // MinerIDCount used to identify miners creation
        protected static long MinerIDCount { get; private set; }

        public NhmConectionType ConectionType { get; protected set; }

        // used to identify miner instance
        protected readonly long MinerID;

        private string _minerTag;
        public string MinerDeviceName { get; set; }

        protected int ApiPort { get; private set; }

        // if miner has no API bind port for reading curentlly only CryptoNight on ccminer
        public bool IsApiReadException { get; protected set; }

        public bool IsNeverHideMiningWindow { get; protected set; }

        // mining algorithm stuff
        protected bool IsInit { get; private set; }

        public MiningSetup MiningSetup { get; protected set; }

        // sgminer/zcash claymore workaround
        protected bool IsKillAllUsedMinerProcs { get; set; }


        public  bool IsRunning { get; protected set; }
        public static bool IsRunningNew { get; protected set; }
        protected string Path { get; private set; }

        protected string LastCommandLine { get; set; }

        // TODO check this
        protected double PreviousTotalMH;

        // the defaults will be
        protected string WorkingDirectory { get; private set; }

        protected string MinerExeName { get; private set; }
        protected NiceHashProcess ProcessHandle;
        private MinerPidData _currentPidData;
        private readonly List<MinerPidData> _allPidData = new List<MinerPidData>();

        // Benchmark stuff
        public bool BenchmarkSignalQuit;

        public bool BenchmarkSignalHanged;
        private Stopwatch _benchmarkTimeOutStopWatch;
        public bool BenchmarkSignalTimedout;
        protected bool BenchmarkSignalFinnished;
        protected IBenchmarkComunicator BenchmarkComunicator;
        protected bool OnBenchmarkCompleteCalled;
        protected Algorithm BenchmarkAlgorithm { get; set; }
        public BenchmarkProcessStatus BenchmarkProcessStatus { get; protected set; }
        protected string BenchmarkProcessPath { get; set; }
        protected Process BenchmarkHandle { get; set; }
        protected Exception BenchmarkException;
        protected int BenchmarkTimeInSeconds;

        private string _benchmarkLogPath = "";
        protected List<string> BenchLines;

        protected bool TimeoutStandard;


        // TODO maybe set for individual miner cooldown/retries logic variables
        // this replaces MinerAPIGraceSeconds(AMD)
        private const int MinCooldownTimeInMilliseconds = 30 * 1000; // 30 seconds for gminer
        //private const int _MIN_CooldownTimeInMilliseconds = 1000; // TESTING

        //private const int _MAX_CooldownTimeInMilliseconds = 60 * 1000; // 1 minute max, whole waiting time 75seconds
        public int _maxCooldownTimeInMilliseconds; // = GetMaxCooldownTimeInMilliseconds();

        protected abstract int GetMaxCooldownTimeInMilliseconds();
        public static Timer _cooldownCheckTimer;
        protected MinerApiReadStatus CurrentMinerReadStatus { get; set; }
        private int _currentCooldownTimeInSeconds = MinCooldownTimeInMilliseconds;
        private int _currentCooldownTimeInSecondsLeft = MinCooldownTimeInMilliseconds;
        private const int IsCooldownCheckTimerAliveCap = 15;
        private bool _needsRestart;

        private bool _isEnded;

        public bool IsUpdatingApi = false;

        protected const string HttpHeaderDelimiter = "\r\n\r\n";

        protected bool IsMultiType;
        public static string BenchmarkStringAdd = "";
        public static string InBenchmark = "";
        /*
        string BeforeOrAfterMiningString = "@echo off\r\n" +
    "\r\n" +
    "rem ****************************************************************************************\r\n" +
    "rem * Set RUN on TRUE to enable this command scrypt\r\n" +
    "rem * Установите параметр RUN в TRUE, что бы включить выполнение этого коммандного файла\r\n" +
    "rem ****************************************************************************************\r\n" +
    "SET RUN=FALSE\r\n" +
    "\r\n" +
    "rem ****************************************************************************************\r\n" +
    "rem * Установите параметр NOVISIBLE в TRUE, что бы скрыть окно выполнения этого коммандного файла\r\n" +
    "rem * Set NOVISIBLE on TRUE to hide command window\r\n" +
    "rem ****************************************************************************************\r\n" +
    "SET NOVISIBLE=FALSE\r\n" +
    "\r\n" +
    "echo Current algo: %3\r\n" +
    "rem ****************************************************************************************\r\n" +
    "rem * Все команды в разделах приведены только для примера\r\n" +
    "rem * All commands in sections are given for example only\r\n" +
    "rem * Добавьте свои команды установки профилей разгона и т.п. в необходимые секции\r\n" +
    "rem * Add your own commands to install overclocking profiles, etc. in necessary sections\r\n" +
    "rem ****************************************************************************************\r\n" +
    "cd..\r\n" +
    "cd..\r\n" +
    "if \"%1\" EQU \"AMD\" goto amd\r\n" +
    "if \"%1\" EQU \"NVIDIA\" goto nvidia\r\n" +
    "if \"%1\" EQU \"CPU\" goto end\r\n" +
    "goto end\r\n" +
    "\r\n" +

    ":nvidia\r\n" +
    "if \"%2\" EQU \"DUAL\" goto nvidiadual\r\n" +
    "rem ****************************************************************************************\r\n" +
    "rem * MSI Afterburner section for NVIDIA\r\n" +
    "rem * MSI Afterburner раздел, для исполнения команд для NVIDIA\r\n" +
    "rem ****************************************************************************************\r\n" +
    "echo NVIDIA\r\n" +
    "::start \"start \"\" \"C:\\Program Files(x86)\\MSI Afterburner\\MSIAfterburner.exe\" -Profile2\r\n" +
    "\r\n" +
    "rem NVIDIA Inspector section\r\n" +
    "::utils\\nvidiaInspector.exe -setBaseClockOffset:0,0,100 -setMemoryClockOffset:0,0,500 -setPowerTarget:0,95 -setTempTarget:0,0,75 -setFanSpeed:0,-1\r\n" +
    "::utils\\nvidiaInspector.exe -setBaseClockOffset:1,0,100 -setMemoryClockOffset:1,0,500 -setPowerTarget:1,95 -setTempTarget:1,0,75 -setFanSpeed:1,-1\r\n" +
    "::utils\\nvidiaInspector.exe -setBaseClockOffset:2,0,100 -setMemoryClockOffset:2,0,500 -setPowerTarget:2,95 -setTempTarget:2,0,75 -setFanSpeed:2,-1\r\n" +
    "::utils\\nvidiaInspector.exe -setBaseClockOffset:3,0,100 -setMemoryClockOffset:3,0,500 -setPowerTarget:3,95 -setTempTarget:3,0,75 -setFanSpeed:3,-1\r\n" +
    "::utils\\nvidiaInspector.exe -setBaseClockOffset:4,0,100 -setMemoryClockOffset:4,0,500 -setPowerTarget:4,95 -setTempTarget:4,0,75 -setFanSpeed:4,-1\r\n" +
    "rem ****************************************************************************************\r\n" +
    "rem * End of MSI Afterburner section for NVIDIA\r\n" +
    "rem * Конец MSI Afterburner раздела, для исполнения команд для NVIDIA\r\n" +
    "rem ****************************************************************************************\r\n" +
    "goto end\r\n" +
    "\r\n" +

    ":nvidiadual\r\n" +
    "rem ****************************************************************************************\r\n" +
    "rem MSI Afterburner section for NVIDIA (claymore dual mode)\r\n" +
    "rem * MSI Afterburner раздел, для исполнения команд для NVIDIA (claymore dual mode)\r\n" +
    "rem ****************************************************************************************\r\n" +
    "echo NVIDIA DUAL\r\n" +
    "::start \"start \"\" \"C:\\Program Files(x86)\\MSI Afterburner\\MSIAfterburner.exe\" -Profile2\r\n" +
    "\r\n" +
    "rem NVIDIA Inspector section\r\n" +
    "::utils\\nvidiaInspector.exe -setBaseClockOffset:0,0,100 -setMemoryClockOffset:0,0,500 -setPowerTarget:0,95 -setTempTarget:0,0,75 -setFanSpeed:0,-1\r\n" +
    "::utils\\nvidiaInspector.exe -setBaseClockOffset:1,0,100 -setMemoryClockOffset:1,0,500 -setPowerTarget:1,95 -setTempTarget:1,0,75 -setFanSpeed:1,-1\r\n" +
    "::utils\\nvidiaInspector.exe -setBaseClockOffset:2,0,100 -setMemoryClockOffset:2,0,500 -setPowerTarget:2,95 -setTempTarget:2,0,75 -setFanSpeed:2,-1\r\n" +
    "::utils\\nvidiaInspector.exe -setBaseClockOffset:3,0,100 -setMemoryClockOffset:3,0,500 -setPowerTarget:3,95 -setTempTarget:3,0,75 -setFanSpeed:3,-1\r\n" +
    "::utils\\nvidiaInspector.exe -setBaseClockOffset:4,0,100 -setMemoryClockOffset:4,0,500 -setPowerTarget:4,95 -setTempTarget:4,0,75 -setFanSpeed:4,-1\r\n" +
    "rem ****************************************************************************************\r\n" +
    "rem End of MSI Afterburner section for NVIDIA (claymore dual mode)\r\n" +
    "rem * Конец MSI Afterburner раздела, для исполнения команд для NVIDIA (claymore dual mode)\r\n" +
    "rem ****************************************************************************************\r\n" +
    "goto end\r\n" +
    "\r\n" +

    ":amd\r\n" +
    "rem ****************************************************************************************\r\n" +
    "rem * MSI Afterburner section for AMD\r\n" +
    "rem * MSI Afterburner раздел, для исполнения команд для AMD\r\n" +
    "rem ****************************************************************************************\r\n" +
    "if \"%2\" EQU \"DUAL\" goto amddual\r\n" +
    "echo AMD\r\n" +
    "rem AMD OverdriveNTool section for AMD only ETH mode\r\n" +
    "::utils\\OverdriveNTool.exe -p0\"card0\"\r\n" +
    "::utils\\OverdriveNTool.exe -p1\"card1\"\r\n" +
    "rem ****************************************************************************************\r\n" +
    "rem * End of MSI Afterburner section for AMD\r\n" +
    "rem * Конец MSI Afterburner раздела, для исполнения команд для AMD\r\n" +
    "rem ****************************************************************************************\r\n" +
    "goto end\r\n" +
    "\r\n" +

    ":amddual\r\n" +
    "rem ****************************************************************************************\r\n" +
    "rem MSI Afterburner section for AMD (claymore dual mode)\r\n" +
    "rem * MSI Afterburner раздел, для исполнения команд для AMD (claymore dual mode)\r\n" +
    "rem ****************************************************************************************\r\n" +
    "echo AMD DUAL\r\n" +
    "rem AMD OverdriveNTool section for AMD dual mode\r\n" +
    "::utils\\OverdriveNTool.exe -p0\"card0\"\r\n" +
    "::utils\\OverdriveNTool.exe -p1\"card1\"\r\n" +
    "rem ****************************************************************************************\r\n" +
    "rem End of MSI Afterburner section for AMD (claymore dual mode)\r\n" +
    "rem * Конец MSI Afterburner раздела, для исполнения команд для AMD (claymore dual mode)\r\n" +
    "rem ****************************************************************************************\r\n" +
    "goto end\r\n" +
    "\r\n" +

    ":end\r\n" +
    "echo END\r\n" +
    "rem Do NOT disable this delay\r\n" +
    "timeout /t 1 /nobreak\r\n";
    */
        protected Miner(string minerDeviceName)
        {
            ConectionType = NhmConectionType.STRATUM_TCP;
            MiningSetup = new MiningSetup(null);
            IsInit = false;
            MinerID = MinerIDCount++;

            MinerDeviceName = minerDeviceName;

            WorkingDirectory = "";

            IsRunning = false;
            IsRunningNew = false;
            PreviousTotalMH = 0.0;

            LastCommandLine = "";

            IsApiReadException = false;
            // Only set minimize if hide is false (specific miners will override true after)
            IsNeverHideMiningWindow = ConfigManager.GeneralConfig.MinimizeMiningWindows &&
                                      !ConfigManager.GeneralConfig.HideMiningWindows;
            IsKillAllUsedMinerProcs = false;
            _maxCooldownTimeInMilliseconds = GetMaxCooldownTimeInMilliseconds();
            //
            Helpers.ConsolePrint(MinerTag(), "NEW MINER CREATED");
        }

        ~Miner()
        {
            // free the port
            MinersApiPortsManager.RemovePort(ApiPort);
            Helpers.ConsolePrint(MinerTag(), "MINER DESTROYED");
        }

        protected void SetWorkingDirAndProgName(string fullPath)
        {
            WorkingDirectory = "";
            Path = fullPath;
            var lastIndex = fullPath.LastIndexOf("\\") + 1;
            if (lastIndex > 0)
            {
                WorkingDirectory = fullPath.Substring(0, lastIndex);
                MinerExeName = fullPath.Substring(lastIndex);
            }
        }

        private void SetApiPort()
        {
            if (IsInit)
            {
                var minerBase = MiningSetup.MiningPairs[0].Algorithm.MinerBaseType;
                var algoType = MiningSetup.MiningPairs[0].Algorithm.NiceHashID;
                var path = MiningSetup.MinerPath;
                var reservedPorts = MinersSettingsManager.GetPortsListFor(minerBase, path, algoType);
                ApiPort = -1; // not set
                foreach (var reservedPort in reservedPorts)
                {
                    if (MinersApiPortsManager.IsPortAvaliable(reservedPort))
                    {
                        if (minerBase.Equals("hsrneoscrypt"))
                        {
                            ApiPort = 4001;
                        }
                        else
                        {
                            ApiPort = reservedPort;
                        }
                        break;
                    }
                }
                if (minerBase.ToString().Equals("hsrneoscrypt"))
                {
                    ApiPort = 4001;
                }
                else
                {
                    ApiPort = MinersApiPortsManager.GetAvaliablePort();
                }

            }
        }


        public virtual void InitMiningSetup(MiningSetup miningSetup)
        {
            MiningSetup = miningSetup;
            IsInit = MiningSetup.IsInit;
            SetApiPort();
            SetWorkingDirAndProgName(MiningSetup.MinerPath);
        }

        public void InitBenchmarkSetup(MiningPair benchmarkPair)
        {
            InitMiningSetup(new MiningSetup(new List<MiningPair>()
            {
                benchmarkPair
            }));
            BenchmarkAlgorithm = benchmarkPair.Algorithm;
        }

        // TAG for identifying miner
        public string MinerTag()
        {
            if (_minerTag == null)
            {
                const string mask = "{0}-MINER_ID({1})-DEVICE_IDs({2})";
                // no devices set
                if (!IsInit)
                {
                    return string.Format(mask, MinerDeviceName, MinerID, "NOT_SET");
                }

                // contains ids
                var ids = MiningSetup.MiningPairs.Select(cdevs => cdevs.Device.ID.ToString()).ToList();
                _minerTag = string.Format(mask, MinerDeviceName, MinerID, string.Join(",", ids));
            }

            return _minerTag;
        }

        private static string ProcessTag(MinerPidData pidData)
        {
            return $"[pid({pidData.Pid})|bin({pidData.MinerBinPath})]";
        }

        public string ProcessTag()
        {
            return _currentPidData == null ? "PidData is NULL" : ProcessTag(_currentPidData);
        }

        public void KillAllUsedMinerProcesses()
        {
            var toRemovePidData = new List<MinerPidData>();
            Helpers.ConsolePrint(MinerTag(), "Trying to kill all miner processes for this instance:");
            foreach (var pidData in _allPidData)
            {
                try
                {
                    var process = Process.GetProcessById(pidData.Pid);
                    if (pidData.MinerBinPath.Contains(process.ProcessName))
                    {
                        Helpers.ConsolePrint(MinerTag(), $"Trying to kill {ProcessTag(pidData)}");
                        try
                        {
                            process.Kill();
                            process.Close();
                            process.WaitForExit(1000 * 60 * 1);
                        }
                        catch (Exception e)
                        {
                            Helpers.ConsolePrint(MinerTag(),
                                $"Exception killing {ProcessTag(pidData)}, exMsg {e.Message}");
                        }
                    }
                }
                catch (Exception e)
                {
                    toRemovePidData.Add(pidData);
                    Helpers.ConsolePrint(MinerTag(), $"Nothing to kill {ProcessTag(pidData)}, exMsg {e.Message}");
                }
            }

            _allPidData.RemoveAll(x => toRemovePidData.Contains(x));
        }

        public abstract void Start(string url, string btcAdress, string worker);

        protected string GetUsername(string btcAdress, string worker)
        {
            if (worker.Length > 0)
            {
                if (Configs.ConfigManager.GeneralConfig.NewPlatform)
                {
                    return btcAdress + "." + worker + "$" + NiceHashMiner.Stats.NiceHashSocket.RigID;
                } else
                {
                    return btcAdress + "." + worker;
                }
                //return $"{btcAdress}.{worker}{"$"+NiceHashMiner.Stats.NiceHashSocket.RigID}";
                //return $"{btcAdress}.{worker}${NiceHashMiner.Stats.NiceHashSocket.RigID}";
                //return $"{btcAdress}.{worker}${NiceHashMiner.Stats.NiceHashSocket.RigID}";
            } else
            {

            }

            return btcAdress;
        }

        protected abstract void _Stop(MinerStopType willswitch);

        public virtual void Stop(MinerStopType willswitch = MinerStopType.SWITCH)
        {
            _cooldownCheckTimer?.Stop();
            _Stop(willswitch);
            PreviousTotalMH = 0.0;
            IsRunning = false;
            IsRunningNew = false;
            RunCMDBeforeOrAfterMining(false);
            if (Configs.ConfigManager.GeneralConfig.NewPlatform)
            {
                NiceHashStats.DeviceStatus_TickNew("STOPPED");
            }
        }

        public void End()
        {
            _isEnded = true;
            Stop(MinerStopType.FORCE_END);
        }
        protected void KillProcessAndChildren(int pid)
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
            /*
            try
            {
                //проблемы с перезапуском майнеров
                int k = ProcessTag().IndexOf("pid(");
                int i = ProcessTag().IndexOf(")|bin");
                var cpid = ProcessTag().Substring(k + 4, i - k - 4).Trim();
                pid = int.Parse(cpid, CultureInfo.InvariantCulture);
                Thread.Sleep(500);
                if (pid > 0) //процесс может быть уже убит?
                {
                    try
                    {
                        Process proc = Process.GetProcessById(pid);
                        //proc.Kill();
                        Helpers.ConsolePrint(MinerTag(), ProcessTag() + " Killing miner with pid: " + pid.ToString());
                    }
                    catch {
                        Helpers.ConsolePrint(MinerTag(), ProcessTag() + " ERROR killing miner with pid: " + pid.ToString());
                    }
                }
                
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
            */
        }
        protected void Stop_cpu_ccminer_sgminer_nheqminer(MinerStopType willswitch)
        {
            if (IsRunning)
            {
                Helpers.ConsolePrint(MinerTag(), ProcessTag() + " Shutting down miner");
            }

            if (ProcessHandle != null)
            {
                int k = ProcessTag().IndexOf("pid(");
                int i = ProcessTag().IndexOf(")|bin");
                var cpid = ProcessTag().Substring(k + 4, i - k - 4).Trim();
                int pid = int.Parse(cpid, CultureInfo.InvariantCulture);
                KillProcessAndChildren(pid);

                if (ProcessHandle != null)
                {
                    try { ProcessHandle.Kill(); }
                    catch { }
                }
                //try { ProcessHandle.SendCtrlC((uint)Process.GetCurrentProcess().Id); } catch { }
                if (ProcessHandle != null)
                {
                    ProcessHandle.Close();
                    ProcessHandle = null;
                }
                // sgminer needs to be removed and kill by PID
                if (IsKillAllUsedMinerProcs) KillAllUsedMinerProcesses();
            }
        }

        protected void KillProspectorClaymoreMinerBase(string exeName)
        {
            foreach (var process in Process.GetProcessesByName(exeName))
            {
                try { process.Kill(); }
                catch (Exception e) { Helpers.ConsolePrint(MinerDeviceName, e.ToString()); }
            }
        }

        protected virtual string GetDevicesCommandString()
        {
            var deviceStringCommand = " ";

            var ids = MiningSetup.MiningPairs.Select(mPair => mPair.Device.ID.ToString()).ToList();
            deviceStringCommand += string.Join(",", ids);

            return deviceStringCommand;
        }

        #region BENCHMARK DE-COUPLED Decoupled benchmarking routines
        protected double BenchmarkParseLine_cpu_hsrneoscrypt_extra(string outdata)
        {
            // parse line
            if (outdata.Contains("Benchmark: ") && outdata.Contains("/s"))
            {
                int i = outdata.IndexOf("Benchmark:");
                int k = outdata.IndexOf("/s");
                string hashspeed = outdata.Substring(i + 11, k - i - 9);
                Helpers.ConsolePrint("BENCHMARK-NS", "Final Speed: " + hashspeed);

                // save speed
                int b = hashspeed.IndexOf(" ");
                if (b < 0)
                {
                    int stub;
                    for (int _i = hashspeed.Length - 1; _i >= 0; --_i)
                    {
                        if (Int32.TryParse(hashspeed[_i].ToString(), out stub))
                        {
                            b = _i;
                            break;
                        }
                    }
                }
                if (b >= 0)
                {
                    string speedStr = hashspeed.Substring(0, b);
                    double spd = Helpers.ParseDouble(speedStr);
                    if (hashspeed.Contains("kH/s"))
                        spd *= 1000;
                    else if (hashspeed.Contains("MH/s"))
                        spd *= 1000000;
                    else if (hashspeed.Contains("GH/s"))
                        spd *= 1000000000;

                    return spd;
                }
            }
            return 0.0d;
        }

        protected async Task<ApiData> GetSummaryCPU_hsrneoscryptAsync()
        {
            string resp;
            // TODO aname
            string aname = null;
            ApiData ad = new ApiData(MiningSetup.CurrentAlgorithmType);

            string DataToSend = GetHttpRequestNhmAgentStrin("summary");

            resp = await GetApiDataAsync(ApiPort, DataToSend);
            if (resp == null)
            {
                Helpers.ConsolePrint(MinerTag(), ProcessTag() + " summary is null");
                CurrentMinerReadStatus = MinerApiReadStatus.NONE;
                return null;
            }

            try
            {
                string[] resps = resp.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < resps.Length; i++)
                {
                    string[] optval = resps[i].Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (optval.Length != 2) continue;
                    if (optval[0] == "ALGO")
                        aname = optval[1];
                    else if (optval[0] == "KHS")
                        ad.Speed = double.Parse(optval[1], CultureInfo.InvariantCulture) * 1000; // HPS
                }
            }
            catch
            {
                Helpers.ConsolePrint(MinerTag(), ProcessTag() + " Could not read data from API bind port");
                CurrentMinerReadStatus = MinerApiReadStatus.NONE;
                return null;
            }

            CurrentMinerReadStatus = MinerApiReadStatus.GOT_READ;
            // check if speed zero
            if (ad.Speed == 0) CurrentMinerReadStatus = MinerApiReadStatus.READ_SPEED_ZERO;

            return ad;
        }

        public int BenchmarkTimeoutInSeconds(int timeInSeconds)
        {
            if (TimeoutStandard) return timeInSeconds;
            if (BenchmarkAlgorithm.NiceHashID == AlgorithmType.DaggerHashimoto)
            {
                return 5 * 60 + 120; // 5 minutes plus two minutes
            }

            if (BenchmarkAlgorithm.NiceHashID == AlgorithmType.CryptoNight)
            {
                return 5 * 60 + 120; // 5 minutes plus two minutes
            }

            return timeInSeconds + 120; // wait time plus two minutes
        }

        // TODO remove algorithm
        protected abstract string BenchmarkCreateCommandLine(Algorithm algorithm, int time);

        // The benchmark config and algorithm must guarantee that they are compatible with miner
        // we guarantee algorithm is supported
        // we will not have empty benchmark configs, all benchmark configs will have device list
        public virtual void BenchmarkStart(int time, IBenchmarkComunicator benchmarkComunicator)
        {
            BenchmarkComunicator = benchmarkComunicator;
            BenchmarkTimeInSeconds = time;
            BenchmarkSignalFinnished = true;
            // check and kill
            BenchmarkHandle = null;
            OnBenchmarkCompleteCalled = false;
            _benchmarkTimeOutStopWatch = null;


            try
            {
                if (!Directory.Exists("logs"))
                {
                    Directory.CreateDirectory("logs");
                }
            }
            catch { }

            BenchLines = new List<string>();
            _benchmarkLogPath =
                $"{Logger.LogPath}Log_{MiningSetup.MiningPairs[0].Device.Uuid}_{MiningSetup.MiningPairs[0].Algorithm.AlgorithmStringID}";


            var commandLine = BenchmarkCreateCommandLine(BenchmarkAlgorithm, time);

            var benchmarkThread = new Thread(BenchmarkThreadRoutine);

            benchmarkThread.Start(commandLine);


        }

        protected virtual Process BenchmarkStartProcess(string commandLine)
        {
            RunCMDBeforeOrAfterMining(true);
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Helpers.ConsolePrint(MinerTag(), "Starting benchmark: " + commandLine);

            var benchmarkHandle = new Process
            {
                StartInfo =
                {
                    FileName = MiningSetup.MinerPath
                }
            };


                BenchmarkProcessPath = benchmarkHandle.StartInfo.FileName;
                Helpers.ConsolePrint(MinerTag(), "Using miner: " + benchmarkHandle.StartInfo.FileName);
                benchmarkHandle.StartInfo.WorkingDirectory = WorkingDirectory;

            // set sys variables
            if (MinersSettingsManager.MinerSystemVariables.ContainsKey(Path))
            {
                foreach (var kvp in MinersSettingsManager.MinerSystemVariables[Path])
                {
                    var envName = kvp.Key;
                    var envValue = kvp.Value;
                    benchmarkHandle.StartInfo.EnvironmentVariables[envName] = envValue;
                }
            }

            benchmarkHandle.StartInfo.Arguments = commandLine;
            benchmarkHandle.StartInfo.UseShellExecute = false;
            benchmarkHandle.StartInfo.RedirectStandardError = true;
            benchmarkHandle.StartInfo.RedirectStandardOutput = true;
            benchmarkHandle.StartInfo.CreateNoWindow = true;
            benchmarkHandle.OutputDataReceived += BenchmarkOutputErrorDataReceived;
            benchmarkHandle.ErrorDataReceived += BenchmarkOutputErrorDataReceived;
            benchmarkHandle.Exited += BenchmarkHandle_Exited;

            Ethlargement.CheckAndStart(MiningSetup);

            if (!benchmarkHandle.Start()) return null;

            _currentPidData = new MinerPidData
            {
                MinerBinPath = benchmarkHandle.StartInfo.FileName,
                Pid = benchmarkHandle.Id
            };
            _allPidData.Add(_currentPidData);

            return benchmarkHandle;
        }

        private void BenchmarkHandle_Exited(object sender, EventArgs e)
        {
            BenchmarkSignalFinnished = true;
        }

        private void BenchmarkOutputErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (_benchmarkTimeOutStopWatch == null)
            {
                _benchmarkTimeOutStopWatch = new Stopwatch();
                _benchmarkTimeOutStopWatch.Start();
            }
            else if (_benchmarkTimeOutStopWatch.Elapsed.TotalSeconds >
                     BenchmarkTimeoutInSeconds(BenchmarkTimeInSeconds))
            {
                _benchmarkTimeOutStopWatch.Stop();
                BenchmarkSignalTimedout = true;
            }

            var outdata = e.Data;
            if (e.Data != null)
            {
                BenchmarkOutputErrorDataReceivedImpl(outdata);
            }

            // terminate process situations
            if (BenchmarkSignalQuit
                || BenchmarkSignalFinnished
                || BenchmarkSignalHanged
                || BenchmarkSignalTimedout
                || BenchmarkException != null)
            {
                FinishUpBenchmark();
                EndBenchmarkProcces();
            }
        }

        protected virtual void FinishUpBenchmark()
        { }

        protected abstract void BenchmarkOutputErrorDataReceivedImpl(string outdata);

        protected void CheckOutdata(string outdata)
        {
            //            Helpers.ConsolePrint("BENCHMARK_CheckOutData" , outdata);
            BenchLines.Add(outdata);
            // ccminer, cpuminer
            if (outdata.Contains("Cuda error"))
                BenchmarkException = new Exception("CUDA error");
            if (outdata.Contains("is not supported"))
                BenchmarkException = new Exception("N/A");
            if (outdata.Contains("illegal memory access"))
                BenchmarkException = new Exception("CUDA error");
            if (outdata.Contains("unknown error"))
                BenchmarkException = new Exception("Unknown error");
            if (outdata.Contains("No servers could be used! Exiting."))
                BenchmarkException = new Exception("No pools or work can be used for benchmarking");
            //if (outdata.Contains("error") || outdata.Contains("Error"))
            //    BenchmarkException = new Exception("Unknown error #2");
            // Ethminer
            if (outdata.Contains("No GPU device with sufficient memory was found"))
                BenchmarkException = new Exception("[daggerhashimoto] No GPU device with sufficient memory was found.");
            // xmr-stak
            if (outdata.Contains("Press any key to exit"))
                BenchmarkException = new Exception("Xmr-Stak erred, check its logs");

            // lastly parse data
            Helpers.ConsolePrint("BENCHMARK_CheckOutData", outdata);
            if (BenchmarkParseLine(outdata))
            {
                BenchmarkSignalFinnished = true;
            }
        }

        public void InvokeBenchmarkSignalQuit()
        {
            KillAllUsedMinerProcesses();
        }

        protected double BenchmarkParseLine_cpu_ccminer_extra(string outdata)
        {
            // parse line
            if (outdata.Contains("Benchmark: ") && outdata.Contains("/s"))
            {
                var i = outdata.IndexOf("Benchmark:");
                var k = outdata.IndexOf("/s");
                var hashspeed = outdata.Substring(i + 11, k - i - 9);
                Helpers.ConsolePrint("BENCHMARK-CC", "Final Speed: " + hashspeed);

                // save speed
                var b = hashspeed.IndexOf(" ");
                if (b < 0)
                {
                    for (var j = hashspeed.Length - 1; j >= 0; --j)
                    {
                        if (!int.TryParse(hashspeed[j].ToString(), out var _)) continue;
                        b = j;
                        break;
                    }
                }

                if (b >= 0)
                {
                    var speedStr = hashspeed.Substring(0, b);
                    var spd = Helpers.ParseDouble(speedStr);
                    if (hashspeed.Contains("kH/s"))
                        spd *= 1000;
                    else if (hashspeed.Contains("MH/s"))
                        spd *= 1000000;
                    else if (hashspeed.Contains("GH/s"))
                        spd *= 1000000000;

                    return spd;
                }
            }

            return 0.0d;
        }

        // killing proccesses can take time
        public virtual void EndBenchmarkProcces()
        {
            if (BenchmarkHandle != null && BenchmarkProcessStatus != BenchmarkProcessStatus.Killing &&
                BenchmarkProcessStatus != BenchmarkProcessStatus.DoneKilling)
            {
                BenchmarkProcessStatus = BenchmarkProcessStatus.Killing;
                try
                {
                    Helpers.ConsolePrint("BENCHMARK-end",
                        $"Trying to kill benchmark process {BenchmarkProcessPath} algorithm {BenchmarkAlgorithm.AlgorithmName}");
                    BenchmarkHandle.Kill();
                    BenchmarkHandle.Close();
                    KillAllUsedMinerProcesses();
                }
                catch { }
                finally
                {
                    BenchmarkProcessStatus = BenchmarkProcessStatus.DoneKilling;
                    Helpers.ConsolePrint("BENCHMARK-end",
                        $"Benchmark process {BenchmarkProcessPath} algorithm {BenchmarkAlgorithm.AlgorithmName} KILLED");
                    //BenchmarkHandle = null;
                }
            }
        }


        protected virtual void BenchmarkThreadRoutineStartSettup()
        {
            BenchmarkHandle.BeginErrorReadLine();
            BenchmarkHandle.BeginOutputReadLine();
        }

        protected void BenchmarkThreadRoutineCatch(Exception ex)
        {
            BenchmarkAlgorithm.BenchmarkSpeed = 0;

            Helpers.ConsolePrint(MinerTag(), "Benchmark Exception: " + ex.Message);
            if (BenchmarkComunicator != null && !OnBenchmarkCompleteCalled)
            {
                OnBenchmarkCompleteCalled = true;
                BenchmarkComunicator.OnBenchmarkComplete(false, GetFinalBenchmarkString());
            }
        }

        protected virtual string GetFinalBenchmarkString()
        {
            return BenchmarkSignalTimedout && !TimeoutStandard
                ? International.GetText("Benchmark_Timedout")
                : International.GetText("Benchmark_Terminated");
        }

        protected void BenchmarkThreadRoutineFinish()
        {
            var status = BenchmarkProcessStatus.Finished;
            RunCMDBeforeOrAfterMining(false);
            if (!BenchmarkAlgorithm.BenchmarkNeeded)
            {
                status = BenchmarkProcessStatus.Success;
            }

            try
            {
                using (StreamWriter sw = File.AppendText(_benchmarkLogPath))
                {
                    foreach (var line in BenchLines)
                    {
                        sw.WriteLine(line);
                    }
                }
            }
            catch { }

            BenchmarkProcessStatus = status;
            if (BenchmarkAlgorithm is DualAlgorithm dualAlg)
            {
                if (!dualAlg.TuningEnabled)
                {
                    // Tuning will report speed
                    Helpers.ConsolePrint("BENCHMARK-finish",
                        "Final Speed: " + Helpers.FormatDualSpeedOutput(dualAlg.BenchmarkSpeed,
                            dualAlg.SecondaryBenchmarkSpeed, dualAlg.DualNiceHashID));
                }
            }
            else
            {
                Helpers.ConsolePrint("BENCHMARK-finish",
                    "Final Speed: " + Helpers.FormatDualSpeedOutput(BenchmarkAlgorithm.BenchmarkSpeed, 0,
                        BenchmarkAlgorithm.NiceHashID));
            }

            Helpers.ConsolePrint("BENCHMARK-finish", "Benchmark ends");
            if (BenchmarkComunicator != null && !OnBenchmarkCompleteCalled)
            {
                OnBenchmarkCompleteCalled = true;
                var isOK = BenchmarkProcessStatus.Success == status;
                var msg = GetFinalBenchmarkString();
                BenchmarkComunicator.OnBenchmarkComplete(isOK, isOK ? "" : msg);
            }
        }


        protected virtual void BenchmarkThreadRoutine(object commandLine)
        {
            BenchmarkSignalQuit = false;
            BenchmarkSignalHanged = false;
            BenchmarkSignalFinnished = false;
            BenchmarkException = null;

            Thread.Sleep(ConfigManager.GeneralConfig.MinerRestartDelayMS);

            try
            {
                Helpers.ConsolePrint("BENCHMARK-routine", "Benchmark starts");
                BenchmarkHandle = BenchmarkStartProcess((string)commandLine);

                BenchmarkThreadRoutineStartSettup();
                // wait a little longer then the benchmark routine if exit false throw
                //var timeoutTime = BenchmarkTimeoutInSeconds(BenchmarkTimeInSeconds);
                //var exitSucces = BenchmarkHandle.WaitForExit(timeoutTime * 1000);
                // don't use wait for it breaks everything
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

        /// <summary>
        /// Thread routine for miners that cannot be scheduled to stop and need speed data read from command line
        /// </summary>
        /// <param name="commandLine"></param>
        /// <param name="benchmarkTimeWait"></param>
        protected void BenchmarkThreadRoutineAlternate(object commandLine, int benchmarkTimeWait)
        {
            CleanOldLogs();

            BenchmarkSignalQuit = false;
            BenchmarkSignalHanged = false;
            BenchmarkSignalFinnished = false;
            BenchmarkException = null;

            Thread.Sleep(ConfigManager.GeneralConfig.MinerRestartDelayMS);

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
                foreach (var file in dirInfo.GetFiles(GetLogFileName()))
                {
                    latestLogFile = file.Name;
                    break;
                }

                BenchmarkHandle?.WaitForExit(10000);
                // read file log
                if (File.Exists(WorkingDirectory + latestLogFile))
                {
                    var lines = File.ReadAllLines(WorkingDirectory + latestLogFile);
                    ProcessBenchLinesAlternate(lines);
                }

                BenchmarkThreadRoutineFinish();
            }
        }

        protected void CleanOldLogs()
        {
            // clean old logs
            try
            {
                var dirInfo = new DirectoryInfo(WorkingDirectory);
                var deleteContains = GetLogFileName();
                if (dirInfo.Exists)
                {
                    foreach (var file in dirInfo.GetFiles())
                    {
                        if (file.Name.Contains(deleteContains))
                        {
                            file.Delete();
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// When parallel benchmarking each device needs its own log files, so this uniquely identifies for the setup
        /// </summary>
        protected string GetDeviceID()
        {
            var ids = MiningSetup.MiningPairs.Select(x => x.Device.ID);
            var idStr = string.Join(",", ids);

            if (!IsMultiType) return idStr;

            // Miners that use multiple dev types need to also discriminate based on that
            var types = MiningSetup.MiningPairs.Select(x => (int)x.Device.DeviceType);
            return $"{string.Join(",", types)}-{idStr}";
        }

        protected string GetLogFileName()
        {
            return $"{GetDeviceID()}_log.txt";
        }

        protected virtual void ProcessBenchLinesAlternate(string[] lines)
        { }

        protected abstract bool BenchmarkParseLine(string outdata);
        public static string[,] myServers = { { "eu", "20000" }, { "usa", "20001" }, { "hk", "20002" }, { "jp", "20003" }, { "in", "20004" }, { "br", "20005" } };
        public static int PingServers()
        {
            Ping ping = new Ping();
            int serverId = 0;
            int bestServerId = 0;
            long bestReplyTime = 10000;

            string server = ""; 
            Helpers.ConsolePrint("PingServers", " start ping");
            for (int i = 0; i < 6; i++)
            {
                try
                {
                    server = "speedtest." + myServers[i, 0] + ".nicehash.com";
                    // Helpers.ConsolePrint("PingServers:", myServers[i,0]);
                    var pingReply = ping.Send(server, 1000);
                    if (pingReply.Status != IPStatus.TimedOut)
                    {
                        var pingReplyTime = pingReply.RoundtripTime;
                        myServers[i, 1] = pingReplyTime.ToString();
                        Helpers.ConsolePrint("PingServers", server + " id:" + serverId.ToString() + " ping: " + pingReplyTime.ToString());
                        if (pingReplyTime < bestReplyTime)
                        {
                            bestServerId = serverId;
                            bestReplyTime = pingReplyTime;
                        }
                    }
                    else
                    {
                        Helpers.ConsolePrint("PingServers", server + " out of range");
                    }
                } catch (PingException)
                {
                    Helpers.ConsolePrint("PingServers", server + " offline");
                }
                serverId++;
            }
            string[,] tmpServers = { { "eu", "20000" }, { "usa", "20001" }, { "hk", "20002" }, { "jp", "20003" }, { "in", "20004" }, { "br", "20005" } }; ;
            int pingReplyTimeTmp;
            long bestReplyTimeTmp = 10000;
            int iTmp = 0;
            for (int k = 0; k < 6; k++)
            {
                for (int i = 0; i < 6; i++)
                {
                    pingReplyTimeTmp = Convert.ToInt32(myServers[i, 1]);
                    if (pingReplyTimeTmp < bestReplyTimeTmp && pingReplyTimeTmp != -1)
                    {
                        iTmp = i;
                        bestReplyTimeTmp = pingReplyTimeTmp;
                    }

                }
                tmpServers[k, 0] = myServers[iTmp, 0];
                tmpServers[k, 1] = myServers[iTmp, 1];
                myServers[iTmp, 1] = "-1";
                bestReplyTimeTmp = 10000;
            }

            myServers = tmpServers;
            for (int i = 0; i < 6; i++)
            {
                server = "speedtest." + myServers[i, 0] + ".nicehash.com";
                Helpers.ConsolePrint("SortedServers", server + " ping: " + myServers[i, 1]);
            }
               
             Helpers.ConsolePrint("PingServers", "BestServerId: " + bestServerId.ToString());
            return bestServerId;
        }
        protected string GetServiceUrl(AlgorithmType algo)
        {
            return Globals.GetLocationUrl(algo, Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], 
                ConectionType);
        }
        protected bool IsActiveProcess(int pid)
        {
            try
            {
                return Process.GetProcessById(pid) != null;
            }
            catch
            {
                return false;
            }
        }

        #endregion //BENCHMARK DE-COUPLED Decoupled benchmarking routines

        protected virtual NiceHashProcess _Start()
        {
           RunCMDBeforeOrAfterMining(true);
            // never start when ended
            if (_isEnded)
            {
                return null;
            }

            PreviousTotalMH = 0.0;
            if (LastCommandLine.Length == 0) return null;

            Ethlargement.CheckAndStart(MiningSetup);

            var P = new NiceHashProcess();

            Ethlargement.CheckAndStart(MiningSetup);

            if (WorkingDirectory.Length > 1)
            {
                P.StartInfo.WorkingDirectory = WorkingDirectory;
            }

            if (MinersSettingsManager.MinerSystemVariables.ContainsKey(Path))
            {
                foreach (var kvp in MinersSettingsManager.MinerSystemVariables[Path])
                {
                    var envName = kvp.Key;
                    var envValue = kvp.Value;
                    P.StartInfo.EnvironmentVariables[envName] = envValue;
                }
            }

            P.StartInfo.FileName = Path;
            P.ExitEvent = Miner_Exited;
            LastCommandLine = System.Text.RegularExpressions.Regex.Replace(LastCommandLine, @"\s+", " ");
            P.StartInfo.Arguments = LastCommandLine;
            if (IsNeverHideMiningWindow)
            {
                P.StartInfo.CreateNoWindow = false;
                if (ConfigManager.GeneralConfig.HideMiningWindows || ConfigManager.GeneralConfig.MinimizeMiningWindows)
                {
                    P.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                    P.StartInfo.UseShellExecute = true;
                }
            }
            else
            {
                P.StartInfo.CreateNoWindow = ConfigManager.GeneralConfig.HideMiningWindows;
            }

            P.StartInfo.UseShellExecute = false;

            try
            {
                if (P.Start())
                {
                    IsRunning = true;
                    IsRunningNew = true;
                    if (Configs.ConfigManager.GeneralConfig.NewPlatform)
                    {
                        NiceHashStats.DeviceStatus_TickNew("MINING");
                    }
                    _currentPidData = new MinerPidData
                    {
                        MinerBinPath = P.StartInfo.FileName,
                        Pid = P.Id
                    };
                    _allPidData.Add(_currentPidData);

                    Helpers.ConsolePrint(MinerTag(), "Starting miner " + ProcessTag() + " " + LastCommandLine);

                    StartCoolDownTimerChecker();

                    return P;
                }

                Helpers.ConsolePrint(MinerTag(), "NOT STARTED " + ProcessTag() + " " + LastCommandLine);
                return null;
            }
            catch (Exception ex)
            {
                Helpers.ConsolePrint(MinerTag(), ProcessTag() + " _Start: " + ex.Message);
                return null;
            }
        }

        protected void StartCoolDownTimerChecker()
        {
            if (ConfigManager.GeneralConfig.CoolDownCheckEnabled)
            {
                Helpers.ConsolePrint(MinerTag(), ProcessTag() + " Starting cooldown checker");
                if (_cooldownCheckTimer != null && _cooldownCheckTimer.Enabled) _cooldownCheckTimer.Stop();
                // cool down init
                _cooldownCheckTimer = new Timer()
                {
                    Interval = MinCooldownTimeInMilliseconds
                };
                _cooldownCheckTimer.Elapsed += MinerCoolingCheck_Tick;
                _cooldownCheckTimer.Start();
                _currentCooldownTimeInSeconds = MinCooldownTimeInMilliseconds;
                _currentCooldownTimeInSecondsLeft = _currentCooldownTimeInSeconds;
            }
            else
            {
                Helpers.ConsolePrint(MinerTag(), "Cooldown checker disabled");
            }

            CurrentMinerReadStatus = MinerApiReadStatus.NONE;
        }


        protected virtual void Miner_Exited()
        {
            ScheduleRestart(5000);
        }

        protected void ScheduleRestart(int ms)
        {
            var restartInMs = ConfigManager.GeneralConfig.MinerRestartDelayMS > ms
                ? ConfigManager.GeneralConfig.MinerRestartDelayMS
                : ms;
            Helpers.ConsolePrint(MinerTag(), ProcessTag() + $" directly Miner_Exited Will restart in {restartInMs} ms");
           // if (ConfigManager.GeneralConfig.CoolDownCheckEnabled)
            //{
             //   CurrentMinerReadStatus = MinerApiReadStatus.RESTART;
              //  _needsRestart = true;
               // _currentCooldownTimeInSecondsLeft = restartInMs;
            //}
           // else
            {
                // directly restart since cooldown checker not running
                Thread.Sleep(restartInMs);
                Restart();
            }
        }

        protected void Restart()
        {
            if (_isEnded) return;
            Helpers.ConsolePrint(MinerTag(), ProcessTag() + " Restarting miner..");
            Stop(MinerStopType.END); // stop miner first
            Thread.Sleep(ConfigManager.GeneralConfig.MinerRestartDelayMS);
            ProcessHandle = _Start(); // start with old command line
        }

        protected virtual bool IsApiEof(byte third, byte second, byte last)
        {
            return false;
        }

        protected async Task<string> GetApiDataAsync(int port, string dataToSend, bool exitHack = false,
            bool overrideLoop = false)
        {
            string responseFromServer = null;
            try
            {
                var tcpc = new TcpClient("127.0.0.1", port);
                var nwStream = tcpc.GetStream();

                var bytesToSend = Encoding.ASCII.GetBytes(dataToSend);
                await nwStream.WriteAsync(bytesToSend, 0, bytesToSend.Length);

                var incomingBuffer = new byte[tcpc.ReceiveBufferSize];
                var prevOffset = -1;
                var offset = 0;
                var fin = false;

                while (!fin && tcpc.Client.Connected)
                {
                    var r = await nwStream.ReadAsync(incomingBuffer, offset, tcpc.ReceiveBufferSize - offset);
                    for (var i = offset; i < offset + r; i++)
                    {
                        if (incomingBuffer[i] == 0x7C || incomingBuffer[i] == 0x00
                                                      || (i > 2 && IsApiEof(incomingBuffer[i - 2],
                                                              incomingBuffer[i - 1], incomingBuffer[i]))
                                                      || overrideLoop)
                        {
                            fin = true;
                            break;
                        }

                        // Not working
                        //if (IncomingBuffer[i] == 0x5d || IncomingBuffer[i] == 0x5e) {
                        //    fin = true;
                        //    break;
                        //}
                    }

                    offset += r;
                    if (exitHack)
                    {
                        if (prevOffset == offset)
                        {
                            fin = true;
                            break;
                        }

                        prevOffset = offset;
                    }
                }

                tcpc.Close();

                if (offset > 0)
                    responseFromServer = Encoding.ASCII.GetString(incomingBuffer);
            }
            catch (Exception ex)
            {
                Helpers.ConsolePrint(MinerTag(), ProcessTag() + " GetAPIData reason: " + ex.Message);
                return null;
            }

            return responseFromServer;
        }

        public abstract Task<ApiData> GetSummaryAsync();

        protected async Task<ApiData> GetSummaryCpuAsync(string method = "", bool overrideLoop = false)
        {
            var ad = new ApiData(MiningSetup.CurrentAlgorithmType);

            try
            {
                CurrentMinerReadStatus = MinerApiReadStatus.WAIT;
                var dataToSend = GetHttpRequestNhmAgentStrin(method);
                var respStr = await GetApiDataAsync(ApiPort, dataToSend);

                if (string.IsNullOrEmpty(respStr))
                {
                    CurrentMinerReadStatus = MinerApiReadStatus.NETWORK_EXCEPTION;
                    throw new Exception("Response is empty!");
                }

                if (respStr.IndexOf("HTTP/1.1 200 OK") > -1)
                {
                    respStr = respStr.Substring(respStr.IndexOf(HttpHeaderDelimiter) + HttpHeaderDelimiter.Length);
                }
                else
                {
                    throw new Exception("Response not HTTP formed! " + respStr);
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

        protected string GetHttpRequestNhmAgentStrin(string cmd)
        {
            return "GET /" + cmd + " HTTP/1.1\r\n" +
                   "Host: 127.0.0.1\r\n" +
                   "User-Agent: NiceHashMiner/" + Application.ProductVersion + "\r\n" +
                   "\r\n";
        }

        protected async Task<ApiData> GetSummaryCpuCcminerAsync()
        {
            // TODO aname
            string aname = null;
            var ad = new ApiData(MiningSetup.CurrentAlgorithmType);

            var dataToSend = GetHttpRequestNhmAgentStrin("summary");
            var resp = await GetApiDataAsync(ApiPort, dataToSend);
            if (resp == null)
            {
                Helpers.ConsolePrint(MinerTag(), ProcessTag() + " summary is null");
                CurrentMinerReadStatus = MinerApiReadStatus.NONE;
                return null;
            }

            try
            {
                var resps = resp.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var res in resps)
                {
                    var optval = res.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (optval.Length != 2) continue;
                    if (optval[0] == "ALGO")
                        aname = optval[1];
                    else if (optval[0] == "KHS")
                        ad.Speed = double.Parse(optval[1], CultureInfo.InvariantCulture) * 1000; // HPS
                }
            }
            catch
            {
                Helpers.ConsolePrint(MinerTag(), ProcessTag() + " Could not read data from API bind port");
                CurrentMinerReadStatus = MinerApiReadStatus.NONE;
                return null;
            }

            CurrentMinerReadStatus = MinerApiReadStatus.GOT_READ;
            // check if speed zero
            if (ad.Speed == 0) CurrentMinerReadStatus = MinerApiReadStatus.READ_SPEED_ZERO;

            return ad;
        }


        #region Cooldown/retry logic

        /// <summary>
        /// decrement time for half current half time, if less then min ammend
        /// </summary>
        private void CoolDown()
        {
            if (_currentCooldownTimeInSeconds > MinCooldownTimeInMilliseconds)
            {
                _currentCooldownTimeInSeconds = MinCooldownTimeInMilliseconds;
                Helpers.ConsolePrint(MinerTag(),
                    $"{ProcessTag()} Reseting cool time = {MinCooldownTimeInMilliseconds} ms");
                CurrentMinerReadStatus = MinerApiReadStatus.NONE;
            }
        }

        /// <summary>
        /// increment time for half current half time, if more then max set restart
        /// </summary>
        private void CoolUp()
        {
            _currentCooldownTimeInSeconds *= 2;
            Helpers.ConsolePrint(MinerTag(),
                $"{ProcessTag()} Cooling UP, cool time is {_currentCooldownTimeInSeconds} ms");
            if (_currentCooldownTimeInSeconds > _maxCooldownTimeInMilliseconds)
            {
                CurrentMinerReadStatus = MinerApiReadStatus.RESTART;
                Helpers.ConsolePrint(MinerTag(), ProcessTag() + " MAX cool time exceeded. RESTARTING");
                Restart();
            }
        }

        private void MinerCoolingCheck_Tick(object sender, ElapsedEventArgs e)
        {
            if (_isEnded)
            {
                End();
                return;
            }

            _currentCooldownTimeInSecondsLeft -= (int)_cooldownCheckTimer.Interval;
            // if times up
            if (_currentCooldownTimeInSecondsLeft > 0) return;
            if (_needsRestart)
            {
                _needsRestart = false;
                Restart();
                return;
            }
            
                switch (CurrentMinerReadStatus)
                {
                    case MinerApiReadStatus.GOT_READ:
                        Helpers.ConsolePrint(MinerTag(), ProcessTag() + "MinerApiReadStatus.GOT_READ");
                        CoolDown();
                        break;
                    case MinerApiReadStatus.READ_SPEED_ZERO:
                        Helpers.ConsolePrint(MinerTag(), ProcessTag() + " READ SPEED ZERO, will cool up");
                        CoolUp();
                        break;
                    case MinerApiReadStatus.RESTART:
                        Helpers.ConsolePrint(MinerTag(), ProcessTag() + "MinerApiReadStatus.RESTART");
                        Restart();
                        break;
                    default:
                        Helpers.ConsolePrint(MinerTag(), ProcessTag() + "MinerApiReadStatus.UNKNOWN");
                        CoolUp();
                        break;
                }

            // set new times left from the CoolUp/Down change
            _currentCooldownTimeInSecondsLeft = _currentCooldownTimeInSeconds;
        }

        #endregion //Cooldown/retry logic

        protected Process RunCMDBeforeOrAfterMining(bool isBefore)
        {
            bool CreateNoWindow = false;
            var CMDconfigHandle = new Process
            {
                StartInfo =
                {
                    FileName = MiningSetup.MinerPath
                }
            };

            var strPlatform = "";
            var strDual = "SINGLE";
            var strAlgo = AlgorithmNiceHashNames.GetName(MiningSetup.CurrentAlgorithmType);

            var minername = MinerDeviceName;
            int subStr;
            subStr = MinerDeviceName.IndexOf("_");
            if (subStr > 0)
            {
                minername = MinerDeviceName.Substring(0, subStr);
            }
            if (minername == "ClaymoreCryptoNight" || minername == "ClaymoreZcash" || minername == "ClaymoreDual" || minername == "ClaymoreNeoscrypt")
            {
                minername = "Claymore";
            }

            var gpus = "";
            gpus += string.Join(",", MiningSetup.MiningPairs.Select(mPair => mPair.Device.ID.ToString()).ToList());

            foreach (var pair in MiningSetup.MiningPairs)
            {
                if (pair.Algorithm.DualNiceHashID == AlgorithmType.DaggerBlake2s ||
                    pair.Algorithm.DualNiceHashID == AlgorithmType.DaggerDecred ||
                    pair.Algorithm.DualNiceHashID == AlgorithmType.DaggerKeccak ||
                    pair.Algorithm.DualNiceHashID == AlgorithmType.DaggerLbry ||
                    pair.Algorithm.DualNiceHashID == AlgorithmType.DaggerPascal ||
                    pair.Algorithm.DualNiceHashID == AlgorithmType.DaggerSia)
                {
                    strDual = "DUAL";
                }
                if (pair.Device.DeviceType == DeviceType.NVIDIA)
                {
                    strPlatform = "NVIDIA";
                }
                else if (pair.Device.DeviceType == DeviceType.AMD)
                {
                    strPlatform = "AMD";
                }
                else if (pair.Device.DeviceType == DeviceType.CPU)
                {
                    strPlatform = "CPU";
                }
            }

            string MinerDir = MiningSetup.MinerPath.Substring(0, MiningSetup.MinerPath.LastIndexOf("\\"));
            if (isBefore)
            {
                CMDconfigHandle.StartInfo.FileName = "GPU-Scrypt.cmd";
            } else
            {
                CMDconfigHandle.StartInfo.FileName = "GPU-Reset.cmd";
            }
//            Helpers.ConsolePrint("BeforeMiningString:", BeforeMiningString);
/*
            if (!File.Exists(CMDconfigHandle.StartInfo.FileName))
            {
                try
                {
                    FileStream fs = new FileStream(CMDconfigHandle.StartInfo.FileName, FileMode.Create, FileAccess.Write);
                    StreamWriter w = new StreamWriter(fs);
                    w.Write(BeforeOrAfterMiningString);
                    w.Flush();
                    w.Close();
                    Thread.Sleep(1000); //wait for file close
                    return null;
                }
                catch (Exception e)
                {
                    Helpers.ConsolePrint("RunCMDBeforeOrAfterMining", e.ToString());
                }

            } else
            */
            {
                var cmd = "";
                FileStream fs = new FileStream(CMDconfigHandle.StartInfo.FileName, FileMode.Open, FileAccess.Read);
                StreamReader w = new StreamReader(fs);
                cmd = w.ReadToEnd();
                w.Close();

                if (cmd.ToUpper().Trim().Contains("SET NOVISIBLE=TRUE"))
                {
                    CreateNoWindow = true;
                }
                if (cmd.ToUpper().Trim().Contains("SET RUN=FALSE"))
                {
                    return null;
                }
            }
                //BenchmarkProcessPath = CMDconfigHandle.StartInfo.WorkingDirectory;
                Helpers.ConsolePrint(MinerTag(), "Using CMD: " + CMDconfigHandle.StartInfo.FileName);
                //CMDconfigHandle.StartInfo.WorkingDirectory = WorkingDirectory;

            if (MinersSettingsManager.MinerSystemVariables.ContainsKey(Path))
            {
                foreach (var kvp in MinersSettingsManager.MinerSystemVariables[Path])
                {
                    var envName = kvp.Key;
                    var envValue = kvp.Value;
                    CMDconfigHandle.StartInfo.EnvironmentVariables[envName] = envValue;
                }
            }

            Thread.Sleep(200);

            CMDconfigHandle.StartInfo.Arguments = " " + strPlatform + " " + strDual + " " + strAlgo + " \"" + gpus +"\"" + " " + minername;
            CMDconfigHandle.StartInfo.UseShellExecute = false;
            // CMDconfigHandle.StartInfo.RedirectStandardError = true;
            // CMDconfigHandle.StartInfo.RedirectStandardOutput = true;
            CMDconfigHandle.StartInfo.CreateNoWindow = CreateNoWindow;
            Thread.Sleep(250);
            Helpers.ConsolePrint(MinerTag(), "Start CMD: " + CMDconfigHandle.StartInfo.FileName + CMDconfigHandle.StartInfo.Arguments);
            CMDconfigHandle.Start();

            try
            {
                if (!CMDconfigHandle.WaitForExit(60 * 1000))
                {
                    CMDconfigHandle.Kill();
                    CMDconfigHandle.WaitForExit(5 * 1000);
                    CMDconfigHandle.Close();
                }
            }
            catch (Exception e)
            {
                Helpers.ConsolePrint("KillCMDBeforeOrAfterMining", e.ToString());
            }

            Thread.Sleep(50);
            return CMDconfigHandle;
        }

        protected virtual void RunCMDAfterMining(string CMDparam, NiceHashProcess ProcessHandle)
        {
 //           while (ProcessHandle != null)
            {
            }
            bool CreateNoWindow = false;
            var CMDconfigHandle = new Process
            {
                StartInfo =
                {
                    FileName = MiningSetup.MinerPath
                }
            };

            // string BeforeMiningString = "pause\n\r"; //pause работает, а нормальная строка нет!!

            string MinerDir = MiningSetup.MinerPath.Substring(0, MiningSetup.MinerPath.LastIndexOf("\\"));
            CMDconfigHandle.StartInfo.FileName = "GPU-Reset.cmd";

            //            Helpers.ConsolePrint("BeforeMiningString:", BeforeMiningString);
            /*
            if (!File.Exists(CMDconfigHandle.StartInfo.FileName))
            {
                try
                {
                    FileStream fs = new FileStream(CMDconfigHandle.StartInfo.FileName, FileMode.Create, FileAccess.Write);
                    StreamWriter w = new StreamWriter(fs);
                    w.Write(BeforeOrAfterMiningString);
                    w.Flush();
                    w.Close();
                    return;
                }
                catch (Exception e)
                {
                    Helpers.ConsolePrint("RunCMDBeforeOrAfterMining", e.ToString());
                }

            }
            else
            */
            {
                var cmd = "";
                FileStream fs = new FileStream(CMDconfigHandle.StartInfo.FileName, FileMode.Open, FileAccess.Read);
                StreamReader w = new StreamReader(fs);
                cmd = w.ReadToEnd();
                w.Close();

                if (cmd.ToUpper().Trim().Contains("SET NOVISIBLE=TRUE"))
                {
                    CreateNoWindow = true;
                }
                if (cmd.ToUpper().Trim().Contains("SET RUN=FALSE"))
                {
                    return;
                }
            }
            //BenchmarkProcessPath = CMDconfigHandle.StartInfo.WorkingDirectory;
            Helpers.ConsolePrint(MinerTag(), "Using CMD: " + CMDconfigHandle.StartInfo.FileName);
            //CMDconfigHandle.StartInfo.WorkingDirectory = WorkingDirectory;

            if (MinersSettingsManager.MinerSystemVariables.ContainsKey(Path))
            {
                foreach (var kvp in MinersSettingsManager.MinerSystemVariables[Path])
                {
                    var envName = kvp.Key;
                    var envValue = kvp.Value;
                    CMDconfigHandle.StartInfo.EnvironmentVariables[envName] = envValue;
                }
            }

            Thread.Sleep(200);

            CMDconfigHandle.StartInfo.Arguments = CMDparam;
            CMDconfigHandle.StartInfo.UseShellExecute = false;
            // CMDconfigHandle.StartInfo.RedirectStandardError = true;
            // CMDconfigHandle.StartInfo.RedirectStandardOutput = true;
            CMDconfigHandle.StartInfo.CreateNoWindow = CreateNoWindow;
            Thread.Sleep(250);
            Helpers.ConsolePrint(MinerTag(), "Start CMD: " + CMDconfigHandle.StartInfo.FileName + CMDconfigHandle.StartInfo.Arguments);
            CMDconfigHandle.Start();

            try
            {
                if (!CMDconfigHandle.WaitForExit(60 * 1000))
                {
                    CMDconfigHandle.Kill();
                    CMDconfigHandle.WaitForExit(5 * 1000);
                    CMDconfigHandle.Close();
                }
            }
            catch (Exception e)
            {
                Helpers.ConsolePrint("KillCMDBeforeOrAfterMining", e.ToString());
            }

            Thread.Sleep(50);
            return;
        }
        /*
        protected void KillCMDBeforeMining(Process CMDconfigHandle)
        {
            try
            {
                    CMDconfigHandle.Kill();
                    CMDconfigHandle.WaitForExit(1 * 1000);
                    CMDconfigHandle.Close();
            }
            catch (Exception e)
            {
                Helpers.ConsolePrint("KillCMDBeforeMining", e.ToString());
            }
        }
        */
    }
}
