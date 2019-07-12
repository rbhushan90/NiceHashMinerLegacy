using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NiceHashMiner.Devices;
using NiceHashMiner.Miners;
using NiceHashMiner.Switching;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using NiceHashMinerLegacy.Common.Enums;
using WebSocketSharp;
using System.Threading;
using System.Timers;
using NiceHashMiner.Configs;


namespace NiceHashMiner.Stats
{
    public class SocketEventArgs : EventArgs
    {
        public readonly string Message;

        public SocketEventArgs(string message)
        {
            Message = message;
        }
    }

    internal static class NiceHashStats
    {
        #region JSON Models
#pragma warning disable 649, IDE1006
        private class NicehashCredentials
        {
            public string method = "credentials.set";
            public string btc;
            public string worker;
        }

        private class NicehashDeviceStatus
        {
            public string method = "devices.status";
            public List<JArray> devices;
        }
        public class ExchangeRateJson
        {
            public List<Dictionary<string, string>> exchanges { get; set; }
            public Dictionary<string, double> exchanges_fiat { get; set; }
        }
#pragma warning restore 649, IDE1006
        #endregion

        private const int DeviceUpdateLaunchDelay = 20 * 1000;
        private const int DeviceUpdateInterval = 60 * 1000;

        public static double Balance { get; private set; }
        public static string Version { get; private set; }

        class github_version
        {
            public string tag_name;
            public string target_commitish;
        }
    public static bool IsAlive => _socket?.IsAlive ?? false;

        // Event handlers for socket
        public static event EventHandler OnBalanceUpdate;

        public static event EventHandler OnSmaUpdate;
        public static event EventHandler OnVersionUpdate;
        public static event EventHandler OnConnectionLost;
        public static event EventHandler OnConnectionEstablished;
        public static event EventHandler<SocketEventArgs> OnVersionBurn;
        public static event EventHandler OnExchangeUpdate;

        public static NiceHashSocket _socket;
        public static NiceHashSocket _socketold;

        public static System.Threading.Timer _deviceUpdateTimer;
        public static System.Threading.Timer _deviceUpdateTimerNew;

        public static void StartConnection(string address)
        {
            //https://nhmws-new.nicehash.com/v3/nhml
            //https://nhmws.nicehash.com/v2/nhm
            // Helpers.ConsolePrint("SOCKET-address", address);

            /*
            if (_deviceUpdateTimer != null)
            {
               
                _deviceUpdateTimer.Change(System.Threading.Timeout.Infinite, 0);
                _deviceUpdateTimer.Dispose();
                _deviceUpdateTimer = null;
                if (_socket != null)
                {
                    _socket = null;
                   NiceHashSocket._connectionEstablished = false;
                   NiceHashSocket._restartConnection = false;
                   NiceHashSocket._endConnection = true;
                   NiceHashSocket._webSocket.Close(CloseStatusCode.Normal);
                }
                Helpers.ConsolePrint("SOCKET", "Old Timer stop");
                //Thread.Sleep(1000);
            }
            if (_deviceUpdateTimerNew != null)
            {

                _deviceUpdateTimerNew.Change(System.Threading.Timeout.Infinite, 0);
                _deviceUpdateTimerNew.Dispose();
                if (_socket != null)
                {
                    _socket = null;
                    NiceHashSocket._connectionEstablished = false;
                    NiceHashSocket._restartConnection = false;
                    NiceHashSocket._endConnection = true;
                    NiceHashSocket._webSocket.Close(CloseStatusCode.Normal);
                }
                    Helpers.ConsolePrint("SOCKET", "New Timer stop");
                //Thread.Sleep(1000);
            }

            if (_socket == null)
            {
                _socket = new NiceHashSocket(address);
                _socket.OnConnectionEstablished += SocketOnOnConnectionEstablished;
                _socket.OnDataReceived += SocketOnOnDataReceived;
                _socket.OnConnectionLost += SocketOnOnConnectionLost;
            } else
            {
                _socket = null;
                _socket = new NiceHashSocket(address);
                _socket.OnConnectionEstablished += SocketOnOnConnectionEstablished;
                _socket.OnDataReceived += SocketOnOnDataReceived;
                _socket.OnConnectionLost += SocketOnOnConnectionLost;
            }
            */

            _socket = null;
            _socket = new NiceHashSocket(address);
            _socket.OnConnectionEstablished += SocketOnOnConnectionEstablished;
            _socket.OnDataReceived += SocketOnOnDataReceived;
            _socket.OnConnectionLost += SocketOnOnConnectionLost;

            NiceHashSocket._restartConnection = true;
            Helpers.ConsolePrint("SOCKET-address:", address);
            if (Configs.ConfigManager.GeneralConfig.NewPlatform)
            {
                _socket.StartConnectionNew();
            } else
            {
                _socket.StartConnection();

            }
            /*
            System.Timers.Timer timer = null;

            timer = new System.Timers.Timer();
            timer.Interval = 1000*30;
            timer.Elapsed += DeviceStatus_Tick;
            timer.Enabled = true;

           // sw = new Stopwatch();
           // sw.Start();
            timer.Start();
            */

            if (Configs.ConfigManager.GeneralConfig.NewPlatform)
            {
                _deviceUpdateTimer = new System.Threading.Timer(DeviceStatus_Tick, null, DeviceUpdateInterval, DeviceUpdateInterval);
            } else
            {
                _deviceUpdateTimer = new System.Threading.Timer(DeviceStatus_Tick, null, DeviceUpdateInterval, DeviceUpdateInterval);
            }

            string ghv = GetVersion("");
            Helpers.ConsolePrint("GITHUB", ghv);
            if (ghv != null)
            {
                SetVersion(ghv);
            }
        }
        /*
        private static void DeviceStatus_Tick(object sender, ElapsedEventArgs e)
        {
            //throw new NotImplementedException();
        }
        */
        #region Socket Callbacks

