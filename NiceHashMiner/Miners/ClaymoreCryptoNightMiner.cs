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
using System.Linq;


namespace NiceHashMiner.Miners
{
    public class ClaymoreCryptoNightMiner : ClaymoreBaseMiner
    {
        public ClaymoreCryptoNightMiner()
            : base("ClaymoreCryptoNightMiner")
        {
            LookForStart = "xmr - total speed:";
            // DevFee = 0
        }

        public override void Start(string url, string btcAdress, string worker)
        {
            var username = GetUsername(btcAdress, worker);
            String epools;
            //            LastCommandLine =
            //                $" {GetDevicesCommandString()} -mport 127.0.0.1:-{ApiPort} -xpool {url} -xwal {username} -xpsw x -dbg -1 -pow7 1";
            LastCommandLine = " " + GetDevicesCommandString() + " -mport -" + ApiPort + " -xpool " + url +
              " -xwal " + username + " -xpsw x -dbg -1 -ftime 10 -retrydelay 5 -pow7 1";

            epools = String.Format("POOL: stratum+ssl://cryptonightv7.usa.nicehash.com:33353, WALLET: {1}, PSW: x, ALLPOOLS: 0", url, username, ApiPort) + "\n"
           + String.Format("POOL: stratum+ssl://cryptonightv7.hk.nicehash.com:33363, WALLET: {1}, PSW: x, ALLPOOLS: 0", url, username, ApiPort) + "\n"
           + String.Format("POOL: stratum+ssl://cryptonightv7.jp.nicehash.com:33363, WALLET: {1}, PSW: x, ALLPOOLS: 0", url, username, ApiPort) + "\n"
           + String.Format("POOL: stratum+ssl://cryptonightv7.in.nicehash.com:33363, WALLET: {1}, PSW: x, ALLPOOLS: 0", url, username, ApiPort) + "\n"
           + String.Format("POOL: stratum+ssl://cryptonightv7.br.nicehash.com:33363, WALLET: {1}, PSW: x, ALLPOOLS: 0", url, username, ApiPort) + "\n"
           + String.Format("POOL: stratum+ssl://cryptonightv7.eu.nicehash.com:33363, WALLET: {1}, PSW: x, ALLPOOLS: 0", url, username, ApiPort) + "\n";

            FileStream fs = new FileStream("bin_3rdparty\\claymore_cryptonight\\epools.txt", FileMode.Create, FileAccess.Write);
            StreamWriter w = new StreamWriter(fs);
            w.WriteAsync(epools);
            w.Flush();
            w.Close();

            ProcessHandle = _Start();
        }

        // benchmark stuff

        protected override string BenchmarkCreateCommandLine(Algorithm algorithm, int time)
        {
            BenchmarkTimeWait = time; // Takes longer as of v10

            // network workaround
            var url = Globals.GetLocationUrl(algorithm.NiceHashID,
                Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation],
                ConectionType);
            // demo for benchmark
            var username = Globals.DemoUser;
            if (ConfigManager.GeneralConfig.WorkerName.Length > 0)
                username += "." + ConfigManager.GeneralConfig.WorkerName.Trim();

            return $" {GetDevicesCommandString()} -mport -{ApiPort} -xpool {url} -xwal {username} -xpsw x -logfile {GetLogFileName()} -pow7 1";
        }
    }
}
