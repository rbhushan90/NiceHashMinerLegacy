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
using System.Linq;

namespace NiceHashMiner.Miners
{
    public class lyclMiner : Miner
    {
        private int st = 0;
        private double speed = 0.0d;
        private string hashspeed = "";

        public lyclMiner() : base("lyclMiner") { }

        bool benchmarkException {
            get {
                return MiningSetup.MinerPath == MinerPaths.Data.lyclMiner;
            }
        }
        
        protected override int GetMaxCooldownTimeInMilliseconds() {
            return 60 * 1000 * 12;
        }
        

        public override void Start(string url, string btcAdress, string worker)
        {
            if (!IsInit) {
                Helpers.ConsolePrint(MinerTag(), "MiningSetup is not initialized exiting Start()");
                return;
            }
            string username = GetUsername(btcAdress, worker);

            IsApiReadException = true; //** in miner 
                                       
            GenerateConfig("");
            Thread.Sleep(100);
            var conf = "";
            FileStream fs = new FileStream("bin\\lyclMiner\\lyclMiner.conf", FileMode.OpenOrCreate, FileAccess.Read);
            StreamReader w = new StreamReader(fs);
            conf = w.ReadToEnd();
            w.Close();
            string[] ids = MiningSetup.MiningPairs.Select(mPair => mPair.Device.IDByBus.ToString()).ToArray();

            Thread.Sleep(100);
            url = Globals.GetLocationUrl(AlgorithmType.Lyra2REv2, Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], NhmConectionType.STRATUM_TCP);
            conf = conf.Replace("stratum+tcp://example.com:port", url);
            conf = conf.Replace("user", username);
            string newconf = "";
            string[] textArray = conf.Split('\n');
            string[] worksize = ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.AMD).Replace("worksize=","").Split(',');

            Array.Reverse(worksize);
            int k = 0;
            for (int i = 1; i < textArray.Length; i++)
            {
                string str = textArray[i].ToString();
                if (!str.Contains("DeviceIndex"))
                {
                    newconf = newconf + str + "\n";
                }
                if (str.Contains("DeviceIndex = "))
                {
                    int st1 = str.IndexOf("DeviceIndex = ");
                    int end1 = str.IndexOf("BinaryFormat");
                    string dev = str.Substring(st1 + 15, end1 - st1 -17);

                    if (Array.IndexOf(ids, dev) < 0)
                    {
                        str = str.Replace("DeviceIndex = \"", "DeviceIndex = \"-255 ");
                        newconf = newconf + str + "\n";
                    } else
                    {
                        /*
                        int st2 = str.IndexOf("WorkSize = ");
                        int end2 = str.IndexOf(">");
                        string work = str.Substring(st2 + 12, end2 - st2 - 12 -1 );
                        str = str.Replace(work, worksize[k].Trim());
                        */
                        newconf = newconf + str + "\n";
                        k++;
                    }
                }
            }
            FileStream fs2 = new FileStream("bin\\lyclMiner\\lyclMinerNHML.conf", FileMode.Create, FileAccess.ReadWrite);
            StreamWriter w2 = new StreamWriter(fs2);
            w2.Write(newconf);
            w2.Flush();
            w2.Close();
            LastCommandLine = " lyclMinerNHML.conf";
            
