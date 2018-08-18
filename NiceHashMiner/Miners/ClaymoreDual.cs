using NiceHashMiner.Configs;
using System;
using NiceHashMiner.Algorithms;
using NiceHashMinerLegacy.Common.Enums;
using System.IO;
using System.Threading;

namespace NiceHashMiner.Miners
{
    public class ClaymoreDual : ClaymoreBaseMiner
    {
        public ClaymoreDual(AlgorithmType secondaryAlgorithmType)
            : base("ClaymoreDual")
        {
            IgnoreZero = true;
            ApiReadMult = 1000;
            ConectionType = NhmConectionType.STRATUM_TCP;
            SecondaryAlgorithmType = secondaryAlgorithmType;

            LookForStart = "eth - total speed:";
            SecondaryLookForStart = SecondaryShortName() + " - total speed:";
            DevFee = IsDual() ? 1.5 : 1.0;

            IsMultiType = true;
        }

        // the short form the miner uses for secondary algo in cmd line and log
        public string SecondaryShortName()
        {
            switch (SecondaryAlgorithmType)
            {
                case AlgorithmType.Decred:
                    return "dcr";
                case AlgorithmType.Lbry:
                    return "lbc";
                case AlgorithmType.Pascal:
                    return "pasc";
                case AlgorithmType.Sia:
                    return "sc";
                case AlgorithmType.Blake2s:
                    return "b2s";
                case AlgorithmType.Keccak:
                    return "kc";
            }

            return "";
        }

        protected override int GetMaxCooldownTimeInMilliseconds()
        {
            return 90 * 1000; // 1.5 minute max, whole waiting time 75seconds
        }