        private static void SocketOnOnConnectionLost(object sender, EventArgs eventArgs)
        {
            OnConnectionLost?.Invoke(sender, eventArgs);
        }

        private static void SocketOnOnDataReceived(object sender, MessageEventArgs e)
        {
            try
            {
                if (e.IsText)
                {
                    Helpers.ConsolePrint("SOCKET", "Received: " + e.Data);
                    dynamic message = JsonConvert.DeserializeObject(e.Data);
                    switch (message.method.Value)
                    {
                        case "sma":
                            {
                                // Try in case stable is not sent, we still get updated paying rates
                                try
                                {
                                    var stable = JsonConvert.DeserializeObject(message.stable.Value);
                                    SetStableAlgorithms(stable);
                                    FileStream fs0 = new FileStream("configs\\stable.dat", FileMode.Create, FileAccess.Write);
                                    StreamWriter w0 = new StreamWriter(fs0);
                                    w0.Write(stable.data);
                                    //w.Write(JsonConvert.SerializeObject(message));
                                    w0.Flush();
                                    w0.Close();
                                }
                                catch
                                { }
                                //***************************
                                FileStream fs = new FileStream("configs\\sma.dat", FileMode.Create, FileAccess.Write);
                                StreamWriter w = new StreamWriter(fs);
                                w.Write(message.data);
                                //w.Write(JsonConvert.SerializeObject(message));
                                w.Flush();
                                w.Close();
                                foreach (var algo in message.data)
                                {
                                    var algoKey = (AlgorithmType)algo[0];
                                    if (!ConfigManager.GeneralConfig.NoShowApiInLog)
                                    {
                                        Helpers.ConsolePrint("SMA-DATA-WS: ", Enum.GetName(typeof(AlgorithmType), algoKey) + " - " + algo[1]);
                                    }
                                }
                                if (!GetSmaAPI())
                                {
                                    SetAlgorithmRates(message.data);
                                }

                                //***************************

//                                SetAlgorithmRates(message.data);
                                break;
                            }

                        case "balance":
                            SetBalance(message.value.Value);
                            break;
                        //case "versions":
                        //    SetVersion(message.legacy.Value);
                        //    break;
                        //case "burn":
                        //    OnVersionBurn?.Invoke(null, new SocketEventArgs(message.message.Value));
                        //    break;
                        case "exchange_rates":
                            SetExchangeRates(message.data.Value);
                            break;
                    }
                }
            } catch (Exception er)
            {
                Helpers.ConsolePrint("SOCKET", er.ToString());
            }
            var timeFrom1 = new TimeSpan(12, 00, 0);
            var timeTo1 = new TimeSpan(12, 01, 0);
            var timeNow = DateTime.Now.TimeOfDay;
            if (timeNow > timeFrom1 && timeNow < timeTo1)
            {
                Helpers.ConsolePrint("GITHUB", "Check new version");
                try
                {
                    string ghv = GetVersion("");
                    Helpers.ConsolePrint("GITHUB", ghv);
                    SetVersion(ghv);
                }
                catch (Exception er)
                {
                    Helpers.ConsolePrint("GITHUB", er.ToString());
                }
            }
        }

        public class Rootobject
        {
            public Result result { get; set; }
            public string method { get; set; }
        }

        public class Result
        {
            public Simplemultialgo[] simplemultialgo { get; set; }
        }

        public class Simplemultialgo
        {
            public string paying { get; set; }
            public int port { get; set; }
            public string name { get; set; }
            public int algo { get; set; }
        }


        public class ProfitsSMA
        {
            public string Method { get; set; }
            public IList<IList<object>> Data { get; set; }
        }