            ProcessHandle = _Start();
        }

        protected override void _Stop(MinerStopType willswitch) {
            Stop_cpu_ccminer_sgminer_nheqminer(willswitch);
        }

        // new decoupled benchmarking routines
        #region Decoupled benchmarking routines

            protected void GenerateConfig(string configfilename)
        {
            var benchmarkconfigHandle = new Process
            {
                StartInfo =
                {
                    FileName = MiningSetup.MinerPath
                }
            };

            {
                BenchmarkProcessPath = benchmarkconfigHandle.StartInfo.FileName;
                Helpers.ConsolePrint(MinerTag(), "Using miner: " + benchmarkconfigHandle.StartInfo.FileName);
                benchmarkconfigHandle.StartInfo.WorkingDirectory = WorkingDirectory;
            }
            if (MinersSettingsManager.MinerSystemVariables.ContainsKey(Path))
            {
                foreach (var kvp in MinersSettingsManager.MinerSystemVariables[Path])
                {
                    var envName = kvp.Key;
                    var envValue = kvp.Value;
                    benchmarkconfigHandle.StartInfo.EnvironmentVariables[envName] = envValue;
                }
            }
            if (File.Exists("bin\\lyclMiner\\lyclMiner.conf" + configfilename))
                File.Delete("bin\\lyclMiner\\lyclMiner.conf" + configfilename);

            Thread.Sleep(250);
            benchmarkconfigHandle.StartInfo.Arguments = " -gr lyclMiner.conf" + configfilename;
            benchmarkconfigHandle.StartInfo.UseShellExecute = false;
            benchmarkconfigHandle.StartInfo.RedirectStandardError = true;
            benchmarkconfigHandle.StartInfo.RedirectStandardOutput = false;
            benchmarkconfigHandle.StartInfo.CreateNoWindow = true;
            Thread.Sleep(250);
            benchmarkconfigHandle.Start();
            try
            {
                if (!benchmarkconfigHandle.WaitForExit(2 * 1000))
                {
                    benchmarkconfigHandle.Kill(); 
                    benchmarkconfigHandle.WaitForExit(2 * 1000);
                    benchmarkconfigHandle.Close();
                }
            }
            catch { }
            Thread.Sleep(50);
        }
        protected override string GetDevicesCommandString()
        {
            string extraParams = ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.AMD);
            string deviceStringCommand = "";
            List<string> ids = new List<string>();

            foreach (var mPair in MiningSetup.MiningPairs.OrderByDescending(pair => pair.Device.IDByBus))
            {
                var id = mPair.Device.IDByBus;
                ids.Add(id.ToString());
            }
            deviceStringCommand += String.Join("", ids);

            return deviceStringCommand;
        }

        protected override string BenchmarkCreateCommandLine(Algorithm algorithm, int time) {

            string configfilename = GetLogFileName();
            GenerateConfig(configfilename);

              Thread.Sleep(1000);

            Helpers.ConsolePrint("lyclMiner", "Start benchmark after config is generated");

            var conf = "";            
            FileStream fs = new FileStream("bin\\lyclMiner\\lyclMiner.conf"+ configfilename, FileMode.OpenOrCreate, FileAccess.Read);
            StreamReader w = new StreamReader(fs);
            conf = w.ReadToEnd();
            w.Close();
           
            Thread.Sleep(500);
            var url = Globals.GetLocationUrl(AlgorithmType.Lyra2REv2, Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], NhmConectionType.STRATUM_TCP);
            var username = Globals.DemoUser;
            conf = conf.Replace("stratum+tcp://example.com:port", url);
            conf = conf.Replace("user", username);
            
            string worksizebench = ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.AMD).Replace("worksize=", "").Trim();
            string newconf = "";
            string[] textArray = conf.Split('\n');

            for (int i = 1; i < textArray.Length; i++)
            {
                string str = textArray[i].ToString();
                if (!str.Contains("DeviceIndex"))
                {
                    newconf = newconf + str + "\n";
                }

                if (str.Contains("DeviceIndex") & !str.Contains("DeviceIndex = \"" + GetDevicesCommandString()+ "\""))
                {
                    str = str.Replace("DeviceIndex = \"", "DeviceIndex = \"-255 ");
                    newconf = newconf + str + "\n";
                }

                if (str.Contains("DeviceIndex") & str.Contains("DeviceIndex = \"" + GetDevicesCommandString() + "\""))
                {
                    if (worksizebench.Length != 0)
                    {
                        int st2 = str.IndexOf("WorkSize = ");
                        int end2 = str.IndexOf(">");
                        string work = str.Substring(st2 + 12, end2 - st2 - 12 - 1);
                        str = str.Replace(work, worksizebench.Trim());
                    }
                    newconf = newconf + str + "\n";
                }
            }

                Helpers.ConsolePrint("lyclMiner.confNEW:", newconf);
            FileStream fs2 = new FileStream("bin\\lyclMiner\\"+ configfilename, FileMode.Create, FileAccess.ReadWrite);
            StreamWriter w2 = new StreamWriter(fs2);
            w2.Write(newconf);
            w2.Flush();
            w2.Close();

            return " " + configfilename;

        }

        protected override bool BenchmarkParseLine(string outdata) {
            Helpers.ConsolePrint(MinerTag(), outdata);

            if (benchmarkException)
            {
                if (outdata.Contains("Accepted"))
                {
                  
                    int end = outdata.IndexOf("H/s");
                    if (outdata.Contains("MH,")) st = outdata.IndexOf("MH,");
                    if (outdata.Contains("kH,")) st = outdata.IndexOf("kH,");
                    if (outdata.Contains("GH,")) st = outdata.IndexOf("GH,");
                    hashspeed = outdata.Substring(st + 3, end - st - 4);
                    speed = Double.Parse(hashspeed, CultureInfo.InvariantCulture);
                    if (outdata.Contains("MH/s"))
                    {
                        BenchmarkAlgorithm.BenchmarkSpeed = speed * 1000000;
                    }
                    if (outdata.Contains("kH/s"))
                    {
                        BenchmarkAlgorithm.BenchmarkSpeed = speed * 1000;
                    }
                    BenchmarkSignalFinnished = true;
                    return true;
                }

            }
            return false;
        }

        protected override void BenchmarkOutputErrorDataReceivedImpl(string outdata) {
            CheckOutdata(outdata);
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
            /*
            var totalSpeed = MiningSetup.MiningPairs
                .Select(miningPair =>
                    miningPair.Device.GetAlgorithm(MinerBaseType.lyclMiner, AlgorithmType.Lyra2REv2, AlgorithmType.NONE))
                .Where(algo => algo != null).Sum(algo => algo.BenchmarkSpeed);
                */
            var totalSpeed = 0.0d;
            foreach (var miningPair in MiningSetup.MiningPairs)
            {
                var algo = miningPair.Device.GetAlgorithm(MinerBaseType.lyclMiner, AlgorithmType.Lyra2REv2, AlgorithmType.NONE);
                if (algo != null)
                {
                    totalSpeed += algo.BenchmarkSpeed;
                }
            }

            var zenemyData = new ApiData(MiningSetup.CurrentAlgorithmType)
            {
                Speed = totalSpeed
            };
            CurrentMinerReadStatus = MinerApiReadStatus.GOT_READ;
            // check if speed zero
            if (zenemyData.Speed == 0) CurrentMinerReadStatus = MinerApiReadStatus.READ_SPEED_ZERO;
            return zenemyData;
        }
    }
}