        private string GetStartCommand(string url, string btcAdress, string worker)
        {
            var username = GetUsername(btcAdress, worker);
            // AlgorithmType alg = AlgorithmType.Lbry;
            var dual = AlgorithmType.NONE;
            string poolport = "3354";
            var dualModeParams = "";
            string epoolsFile = "";
            string dpoolsFile = "";

            foreach (var pair in MiningSetup.MiningPairs)
            {
                if (pair.Device.DeviceType == DeviceType.NVIDIA)
                {
                    epoolsFile = "epoolsNV" + GetLogFileName().Replace("_log", "");
                    dpoolsFile = "dpoolsNV" + GetLogFileName().Replace("_log", "");
                } else
                {
                    epoolsFile = "epoolsAMD" + GetLogFileName().Replace("_log", "");
                    dpoolsFile = "dpoolsAMD" + GetLogFileName().Replace("_log", "");
                }
            }

            if (File.Exists("bin_3rdparty\\claymore_dual\\epools.txt"))
                File.Delete("bin_3rdparty\\claymore_dual\\epools.txt");
            if (File.Exists("bin_3rdparty\\claymore_dual\\dpools.txt"))
                File.Delete("bin_3rdparty\\claymore_dual\\dpools.txt");

            if (File.Exists("bin_3rdparty\\claymore_dual\\"+ epoolsFile))
                File.Delete("bin_3rdparty\\claymore_dual\\"+ epoolsFile);
            if (File.Exists("bin_3rdparty\\claymore_dual\\" + dpoolsFile))
                File.Delete("bin_3rdparty\\claymore_dual\\" + dpoolsFile);

            Thread.Sleep(200);

            String epools = String.Format("POOL: daggerhashimoto.usa.nicehash.com:3353, WALLET: {1}, PSW: x, ESM: 3, ALLPOOLS: 1", url, username) + "\n"
               + String.Format("POOL: daggerhashimoto.hk.nicehash.com:3353, WALLET: {1}, PSW: x, ESM: 3, ALLPOOLS: 1", url, username) + "\n"
               + String.Format("POOL: daggerhashimoto.jp.nicehash.com:3353, WALLET: {1}, PSW: x, ESM: 3, ALLPOOLS: 1", url, username) + "\n"
               + String.Format("POOL: daggerhashimoto.in.nicehash.com:3353, WALLET: {1}, PSW: x, ESM: 3, ALLPOOLS: 1", url, username) + "\n"
               + String.Format("POOL: daggerhashimoto.br.nicehash.com:3353, WALLET: {1}, PSW: x, ESM: 3, ALLPOOLS: 1", url, username) + "\n"
               + String.Format("POOL: daggerhashimoto.eu.nicehash.com:3353, WALLET: {1}, PSW: x, ESM: 3, ALLPOOLS: 1", url, username) + "\n";
            try
            {
                FileStream fs = new FileStream("bin_3rdparty\\claymore_dual\\"+ epoolsFile, FileMode.Create, FileAccess.Write);
                StreamWriter w = new StreamWriter(fs);
                w.WriteAsync(epools);
                w.Flush();
                w.Close();
            }
            catch (Exception e)
            {
                Helpers.ConsolePrint("GetStartCommand", e.ToString());
            }

            bool istuned = false;

            foreach (var mPair in MiningSetup.MiningPairs)
            {
                if (mPair.Algorithm is DualAlgorithm algo && algo.TuningEnabled)
                {
                   // var intensity = algo.MostProfitableIntensity;
                   // if (intensity < 0) intensity = defaultIntensity;
                    istuned = true;
                }
            }


                Thread.Sleep(200);
            if (!IsDual())
            {
                // leave convenience param for non-dual entry
                foreach (var pair in MiningSetup.MiningPairs)
                {
                    if (!pair.CurrentExtraLaunchParameters.Contains("-dual=")) continue;
                    dual = AlgorithmType.NONE;
                    var coinP = "";
                    if (pair.CurrentExtraLaunchParameters.Contains("Decred"))
                    {
                        dual = AlgorithmType.Decred;
                        coinP = " -dcoin dcr ";
                    }

                    if (pair.CurrentExtraLaunchParameters.Contains("Siacoin"))
                    {
                        dual = AlgorithmType.Sia;
                        coinP = " -dcoin sc";
                    }

                    if (pair.CurrentExtraLaunchParameters.Contains("Lbry"))
                    {
                        dual = AlgorithmType.Lbry;
                        coinP = " -dcoin lbc ";
                    }

                    if (pair.CurrentExtraLaunchParameters.Contains("Pascal"))
                    {
                        dual = AlgorithmType.Pascal;
                        coinP = " -dcoin pasc ";
                    }

                    if (dual != AlgorithmType.NONE)
                    {
                        var urlSecond = Globals.GetLocationUrl(dual,
                            Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation],
                            ConectionType);
                        dualModeParams = $" {coinP} -dpool {urlSecond} -dwal {username}";
                        break;
                    }
                }
                
            }
            else //dual
            {
                String dpools = "POOL: stratum+tcp://" + SecondaryAlgorithmType.ToString().ToLower() + ".usa.nicehash.com:" + poolport + String.Format(", WALLET: {0}, PSW: x", username) + "\n"
                 + "POOL: stratum+tcp://" + SecondaryAlgorithmType.ToString().ToLower() + ".hk.nicehash.com:" + poolport + String.Format(", WALLET: {0}, PSW: x", username) + "\n"
                 + "POOL: stratum+tcp://" + SecondaryAlgorithmType.ToString().ToLower() + ".jp.nicehash.com:" + poolport + String.Format(", WALLET: {0}, PSW: x", username) + "\n"
                 + "POOL: stratum+tcp://" + SecondaryAlgorithmType.ToString().ToLower() + ".in.nicehash.com:" + poolport + String.Format(", WALLET: {0}, PSW: x", username) + "\n"
                 + "POOL: stratum+tcp://" + SecondaryAlgorithmType.ToString().ToLower() + ".br.nicehash.com:" + poolport + String.Format(", WALLET: {0}, PSW: x", username) + "\n"
                 + "POOL: stratum+tcp://" + SecondaryAlgorithmType.ToString().ToLower() + ".eu.nicehash.com:" + poolport + String.Format(", WALLET: {0}, PSW: x", username) + "\n";
                try
                {
                    FileStream fs1 = new FileStream("bin_3rdparty\\claymore_dual\\"+dpoolsFile, FileMode.Create, FileAccess.Write);
                    StreamWriter w1 = new StreamWriter(fs1);
                    w1.WriteAsync(dpools);
                    w1.Flush();
                    w1.Close();
                    Thread.Sleep(200);
                }
                catch (Exception e)
                {
                    Helpers.ConsolePrint("GetStartCommand", e.ToString());
                }

                var urlSecond = Globals.GetLocationUrl(SecondaryAlgorithmType,
                    Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], ConectionType);
                dualModeParams = $" -dcoin {SecondaryShortName()} -dpool {urlSecond} -dwal {username} -dpsw x -dpoolsfile "+dpoolsFile;
            }


            
            string addParam;
            bool needdcri = true;
            bool isNvidia = false;
            var dcri = "-dcri 7";
            foreach (var pair in MiningSetup.MiningPairs)
            {
                if (pair.CurrentExtraLaunchParameters.Contains("-dcri"))
                {
                    needdcri = false;
                }


                if (pair.Algorithm is DualAlgorithm algo && algo.TuningEnabled && pair.CurrentExtraLaunchParameters.Contains("-dcri"))
                {
                    if (btcAdress == Globals.DemoUser)
                    {
                        algo.TuningEnabled = true;
                        Helpers.ConsolePrint("Tuning ENABLE ", "");
                    }
                    else
                    {
                        algo.TuningEnabled = false;
                        Helpers.ConsolePrint("Tuning DISABLE ", "");
                    }
                }

                if (pair.Device.DeviceType == DeviceType.NVIDIA)
                {
                    isNvidia = true;
                } else
                {
                    isNvidia = false;
                }
            }




