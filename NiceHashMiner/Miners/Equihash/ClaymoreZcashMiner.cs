using Newtonsoft.Json;
using NiceHashMiner.Configs;
using NiceHashMiner.Enums;
using NiceHashMiner.Miners.Grouping;
using NiceHashMiner.Miners.Parsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NiceHashMiner.Miners {
    public class ClaymoreZcashMiner : ClaymoreBaseMiner {

        const string _LOOK_FOR_START = "ZEC - Total Speed:";
        public ClaymoreZcashMiner()
            : base("ClaymoreZcashMiner", _LOOK_FOR_START) {
                ignoreZero = true;
        }

        protected override double DevFee() {
            return 2.0;
        }

        
        public override void Start(string url, string btcAdress, string worker) {
            string username = GetUsername(btcAdress, worker);
            LastCommandLine = " " + GetDevicesCommandString() + " -mport 127.0.0.1:" + APIPort + " -zpool " + url + " -zwal " + username + " -zpsw x -dbg -1 -ftime 10 -retrydelay 5";
            String epools = String.Format("POOL: stratum+ssl://equihash.usa.nicehash.com:3357, WALLET: {1}, PSW: x, ALLPOOLS: 0", url, username, APIPort) + "\n"
              + String.Format("POOL: stratum+ssl://equihash.hk.nicehash.com:3357, WALLET: {1}, PSW: x, ALLPOOLS: 0", url, username, APIPort) + "\n"
              + String.Format("POOL: stratum+ssl://equihash.jp.nicehash.com:3357, WALLET: {1}, PSW: x, ALLPOOLS: 0", url, username, APIPort) + "\n"
              + String.Format("POOL: stratum+ssl://equihash.in.nicehash.com:3357, WALLET: {1}, PSW: x, ALLPOOLS: 0", url, username, APIPort) + "\n"
              + String.Format("POOL: stratum+ssl://equihash.br.nicehash.com:3357, WALLET: {1}, PSW: x, ALLPOOLS: 0", url, username, APIPort) + "\n"
              + String.Format("POOL: stratum+ssl://equihash.eu.nicehash.com:3357, WALLET: {1}, PSW: x, ALLPOOLS: 0", url, username, APIPort) + "\n";

            FileStream fs = new FileStream("bin_3rdparty\\claymore_zcash\\epools.txt", FileMode.Create, FileAccess.Write);
            StreamWriter w = new StreamWriter(fs);
            w.WriteAsync(epools);
            w.Flush();
            w.Close();
            ProcessHandle = _Start();
        }

        // benchmark stuff
        protected override string BenchmarkCreateCommandLine(Algorithm algorithm, int time) {
            benchmarkTimeWait = time; // 3 times faster than sgminer
            string username = GetUsername(Globals.DemoUser, ConfigManager.GeneralConfig.WorkerName.Trim());
            string url = "equihash.eu.nicehash.com:3357";
            string ret = " " + GetDevicesCommandString() + " -mport 127.0.0.1:" + APIPort + " -zpool " + url + " -zwal " + username + " -zpsw x -dbg 0";

          //  string ret =  " -mport 127.0.0.1:" + APIPort + " -benchmark 1 " + GetDevicesCommandString();
            return ret;
        }
    }
}
