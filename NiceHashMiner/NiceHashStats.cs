using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using Newtonsoft.Json;
using NiceHashMiner.Enums;
using NiceHashMiner.Miners;
using NiceHashMiner.Devices;
using Newtonsoft.Json.Linq;
using WebSocketSharp;



namespace NiceHashMiner
{ 
    public class SocketEventArgs : EventArgs
    {
        public string Message = "";

        public SocketEventArgs(string message) {
            Message = message;
        }
    }
    class NiceHashStats {
#pragma warning disable 649
        #region JSON Models

        class nicehash_login {
            public string method = "login";
            public string version;
            public int protocol = 1;
        }

        class nicehash_credentials {
            public string method = "credentials.set";
            public string btc;
            public string worker;
        }

        class nicehash_device_status
        {
            public string method = "devices.status";
            public List<JArray> devices;
        }

        #endregion
#pragma warning restore 649

        const int deviceUpdateLaunchDelay = 20 * 1000;
        const int deviceUpdateInterval = 60 * 1000;

        public static Dictionary<AlgorithmType, NiceHashSMA> AlgorithmRates { get; private set; }
        private static NiceHashData niceHashData;
        public static double Balance { get; private set; }
        public static string Version { get; private set; }
        public static bool IsAlive { get { return NiceHashConnection.IsAlive; } }
        // Event handlers for socket
        public static event EventHandler OnBalanceUpdate = delegate { };
        public static event EventHandler OnSMAUpdate = delegate { };
        public static event EventHandler OnVersionUpdate = delegate { };
        public static event EventHandler OnConnectionLost = delegate { };
        public static event EventHandler OnConnectionEstablished = delegate { };
        public static event EventHandler<SocketEventArgs> OnVersionBurn = delegate { };

        static readonly Random random = new Random();

        static System.Threading.Timer deviceUpdateTimer;

        #region Socket
        private class NiceHashConnection
        {
            static WebSocket webSocket;
            public static bool IsAlive { get { return webSocket.IsAlive; } }
            static bool attemptingReconnect = false;
            static bool connectionAttempted = false;
            static bool connectionEstablished = false;

            public static void StartConnection(string address) {
                //           Helpers.ConsolePrint("SOCKET", "StartConnection");
                LoadSMA(); //for first run
                connectionAttempted = true;
                try {
                    if (webSocket == null) {
                        webSocket = new WebSocket(address);
                    } else {
                        webSocket.Close();
                    }
                    webSocket.OnOpen += ConnectCallback;
                    webSocket.OnMessage += ReceiveCallback;
                    webSocket.OnError += ErrorCallback;
                    webSocket.OnClose += CloseCallback;
                    webSocket.EmitOnPing = true;
                    webSocket.Log.Level = LogLevel.Debug;
                    webSocket.Log.Output = (data, s) => Helpers.ConsolePrint("SOCKET", data.ToString());
                    webSocket.ConnectAsync();
                    connectionEstablished = true;
                } catch (Exception e) {
                    Helpers.ConsolePrint("SOCKET", e.ToString());
                }
            }

            private static void ConnectCallback(object sender, EventArgs e) {
         //       Helpers.ConsolePrint("SOCKET", "ConnectCallback");
                try {
                    if (AlgorithmRates == null || niceHashData == null) {
                        niceHashData = new NiceHashData();
                        AlgorithmRates = niceHashData.NormalizedSMA();
                    }
                    //send login
                    var version = "NHML/" + Application.ProductVersion;
                    var login = new nicehash_login();
                    login.version = version;
                    var loginJson = JsonConvert.SerializeObject(login);
                    SendData(loginJson);

                    DeviceStatus_Tick(null);  // Send device to populate rig stats

                    OnConnectionEstablished.Emit(null, EventArgs.Empty);
                } catch (Exception er) {
                    Helpers.ConsolePrint("SOCKET", er.ToString());
                }
            }