            if (SecondaryAlgorithmType == AlgorithmType.Blake2s & needdcri & !istuned)
            {
                if (isNvidia)
                {
                    dcri = "-dcri 40";
                }
                else
                {
                    dcri = "-dcri 30";
                }
                addParam = " "
                    + GetDevicesCommandString()
                    + String.Format("  -epool {0} -ewal {1} -mport 127.0.0.1:-{2} -esm 3 -epsw x -allpools 1 -ftime 10 -retrydelay 5 " + dcri + " ", url, username, ApiPort)
                    + dualModeParams;
            }
            else if (SecondaryAlgorithmType == AlgorithmType.Keccak & needdcri & !istuned)
            {
                if (isNvidia)
                {
                    dcri = "-dcri 20";
                } else
                {
                    dcri = "-dcri 7";
                }

                addParam = " "
                                    + GetDevicesCommandString()
                                    + String.Format("  -epool {0} -ewal {1} -mport 127.0.0.1:-{2} -esm 3 -epsw x -allpools 1 -ftime 10 -retrydelay 5 " +dcri+ " ", url, username, ApiPort)
                                    + dualModeParams;
            }
            else
            {

            addParam = " "
                                    + GetDevicesCommandString()
                                    + String.Format("  -epool {0} -ewal {1} -mport 127.0.0.1:-{2} -esm 3 -epsw x -allpools 1 -ftime 10 -retrydelay 5", url, username, ApiPort)
                                    + dualModeParams;
            }
            return addParam + " -epoolsfile "+epoolsFile;
/*
            return " "
                   + GetDevicesCommandString()
                   + $"  -epool {url} -ewal {username} -mport 127.0.0.1:-{ApiPort} -esm 3 -epsw x -allpools 1"
                   + dualModeParams;
*/
        }

        public override void Start(string url, string btcAdress, string worker)
        {
            var strdual = "";
            // Update to most profitable intensity
            foreach (var mPair in MiningSetup.MiningPairs)
            {
                if (mPair.Algorithm is DualAlgorithm algo && algo.TuningEnabled)
                {
                    var intensity = algo.MostProfitableIntensity;
                    if (intensity < 0) intensity = defaultIntensity;
                    algo.CurrentIntensity = intensity;
                }
            }

            LastCommandLine = GetStartCommand(url, btcAdress, worker) + " -dbg -1";
            /*
            if (IsDual())
            {
                strdual = "DUAL";
            }

            foreach (var pair in MiningSetup.MiningPairs)
            {
                if (pair.Device.DeviceType == DeviceType.NVIDIA)
                {
                    RunCMDBeforeMining("NVIDIA" + " " + strdual, true);
                }
                else if (pair.Device.DeviceType == DeviceType.AMD)
                {
                    RunCMDBeforeMining("AMD" + " " + strdual, true);
                }
                else if (pair.Device.DeviceType == DeviceType.CPU)
                {
                    RunCMDBeforeMining("CPU", true);
                }
            }
*/
            ProcessHandle = _Start();
        }

        protected override string DeviceCommand(int amdCount = 1)
        {
            // If no AMD cards loaded, instruct CD to only regard NV cards for indexing
            // This will allow proper indexing if AMD GPUs or APUs are present in the system but detection disabled
            var ret = (amdCount == 0) ? " -platform 2" : "";
            return ret + base.DeviceCommand(amdCount);
        }

        // benchmark stuff

        protected override string BenchmarkCreateCommandLine(Algorithm algorithm, int time)
        {
            // network stub
            var url = GetServiceUrl(algorithm.NiceHashID);
            // demo for benchmark
            var ret = GetStartCommand(url, Globals.DemoUser, ConfigManager.GeneralConfig.WorkerName.Trim())
                         + " -logfile " + GetLogFileName();
            // local benhcmark
            /*
            if (!IsDual())
            {
                BenchmarkTimeWait = time;
                return ret + "  -benchmark 1"; // benchmark 1 does not output secondary speeds
            }
            */
            // dual seems to stop mining after this time if redirect output is true
            BenchmarkTimeWait = Math.Max(60, Math.Min(120, time * 3));
            return ret;
        }
    }
}
