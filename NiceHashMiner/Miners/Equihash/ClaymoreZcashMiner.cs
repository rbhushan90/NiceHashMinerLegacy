using NiceHashMiner.Algorithms;
using NiceHashMiner.Configs;
using System;
using System.IO;

namespace NiceHashMiner.Miners
{
    public class ClaymoreZcashMiner : ClaymoreBaseMiner
    {
        public ClaymoreZcashMiner()
            : base("ClaymoreZcashMiner")
        {
            IgnoreZero = true;
            LookForStart = "zec - total speed:";
            DevFee = 2.0;
        }

        public override void Start(string url, string btcAdress, string worker)
        {
            var username = GetUsername(btcAdress, worker);
            string epools;
            //            LastCommandLine =
            //                $" {GetDevicesCommandString()} -mport 127.0.0.1:-{ApiPort} -xpool {url} -xwal {username} -xpsw x -dbg -1 -pow7 1";
            LastCommandLine = " " + GetDevicesCommandString() + " -mport -" + ApiPort + " -zpool " + url +
              " -zwal " + username + " -zpsw x -dbg -1 -ftime 10 -retrydelay 5 ";

            epools = String.Format("POOL: stratum+ssl://equihash.usa.nicehash.com:33353, WALLET: {1}, PSW: x, ALLPOOLS: 0", url, username, ApiPort) + "\n"
           + String.Format("POOL: stratum+ssl://equihash.hk.nicehash.com:33363, WALLET: {1}, PSW: x, ALLPOOLS: 0", url, username, ApiPort) + "\n"
           + String.Format("POOL: stratum+ssl://equihash.jp.nicehash.com:33363, WALLET: {1}, PSW: x, ALLPOOLS: 0", url, username, ApiPort) + "\n"
           + String.Format("POOL: stratum+ssl://equihash.in.nicehash.com:33363, WALLET: {1}, PSW: x, ALLPOOLS: 0", url, username, ApiPort) + "\n"
           + String.Format("POOL: stratum+ssl://equihash.br.nicehash.com:33363, WALLET: {1}, PSW: x, ALLPOOLS: 0", url, username, ApiPort) + "\n"
           + String.Format("POOL: stratum+ssl://equihash.eu.nicehash.com:33363, WALLET: {1}, PSW: x, ALLPOOLS: 0", url, username, ApiPort) + "\n";

            FileStream fs = new FileStream("bin_3rdparty\\claymore_zcash\\epools.txt", FileMode.Create, FileAccess.Write);
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

            return $" {GetDevicesCommandString()} -mport -{ApiPort} -zpool {url} -zwal {username} -zpsw x -logfile {GetLogFileName()} ";
        }
    }
}