            private static void ReceiveCallback(object sender, MessageEventArgs e) {
                
                try {
                    if (e.IsText) {
                        Helpers.ConsolePrint("SOCKET", "Received: " + e.Data);
                        dynamic message = JsonConvert.DeserializeObject(e.Data);
                        if (message.method == "sma") {
                            /*
                            foreach (var algo in message.data)
                            {
                                var algoKey = (AlgorithmType)algo[0].Value<int>();
                                niceHashData.AppendPayingForAlgo(algoKey, algo[1].Value<double>());
                                Helpers.ConsolePrint("SMA-DATA", algoKey.ToString() + " - " + algo[1].Value<double>().toString());
                            }
                            AlgorithmRates = niceHashData.NormalizedSMA();
                            OnSMAUpdate.Emit(null, EventArgs.Empty);
                            */
                           
                                FileStream fs = new FileStream("configs\\sma.dat", FileMode.Create, FileAccess.Write);
                                StreamWriter w = new StreamWriter(fs);
                                w.Write(message.data);
                                //w.Write(JsonConvert.SerializeObject(message));
                                w.Flush();
                                w.Close();
                            foreach (var algo in message.data)
                            {
                                var algoKey = (AlgorithmType)algo[0];
                                    Helpers.ConsolePrint("SMA-DATA-WS: ", Enum.GetName(typeof(AlgorithmType), algoKey) + " - " + algo[1]);
                            }
                            if (!GetSmaAPI())
                            {
                                SetAlgorithmRates(message.data);
                            }
                        } else if (message.method == "balance") {
                            FileStream fs = new FileStream("configs\\balance.dat", FileMode.Create, FileAccess.Write);
                            StreamWriter w = new StreamWriter(fs);
                            w.WriteAsync(e.Data);
                            w.Flush();
                            w.Close();
                            SetBalance(message.value.Value);
                        } else if (message.method == "versions") {
                            FileStream fs = new FileStream("configs\\versions.dat", FileMode.Create, FileAccess.Write);
                            StreamWriter w = new StreamWriter(fs);
                            w.WriteAsync(e.Data);
                            w.Flush();
                            w.Close();
                            SetVersion(message.legacy.Value);
                        } else if (message.method == "burn") {
                            OnVersionBurn.Emit(null, new SocketEventArgs(message.message.Value));
                        }
                    }
                } catch (Exception er) {
                    Helpers.ConsolePrint("SOCKET", er.ToString());
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
                try
                {
                    string resp = NiceHashStats.GetNiceHashAPIData("https://api.nicehash.com/api?method=simplemultialgo.info", "x");

                    if (resp != null)
                    {
                        Helpers.ConsolePrint("NHM_API_info", resp);
                        dynamic list = JsonConvert.DeserializeObject<Rootobject>(resp);
                        ProfitsSMA profdata = new ProfitsSMA();

                        List<ProfitsSMA> profdata2 = new List<ProfitsSMA>();

                        string outProf = "[\n";

                        foreach (var result in list.result.simplemultialgo)
                        {
                            if (!result.algo.ToString().Contains("UNUSED"))
                            {
                                Helpers.ConsolePrint("SMA-DATA-API: ", Enum.GetName(typeof(AlgorithmType), result.algo) + " - " + result.paying);
                            }
                            /*
                                   var algoKey = (AlgorithmType)result.algo;
                               CultureInfo temp_culture = System.Threading.Thread.CurrentThread.CurrentCulture;
                               System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");

                               Double paying2 = Double.Parse(result.paying);
                               System.Threading.Thread.CurrentThread.CurrentCulture = temp_culture;

                           */
                            outProf = outProf + "  [\n" + "    " + result.algo + ",\n" + "    " + result.paying + "\n" + "  ],\n";

                        }
                        outProf = outProf.Remove(outProf.Length - 2) + "]";
                       // Helpers.ConsolePrint("NHM_API_info2", outProf);
                        JArray smadata = (JArray.Parse(outProf));
                        if (AlgorithmRates == null || niceHashData == null)
                        {
                            niceHashData = new NiceHashData();
                            AlgorithmRates = niceHashData.NormalizedSMA();
                        }
                        NiceHashStats.SetAlgorithmRates(smadata);

                        FileStream fs = new FileStream("configs\\sma.dat", FileMode.Create, FileAccess.Write);
                        StreamWriter w = new StreamWriter(fs);
                        w.Write(smadata);
                        //w.Write(JsonConvert.SerializeObject(message));
                        w.Flush();
                        w.Close();
                        Helpers.ConsolePrint("NHM_API_info", "GetSmaAPI OK");
                        return true;
                    }
                    Helpers.ConsolePrint("NHM_API_info", "GetSmaAPI ERROR");
                    return false;

                }
                catch (Exception erapi) { Helpers.ConsolePrint("NHM_API_info", erapi.ToString());
                    Helpers.ConsolePrint("NHM_API_info", "GetSmaAPI fatal ERROR");
                    return false;
                }

                return false;

            }

            private static void LoadSMA()
            {
                Helpers.ConsolePrint("SMA", "Trying LoadSMA");
                try
                {
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
                    //******
                    if (!GetSmaAPI())
                    {
                        if (System.IO.File.Exists("configs\\sma.dat"))
                        {
                            if (AlgorithmRates == null || niceHashData == null)
                            {
                                niceHashData = new NiceHashData();
                                AlgorithmRates = niceHashData.NormalizedSMA();
                            }

                            dynamic jsonData = (File.ReadAllText("configs\\sma.dat"));
                            Helpers.ConsolePrint("SOCKET", "Using previous SMA");
                            JArray smadata = (JArray.Parse(jsonData));
                            SetAlgorithmRates(smadata);
                        }
                        else
                        {
                            Helpers.ConsolePrint("SOCKET", "Using default SMA");
                            if (AlgorithmRates == null || niceHashData == null)
                            {
                                niceHashData = new NiceHashData();
                                AlgorithmRates = niceHashData.NormalizedSMA();
                            }
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
                catch (Exception ersma) {
                    Helpers.ConsolePrint("SOCKET", "Using default SMA");
                    if (AlgorithmRates == null || niceHashData == null)
                    {
                        niceHashData = new NiceHashData();
                        AlgorithmRates = niceHashData.NormalizedSMA();
                    }
                    dynamic defsma = "[[5,\"0.00031031\"],[7,\"0.00401\"],[8,\"0.26617936\"],[14,\"0.00677556\"],[20,\"0.00833567\"],[21,\"0.00005065\"],[22,\"352.1073569\"],[23,\"0.00064179\"],[24,\"620.89332464\"],[25,\"0.00009207\"],[26,\"0.01044116\"],[27,\"0.00005085\"],[28,\"0.00003251\"],[29,\"0.00778864\"]]";
                    JArray smadata = (JArray.Parse(defsma));
                    SetAlgorithmRates(smadata);
                    Helpers.ConsolePrint("OLDSMA", ersma.ToString()); }
            }

            private static void ErrorCallback(object sender, WebSocketSharp.ErrorEventArgs e)
            {
                Helpers.ConsolePrint("SOCKET", e.ToString());
            }

            private static void CloseCallback(object sender, CloseEventArgs e) {
                Helpers.ConsolePrint("SOCKET", $"Connection closed code {e.Code}: {e.Reason}");
                LoadSMA();
                AttemptReconnect();
            }

            // Don't call SendData on UI threads, since it will block the thread for a bit if a reconnect is needed
            public static bool SendData(string data, bool recurs = false) {
                try { 
                    if (webSocket != null && webSocket.IsAlive) {  // Make sure connection is open
                        // Verify valid JSON and method
                        dynamic dataJson = JsonConvert.DeserializeObject(data);
                        if (dataJson.method == "credentials.set" || dataJson.method == "devices.status" || dataJson.method == "login") {
                            Helpers.ConsolePrint("SOCKET", "Sending data: " + data);
                            webSocket.Send(data);
                            return true;
                        }
                    } else if (webSocket != null) {
                        if (AttemptReconnect() && !recurs) {  // Reconnect was successful, send data again (safety to prevent recursion overload)
                            SendData(data, true);
                        } else {
                            Helpers.ConsolePrint("SOCKET", "Socket connection unsuccessfull, will try again on next device update (1min)");
                        }
                    } else {
                        if (!connectionAttempted) {
                            Helpers.ConsolePrint("SOCKET", "Data sending attempted before socket initialization");
                        } else {
                            Helpers.ConsolePrint("SOCKET", "webSocket not created, retrying");
                            StartConnection(Links.NHM_Socket_Address);
                        }
                    }
                } catch (Exception e) {
                    Helpers.ConsolePrint("SOCKET", e.ToString());
                }
                return false;
            }

            private static bool AttemptReconnect() {
                if (attemptingReconnect) {
                    return false;
                }
                if (webSocket.IsAlive) {  // no reconnect needed
                    return true;
                }
                attemptingReconnect = true;
                var sleep = connectionEstablished ? 10 + random.Next(0, 20) : 0;
                Helpers.ConsolePrint("SOCKET", "Attempting reconnect in " + sleep + " seconds");
                // More retries on first attempt
                var retries = connectionEstablished ? 5 : 25;
                if (connectionEstablished) {  // Don't wait if no connection yet
                    Thread.Sleep(sleep * 1000);
                } else {
                    // Don't not wait again
                    connectionEstablished = true;
                }
                for (int i = 0; i < retries; i++) {
                    webSocket.ConnectAsync();
                    Thread.Sleep(100);
                    if (webSocket.IsAlive) {
                        attemptingReconnect = false;
                        return true;
                    }
                    Thread.Sleep(1000);
                }
                attemptingReconnect = false;
                OnConnectionLost.Emit(null, EventArgs.Empty);
                return false;
            }
        }

        public static void StartConnection(string address) {
            NiceHashConnection.StartConnection(address);
            deviceUpdateTimer = new System.Threading.Timer(DeviceStatus_Tick, null, deviceUpdateInterval, deviceUpdateInterval);
        }

        #endregion

        #region Incoming socket calls
        public static void SetAlgorithmRates(JArray data) {
            try {
                foreach (var algo in data) {
                    var algoKey = (AlgorithmType)algo[0].Value<int>();
                    if (algo[1].Value<double>() != 0)
                    {
                       niceHashData.AppendPayingForAlgo(algoKey, algo[1].Value<double>());
                    }
                    else
                    {
                        if (!algoKey.ToString().Contains("UNUSED"))
                        {
                            Helpers.ConsolePrint("SMA-DATA-BUG", algoKey.ToString() + " - " + algo[1].Value<double>() + " use previous profit");
                        }
                    }
                }
                AlgorithmRates = niceHashData.NormalizedSMA();
                OnSMAUpdate.Emit(null, EventArgs.Empty);
            } catch (Exception e) {
                Helpers.ConsolePrint("SOCKET", e.ToString());
            }
        }

        private static void SetBalance(string balance) {
            try {
                double bal = 0d;
                double.TryParse(balance, NumberStyles.Number, CultureInfo.InvariantCulture, out bal);
                Balance = bal;
                OnBalanceUpdate.Emit(null, EventArgs.Empty);
            } catch (Exception e) {
                Helpers.ConsolePrint("SOCKET", e.ToString());
            }
        }

        private static void SetVersion(string version) {
            Version = version;
            OnVersionUpdate.Emit(null, EventArgs.Empty);
        }

        #endregion

        #region Outgoing socket calls

        public static void SetCredentials(string btc, string worker) {
            var data = new nicehash_credentials();
            data.btc = btc;
            data.worker = worker;
            if (BitcoinAddress.ValidateBitcoinAddress(data.btc) && BitcoinAddress.ValidateWorkerName(worker)) {
                var sendData = JsonConvert.SerializeObject(data);

                // Send as task since SetCredentials is called from UI threads
                Task.Factory.StartNew(() => NiceHashConnection.SendData(sendData));
            }
        }

        public static void DeviceStatus_Tick(object state) {
            var devices = ComputeDeviceManager.Avaliable.AllAvaliableDevices;
            var deviceList = new List<JArray>();
            var activeIDs = MinersManager.GetActiveMinersIndexes();
            foreach (var device in devices) {
                try {
                    var array = new JArray();
                    array.Add(device.Index);
                    array.Add(device.Name);
                    int status = Convert.ToInt32(activeIDs.Contains(device.Index)) + (((int)device.DeviceType + 1) * 2);
                    array.Add(status);
                    array.Add((uint)device.Load);
                    array.Add((uint)device.Temp);
                    array.Add((uint)device.FanSpeed);

                    deviceList.Add(array);
                } catch (Exception e) { Helpers.ConsolePrint("SOCKET-ErrorDeviceStatus", e.ToString()); }
            }
            var data = new nicehash_device_status();
            data.devices = deviceList;
            var sendData = JsonConvert.SerializeObject(data);
            // This function is run every minute and sends data every run which has two auxiliary effects
            // Keeps connection alive and attempts reconnection if internet was dropped
            NiceHashConnection.SendData(sendData);
        }

        #endregion

        public static string GetNiceHashAPIData(string URL, string worker)
        {
            string ResponseFromServer;
            try
            {
                string ActiveMinersGroup = MinersManager.GetActiveMinersGroup();

                HttpWebRequest WR = (HttpWebRequest)WebRequest.Create(URL);
                WR.UserAgent = "NiceHashMiner/" + Application.ProductVersion;
                if (worker.Length > 64) worker = worker.Substring(0, 64);
                WR.Headers.Add("NiceHash-Worker-ID", worker);
                WR.Headers.Add("NHM-Active-Miners-Group", ActiveMinersGroup);
                WR.Timeout = 30 * 1000;
                WebResponse Response = WR.GetResponse();
                Stream SS = Response.GetResponseStream();
                SS.ReadTimeout = 20 * 1000;
                StreamReader Reader = new StreamReader(SS);
                ResponseFromServer = Reader.ReadToEnd();
                if (ResponseFromServer.Length == 0 || ResponseFromServer[0] != '{')
                    throw new Exception("Not JSON!");
                Reader.Close();
                Response.Close();
            }
            catch (Exception ex)
            {
                Helpers.ConsolePrint("NICEHASH", ex.Message);
                return null;
            }

            return ResponseFromServer;
        }
    }
}