        public static bool GetSmaAPI()
        {
            Helpers.ConsolePrint("NHM_API_info", "Trying GetSmaAPI");
            
            if (ConfigManager.GeneralConfig.NewPlatform)
            {
                return true;
            }
            
                try
            {
                string resp;
                if (!ConfigManager.GeneralConfig.NewPlatform)
                {
                    resp = NiceHashStats.GetNiceHashApiData("https://api.nicehash.com/api?method=simplemultialgo.info", "x");
                }
                else
                {
                    resp = NiceHashStats.GetNiceHashApiData("https://api2.nicehash.com/main/api/v2/public/simplemultialgo/info/", "x");
                    //resp = NiceHashStats.GetNiceHashApiData("https://api2.nicehash.com/main/api/v2/mining/algorithms/", "x");
                }

                if (resp != null)
                {
                    if (!ConfigManager.GeneralConfig.NoShowApiInLog)
                    {
                        Helpers.ConsolePrint("NHM_API_info", resp);
                    }
                    //{"miningAlgorithms":[
                    //{ "algorithm":"SCRYPT","title":"Scrypt","speed":"139179.34974628","paying":"0.00002995"},
                    //{ "algorithm":"SHA256","title":"SHA256","speed":"3135.32612608","paying":"0"},
                    //{ "algorithm":"X11","title":"X11","speed":"901927.01007968","paying":"0.00000023"},
                    //{ "algorithm":"X13","title":"X13","speed":"4129.39471601","paying":"0.00000071"},
                    //{ "algorithm":"KECCAK","title":"Keccak","speed":"270492.97552064","paying":"0.00000007"},
                    //{ "algorithm":"X15","title":"X15","speed":"0","paying":"0"},
                    //{ "algorithm":"NIST5","title":"Nist5","speed":"6442.450944","paying":"0.00000001"},
                    //{ "algorithm":"NEOSCRYPT","title":"NeoScrypt","speed":"0","paying":"0"},
                    //{ "algorithm":"QUBIT","title":"Qubit","speed":"31953.0520186","paying":"0.00000001"},
                    //{ "algorithm":"QUARK","title":"Quark","speed":"0","paying":"0"},
                    //{ "algorithm":"LYRA2REV2","title":"Lyra2REv2","speed":"105707.85435114","paying":"0.00001198"},
                    //{ "algorithm":"BLAKE256R8","title":"Blake256r8","speed":"0","paying":"0"},
                    //{ "algorithm":"DAGGERHASHIMOTO","title":"DaggerHashimoto","speed":"130451.77279822","paying":"0.00017779"},
                    //{ "algorithm":"DECRED","title":"Decred","speed":"330.49773343","paying":"0"},
                    //{ "algorithm":"CRYPTONIGHT","title":"CryptoNight","speed":"0","paying":"0.01501"},
                    //{ "algorithm":"LBRY","title":"Lbry","speed":"66.54335997","paying":"0.00000005"},
                    //{ "algorithm":"EQUIHASH","title":"Equihash","speed":"3904401.60171067","paying":"0.8332256"},
                    //{ "algorithm":"PASCAL","title":"Pascal","speed":"0","paying":"0"},
                    //{ "algorithm":"SIA","title":"Sia","speed":"0","paying":"0"},
                    //{ "algorithm":"BLAKE2S","title":"Blake2s","speed":"53.3206567","paying":"0.00000001"},
                    //{ "algorithm":"SKUNK","title":"Skunk","speed":"3.43592141","paying":"0.00001"},
                    //{ "algorithm":"CRYPTONIGHTV7","title":"CryptoNightV7","speed":"0","paying":"0.15"},
                    //{ "algorithm":"CRYPTONIGHTHEAVY","title":"CryptoNightHeavy","speed":"0","paying":"0.01"},
                    //{ "algorithm":"LYRA2Z","title":"Lyra2Z","speed":"0","paying":"0"},
                    //{ "algorithm":"X16R","title":"X16R","speed":"17255.65307391","paying":"0.00026866"},
                    //{ "algorithm":"CRYPTONIGHTV8","title":"CryptoNightV8","speed":"0","paying":"0.01"},
                    //{ "algorithm":"SHA256ASICBOOST","title":"SHA256AsicBoost","speed":"0","paying":"0"},
                    //{ "algorithm":"ZHASH","title":"ZHash","speed":"48391.18053136","paying":"105.46999042"},
                    //{ "algorithm":"BEAM","title":"Beam","speed":"21114.88000001","paying":"234.96347194"},
                    //{ "algorithm":"GRINCUCKAROO29","title":"GrinCuckaroo29","speed":"17384.52357913","paying":"1320.6930785"},
                    //{ "algorithm":"GRINCUCKATOO31","title":"GrinCuckatoo31","speed":"231.25150828","paying":"2132.47314021"},
                    //{ "algorithm":"LYRA2REV3","title":"Lyra2REv3","speed":"1576.61091157","paying":"0.00005667"},
                    //{ "algorithm":"CRYPTONIGHTR","title":"CryptoNightR","speed":"616","paying":"4.22511377"},
                    //{ "algorithm":"CUCKOOCYCLE","title":"CuckooCycle","speed":"65.70663304","paying":"375.86200525"}
                    //]}
                    /*
                    { "result":{ "simplemultialgo":[{"paying":"0.00029919","port":3333,"name":"scrypt","algo":0},
                    {"paying":"0.00000003","port":3334,"name":"sha256","algo":1},
                    {"paying":"0","port":3335,"name":"scryptnf","algo":2},
                    {"paying":"0.00000281","port":3336,"name":"x11","algo":3},{"paying":"0.00008571","port":3337,"name":"x13","algo":4},{"paying":"0.00000152","port":3338,"name":"keccak","algo":5},{"paying":"0.0001","port":3339,"name":"x15","algo":6},{"paying":"0.00000575","port":3340,"name":"nist5","algo":7},{"paying":"0.02277784","port":3341,"name":"neoscrypt","algo":8},{"paying":"0","port":3342,"name":"lyra2re","algo":9},{"paying":"0","port":3343,"name":"whirlpoolx","algo":10},{"paying":"0.00000746","port":3344,"name":"qubit","algo":11},{"paying":"0.00000291","port":3345,"name":"quark","algo":12},{"paying":"0","port":3346,"name":"axiom","algo":13},{"paying":"0.00014356","port":3347,"name":"lyra2rev2","algo":14},{"paying":"0","port":3348,"name":"scryptjanenf16","algo":15},{"paying":"0.00000011","port":3349,"name":"blake256r8","algo":16},{"paying":"0","port":3350,"name":"blake256r14","algo":17},{"paying":"0","port":3351,"name":"blake256r8vnl","algo":18},{"paying":"0","port":3352,"name":"hodl","algo":19},{"paying":"0.00192647","port":3353,"name":"daggerhashimoto","algo":20},{"paying":"0.00000001","port":3354,"name":"decred","algo":21},{"paying":"0.31717939","port":3355,"name":"cryptonight","algo":22},{"paying":"0.00000078","port":3356,"name":"lbry","algo":23},{"paying":"9.81508791","port":3357,"name":"equihash","algo":24},{"paying":"0.00000011","port":3358,"name":"pascal","algo":25},{"paying":"0","port":3359,"name":"x11gost","algo":26},{"paying":"0","port":3360,"name":"sia","algo":27},{"paying":"0.00000011","port":3361,"name":"blake2s","algo":28},{"paying":"0.00031","port":3362,"name":"skunk","algo":29},{"paying":"3.08160114","port":3363,"name":"cryptonightv7","algo":30},{"paying":"5.56135135","port":3364,"name":"cryptonightheavy","algo":31},{"paying":"0.00082045","port":3365,"name":"lyra2z","algo":32},{"paying":"0.00245316","port":3366,"name":"x16r","algo":33},{"paying":"31.11766068","port":3367,"name":"cryptonightv8","algo":34},{"paying":"0.00000003","port":3368,"name":"sha256asicboost","algo":35},{"paying":"1214.77312142","port":3369,"name":"zhash","algo":36},{"paying":"3259.48313625","port":3370,"name":"beam","algo":37},{"paying":"14827.46208488","port":3371,"name":"grincuckaroo29","algo":38},{"paying":"99025.75291859","port":3372,"name":"grincuckatoo31","algo":39},{"paying":"0.00071719","port":3373,"name":"lyra2rev3","algo":40},{"paying":"0.03104377","port":3374,"name":"mtp","algo":41},{"paying":"41.89290546","port":3375,"name":"cryptonightr","algo":42},{"paying":"10623.31220285","port":3376,"name":"cuckoocycle","algo":43}]},"method":"simplemultialgo.info"}
                    */
                    /*
                     {"miningAlgorithms":[{"algorithm":"SCRYPT","title":"Scrypt","enabled":true,"order":0,"displayMiningFactor":"MH","miningFactor":"1000000","displayMarketFactor":"TH","marketFactor":"1000000000000","minimalOrderAmount":"0.005","minSpeedLimit":"0.01","maxSpeedLimit":"10000","priceDownStep":"-0.00001","minimalPoolDifficulty":"500000","port":3333,"color":"#afafaf","ordersEnabled":true},
                     */
                    dynamic list;
                    if (ConfigManager.GeneralConfig.NewPlatform)
                    {
                        list = JsonConvert.DeserializeObject<Rootobject>(resp);
                    } else
                    {
                        list = JsonConvert.DeserializeObject<Rootobject>(resp);
                    }
                    ProfitsSMA profdata = new ProfitsSMA();

                    List<ProfitsSMA> profdata2 = new List<ProfitsSMA>();

                    string outProf = "[\n";

                        foreach (var result in list.result.simplemultialgo)
                        {
                            if (!result.algo.ToString().Contains("UNUSED"))
                            {
                                if (!ConfigManager.GeneralConfig.NoShowApiInLog)
                                {
                                    Helpers.ConsolePrint("SMA-DATA-API: ", Enum.GetName(typeof(AlgorithmType), result.algo) + " - " + result.paying);
                                }
                            }
                            outProf = outProf + "  [\n" + "    " + result.algo + ",\n" + "    " + result.paying + "\n" + "  ],\n";

                        }
                        outProf = outProf.Remove(outProf.Length - 2) + "]";
                    
                    //Helpers.ConsolePrint("SMA-DATA-API***: ", outProf);
                    JArray smadata = (JArray.Parse(outProf));
                    /*
                    if (AlgorithmRates == null || niceHashData == null)
                    {
                        niceHashData = new NiceHashData();
                        AlgorithmRates = niceHashData.NormalizedSMA();
                    }
                    */
                    NiceHashStats.SetAlgorithmRates(smadata);

                    FileStream fs = new FileStream("configs\\sma.dat", FileMode.Create, FileAccess.Write);
                    StreamWriter w = new StreamWriter(fs);
                    w.Write(smadata);
                    //w.Write(JsonConvert.SerializeObject(message));
                    w.Flush();
                    w.Close();
                    if (!ConfigManager.GeneralConfig.NoShowApiInLog)
                    {
                        Helpers.ConsolePrint("NHM_API_info", "GetSmaAPI OK");
                    }
                    return true;
                }
                Helpers.ConsolePrint("NHM_API_info", "GetSmaAPI ERROR");
                return false;

            }
            catch (Exception erapi)
            {
                Helpers.ConsolePrint("NHM_API_info", erapi.ToString());
                Helpers.ConsolePrint("NHM_API_info", "GetSmaAPI fatal ERROR");
                return false;
            }
            return false;

        }

