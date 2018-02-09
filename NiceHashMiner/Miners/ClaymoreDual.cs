using NiceHashMiner.Configs;
using NiceHashMiner.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NiceHashMiner.Miners {
    public class ClaymoreDual : ClaymoreBaseMiner {

        const string _LOOK_FOR_START = "ETH - Total Speed:";
        public ClaymoreDual(AlgorithmType secondaryAlgorithmType)
            : base("ClaymoreDual", _LOOK_FOR_START) {
            ignoreZero = true;
            api_read_mult = 1000;
            ConectionType = NHMConectionType.STRATUM_TCP;
            SecondaryAlgorithmType = secondaryAlgorithmType;
        }

        // eth-only: 1%
        // eth-dual-mine: 2%
        protected override double DevFee() {
            return IsDual() ? 2.0 : 1.0;
        }

        // the short form the miner uses for secondary algo in cmd line and log
        public string SecondaryShortName() {
            switch (SecondaryAlgorithmType) {
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

        protected override string SecondaryLookForStart() {
            return (SecondaryShortName() + " - Total Speed:").ToLower();
        }

        protected override int GET_MAX_CooldownTimeInMilliseconds() {
            return 90 * 1000; // 1.5 minute max, whole waiting time 75seconds
        }

        private string GetStartCommand(string url, string btcAdress, string worker) {
            string username = GetUsername(btcAdress, worker);
            AlgorithmType dual = AlgorithmType.Lbry; //on default
            string dualModeParams = "";
            AlgorithmType alg = dual;
            string poolport = "3354";
            if (!IsDual())
            {  // leave convenience param for non-dual entry
                foreach (var pair in MiningSetup.MiningPairs)
                {
                    if (pair.CurrentExtraLaunchParameters.Contains("-dual="))
                    {
                        dual = AlgorithmType.NONE;
                        string coinP = "";
                        if (pair.CurrentExtraLaunchParameters.Contains("Decred")) {
                            dual = AlgorithmType.Decred;
                            coinP = " -dcoin dcr ";
                        }
                        if (pair.CurrentExtraLaunchParameters.Contains("Siacoin")) {
                            dual = AlgorithmType.Sia;
                            coinP = " -dcoin sc";
                        }
                        if (pair.CurrentExtraLaunchParameters.Contains("Lbry"))  {
                            dual = AlgorithmType.Lbry;
                            coinP = " -dcoin lbc ";
                        }
                        if (pair.CurrentExtraLaunchParameters.Contains("Pascal")) {
                            dual = AlgorithmType.Pascal;
                            coinP = " -dcoin pasc ";
                        }
                        if (pair.CurrentExtraLaunchParameters.Contains("Blake2s"))
                        {
                            dual = AlgorithmType.Blake2s;
                            coinP = " -dcoin blake2s ";
                        }
                        if (pair.CurrentExtraLaunchParameters.Contains("Keccak"))
                        {
                            dual = AlgorithmType.Keccak;
                            coinP = " -dcoin keccak ";
                        }
                        if (dual != AlgorithmType.NONE)  {
                            string urlSecond = Globals.GetLocationURL(dual, Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], this.ConectionType);
                            dualModeParams = String.Format(" {0} -dpool {1} -dwal {2}", coinP, urlSecond, username);
                            break;
                        }
                        if (dual == AlgorithmType.Decred) { poolport = "3354"; }
                        if (dual == AlgorithmType.Lbry) { poolport = "3356"; }
                        if (dual == AlgorithmType.Pascal) { poolport = "3358"; }
                        if (dual == AlgorithmType.Sia) { poolport = "3360"; }
                        if (dual == AlgorithmType.Blake2s) { poolport = "3361"; }
                        if (dual == AlgorithmType.Keccak) { poolport = "3338"; }
                        alg = dual;
                    }
                }
            } else {
                string urlSecond = Globals.GetLocationURL(SecondaryAlgorithmType, Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], this.ConectionType);
                dualModeParams = String.Format(" -dcoin {0} -dpool {1} -dwal {2} -dpsw x", SecondaryShortName(), urlSecond, username);
                if (SecondaryAlgorithmType == AlgorithmType.Decred) { poolport = "3354"; }
                if (SecondaryAlgorithmType == AlgorithmType.Lbry) { poolport = "3356"; }
                if (SecondaryAlgorithmType == AlgorithmType.Pascal) { poolport = "3358"; }
                if (SecondaryAlgorithmType == AlgorithmType.Sia) { poolport = "3360"; }
                if (SecondaryAlgorithmType == AlgorithmType.Blake2s) { poolport = "3361"; }
                if (SecondaryAlgorithmType == AlgorithmType.Keccak) { poolport = "3338"; }
                alg = SecondaryAlgorithmType;
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
            if (SecondaryAlgorithmType == AlgorithmType.Blake2s)
            {
                addParam = " "
                    + GetDevicesCommandString()
                    + String.Format("  -epool {0} -ewal {1} -mport 127.0.0.1:{2} -esm 3 -epsw x -allpools 1 -ftime 10 -retrydelay 5 -dcri 60", url, username, APIPort)
                    + dualModeParams;
            }
            else if (SecondaryAlgorithmType == AlgorithmType.Keccak)
            {
                addParam = " "
                                    + GetDevicesCommandString()
                                    + String.Format("  -epool {0} -ewal {1} -mport 127.0.0.1:{2} -esm 3 -epsw x -allpools 1 -ftime 10 -retrydelay 5 -dcri 7", url, username, APIPort)
                                    + dualModeParams;
            }
            else
            {
                addParam = " "
                                    + GetDevicesCommandString()
                                    + String.Format("  -epool {0} -ewal {1} -mport 127.0.0.1:{2} -esm 3 -epsw x -allpools 1 -ftime 10 -retrydelay 5", url, username, APIPort)
                                    + dualModeParams;
            }
            //  if (DeviceType.NVIDIA == DeviceType && SecondaryAlgorithmType == AlgorithmType.Keccak)
           

            return addParam;
        }

        public override void Start(string url, string btcAdress, string worker) {
            string username = GetUsername(btcAdress, worker);
            LastCommandLine = GetStartCommand(url, btcAdress, worker) + " -dbg -1";
            ProcessHandle = _Start();
        }

        protected override string DeviceCommand(int amdCount = 1) {
            // If no AMD cards loaded, instruct CD to only regard NV cards for indexing
            // This will allow proper indexing if AMD GPUs or APUs are present in the system but detection disabled
            string ret = (amdCount == 0) ? " -platform 2" : "";
            return ret + base.DeviceCommand(amdCount);
        }

        // benchmark stuff

        protected override string BenchmarkCreateCommandLine(Algorithm algorithm, int time) {
            // network stub
            string url = Globals.GetLocationURL(algorithm.NiceHashID, Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], this.ConectionType);
            // demo for benchmark
            string ret = GetStartCommand(url, Globals.DemoUser, ConfigManager.GeneralConfig.WorkerName.Trim());
            // local benhcmark
            if (!IsDual()) {
                benchmarkTimeWait = time;
                return ret;
                //return ret + "  -benchmark 1";
            } else {
                benchmarkTimeWait = Math.Max(60, Math.Min(120, time*3));  // dual seems to stop mining after this time if redirect output is true
                return ret;  // benchmark 1 does not output secondary speeds
            }
        }

    }
}
