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
using System.Windows.Forms;

namespace NiceHashMiner.Miners
{
    public class TTMiner : Miner
    {
        public TTMiner() : base("TTMiner_NVIDIA")
        { }

        private int TotalCount = 12;
        private double speed = 0;

        private double Total = 0;
        private const int TotalDelim = 2;

        private bool _benchmarkException => MiningSetup.MinerPath == MinerPaths.Data.TTMiner;

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

            IsApiReadException = MiningSetup.MinerPath == MinerPaths.Data.TTMiner;

            var algo = "";
            var apiBind = "";
            string alg = url.Substring(url.IndexOf("://") + 3, url.IndexOf(".") - url.IndexOf("://") - 3);
            string port = url.Substring(url.IndexOf(".com:") + 5, url.Length - url.IndexOf(".com:") - 5);
            algo = "-a " + MiningSetup.MinerName;
            //apiBind = " --api-bind 127.0.0.1:" + ApiPort;
            apiBind = "";
            url = url.Replace("stratum+tcp://", "");
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.Lyra2REv3))
            {
                algo = "-a LYRA2V3";
            }

            LastCommandLine = algo +
                " -P " + username + ":x@" + url +
                " -P " + username + ":x@" + alg + ".hk.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".jp.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".in.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".usa.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".br.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".eu.nicehash.com:" + port +

                apiBind +
                " -devices " + GetDevicesCommandString() + " " +
                ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.NVIDIA) + " ";
            ProcessHandle = _Start();
        }

        protected override void _Stop(MinerStopType willswitch)
        {
            Stop_cpu_ccminer_sgminer_nheqminer(willswitch);
        }

        // new decoupled benchmarking routines

        #region Decoupled benchmarking routines

        protected override string BenchmarkCreateCommandLine(Algorithm algorithm, int time)
        {
            string url = Globals.GetLocationUrl(algorithm.NiceHashID, Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], this.ConectionType);
            string alg = url.Substring(url.IndexOf("://") + 3, url.IndexOf(".") - url.IndexOf("://") - 3);
            string port = url.Substring(url.IndexOf(".com:") + 5, url.Length - url.IndexOf(".com:") - 5);
            var username = GetUsername(Globals.GetBitcoinUser(), ConfigManager.GeneralConfig.WorkerName.Trim());
            var commandLine = "";

            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.MTP))
            {
                commandLine = " -a MTP" +
                " -P aMGfYX8ARy4wKE57fPxkEBcnNuHegDBweE." + ConfigManager.GeneralConfig.WorkerName.Trim() + ":x@xzc.2miners.com:8080" +
                " -P " + username + ":x@" + alg + ".eu.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".hk.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".jp.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".in.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".usa.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".br.nicehash.com:" + port +
                              ExtraLaunchParametersParser.ParseForMiningSetup(
                                  MiningSetup,
                                  DeviceType.NVIDIA) +
                              " -nocolor -devices ";
            }
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.Lyra2REv3))
            {
                commandLine = " -a LYRA2V3" +
                " -P 1JqFnUR3nDFCbNUmWiQ4jX6HRugGzX55L2." + ConfigManager.GeneralConfig.WorkerName.Trim() + ":x@lyra2v3.eu.mine.zpool.ca:4550" +
                " -P " + username + ":x@" + alg + ".eu.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".hk.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".jp.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".in.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".usa.nicehash.com:" + port +
                " -P " + username + ":x@" + alg + ".br.nicehash.com:" + port +
                              ExtraLaunchParametersParser.ParseForMiningSetup(
                                  MiningSetup,
                                  DeviceType.NVIDIA) +
                              " -nocolor -devices ";
            }

            commandLine += GetDevicesCommandString();

            TotalCount = 14;

            Total = 0.0d;

            return commandLine;
        }

        protected override bool BenchmarkParseLine(string outdata)
        {
            int count = 0;
            double tmp = 0;

            if (_benchmarkException)
            {
                //Helpers.ConsolePrint("BENCHMARK:", outdata);
                if ( outdata.Contains(variables.TTMiner_bench1) && outdata.Contains(variables.TTMiner_bench2)) 
                {

                    var st = outdata.IndexOf(variables.TTMiner_bench3);
                    var e = outdata.IndexOf(variables.TTMiner_bench4);
                    try
                    {
                        var parse = outdata.Substring(st + 6, e - st - 8).Trim();
                        //Helpers.ConsolePrint("BENCHMARK!:", parse);
                        tmp = Double.Parse(parse, CultureInfo.InvariantCulture);
                    } catch
                    {
                        MessageBox.Show("Unsupported miner version - " + MiningSetup.MinerPath,
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        BenchmarkSignalFinnished = true;
                        return false;
                    }
                    // save speed

                    if (outdata.ToUpper().Contains("KH/S"))
                        tmp *= 1000;
                    else if (outdata.ToUpper().Contains("MH/S"))
                        tmp *= 1000000;
                    else if (outdata.ToUpper().Contains("GH/S"))
                        tmp *= 1000000000;

                    speed = Math.Max(speed, tmp);
                    count++;
                    TotalCount--;
                }

                if (TotalCount <= 0)
                {
                    BenchmarkAlgorithm.BenchmarkSpeed = speed;
                    BenchmarkSignalFinnished = true;
                    return true;
                }

               // return false;
            }
            /*
            if (speed > 0.0d)
            {
                BenchmarkAlgorithm.BenchmarkSpeed = speed/2;
                return true;
            }
            */
            return false;
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
            if (!IsApiReadException) return await GetSummaryCpuCcminerAsync();
            // check if running
            if (ProcessHandle == null)
            {
                CurrentMinerReadStatus = MinerApiReadStatus.RESTART;
                Helpers.ConsolePrint(MinerTag(), ProcessTag() + " Could not read data from CryptoNight Proccess is null");
                return null;
            }
            try
            {
                Process.GetProcessById(ProcessHandle.Id);
            }
            catch (ArgumentException ex)
            {
                CurrentMinerReadStatus = MinerApiReadStatus.RESTART;
                Helpers.ConsolePrint(MinerTag(), ProcessTag() + " Could not read data from CryptoNight reason: " + ex.Message);
                return null; // will restart outside
            }
            catch (InvalidOperationException ex)
            {
                CurrentMinerReadStatus = MinerApiReadStatus.RESTART;
                Helpers.ConsolePrint(MinerTag(), ProcessTag() + " Could not read data from CryptoNight reason: " + ex.Message);
                return null; // will restart outside
            }

            var totalSpeed = MiningSetup.MiningPairs
                .Select(miningPair =>
                    miningPair.Device.GetAlgorithm(MinerBaseType.TTMiner, MiningSetup.CurrentAlgorithmType, AlgorithmType.NONE))
                .Where(algo => algo != null).Sum(algo => algo.BenchmarkSpeed);

            var ttminerData = new ApiData(MiningSetup.CurrentAlgorithmType)
            {
                Speed = totalSpeed
            };
            CurrentMinerReadStatus = MinerApiReadStatus.GOT_READ;
            // check if speed zero
            if (ttminerData.Speed == 0) CurrentMinerReadStatus = MinerApiReadStatus.READ_SPEED_ZERO;
            return ttminerData;
        }
    }
}
