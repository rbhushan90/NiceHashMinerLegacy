using NiceHashMiner.Configs;
using NiceHashMiner.Enums;
using NiceHashMiner.Miners.Grouping;
using NiceHashMiner.Miners.Parsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NiceHashMiner.Miners {
    public class ClaymoreNeoscryptMiner : ClaymoreBaseMiner
    {

        private bool isOld;

        const string _LOOK_FOR_START = "NS - Total Speed:";
        const string _LOOK_FOR_START_OLD = "hashrate =";
        public ClaymoreNeoscryptMiner()
            : base("ClaymoreNeoscryptMiner", _LOOK_FOR_START) {
        }

        protected override double DevFee() {
            return 5.0;
        }
        
        protected override string GetDevicesCommandString() {
            if (!isOld) return base.GetDevicesCommandString();

            string extraParams = ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.AMD);
            string deviceStringCommand = "";
            List<string> ids = new List<string>();
            foreach (var mPair in MiningSetup.MiningPairs) {
                var id = mPair.Device.ID;
                ids.Add(id.ToString());
            }
            deviceStringCommand += String.Join("", ids);

            return deviceStringCommand + extraParams;
        }

        public override void Start(string url, string btcAdress, string worker) {
            string username = GetUsername(btcAdress, worker);
            url = Globals.GetLocationURL(AlgorithmType.NeoScrypt, Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], NHMConectionType.STRATUM_TCP);
                LastCommandLine = " " + GetDevicesCommandString() + " -mport -" + APIPort + " -pool " + url +
                                  " -wal " + username + " -psw x -dbg -1 -ftime 10 -retrydelay 5";

            String epools = String.Format("POOL: stratum+tcp://neoscrypt.usa.nicehash.com:3341, WALLET: {1}, PSW: x", url, username, APIPort) + "\n"
               + String.Format("POOL: stratum+tcp://neoscrypt.hk.nicehash.com:3341, WALLET: {1}, PSW: x", url, username, APIPort) + "\n"
               + String.Format("POOL: stratum+tcp://neoscrypt.jp.nicehash.com:3341, WALLET: {1}, PSW: x", url, username, APIPort) + "\n"
               + String.Format("POOL: stratum+tcp://neoscrypt.in.nicehash.com:3341, WALLET: {1}, PSW: x", url, username, APIPort) + "\n"
               + String.Format("POOL: stratum+tcp://neoscrypt.br.nicehash.com:3341, WALLET: {1}, PSW: x", url, username, APIPort) + "\n"
               + String.Format("POOL: stratum+tcp://neoscrypt.eu.nicehash.com:3341, WALLET: {1}, PSW: x", url, username, APIPort) + "\n";

            FileStream fs = new FileStream("bin_3rdparty\\claymore_neoscrypt\\pools.txt", FileMode.Create, FileAccess.Write);
            StreamWriter w = new StreamWriter(fs);
            w.WriteAsync(epools);
            w.Flush();
            w.Close();

            ProcessHandle = _Start();
        }

        // benchmark stuff

        protected override string BenchmarkCreateCommandLine(Algorithm algorithm, int time) {
            benchmarkTimeWait = time; // Takes longer as of v10

            // network workaround
            string url = Globals.GetLocationURL(algorithm.NiceHashID, Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], NHMConectionType.STRATUM_TCP);
            // demo for benchmark
            string username = Globals.DemoUser;
            if (ConfigManager.GeneralConfig.WorkerName.Length > 0)
                username += "." + ConfigManager.GeneralConfig.WorkerName.Trim();
            string ret;
                ret = " " + GetDevicesCommandString() + " -mport -" + APIPort + " -pool " + url + " -wal " +
                             username + " -psw x";
            return ret;
        }

    }
}
