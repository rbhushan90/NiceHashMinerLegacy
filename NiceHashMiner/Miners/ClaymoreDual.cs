using NiceHashMiner.Configs;
using System;
using NiceHashMiner.Algorithms;
using NiceHashMinerLegacy.Common.Enums;
using System.IO;

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
            AlgorithmType alg = AlgorithmType.Lbry;
            string poolport = "3354";
            var dualModeParams = "";
            if (!IsDual())
            {
                // leave convenience param for non-dual entry
                foreach (var pair in MiningSetup.MiningPairs)
                {
                    if (!pair.CurrentExtraLaunchParameters.Contains("-dual=")) continue;
                    var dual = AlgorithmType.NONE;
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
            else
            {
                var urlSecond = Globals.GetLocationUrl(SecondaryAlgorithmType,
                    Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], ConectionType);
                dualModeParams = $" -dcoin {SecondaryShortName()} -dpool {urlSecond} -dwal {username} -dpsw x";
            }

           String dpools = "POOL: stratum+tcp://" + alg.ToString().ToLower() + ".usa.nicehash.com:" + poolport + String.Format(", WALLET: {0}, PSW: x", username) + "\n"
            + "POOL: stratum+tcp://" + alg.ToString().ToLower() + ".hk.nicehash.com:" + poolport + String.Format(", WALLET: {0}, PSW: x", username) + "\n"
            + "POOL: stratum+tcp://" + alg.ToString().ToLower() + ".jp.nicehash.com:" + poolport + String.Format(", WALLET: {0}, PSW: x", username) + "\n"
            + "POOL: stratum+tcp://" + alg.ToString().ToLower() + ".in.nicehash.com:" + poolport + String.Format(", WALLET: {0}, PSW: x", username) + "\n"
            + "POOL: stratum+tcp://" + alg.ToString().ToLower() + ".br.nicehash.com:" + poolport + String.Format(", WALLET: {0}, PSW: x", username) + "\n"
            + "POOL: stratum+tcp://" + alg.ToString().ToLower() + ".eu.nicehash.com:" + poolport + String.Format(", WALLET: {0}, PSW: x", username) + "\n";

            FileStream fs1 = new FileStream("bin_3rdparty\\claymore_dual\\dpools.txt", FileMode.Create, FileAccess.Write);
            StreamWriter w1 = new StreamWriter(fs1);
            w1.WriteAsync(dpools);
            w1.Flush();
            w1.Close();

            String epools = String.Format("POOL: daggerhashimoto.usa.nicehash.com:3353, WALLET: {1}, PSW: x, ESM: 3, ALLPOOLS: 1", url, username) +"\n"
                + String.Format("POOL: daggerhashimoto.hk.nicehash.com:3353, WALLET: {1}, PSW: x, ESM: 3, ALLPOOLS: 1", url, username) + "\n"
                + String.Format("POOL: daggerhashimoto.jp.nicehash.com:3353, WALLET: {1}, PSW: x, ESM: 3, ALLPOOLS: 1", url, username) + "\n"
                + String.Format("POOL: daggerhashimoto.in.nicehash.com:3353, WALLET: {1}, PSW: x, ESM: 3, ALLPOOLS: 1", url, username) + "\n"
                + String.Format("POOL: daggerhashimoto.br.nicehash.com:3353, WALLET: {1}, PSW: x, ESM: 3, ALLPOOLS: 1", url, username) + "\n"
                + String.Format("POOL: daggerhashimoto.eu.nicehash.com:3353, WALLET: {1}, PSW: x, ESM: 3, ALLPOOLS: 1", url, username) + "\n";

            FileStream fs = new FileStream("bin_3rdparty\\claymore_dual\\epools.txt", FileMode.Create, FileAccess.Write);
            StreamWriter w = new StreamWriter(fs);
            w.WriteAsync(epools);
            w.Flush();
            w.Close();
            string addParam;
            bool needdcri = true;
            var dcri = "-dcri 7";
            foreach (var pair in MiningSetup.MiningPairs)
            {
                if (pair.CurrentExtraLaunchParameters.Contains("-dcri"))
                {
                    needdcri = false;
                }
                if (pair.Device.DeviceType == DeviceType.NVIDIA)
                {
                    dcri = "-dcri 20";
                }
            }

            if (SecondaryAlgorithmType == AlgorithmType.Blake2s && needdcri)
            {
                addParam = " "
                    + GetDevicesCommandString()
                    + String.Format("  -epool {0} -ewal {1} -mport 127.0.0.1:{2} -esm 3 -epsw x -allpools 1 -ftime 10 -retrydelay 5 " +dcri+" ", url, username, ApiPort)
                    + dualModeParams;
            }
            else if (SecondaryAlgorithmType == AlgorithmType.Keccak && needdcri)
            {
                addParam = " "
                                    + GetDevicesCommandString()
                                    + String.Format("  -epool {0} -ewal {1} -mport 127.0.0.1:{2} -esm 3 -epsw x -allpools 1 -ftime 10 -retrydelay 5 " +dcri+ " ", url, username, ApiPort)
                                    + dualModeParams;
            }
            else
            {

            addParam = " "
                                    + GetDevicesCommandString()
                                    + String.Format("  -epool {0} -ewal {1} -mport 127.0.0.1:{2} -esm 3 -epsw x -allpools 1 -ftime 10 -retrydelay 5", url, username, ApiPort)
                                    + dualModeParams;
            }
            return addParam;
/*
            return " "
                   + GetDevicesCommandString()
                   + $"  -epool {url} -ewal {username} -mport 127.0.0.1:-{ApiPort} -esm 3 -epsw x -allpools 1"
                   + dualModeParams;
*/
        }

        public override void Start(string url, string btcAdress, string worker)
        {
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
            if (!IsDual())
            {
                BenchmarkTimeWait = time;
                return ret + "  -benchmark 1"; // benchmark 1 does not output secondary speeds
            }

            // dual seems to stop mining after this time if redirect output is true
            BenchmarkTimeWait = Math.Max(60, Math.Min(120, time * 3));
            return ret;
        }
    }
}
