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
    class mkxminer : Miner
    {
        private readonly int GPUPlatformNumber;
        Stopwatch _benchmarkTimer = new Stopwatch();

        public mkxminer()
            : base("mkxminer_AMD")
        {
            GPUPlatformNumber = ComputeDeviceManager.Available.AmdOpenCLPlatformNum;
            IsKillAllUsedMinerProcs = true;
            IsNeverHideMiningWindow = true;

        }

        // use ONLY for exiting a benchmark
        public void Killmkxminer() {
           
            foreach (Process process in Process.GetProcessesByName("mkxminer")) {
                try { process.Kill(); } catch (Exception e) { Helpers.ConsolePrint(MinerDeviceName, e.ToString()); }
            }
           
        }

        public override void EndBenchmarkProcces() {
            if (BenchmarkProcessStatus != BenchmarkProcessStatus.Killing && BenchmarkProcessStatus != BenchmarkProcessStatus.DoneKilling) {
                BenchmarkProcessStatus = BenchmarkProcessStatus.Killing;
                try {
                    Helpers.ConsolePrint("BENCHMARK", String.Format("Trying to kill benchmark process {0} algorithm {1}", BenchmarkProcessPath, BenchmarkAlgorithm.AlgorithmName));
                    Killmkxminer();
                } catch { } finally {
                    BenchmarkProcessStatus = BenchmarkProcessStatus.DoneKilling;
                    Helpers.ConsolePrint("BENCHMARK", String.Format("Benchmark process {0} algorithm {1} KILLED", BenchmarkProcessPath, BenchmarkAlgorithm.AlgorithmName));
                    //BenchmarkHandle = null;
                }
            }
        }

        protected override int GetMaxCooldownTimeInMilliseconds() {
            return 100; // 1.5 minute max, whole waiting time 75seconds
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
            IsApiReadException = false; //** in miner
            //add failover
            string alg = url.Substring(url.IndexOf("://") + 3, url.IndexOf(".") - url.IndexOf("://") - 3);
            string port = url.Substring(url.IndexOf(".com:") + 5, url.Length - url.IndexOf(".com:") - 5);

            LastCommandLine = " -o " + url +
                              " -u " + username +
                              " -p x " +
                              " " +
                              ExtraLaunchParametersParser.ParseForMiningSetup(
                                                                MiningSetup,
                                                                DeviceType.AMD) +
                              " -d ";

            LastCommandLine += GetDevicesCommandString();

            // ProcessHandle = _Start();
            /*
            var mkxminerHandle = new Process
            {
                StartInfo =
                {
                    FileName = MiningSetup.MinerPath
                }
            };
            */
            // mkxminerHandle.StartInfo.FileName = "start /w wscript.exe mkxminer.vbs";
            //Process.Start("cscript.exe", " bin_3rdparty\\mkxminer\\mkxminer.vbs --exitsick --asm " + LastCommandLine);
            Process.Start("powershell.exe", " -Command &bin_3rdparty\\mkxminer\\mkxminer.exe --exitsick --asm " + LastCommandLine);
            
            /*
             Type scriptType = Type.GetTypeFromCLSID(Guid.Parse("0E59F1D5-1FBE-11D0-8FF2-00A0D10038BC"));
             dynamic obj = Activator.CreateInstance(scriptType, false);
             obj.Language = "vbscript";
             string vbscript = "msgbox(\"test\")";
             obj.Eval(vbscript);
             */

            //BenchmarkProcessPath = CMDconfigHandle.StartInfo.WorkingDirectory;
            Helpers.ConsolePrint(MinerTag(), "Using CMD: bin_3rdparty\\mkxminer\\mkxminer.vbs --exitsick --asm " + LastCommandLine);
            //CMDconfigHandle.StartInfo.WorkingDirectory = WorkingDirectory;
            /*
            if (MinersSettingsManager.MinerSystemVariables.ContainsKey(Path))
            {
                foreach (var kvp in MinersSettingsManager.MinerSystemVariables[Path])
                {
                    var envName = kvp.Key;
                    var envValue = kvp.Value;
                    mkxminerHandle.StartInfo.EnvironmentVariables[envName] = envValue;
                }
            }
            */
            Thread.Sleep(200);
            /*
            mkxminerHandle.StartInfo.Arguments = LastCommandLine;
            mkxminerHandle.StartInfo.UseShellExecute = false;
            // CMDconfigHandle.StartInfo.RedirectStandardError = true;
            // CMDconfigHandle.StartInfo.RedirectStandardOutput = true;
            mkxminerHandle.StartInfo.CreateNoWindow = false;
            Thread.Sleep(250);
            Helpers.ConsolePrint(MinerTag(), "Start CMD: " + mkxminerHandle.StartInfo.FileName + mkxminerHandle.StartInfo.Arguments);
            mkxminerHandle.Start();
            */
        }

        // new decoupled benchmarking routines
        #region Decoupled benchmarking routines

        protected override string BenchmarkCreateCommandLine(Algorithm algorithm, int time) {
            string CommandLine ="";

            MessageBox.Show("Enter hashrate manually for mkxminer", "Warning!", MessageBoxButtons.OK);
            BenchmarkProcessStatus = BenchmarkProcessStatus.Killing;
            _benchmarkTimer.Stop();
            EndBenchmarkProcces();

            return CommandLine;
        }

        protected override bool BenchmarkParseLine(string outdata) {
            
            return false;
        }

        protected override void BenchmarkThreadRoutineStartSettup() {
            // sgminer extra settings
          
        }

        protected override void BenchmarkOutputErrorDataReceivedImpl(string outdata)
        {
                _benchmarkTimer.Stop();
                // this is safe in a benchmark
                Killmkxminer();
                BenchmarkSignalHanged = true;
        }

        protected override string GetFinalBenchmarkString() {
            if (BenchmarkAlgorithm.BenchmarkSpeed <= 0) {
                Helpers.ConsolePrint("mkxminer_GetFinalBenchmarkString", International.GetText("mkxminer_precise_try"));
                return International.GetText("mkxminer_precise_try");
            }

            return base.GetFinalBenchmarkString();
        }

        protected override void BenchmarkThreadRoutine(object CommandLine) {
            Thread.Sleep(ConfigManager.GeneralConfig.MinerRestartDelayMS * 3); // increase wait for sgminer

            BenchmarkSignalQuit = false;
            BenchmarkSignalHanged = false;
            BenchmarkSignalFinnished = false;
            BenchmarkException = null;

            try {
                Helpers.ConsolePrint("BENCHMARK", "Benchmark starts");
                BenchmarkHandle = BenchmarkStartProcess((string)CommandLine);
                BenchmarkThreadRoutineStartSettup();
                // wait a little longer then the benchmark routine if exit false throw
                //var timeoutTime = BenchmarkTimeoutInSeconds(BenchmarkTimeInSeconds);
                //var exitSucces = BenchmarkHandle.WaitForExit(timeoutTime * 1000);
                // don't use wait for it breaks everything
                BenchmarkProcessStatus = BenchmarkProcessStatus.Running;
                while(true) {
                    string outdata = BenchmarkHandle.StandardOutput.ReadLine();
                    BenchmarkOutputErrorDataReceivedImpl(outdata);
                    // terminate process situations
                    if (BenchmarkSignalQuit
                        || BenchmarkSignalFinnished
                        || BenchmarkSignalHanged
                        || BenchmarkSignalTimedout
                        || BenchmarkException != null) {
                        //EndBenchmarkProcces();
                        // this is safe in a benchmark
                        Killmkxminer();
                        if (BenchmarkSignalTimedout) {
                            throw new Exception("Benchmark timedout");
                        }
                        if (BenchmarkException != null) {
                            throw BenchmarkException;
                        }
                        if (BenchmarkSignalQuit) {
                            throw new Exception("Termined by user request");
                        }
                        if (BenchmarkSignalHanged) {
                            throw new Exception("mkxminer is not responding");
                        }
                        if (BenchmarkSignalFinnished) {
                            break;
                        }
                    } else {
                        // wait a second reduce CPU load
                        Thread.Sleep(1000);
                    }
                }
            } catch (Exception ex) {
                BenchmarkThreadRoutineCatch(ex);
            } finally {
                BenchmarkThreadRoutineFinish();
            }
        }

        #endregion // Decoupled benchmarking routines

        // TODO _currentMinerReadStatus
        public override async Task<ApiData> GetSummaryAsync() {
            string resp;
            ApiData ad = new ApiData(MiningSetup.CurrentAlgorithmType);

            resp = await GetApiDataAsync(ApiPort, "summary");
            if (resp == null) {
                CurrentMinerReadStatus = MinerApiReadStatus.NONE;
                return null;
            }
            //// sgminer debug log
            //Helpers.ConsolePrint("sgminer-DEBUG_resp", resp);

            try {
                // Checks if all the GPUs are Alive first
                string resp2 = await GetApiDataAsync(ApiPort, "devs");
                if (resp2 == null) {
                    CurrentMinerReadStatus = MinerApiReadStatus.NONE;
                    return null;
                }
                //// sgminer debug log
                //Helpers.ConsolePrint("sgminer-DEBUG_resp2", resp2);

                string[] checkGPUStatus = resp2.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 1; i < checkGPUStatus.Length - 1; i++) {
                    if (checkGPUStatus[i].Contains("Enabled=Y") && !checkGPUStatus[i].Contains("Status=Alive")) {
                        Helpers.ConsolePrint(MinerTag(), ProcessTag() + " GPU " + i + ": Sick/Dead/NoStart/Initialising/Disabled/Rejecting/Unknown");
                        CurrentMinerReadStatus = MinerApiReadStatus.WAIT;
                        return null;
                    }
                }

                string[] resps = resp.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                if (resps[1].Contains("SUMMARY")) {
                    string[] data = resps[1].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    // Get miner's current total speed
                    string[] speed = data[4].Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    // Get miner's current total MH
                    double total_mh = Double.Parse(data[18].Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries)[1], new CultureInfo("en-US"));

                    ad.Speed = Double.Parse(speed[1]) * 1000;

                    if (total_mh <= PreviousTotalMH) {
                        Helpers.ConsolePrint(MinerTag(), ProcessTag() + " mkxminer might be stuck as no new hashes are being produced");
                        Helpers.ConsolePrint(MinerTag(), ProcessTag() + " Prev Total MH: " + PreviousTotalMH + " .. Current Total MH: " + total_mh);
                        CurrentMinerReadStatus = MinerApiReadStatus.NONE;
                        return null;
                    }

                    PreviousTotalMH = total_mh;
                } else {
                    ad.Speed = 0;
                }
            } catch {
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