        private static void LoadSMA()
        {
            if (!ConfigManager.GeneralConfig.NoShowApiInLog)
            {
                Helpers.ConsolePrint("SMA", "Trying LoadSMA");
            }
            try
            {
                /*
                if (System.IO.File.Exists("configs\\versions.dat"))
                {
                    FileStream fs1 = new FileStream("configs\\versions.dat", FileMode.Open, FileAccess.Read);
                    StreamReader w1 = new StreamReader(fs1);
                    String fakeSMA1 = w1.ReadToEnd();
                    dynamic message1 = JsonConvert.DeserializeObject(fakeSMA1);
                    //      Helpers.ConsolePrint("SOCKET-oldSMA", "Received: " + fakeSMA1);
                    Helpers.ConsolePrint("SOCKET", "Using previous versions data");
                    w1.Close();
                    if (message1.method == "versions")
                    {
                        SetVersion(message1.legacy.Value);
                    }
                }
                else
                {
                    //                    Helpers.ConsolePrint("SOCKET", "Using default SMA");

                    //                    dynamic defversion = "{\"method\":\"versions\",\"v2\":\"2.0.1.1\",\"legacy\":\"1.8.1.5\"}";
                    //                    JArray verdata = (JArray.Parse(defversion));
                    //                    SetVersion(verdata.legacy.Value);
                }
                */
                //******
                if (!GetSmaAPI())
                {
                    if (System.IO.File.Exists("configs\\sma.dat"))
                    {
                        /*
                        if (AlgorithmRates == null || niceHashData == null)
                        {
                            niceHashData = new NiceHashData();
                            AlgorithmRates = niceHashData.NormalizedSMA();
                        }
                        */

                        dynamic jsonData = (File.ReadAllText("configs\\sma.dat"));
                        Helpers.ConsolePrint("SOCKET", "Using previous SMA");
                        JArray smadata = (JArray.Parse(jsonData));
                        SetAlgorithmRates(smadata);
                    }
                    else
                    {
                        Helpers.ConsolePrint("SOCKET", "Using default SMA");
                        /*
                        if (AlgorithmRates == null || niceHashData == null)
                        {
                            niceHashData = new NiceHashData();
                            AlgorithmRates = niceHashData.NormalizedSMA();
                        }
                        */
                        dynamic defsma = "[[5,\"0.00031031\"],[7,\"0.00401\"],[8,\"0.26617936\"],[14,\"0.00677556\"],[20,\"0.00833567\"],[21,\"0.00005065\"],[22,\"352.1073569\"],[23,\"0.00064179\"],[24,\"620.89332464\"],[25,\"0.00009207\"],[26,\"0.01044116\"],[27,\"0.00005085\"],[28,\"0.00003251\"],[29,\"0.00778864\"]]";
                        JArray smadata = (JArray.Parse(defsma));
                        SetAlgorithmRates(smadata);
                    }
                }
                //******
                if (System.IO.File.Exists("configs\\balance.dat"))
                {
                    FileStream fs3 = new FileStream("configs\\balance.dat", FileMode.Open, FileAccess.Read);
                    StreamReader w3 = new StreamReader(fs3);
                    String fakeSMA3 = w3.ReadToEnd();
                    dynamic message3 = JsonConvert.DeserializeObject(fakeSMA3);
                    //Helpers.ConsolePrint("SOCKET-oldSMA", "Received: " + fakeSMA3);
                    Helpers.ConsolePrint("SOCKET", "Using previous balance");
                    w3.Close();
                    if (message3.method == "balance")
                    {
                        SetBalance(message3.value.Value);
                    }
                }
            }
            catch (Exception ersma)
            {
                Helpers.ConsolePrint("SOCKET", "Using default SMA");
                /*
                if (AlgorithmRates == null || niceHashData == null)
                {
                    niceHashData = new NiceHashData();
                    AlgorithmRates = niceHashData.NormalizedSMA();
                }
                */
                dynamic defsma = "[[5,\"0.00031031\"],[7,\"0.00401\"],[8,\"0.26617936\"],[14,\"0.00677556\"],[20,\"0.00833567\"],[21,\"0.00005065\"],[22,\"352.1073569\"],[23,\"0.00064179\"],[24,\"620.89332464\"],[25,\"0.00009207\"],[26,\"0.01044116\"],[27,\"0.00005085\"],[28,\"0.00003251\"],[29,\"0.00778864\"]]";
                JArray smadata = (JArray.Parse(defsma));
                SetAlgorithmRates(smadata);
                Helpers.ConsolePrint("OLDSMA", ersma.ToString());
            }
        }

