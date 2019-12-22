﻿/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
using NiceHashMiner.Miners.Grouping;
using NiceHashMiner.Miners.Parsing;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NiceHashMiner.Algorithms;
using NiceHashMinerLegacy.Common.Enums;
using NiceHashMiner.Configs;
using System.Threading;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Windows.Forms;

namespace NiceHashMiner.Miners
{
    public class CryptoDredge : Miner
    {
        public CryptoDredge() : base("CryptoDredge_NVIDIA")
        { }

        private int TotalCount = 0;

        private double Total = 0;
        private const int TotalDelim = 2;
        double speed = 0;
        int count = 0;
        private int _benchmarkTimeWait = 120;

        private bool _benchmarkException => MiningSetup.MinerPath == MinerPaths.Data.CryptoDredge;

        protected override int GetMaxCooldownTimeInMilliseconds()
        {
            return 60 * 1000 * 8;
        }

        public override void Start(string url, string btcAdress, string worker)
        {
            if (!IsInit)
            {
                Helpers.ConsolePrint(MinerTag(), "MiningSetup is not initialized exiting Start()");
                return;
            }
            var username = GetUsername(btcAdress, worker);

             IsApiReadException = MiningSetup.MinerPath == MinerPaths.Data.CryptoDredge;

            var algo = "";
            var apiBind = "";
            string alg = url.Substring(url.IndexOf("://") + 3, url.IndexOf(".") - url.IndexOf("://") - 3);
            string port = url.Substring(url.IndexOf(".com:") + 5, url.Length - url.IndexOf(".com:") - 5);
            algo = "--algo " + MiningSetup.MinerName;
            apiBind = " --api-bind 127.0.0.1:" + ApiPort;
            IsApiReadException = false;
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.CryptoNightV8))
            {
                algo = "--algo cnv8";
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.CryptoNightHeavy))
            {
                algo = "--algo cnheavy";
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.Lyra2REv3))
            {
                algo = "--algo lyra2v3";
            }
            if (MiningSetup.CurrentAlgorithmType == AlgorithmType.GrinCuckaroo29)
            {
                algo = "--algo cuckaroo29";
                //IsApiReadException = MiningSetup.MinerPath == MinerPaths.Data.CryptoDredge;
                IsApiReadException = true; //0.18.0 api broken
            }
            if (MiningSetup.CurrentAlgorithmType == AlgorithmType.CuckooCycle)
            {
                algo = "--algo aeternity";
                //IsApiReadException = MiningSetup.MinerPath == MinerPaths.Data.CryptoDredge;
                //IsApiReadException = true; //0.18.0 api broken
            }
            string nhsuff = "";
            if (Configs.ConfigManager.GeneralConfig.NewPlatform)
            {
                nhsuff = Configs.ConfigManager.GeneralConfig.StratumSuff;
            }
            LastCommandLine = algo +
                " -o " + url + " -u " + username + " -p x " +
                " -o " + alg + "." + myServers[1, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o " + alg + "." + myServers[2, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o " + alg + "." + myServers[3, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o " + alg + "." + myServers[4, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o " + alg + "." + myServers[5, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o " + alg + "." + myServers[0, 0] + nhsuff + ".nicehash.com:" + port + " -u " + username + " -p x " +
                " -o " + url + " -u " + username + " -p x --log " + GetLogFileName() +
                apiBind + 
                " -d " + GetDevicesCommandString() + " " +
                ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.NVIDIA) + " ";
            ProcessHandle = _Start();
        }

        protected override void _Stop(MinerStopType willswitch)
        {
            Stop_cpu_ccminer_sgminer_nheqminer(willswitch);
            Thread.Sleep(200);
            try { ProcessHandle.SendCtrlC((uint)Process.GetCurrentProcess().Id); } catch { }
            Thread.Sleep(200);
            foreach (var process in Process.GetProcessesByName("CryptoDredge"))
            {
                try {
                    process.Kill();
                    Thread.Sleep(200);
                    process.Kill();
                }
                catch (Exception e) { Helpers.ConsolePrint(MinerDeviceName, e.ToString()); }
            }
        }

        // new decoupled benchmarking routines

        #region Decoupled benchmarking routines

        protected override string BenchmarkCreateCommandLine(Algorithm algorithm, int time)
        {
            string url = Globals.GetLocationUrl(algorithm.NiceHashID, Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], this.ConectionType);
            string alg = url.Substring(url.IndexOf("://") + 3, url.IndexOf(".") - url.IndexOf("://") - 3);
            string port = url.Substring(url.IndexOf(".com:") + 5, url.Length - url.IndexOf(".com:") - 5);
            var username = GetUsername(Globals.GetBitcoinUser(), ConfigManager.GeneralConfig.WorkerName.Trim());
            var apiBind = " --api-bind 127.0.0.1:" + ApiPort;
            var algo = "--algo " + MiningSetup.MinerName;
            var commandLine = "";
            _benchmarkTimeWait = time;
            TotalCount = _benchmarkTimeWait/60;

            if (File.Exists("bin_3rdparty\\CryptoDredge\\" + GetLogFileName()))
                File.Delete("bin_3rdparty\\CryptoDredge\\" + GetLogFileName());
            string nhsuff = "";

            if (Configs.ConfigManager.GeneralConfig.NewPlatform)
            {
                nhsuff = Configs.ConfigManager.GeneralConfig.StratumSuff;
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.Blake2s))
            {
                commandLine = "--algo blake2s" +
                " --url=stratum+tcp://" + alg + "." + myServers[0, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o stratum+tcp://blake2s.eu.mine.zpool.ca:5766" + " -u 1JqFnUR3nDFCbNUmWiQ4jX6HRugGzX55L2" + " -p c=BTC " +
                " --log " + GetLogFileName() +
                apiBind +
                " -d " + GetDevicesCommandString() + " " +
                ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.NVIDIA) + " ";
              //  TotalCount = 3;
                Total = 0.0d;
                return commandLine;
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.X16R))
            {
                commandLine = "--algo x16r" +
                " --url=stratum+tcp://" + alg + "." + myServers[0, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o stratum+tcp://x16r.eu.mine.zpool.ca:3636" + " -u 1JqFnUR3nDFCbNUmWiQ4jX6HRugGzX55L2" + " -p c=BTC " +
                " --log " + GetLogFileName() +
                apiBind +
                " -d " + GetDevicesCommandString() + " " +
                ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.NVIDIA) + " ";
             //   TotalCount = 3;
                Total = 0.0d;
                return commandLine;
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.X16RV2))
            {
                commandLine = "--algo x16rv2" +
                " --url=stratum+tcp://" + alg + "." + myServers[0, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o stratum+tcp://x16rv2.eu.mine.zpool.ca:3637" + " -u 1JqFnUR3nDFCbNUmWiQ4jX6HRugGzX55L2" + " -p c=BTC " +
                " --log " + GetLogFileName() +
                apiBind +
                " -d " + GetDevicesCommandString() + " " +
                ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.NVIDIA) + " ";
                //   TotalCount = 3;
                Total = 0.0d;
                return commandLine;
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.Lyra2z))
            {
                commandLine = "--algo lyra2z" +
                " --url=stratum+tcp://" + alg + "." + myServers[0, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o stratum+tcp://lyra2z.eu.mine.zpool.ca:4553" + " -u 1JqFnUR3nDFCbNUmWiQ4jX6HRugGzX55L2" + " -p c=BTC " +
                " --log " + GetLogFileName() +
                apiBind +
                " -d " + GetDevicesCommandString() + " " +
                ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.NVIDIA) + " ";
            //    TotalCount = 2;
                Total = 0.0d;
                return commandLine;
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.Lyra2REv2))
            {
                commandLine = "--algo lyra2v2" +
                " --url=stratum+tcp://" + alg + "." + myServers[0, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o stratum+tcp://lyra2v2.eu.mine.zpool.ca:4533" + " -u 1JqFnUR3nDFCbNUmWiQ4jX6HRugGzX55L2" + " -p c=BTC " +
                " --log " + GetLogFileName() +
                apiBind +
                " -d " + GetDevicesCommandString() + " " +
                ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.NVIDIA) + " ";
             //   TotalCount = 2;
                Total = 0.0d;
                return commandLine;
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.Lyra2REv3))
            {
                commandLine = "--algo lyra2v3" +
                " --url=stratum+tcp://" + alg + "." + myServers[0, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o stratum+tcp://lyra2v3.eu.mine.zpool.ca:4550" + " -u 1JqFnUR3nDFCbNUmWiQ4jX6HRugGzX55L2" + " -p c=BTC " +
                " --log " + GetLogFileName() +
                apiBind +
                " -d " + GetDevicesCommandString() + " " +
                ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.NVIDIA) + " ";
              //  TotalCount = 2;
                Total = 0.0d;
                return commandLine;
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.NeoScrypt))
            {
                commandLine = "--algo neoscrypt --retry-pause 5" +
                " --url=stratum+tcp://" + alg + "." + myServers[0, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o stratum+tcp://neoscrypt.na.mine.zpool.ca:4233" + " -u 1JqFnUR3nDFCbNUmWiQ4jX6HRugGzX55L2" + " -p c=BTC " + //no support for failover pools?
                " -o stratum+tcp://neoscrypt.jp.mine.zpool.ca:4233" + " -u 1JqFnUR3nDFCbNUmWiQ4jX6HRugGzX55L2" + " -p c=BTC " +
                " -o stratum+tcp://neoscrypt.sea.mine.zpool.ca:4233" + " -u 1JqFnUR3nDFCbNUmWiQ4jX6HRugGzX55L2" + " -p c=BTC " +
                " -o stratum+tcp://neoscrypt.eu.mine.zpool.ca:4233" + " -u 1JqFnUR3nDFCbNUmWiQ4jX6HRugGzX55L2" + " -p c=BTC " +
                " --log " + GetLogFileName() +
                apiBind +
                " -d " + GetDevicesCommandString() + " " +
                ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.NVIDIA) + " ";
             //   TotalCount = 2;
                Total = 0.0d;
                return commandLine;
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.Skunk))
            {
                commandLine = "--algo skunk" +
                " --url=stratum+tcp://" + alg + "." + myServers[0, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
               // " -o stratum+tcp://hdac.moricpool.com:3333" + " -u HGr2JYPDMgYr9GzS9TcadBxxkyxo4v9XAJ" + " -p x " +
               "-o stratum+tcp://skunk.eu.mine.zpool.ca:8433 -u 1JqFnUR3nDFCbNUmWiQ4jX6HRugGzX55L2 -p c=BTC" +
                " --log " + GetLogFileName() +
                apiBind +
                " -d " + GetDevicesCommandString() + " " +
                ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.NVIDIA) + " ";
              //  TotalCount = 2;
                Total = 0.0d;
                return commandLine;
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.CryptoNightHeavy))
            {
                commandLine = "--algo cnheavy" +
                " --url=stratum+tcp://" + alg + "." + myServers[2, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " --url=stratum+tcp://" + alg + "." + myServers[1, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " --url=stratum+tcp://" + alg + "." + myServers[0, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                //" -o stratum+tcp://loki.miner.rocks:5555" + " -u L95cF8XmPzzhBA1tkiL1NMijNNbj58vs1iJExK84oi2LKc6RQm2q1Z4PmDxYB7sicHVXY1J5YV9yg6vkMxKpuCK1L1SwoDi"+ " -p w=" + ConfigManager.GeneralConfig.WorkerName.Trim() +
                " --log " + GetLogFileName() +
                apiBind +
                " -d " + GetDevicesCommandString() + " " +
                ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.NVIDIA) + " ";
             //   TotalCount = 2;
                Total = 0.0d;
                return commandLine;
            }

            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.CryptoNightV8))
            {
                /*
                if (File.Exists("bin_3rdparty\\CryptoDredgeV8\\"+ GetLogFileName()))
                    File.Delete("bin_3rdparty\\CryptoDredgeV8\\" + GetLogFileName());
                    */
                algo = "--algo cnv8";
                commandLine = algo +
                " --url=stratum+tcp://" + alg + "." + myServers[2, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " --url=stratum+tcp://" + alg + "." + myServers[1, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " --url=stratum+tcp://" + alg + "." + myServers[0, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
              //  " -o stratum+tcp://xmr-usa.dwarfpool.com:8005" + " -u 42fV4v2EC4EALhKWKNCEJsErcdJygynt7RJvFZk8HSeYA9srXdJt58D9fQSwZLqGHbijCSMqSP4mU7inEEWNyer6F7PiqeX." + ConfigManager.GeneralConfig.WorkerName.Trim() + " -p x " +
              //  " -o stratum+tcp://xmr-eu.dwarfpool.com:8005" + " -u 42fV4v2EC4EALhKWKNCEJsErcdJygynt7RJvFZk8HSeYA9srXdJt58D9fQSwZLqGHbijCSMqSP4mU7inEEWNyer6F7PiqeX."+ ConfigManager.GeneralConfig.WorkerName.Trim() + " -p x " +
                " --log " + GetLogFileName() +
                apiBind +
                " -d " + GetDevicesCommandString() + " " +
                ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.NVIDIA) + " ";
             //   TotalCount = 3;
                Total = 0.0d;
                return commandLine;
            }

            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.MTP))
            {
                algo = "--algo mtp";
                commandLine = algo +
                " --url=stratum+tcp://" + alg + "." + myServers[0, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o stratum+tcp://xzc.2miners.com:8080" + " -u aMGfYX8ARy4wKE57fPxkEBcnNuHegDBweE." + ConfigManager.GeneralConfig.WorkerName.Trim() + " -p x " +
                " --log " + GetLogFileName() +
                apiBind +
                " -d " + GetDevicesCommandString() + " " +
                ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.NVIDIA) + " ";
             //   TotalCount = 3;
                Total = 0.0d;
                return commandLine;
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.GrinCuckaroo29))
            {
                algo = "--algo cuckaroo29";
                commandLine = algo +
                " --url=stratum+tcp://" + alg + "." + myServers[1, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " --url=stratum+tcp://" + alg + "." + myServers[0, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                //" -o stratum+tcp://grin.sparkpool.com:6666" + " -u angelbbs@mail.ru/" + ConfigManager.GeneralConfig.WorkerName.Trim() + " -p x " +
                " --log " + GetLogFileName() +
                apiBind +
                " -d " + GetDevicesCommandString() + " " +
                ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.NVIDIA) + " ";
             //   TotalCount = 3;
                Total = 0.0d;
                return commandLine;
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.CuckooCycle))
            {
                algo = "--algo aeternity";
                commandLine = algo +
                " --url=stratum+tcp://" + alg + "." + myServers[0, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o stratum+tcp://ae.f2pool.com:7898" + " -u ak_2f9AMwztStKs5roPmT592wTbUEeTyqRgYVZNrc5TyZfr94m7fM." + ConfigManager.GeneralConfig.WorkerName.Trim() + " -p x " +
                " --log " + GetLogFileName() +
                apiBind +
                " -d " + GetDevicesCommandString() + " " +
                ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.NVIDIA) + " ";
             //   TotalCount = 3;
                Total = 0.0d;
                return commandLine;
            }
            commandLine = algo +
                " -o " + url + " -u " + username + " -p x " +
                " --url=stratum+tcp://" + alg + "." + myServers[0, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o " + alg + "." + myServers[5, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o " + alg + "." + myServers[4, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o " + alg + "." + myServers[3, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o " + alg + "." + myServers[2, 0] + nhsuff + ".nicehash.com:" + port + " " + " -u " + username + " -p x " +
                " -o " + alg + "." + myServers[1, 0] + nhsuff + ".nicehash.com:" + port + " -u " + username + " -p x " +
                " -o " + url + " -u " + username + " -p x --log " + GetLogFileName() +
                apiBind +
                " -d " + GetDevicesCommandString() + " " +
                ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.NVIDIA) + " ";

           // TotalCount = 2;
            Total = 0.0d;
            return commandLine;
        }

        protected override bool BenchmarkParseLine(string outdata)
        {
          try
          {
                //[20:42:11] INFO  - GPU0 GTX 1070 : 18,83MH/s (Avr 17,33MH/s) : 124,1KH/W : T=60C Fan=0%
                double tmp = 0;
                    if (outdata.Contains("GPU") && outdata.ToUpper().Contains("GH/S)"))
                    {
                        var st = outdata.IndexOf("Avr ");
                        var e = outdata.ToUpper().IndexOf("GH/S)");
                        var parse = outdata.Substring(st + 4, e - st - 4).Trim().Replace(",",".");
                  //  if (count > 0)//skip first
                    {
                        tmp = Double.Parse(parse, CultureInfo.InvariantCulture);
                        tmp *= 10000000000;
                    }
                        speed += tmp;
                        count++;
                        TotalCount--;
                        goto norm;    
                    } else if (outdata.Contains("GPU") && outdata.ToUpper().Contains("MH/S)"))
                    {
                    var st = outdata.IndexOf("Avr ");
                        var e = outdata.ToUpper().IndexOf("MH/S)");
                        var parse = outdata.Substring(st + 4, e - st - 4).Trim().Replace(",", ".");
                   // if (count > 0)//skip first
                    {
                        tmp = Double.Parse(parse, CultureInfo.InvariantCulture);
                        tmp *= 1000000;
                    }
                        speed += tmp;
                        count++;
                        TotalCount--;
                        goto norm;
                    } else if (outdata.Contains("GPU") && outdata.ToUpper().Contains("KH/S)"))
                    {
                    var st = outdata.IndexOf("Avr ");
                        var e = outdata.ToUpper().IndexOf("KH/S)");
                        var parse = outdata.Substring(st + 4, e - st - 4).Trim().Replace(",", ".");
                 //   if (count > 0)//skip first
                    {
                        tmp = Double.Parse(parse, CultureInfo.InvariantCulture);
                        tmp *= 1000;
                    }
                        speed += tmp;
                        count++;
                        TotalCount--;
                        goto norm;
                    }
                    else if (outdata.Contains("GPU") && outdata.ToUpper().Contains("H/S)"))
                    {
                    var st = outdata.IndexOf("Avr ");
                        var e = outdata.ToUpper().IndexOf("H/S)");
                        var parse = outdata.Substring(st + 4, e - st - 4).Trim().Replace(",", ".");
                 //   if (count > 0)//skip first
                    {
                        tmp = Double.Parse(parse, CultureInfo.InvariantCulture);
                    }
                        speed += tmp;
                        count++;
                        TotalCount--;
                        goto norm;
                    }
norm:
                    if (TotalCount <= 0 && speed > 0.0d)
                    {
                    BenchmarkAlgorithm.BenchmarkSpeed = speed / (count);
                    BenchmarkSignalFinnished = true;
                    return true;
                    }

                return false;
          }
          catch
          {
                MessageBox.Show("Unsupported miner version " + MiningSetup.MinerPath,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                BenchmarkSignalFinnished = true;
                return false;
          }
        }

        protected override void BenchmarkOutputErrorDataReceivedImpl(string outdata)
        {
            CheckOutdata(outdata);
        }

        protected override void BenchmarkThreadRoutine(object CommandLine)
        {
            BenchmarkSignalQuit = false;
            BenchmarkSignalHanged = false;
            BenchmarkSignalFinnished = false;
            BenchmarkException = null;

            Thread.Sleep(ConfigManager.GeneralConfig.MinerRestartDelayMS);

            try
            {
                Helpers.ConsolePrint("BENCHMARK", "Benchmark starts");
                BenchmarkHandle = BenchmarkStartProcess((string)CommandLine);

                BenchmarkThreadRoutineStartSettup();
                BenchmarkTimeInSeconds = 300;
                BenchmarkProcessStatus = BenchmarkProcessStatus.Running;
                var exited = BenchmarkHandle.WaitForExit((BenchmarkTimeoutInSeconds(BenchmarkTimeInSeconds) + 20) * 1000);
                if (BenchmarkSignalTimedout && !TimeoutStandard)
                {
                    throw new Exception("Benchmark timedout");
                }

                if (BenchmarkException != null)
                {
                    throw BenchmarkException;
                }

                if (BenchmarkSignalQuit)
                {
                    throw new Exception("Termined by user request");
                }

                if (BenchmarkSignalHanged || !exited)
                {
                    throw new Exception("Miner is not responding");
                }

                if (BenchmarkSignalFinnished)
                {
                    //break;
                }
            }
            catch (Exception ex)
            {
                BenchmarkThreadRoutineCatch(ex);
            }
            finally
            {
                BenchmarkThreadRoutineFinish();
            }
        }

        #endregion // Decoupled benchmarking routines

        public override async Task<ApiData> GetSummaryAsync()
        {
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.GrinCuckaroo29)) //0.18.0 api broken
            {
                var totalSpeed = 0.0d;
                foreach (var miningPair in MiningSetup.MiningPairs)
                {
                    var algo = miningPair.Device.GetAlgorithm(MinerBaseType.CryptoDredge, AlgorithmType.GrinCuckaroo29, AlgorithmType.NONE);
                    if (algo != null)
                    {
                        totalSpeed += algo.BenchmarkSpeed;
                    }
                }

                var cdData = new ApiData(MiningSetup.CurrentAlgorithmType)
                {
                    Speed = totalSpeed
                };
                CurrentMinerReadStatus = MinerApiReadStatus.GOT_READ;
                // check if speed zero
                if (cdData.Speed == 0) CurrentMinerReadStatus = MinerApiReadStatus.READ_SPEED_ZERO;
                return cdData;
            }

            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.CuckooCycle)) //0.18.0 api broken
            {
                var totalSpeed = 0.0d;
                foreach (var miningPair in MiningSetup.MiningPairs)
                {
                    var algo = miningPair.Device.GetAlgorithm(MinerBaseType.CryptoDredge, AlgorithmType.CuckooCycle, AlgorithmType.NONE);
                    if (algo != null)
                    {
                        totalSpeed += algo.BenchmarkSpeed;
                    }
                }

                var cdData = new ApiData(MiningSetup.CurrentAlgorithmType)
                {
                    Speed = totalSpeed
                };
                CurrentMinerReadStatus = MinerApiReadStatus.GOT_READ;
                // check if speed zero
                if (cdData.Speed == 0) CurrentMinerReadStatus = MinerApiReadStatus.READ_SPEED_ZERO;
                return cdData;
            }


            CurrentMinerReadStatus = MinerApiReadStatus.NONE;
            var ad = new ApiData(MiningSetup.CurrentAlgorithmType, MiningSetup.CurrentSecondaryAlgorithmType);
            double tmp = 0;

            string resp = null;
            try
            {
                var bytesToSend = Encoding.ASCII.GetBytes("summary");
                var client = new TcpClient("127.0.0.1", ApiPort);
                var nwStream = client.GetStream();
                await nwStream.WriteAsync(bytesToSend, 0, bytesToSend.Length);
                var bytesToRead = new byte[client.ReceiveBufferSize];
                var bytesRead = await nwStream.ReadAsync(bytesToRead, 0, client.ReceiveBufferSize);
                var respStr = Encoding.ASCII.GetString(bytesToRead, 0, bytesRead);
                //Helpers.ConsolePrint(MinerTag(), "API: " + respStr);
                client.Close();
                resp = respStr;
            }
            catch (Exception ex)
            {
                Helpers.ConsolePrint(MinerTag(), "GetSummary exception: " + ex.Message);
            }

            if (resp != null )
            {
                    var st = resp.IndexOf(";KHS=");
                    var e = resp.IndexOf(";SOLV=");
                    var parse = resp.Substring(st + 5, e - st - 5).Trim();
                try
                {
                    tmp = Double.Parse(parse, CultureInfo.InvariantCulture);
                }
                catch
                {
                    MessageBox.Show("Unsupported miner version - " + MiningSetup.MinerPath,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    BenchmarkSignalFinnished = true;
                }
                ad.Speed = tmp*1000;
                /*
                if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.GrinCuckaroo29))
                {
                    ad.Speed = BenchmarkAlgorithm.BenchmarkSpeed;
                }
                */
                
                if (ad.Speed == 0)
                {
                    CurrentMinerReadStatus = MinerApiReadStatus.READ_SPEED_ZERO;
                } else
                {
                    CurrentMinerReadStatus = MinerApiReadStatus.GOT_READ;
                }

                if (ad.Speed < 0)
                {
                    Helpers.ConsolePrint(MinerTag(), "Reporting negative speeds will restart...");
                    Restart();
                }
            }

            return ad;
        }


    }

}
