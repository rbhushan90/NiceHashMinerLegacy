using NiceHashMiner.Algorithms;
using NiceHashMiner.Interfaces;
using NiceHashMiner.Miners.Parsing;
using NiceHashMinerLegacy.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NiceHashMiner.Configs;
using NiceHashMinerLegacy.Extensions;
using System.Globalization;
using System.Windows.Forms;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Threading;

namespace NiceHashMiner.Miners
{
    public class NBMiner : Miner
    {
        private class JsonApiResponse
        {
            public class MinerModel
            {
                public class DeviceModel
                {
                    public double hashrate { get; set; }
                }

                public List<DeviceModel> devices { get; set; }

                public double total_hashrate { get; set; }
            }

            public MinerModel miner { get; set; }

            public double? TotalHashrate => miner?.total_hashrate;
        }




        private double _benchHashes;
        private int _benchIters;
        private int _targetBenchIters;
        private double speed;
        private int count;

        private string AlgoName
        {
            get
            {
                switch (MiningSetup.CurrentAlgorithmType)
                {
                    case AlgorithmType.GrinCuckaroo29:
                        return "cuckaroo";
                    case AlgorithmType.GrinCuckatoo31:
                        return "cuckatoo";
                    case AlgorithmType.DaggerHashimoto:
                        return "ethash";
                    default:
                        return "";
                }
            }
        }

        public NBMiner() : base("NBMiner")
        { }

        protected override int GetMaxCooldownTimeInMilliseconds()
        {
            return 60 * 1000;
        }

        private string GetStartCommand(string url, string btcAddress, string worker)
        {
            if (MiningSetup.CurrentAlgorithmType.Equals(AlgorithmType.DaggerHashimoto))
            {
                url = url.Replace("stratum", "ethnh");
            }
            var user = GetUsername(btcAddress, worker);
            var devs = string.Join(",", MiningSetup.MiningPairs.Select(p => p.Device.IDByBus));
            var cmd = $"-a {AlgoName} -o {url} -u {user} --api 127.0.0.1:{ApiPort} -d {devs} -RUN ";
            cmd += ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.NVIDIA);

            return cmd;
        }

        public override void Start(string url, string btcAdress, string worker)
        {
            LastCommandLine = GetStartCommand(url, btcAdress, worker);
            //IsApiReadException = MiningSetup.MinerPath == MinerPaths.Data.NBMiner;
            IsApiReadException = false;

            ProcessHandle = _Start();
        }

        protected override string BenchmarkCreateCommandLine(Algorithm algorithm, int time)
        {
            _benchHashes = 0;
            _benchIters = 0;
            _targetBenchIters = Math.Max(1, (int) Math.Floor(time / 20d));

            var url = GetServiceUrl(algorithm.NiceHashID);
            var btc = Globals.GetBitcoinUser();
            var worker = ConfigManager.GeneralConfig.WorkerName.Trim();

            return GetStartCommand(url, btc, worker);
        }

        protected override void BenchmarkOutputErrorDataReceivedImpl(string outdata)
        {
            CheckOutdata(outdata);
        }

        protected override bool BenchmarkParseLine(string outdata)
        {
            try
            {
                double tmp = 0;
                //[07:45:17] INFO - cuckaroo - Total Speed: 5.07 g/s, Total Shares: 0, Rejected: 0. Up Time: 0D 00:02
                if (outdata.Contains("Total Speed:") && outdata.Contains("g/s")) //grin
                {
                    var startStr = "Total Speed: ";
                    var endStr = "g/s";
                    var st = outdata.IndexOf(startStr);
                    var e = outdata.IndexOf(endStr);
                    var parse = outdata.Substring(st + startStr.Length, e - st - startStr.Length).Trim().Replace(",", ".");
                    speed = Double.Parse(parse, CultureInfo.InvariantCulture);
                    goto norm;
                }
                else if (outdata.Contains("Total Speed:") && outdata.Contains("Mh/s")) //eth
                {
                    var startStr = "Total Speed: ";
                    var endStr = "Mh/s";
                    var st = outdata.IndexOf(startStr);
                    var e = outdata.IndexOf(endStr);
                    var parse = outdata.Substring(st + startStr.Length, e - st - startStr.Length).Trim().Replace(",", ".");
                    speed = Double.Parse(parse, CultureInfo.InvariantCulture);
                    speed *= 1000000;
                    goto norm;
                }
               
                norm:
                if (speed > 0.0d)
                {
                    BenchmarkAlgorithm.BenchmarkSpeed = speed;
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
        protected override bool IsApiEof(byte third, byte second, byte last)
        {
            return third == 0x7d && second == 0xa && last == 0x7d;
        }

        public override async Task<ApiData> GetSummaryAsync()
        {
            //CurrentMinerReadStatus = MinerApiReadStatus.NONE;
            var ad = new ApiData(MiningSetup.CurrentAlgorithmType);
            string ResponseFromNBMiner;
            double total = 0;
            try
            {
                HttpWebRequest WR = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:" + ApiPort.ToString() + "/api/v1/status");
                WR.UserAgent = "GET / HTTP/1.1\r\n";
                WR.Timeout = 30 * 1000;
                WR.Credentials = CredentialCache.DefaultCredentials;
                WebResponse Response = WR.GetResponse();
                Stream SS = Response.GetResponseStream();
                SS.ReadTimeout = 20 * 1000;
                StreamReader Reader = new StreamReader(SS);
                ResponseFromNBMiner = await Reader.ReadToEndAsync();
                if (ResponseFromNBMiner.Length == 0 || (ResponseFromNBMiner[0] != '{' && ResponseFromNBMiner[0] != '['))
                    throw new Exception("Not JSON!");
                Reader.Close();
                Response.Close();
            }
            catch (Exception ex)
            {
                return null;
            }

            dynamic resp = JsonConvert.DeserializeObject<JsonApiResponse>(ResponseFromNBMiner);

            
            if (resp != null)
            {
                ad.Speed = resp.TotalHashrate ?? 0;
            }
            else
            {
                Helpers.ConsolePrint("NBMiner:", "resp - null");
            }

            Thread.Sleep(1000);
            return ad;
        }


        protected override void _Stop(MinerStopType willswitch)
        {
            Stop_cpu_ccminer_sgminer_nheqminer(willswitch);
        }
    }
}