        public static string GetVersion(string worker)
        {
            string url = "https://api.github.com/repos/angelbbs/nicehashminerlegacy/releases";
            string r1 = GetGitHubAPIData(url);
            //Helpers.ConsolePrint("GITHUB!", r1);
            //string r1 = GetNiceHashApiData(url, "");
            if (r1 == null) return null;
            github_version[] nhjson;
            try
            {
                nhjson = JsonConvert.DeserializeObject<github_version[]>(r1, Globals.JsonSettings);
                var latest = Array.Find(nhjson, (n) => n.target_commitish == "master");
                return latest.tag_name;
            }
            catch
            { }
            return "";
        }

        public static string GetGitHubAPIData(string URL)
        {
            string ResponseFromServer;
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                HttpWebRequest WR = (HttpWebRequest)WebRequest.Create(URL);
                WR.UserAgent = "NiceHashMinerLegacy/" + Application.ProductVersion;
                WR.Timeout = 10 * 1000;
                WR.Credentials = CredentialCache.DefaultCredentials;
                //idHTTP1.IOHandler:= IdSSLIOHandlerSocket1;
                // ServicePointManager.SecurityProtocol = (SecurityProtocolType)SslProtocols.Tls12;
                Thread.Sleep(100);
                WebResponse Response = WR.GetResponse();
                Stream SS = Response.GetResponseStream();
                SS.ReadTimeout = 5 * 1000;
                StreamReader Reader = new StreamReader(SS);
                ResponseFromServer = Reader.ReadToEnd();
                if (ResponseFromServer.Length == 0 || (ResponseFromServer[0] != '{' && ResponseFromServer[0] != '['))
                    throw new Exception("Not JSON!");
                Reader.Close();
                Response.Close();
            }
            catch (Exception ex)
            {
                Helpers.ConsolePrint("GITHUB", ex.Message);
                return null;
            }
            return ResponseFromServer;
        }

