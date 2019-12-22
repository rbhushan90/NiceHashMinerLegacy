﻿/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
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

        public override void Start(string url, string btcAdress, string worker) {
            string username = GetUsername(btcAdress, worker);
            //url = Globals.GetLocationUrl(AlgorithmType.NeoScrypt, Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], NhmConectionType.STRATUM_TCP);
            url = url.Replace("stratum+ssl", "stratum+tcp").Replace("33341", "3341");
            LastCommandLine = " " + GetDevicesCommandString() + " -mport -" + ApiPort + " -pool " + url +
                                  " -wal " + username + " -psw x -dbg -1 -ftime 10 -retrydelay 5";
            string nhsuff = "";
            if (Configs.ConfigManager.GeneralConfig.NewPlatform)
            {
                nhsuff = Configs.ConfigManager.GeneralConfig.StratumSuff;
            }
            String epools = String.Format("POOL: stratum+tcp://neoscrypt.{0}{1}.nicehash.com:3341, WALLET: {2}, PSW: x", myServers[0, 0], nhsuff, username, ApiPort) + "\n"
               + String.Format("POOL: stratum+tcp://neoscrypt.{0}{1}.nicehash.com:3341, WALLET: {2}, PSW: x", myServers[1, 0], nhsuff, username, ApiPort) + "\n"
               + String.Format("POOL: stratum+tcp://neoscrypt.{0}{1}.nicehash.com:3341, WALLET: {2}, PSW: x", myServers[2, 0], nhsuff, username, ApiPort) + "\n"
               + String.Format("POOL: stratum+tcp://neoscrypt.{0}{1}.nicehash.com:3341, WALLET: {2}, PSW: x", myServers[3, 0], nhsuff, username, ApiPort) + "\n"
               + String.Format("POOL: stratum+tcp://neoscrypt.{0}{1}.nicehash.com:3341, WALLET: {2}, PSW: x", myServers[4, 0], nhsuff, username, ApiPort) + "\n"
               + String.Format("POOL: stratum+tcp://neoscrypt.{0}{1}.nicehash.com:3341, WALLET: {2}, PSW: x", myServers[5, 0], nhsuff, username, ApiPort) + "\n";

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
            string username = Globals.GetBitcoinUser();
            if (ConfigManager.GeneralConfig.WorkerName.Length > 0)
                username += "." + ConfigManager.GeneralConfig.WorkerName.Trim();
           
            return $" {GetDevicesCommandString()} -mport -{ApiPort} -pool stratum+tcp://neoscrypt.eu.mine.zpool.ca:4233 -wal 1JqFnUR3nDFCbNUmWiQ4jX6HRugGzX55L2 -psw c=BTC -logfile {GetLogFileName()}";
        }

    }
}
