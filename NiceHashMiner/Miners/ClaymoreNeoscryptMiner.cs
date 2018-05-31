using NiceHashMiner.Configs;
using NiceHashMiner.Miners.Grouping;
using NiceHashMiner.Miners.Parsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NiceHashMiner.Algorithms;
using NiceHashMinerLegacy.Common.Enums;

namespace NiceHashMiner.Miners {
    public class ClaymoreNeoscryptMiner : ClaymoreBaseMiner
    {
        public ClaymoreNeoscryptMiner()
            : base("ClaymoreNeoscryptMiner") {
            LookForStart = "ns - total speed:";
        }

       /*
        protected override string GetDevicesCommandString() {

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
*/
        public override void Start(string url, string btcAdress, string worker) {
            string username = GetUsername(btcAdress, worker);
            url = Globals.GetLocationUrl(AlgorithmType.NeoScrypt, Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], NhmConectionType.STRATUM_TCP);
                LastCommandLine = " " + GetDevicesCommandString() + " -mport -" + ApiPort + " -pool " + url +
                                  " -wal " + username + " -psw x -dbg -1 -ftime 10 -retrydelay 5";

            String epools = String.Format("POOL: stratum+tcp://neoscrypt.usa.nicehash.com:3341, WALLET: {1}, PSW: x", url, username, ApiPort) + "\n"
               + String.Format("POOL: stratum+tcp://neoscrypt.hk.nicehash.com:3341, WALLET: {1}, PSW: x", url, username, ApiPort) + "\n"
               + String.Format("POOL: stratum+tcp://neoscrypt.jp.nicehash.com:3341, WALLET: {1}, PSW: x", url, username, ApiPort) + "\n"
               + String.Format("POOL: stratum+tcp://neoscrypt.in.nicehash.com:3341, WALLET: {1}, PSW: x", url, username, ApiPort) + "\n"
               + String.Format("POOL: stratum+tcp://neoscrypt.br.nicehash.com:3341, WALLET: {1}, PSW: x", url, username, ApiPort) + "\n"
               + String.Format("POOL: stratum+tcp://neoscrypt.eu.nicehash.com:3341, WALLET: {1}, PSW: x", url, username, ApiPort) + "\n";

            FileStream fs = new FileStream("bin_3rdparty\\claymore_neoscrypt\\pools.txt", FileMode.Create, FileAccess.Write);
            StreamWriter w = new StreamWriter(fs);
            w.WriteAsync(epools);
            w.Flush();
            w.Close();

            ProcessHandle = _Start();
        }

        // benchmark stuff

        protected override string BenchmarkCreateCommandLine(Algorithm algorithm, int time) {
            BenchmarkTimeWait = time; 

            // network workaround
            string url = Globals.GetLocationUrl(algorithm.NiceHashID, Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], NhmConectionType.STRATUM_TCP);
            // demo for benchmark
            string username = Globals.DemoUser;
            if (ConfigManager.GeneralConfig.WorkerName.Length > 0)
                username += "." + ConfigManager.GeneralConfig.WorkerName.Trim();
            /*
            string ret;
                ret = " " + GetDevicesCommandString() + " -mport -" + ApiPort + " -pool " + url + " -wal " +
                             username + " -psw x";
            return ret;
            */
            return $" {GetDevicesCommandString()} -mport -{ApiPort} -pool {url} -wal {username} -psw x -logfile {GetLogFileName()}";
        }

    }
}