        private static void SocketOnOnConnectionEstablished(object sender, EventArgs e)
        {
            //DeviceStatus_Tick(null); // Send device to populate rig stats
            //LoadSMA(); //for first run
            //string ghv = GetVersion("");
            //Helpers.ConsolePrint("GITHUB", ghv);
            //SetVersion(ghv);
            OnConnectionEstablished?.Invoke(null, EventArgs.Empty);
        }

        #endregion

        #region Incoming socket calls
        public static void ClearAlgorithmRates()
        {
            var _currentSma = new Dictionary<AlgorithmType, NiceHashSma>();
            try
            { 
            foreach (AlgorithmType algo in Enum.GetValues(typeof(AlgorithmType)))
            {
                if (algo >= 0)
                {
                    var paying = 0d;

                    _currentSma[algo] = new NiceHashSma
                    {
                        Port = (int)algo + 3333,
                        Name = algo.ToString().ToLower(),
                        Algo = (int)algo,
                        Paying = paying
                    };
                    //_recentPaying[algo] = new List<double>
                    //{
                    //    0
                    //};
                }
            }

          //  Initialized = true;


         //   NHSmaData.UpdateSmaPaying(payingDict);

                OnSmaUpdate?.Invoke(null, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Helpers.ConsolePrint("SOCKET", e.ToString());
            }
        }
        private static void SetAlgorithmRates(JArray data)
        {
            try
            {
                var payingDict = new Dictionary<AlgorithmType, double>();
                if (data != null)
                {
                    foreach (var algo in data)
                    {
                        var algoKey = (AlgorithmType) algo[0].Value<int>();
                        payingDict[algoKey] = algo[1].Value<double>();
                    }
                }

                NHSmaData.UpdateSmaPaying(payingDict);

                OnSmaUpdate?.Invoke(null, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Helpers.ConsolePrint("SOCKET", e.ToString());
            }
        }

        private static void SetStableAlgorithms(JArray stable)
        {
            var stables = stable.Select(algo => (AlgorithmType) algo.Value<int>());
            NHSmaData.UpdateStableAlgorithms(stables);
        }

        private static void SetBalance(string balance)
        {
            try
            {
                if (double.TryParse(balance, NumberStyles.Float, CultureInfo.InvariantCulture, out var bal))
                {
                    Balance = bal;
                    OnBalanceUpdate?.Invoke(null, EventArgs.Empty);
                }
            }
            catch (Exception e)
            {
                Helpers.ConsolePrint("SOCKET", e.ToString());
            }
        }

        private static void SetVersion(string version)
        {
            Version = version;
            OnVersionUpdate?.Invoke(null, EventArgs.Empty);
        }

        private static void SetExchangeRates(string data)
        {
            try
            {
                var exchange = JsonConvert.DeserializeObject<ExchangeRateJson>(data);
                if (exchange?.exchanges_fiat != null && exchange.exchanges != null)
                {
                    foreach (var exchangePair in exchange.exchanges)
                    {
                        if (exchangePair.TryGetValue("coin", out var coin) &&
                            coin == "BTC" &&
                            exchangePair.TryGetValue("USD", out var usd) &&
                            double.TryParse(usd, NumberStyles.Float, CultureInfo.InvariantCulture, out var usdD))
                        {
                            ExchangeRateApi.UsdBtcRate = usdD;
                            break;
                        }
                    }
                    ExchangeRateApi.UpdateExchangesFiat(exchange.exchanges_fiat);
                    Thread.Sleep(200);
                    OnExchangeUpdate?.Invoke(null, EventArgs.Empty);
                }
            }
            catch (Exception e)
            {
                Helpers.ConsolePrint("SOCKET", e.ToString());
            }
        }

        #endregion

        #region Outgoing socket calls

        public static async Task SetCredentials(string btc, string worker)
        {
            if (Configs.ConfigManager.GeneralConfig.NewPlatform)
            {
                return;
            }
                var data = new NicehashCredentials
            {
                btc = btc,
                worker = worker
            };
            if (BitcoinAddress.ValidateBitcoinAddress(data.btc) && BitcoinAddress.ValidateWorkerName(worker))
            {
                var sendData = JsonConvert.SerializeObject(data);

                // Send as task since SetCredentials is called from UI threads
                //Task.Factory.StartNew(() => _socket?.SendData(sendData));
                if (_socket != null)
                {
                    await _socket.SendData(sendData);
                }
            }
        }
        //******************************************************************************************************************************
        // private static void SendMinerStatus(bool sendDeviceNames)
        /*
        private static async void MinerStatus_Tick(object state)
        {
            bool sendDeviceNames = true;
            //var devices = AvailableDevices.Devices;
            var devices = ComputeDeviceManager.Available.Devices;
            //var rigStatus = ApplicationStateManager.CalcRigStatusString();
            var rigStatus = "STOPPED";
            var paramList = new List<JToken>
            {
                rigStatus
            };

            var deviceList = new JArray();
            foreach (var device in devices)
            {
                try
                {
                    var array = new JArray
                    {
                        sendDeviceNames ? device.Name : "",
                        device.B64Uuid  // TODO
                    };
                    var status = DeviceReportStatus(device.DeviceType, device.State);
                    array.Add(status);

                    array.Add((int)Math.Round(device.Load));

                    var speedsJson = new JArray();
                    var speeds = MiningStats.GetSpeedForDevice(device.Uuid);
                    if (speeds != null && device.State == DeviceState.Mining)
                    {
                        foreach (var kvp in speeds)
                        {
                            speedsJson.Add(new JArray((int)kvp.type, kvp.speed));
                        }
                    }
                    array.Add(speedsJson);

                    // Hardware monitoring
                    array.Add((int)Math.Round(device.Temp));
                    array.Add(device.FanSpeed);
                    array.Add((int)Math.Round(device.PowerUsage));

                    // Power mode
                    array.Add((int)device.PowerLevel);

                    // Intensity mode
                    array.Add(0);

                    deviceList.Add(array);
                }
                catch (Exception e)
                {
                    NiceHashMinerLegacy.Common.Logger.Error("SOCKET", e.Message);
                }
            }

            paramList.Add(deviceList);

            var data = new MinerStatusMessage
            {
                param = paramList
            };
            var sendData = JsonConvert.SerializeObject(data);

            // This function is run every minute and sends data every run which has two auxiliary effects
            // Keeps connection alive and attempts reconnection if internet was dropped
            _socket?.SendData(sendData);
        }
        */
        //******************************************************************************************************************************
        //private static async void DeviceStatus_Tick(object sender, ElapsedEventArgs e1)
        private static async void DeviceStatus_Tick(object state)
        {
            if (Configs.ConfigManager.GeneralConfig.NewPlatform)
            {
              //  return;
            }

                var devices = ComputeDeviceManager.Available.Devices;
            var deviceList = new List<JArray>();
            var activeIDs = MinersManager.GetActiveMinersIndexes();
            foreach (var device in devices)
            {
                try
                {
                    var array = new JArray
                    {
                        device.Index,
                        device.Name
                    };
                    Thread.Sleep(50);
                    var status = Convert.ToInt32(activeIDs.Contains(device.Index)) + ((int) device.DeviceType + 1) * 2;
                    array.Add(status);
                    array.Add((int) Math.Round(device.Load));
                    array.Add((int) Math.Round(device.Temp));
                    array.Add(device.FanSpeed);
                    deviceList.Add(array);
                }
                catch (Exception e) { Helpers.ConsolePrint("SOCKET", e.ToString()); }
            }
            var data = new NicehashDeviceStatus
            {
                devices = deviceList
            };
            var sendData = JsonConvert.SerializeObject(data);
            // This function is run every minute and sends data every run which has two auxiliary effects
            // Keeps connection alive and attempts reconnection if internet was dropped
            // _socket?.SendData(sendData);

            if (_socket != null)
            {
                await _socket.SendData(sendData);
            }
        }

        #endregion

        public static string GetNiceHashApiData(string url, string worker)
        {
            var responseFromServer = "";
            try
            {
                var activeMinersGroup = MinersManager.GetActiveMinersGroup();

                var wr = (HttpWebRequest) WebRequest.Create(url);
                wr.UserAgent = "NiceHashMiner/" + Application.ProductVersion;
                if (worker.Length > 64) worker = worker.Substring(0, 64);
                wr.Headers.Add("NiceHash-Worker-ID", worker);
                wr.Headers.Add("NHM-Active-Miners-Group", activeMinersGroup);
                wr.Timeout = 5 * 1000;
                var response = wr.GetResponse();
                var ss = response.GetResponseStream();
                if (ss != null)
                {
                    ss.ReadTimeout = 3 * 1000;
                    var reader = new StreamReader(ss);
                    responseFromServer = reader.ReadToEnd();
                    if (responseFromServer.Length == 0 || responseFromServer[0] != '{')
                        throw new Exception("Not JSON!");
                    reader.Close();
                }
                response.Close();
            }
            catch (Exception ex)
            {
                Helpers.ConsolePrint("NICEHASH", ex.Message);
                return null;
            }
            return responseFromServer;
        }
        public static string GetNiceHashApiDataNew(string url, string worker)
        {
            var responseFromServer = "";
            try
            {
                var activeMinersGroup = MinersManager.GetActiveMinersGroup();

                var wr = (HttpWebRequest)WebRequest.Create(url);
                wr.UserAgent = "NiceHashMiner/" + Application.ProductVersion;
                //if (worker.Length > 64) worker = worker.Substring(0, 64);
                //wr.Headers.Add("NiceHash-Worker-ID", worker);
                //wr.Headers.Add("NHM-Active-Miners-Group", activeMinersGroup);
                wr.Timeout = 5 * 1000;
                var response = wr.GetResponse();
                var ss = response.GetResponseStream();
                if (ss != null)
                {
                    ss.ReadTimeout = 3 * 1000;
                    var reader = new StreamReader(ss);
                    responseFromServer = reader.ReadToEnd();
                    if (responseFromServer.Length == 0 || responseFromServer[0] != '{')
                        throw new Exception("Not JSON!");
                    reader.Close();
                }
                response.Close();
            }
            catch (Exception ex)
            {
                Helpers.ConsolePrint("NICEHASH", ex.Message);
                return null;
            }
            return responseFromServer;
        }
    }
}
namespace TimerDispose
{
    /// <summary>
    /// A timer-containing class that can be disposed safely by allowing the timer 
    /// callback that it must exit/cancel its processes
    /// </summary>
    class TimerOwner : IDisposable
    {
        const int dueTime = 5 * 100;       //halve a second
        const int timerPeriod = 1 * 1000;   //Repeat timer every one second (make it Timeout.Inifinite if no repeating required)

        private TimerCanceller timerCanceller = new TimerCanceller();

        private System.Threading.Timer timer;

        public TimerOwner()
        {
            timerInit(dueTime);
        }

        byte[] dummy = new byte[100000];

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dueTime">Pass dueTime for the first time, then TimerPeriod will be passed automatically</param>
        private void timerInit(int dueTime)
        {

            timer = new System.Threading.Timer(timerCallback,
                timerCanceller,     //this is the trick, it will be kept in the heap until it is consumed by the callback 
                dueTime,
                Timeout.Infinite
            );

        }

        private void timerCallback(object state)
        {
            try
            {
                //First exit if the timer was stoped before calling callback. This info is saved in state
                var canceller = (TimerCanceller)state;
                if (canceller.Cancelled)
                {
                    return; //
                }

                //Your logic goes here. Please take care ! the callback might have already been called before stoping the timer
                //and we might be already here after intending of stoping the timer. In most cases it is fine but try not to consume
                //an object of this class because it might be already disposed. If you have to do that, hopefully it will be catched by
                //the ObjectDisposedException below




                dummy[1] = 50;  //just messing up with the object after it might be disposed/nulled

                //Yes, we need to check again. Read above note
                if (canceller.Cancelled)
                {
                    //Dispose any resource that might have been initialized above
                    return; //
                }

                if (timerPeriod != Timeout.Infinite)
                {
                    timerInit(timerPeriod);
                }
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine("A disposed object accessed");
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine("A nulled object accessed");
            }
            catch (Exception ex)
            {

            }
        }

        public void releaseTimer()
        {
            timerCanceller.Cancelled = true;
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            timer.Dispose();
        }

        public void Dispose()
        {
            releaseTimer();
            dummy = null;   //for testing
            GC.SuppressFinalize(this);
        }
    }

    class TimerCanceller
    {
        public bool Cancelled = false;
    }

}

