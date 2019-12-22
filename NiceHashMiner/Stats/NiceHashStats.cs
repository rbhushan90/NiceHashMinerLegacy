﻿/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
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

using NiceHashMiner.Configs;
using static NiceHashMiner.Devices.ComputeDeviceManager;
using NiceHashMinerLegacy.UUID;
using NiceHashMiner.Miners.Grouping;
using System.Management;
using System.Text;

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

    //internal static class NiceHashStats
    internal class NiceHashStats
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
        private class NicehashDeviceStatusNew
        {
            public string method = "miner.status";
            public List<JArray> devices;
        }
    private class MinerStatusMessage
    {
        public string method = "miner.status";
        [JsonProperty("params")]
        public List<JToken> param { get; set; }
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

        public static bool remoteMiningStart = false;
        public static bool remoteMiningStop = false;
        public static bool remoteUpdateUI = false;


        public static void StartConnection(string address)
        {


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


            if (Configs.ConfigManager.GeneralConfig.NewPlatform)
            {
                _deviceUpdateTimer = new System.Threading.Timer(DeviceStatus_TickNew, null, DeviceUpdateInterval, DeviceUpdateInterval);
            } else
            {
                _deviceUpdateTimer = new System.Threading.Timer(DeviceStatus_Tick, null, DeviceUpdateInterval, DeviceUpdateInterval);
            }

//            string ghv = GetVersion("");
  //          Helpers.ConsolePrint("GITHUB", ghv);
    //        if (ghv != null)
      //      {
        //        SetVersion(ghv);
          //  }
        }

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
                 //   Helpers.ConsolePrint("SOCKET", "Received: " + e.Data);
                    dynamic message = JsonConvert.DeserializeObject(e.Data);
                   // Helpers.ConsolePrint("SOCKET", "Received1: " + e.Data);
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
                                if (ConfigManager.GeneralConfig.MOPA5)
                                {
                                    ClearAlgorithmRates();
                                }

                                SetAlgorithmRates(message.data);
                                GetSmaAPI();

                                // if (!GetSmaAPI())
                                // {
                                // }

                                //***************************
                                /*
                                if (ConfigManager.GeneralConfig.NewPlatform)
                                {
                                    SetAlgorithmRates(message.data);
                                }
                                */
                                break;
                            }

                        case "balance":
                          //  Helpers.ConsolePrint("SOCKET", "Received2: " + e.Data);
                            ExchangeRateApi.GetNewBTCRate();
                            //Helpers.ConsolePrint("SOCKET", "Received3: " + e.Data);
                            SetBalance(message.value.Value);
                            //Helpers.ConsolePrint("SOCKET", "Received4: " + e.Data);
                            break;
                        //case "versions":
                        //    SetVersion(message.legacy.Value);
                        //    break;
                        //case "burn":
                        //    OnVersionBurn?.Invoke(null, new SocketEventArgs(message.message.Value));
                        //    break;
                        case "mining.start":
                             RemoteMiningStart(message.id.Value.ToString(), message.device.Value);
                            break;
                        case "mining.stop":
                            RemoteMiningStop(message.id.Value.ToString(), message.device.Value);
                            break;
                        case "mining.set.username":
                            RemoteMiningNotImplemented(message.id.Value.ToString());
                            break;
                        case "mining.set.worker":
                            RemoteMiningNotImplemented(message.id.Value.ToString());
                            break;
                        case "mining.set.group":
                            RemoteMiningNotImplemented(message.id.Value.ToString());
                            break;
                        case "mining.enable":
                            RemoteMiningEnable(message.id.Value.ToString(), message.device.Value.ToString(), true);
                            break;
                        case "mining.disable":
                            RemoteMiningEnable(message.id.Value.ToString(), message.device.Value.ToString(), false);
                            break;
                        case "mining.set.power_mode":
                            RemoteMiningNotImplemented(message.id.Value.ToString());
                            break;
                        case "exchange_rates":
                            SetExchangeRates(message.data.Value);
                            break;
                    }
                }
               // Helpers.ConsolePrint("SOCKET", "Received8: ");
            } catch (Exception er)
            {
                Helpers.ConsolePrint("SOCKET", er.ToString());
            }

           // Helpers.ConsolePrint("SOCKET", "Received9: ");
            for (int h = 0; h < 24; h += 3)
            {

                var timeFrom1 = new TimeSpan(h, 00, 0);
                var timeTo1 = new TimeSpan(h, 01, 30);
                var timeNow = DateTime.Now.TimeOfDay;
               // Helpers.ConsolePrint("SOCKET", "Received10: ");
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
              //  Helpers.ConsolePrint("SOCKET", "Received11: ");
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

        public class RootobjectCurrent
        {
            public MiningAlgorithms[] miningAlgorithms { get; set; }
        }
        public class MiningAlgorithms
        {
            public string algorithm { get; set; }
            public string title { get; set; }
            public string speed { get; set; }
            public string paying { get; set; }
        }

        public class Rootobject5m
        {
            public Algos5m[] algos { get; set; }
        }
        public class Algos5m
        {
            public int a { get; set; }
            public string p { get; set; }
            public float s { get; set; }
        }
        public class Rootobject24h
        {
            public Algos24h[] algos { get; set; }
        }
        public class Algos24h
        {
            public int a { get; set; }
            public string p { get; set; }
            public float s { get; set; }
        }

        public class ProfitsSMA
        {
            public string Method { get; set; }
            public IList<IList<object>> Data { get; set; }
        }
        public static async Task RemoteMiningEnable(string id, string deviceToSwitch, bool Enabled)
        {
            if (!ConfigManager.GeneralConfig.Allow_remote_management)
            {
                Helpers.ConsolePrint("REMOTE", "Remote management disabled");
                var cExecutedDisabled = "{\"method\":\"executed\",\"params\":[" + id + ",1,\"Remote management disabled\"]}";
                await _socket.SendData(cExecutedDisabled);
                return;
            }
            Helpers.ConsolePrint("REMOTE", "id: "+id+" device: "+ deviceToSwitch);

            string type;
            string b64Web;
            string nuuid = "";
            var devices = ComputeDeviceManager.Available.Devices;
            var deviceList = new JArray();
            foreach (var device in devices)
            {
                try
                {
                    if (device.DeviceType == DeviceType.CPU)
                    {
                        type = "1";
                        b64Web = UUID.GetB64UUID(device.NewUuid);
                        nuuid = $"{type}-{b64Web}";
                    }
                    if (device.DeviceType == DeviceType.NVIDIA)
                    {
                        type = "2";
                        b64Web = UUID.GetB64UUID(device.Uuid);
                        nuuid = $"{type}-{b64Web}";
                    }
                    if (device.DeviceType == DeviceType.AMD)
                    {
                        type = "3";
                        var uuidHEX = UUID.GetHexUUID(device.Uuid);//это не правильный uuid, но будет работать
                        var Newuuid = $"AMD-{uuidHEX}";
                        b64Web = UUID.GetB64UUID(Newuuid);
                        nuuid = $"{type}-{b64Web}";
                    }
                    var deviceName = device.Name;
                    device.Enabled = Enabled;
                    remoteUpdateUI = true;
                    /*
                    if (rigStatus != "PENDING")
                    {
                        deviceName = "";
                    }
                    */

                }
                catch (Exception e) { Helpers.ConsolePrint("REMOTE", e.ToString()); }
            }


            //var cExecutedNotImplemented = "{\"method\":\"executed\",\"params\":[" + id + ",1,\"Not implemented in Fork Fix " + ConfigManager.GeneralConfig.ForkFixVersion.ToString().Replace(",", ".") + "\"]}";
            var cExecuted = "{\"method\":\"executed\",\"params\":[" + id + ",0]}";
            await _socket.SendData(cExecuted);
            return;
        }
        public static async Task RemoteMiningNotImplemented(string id)
        {
            if (!ConfigManager.GeneralConfig.Allow_remote_management)
            {
                Helpers.ConsolePrint("REMOTE", "Remote management disabled");
                var cExecutedDisabled = "{\"method\":\"executed\",\"params\":[" + id + ",1,\"Remote management disabled\"]}";
                await _socket.SendData(cExecutedDisabled);
                return;
            }
            Helpers.ConsolePrint("REMOTE", "Not implemented");
            var cExecutedNotImplemented = "{\"method\":\"executed\",\"params\":[" + id + ",1,\"Not implemented in Fork Fix " + ConfigManager.GeneralConfig.ForkFixVersion.ToString().Replace(",",".") + "\"]}";
            await _socket.SendData(cExecutedNotImplemented);
            return;
        }
            public static async Task RemoteMiningStart(string id, string device)
        {
            if (!ConfigManager.GeneralConfig.Allow_remote_management)
            {
                Helpers.ConsolePrint("REMOTE", "Remote management disabled");
                var cExecutedDisabled = "{\"method\":\"executed\",\"params\":[" + id + ",1,\"Remote management disabled\"]}";
                await _socket.SendData(cExecutedDisabled);
                return;
            }
            var cExecuted = "{\"method\":\"executed\",\"params\":[" + id + ",0]}";
            if (Miner.IsRunningNew)
            {
                await _socket.SendData(cExecuted);
                Helpers.ConsolePrint("REMOTE", "Already mining");
                return;
            }
            remoteMiningStart = true;
            Thread.Sleep(3000);
            await _socket.SendData(cExecuted);
            Helpers.ConsolePrint("REMOTE", "Mining start. ID:" + id + " Device:" + device);
           //Thread.Sleep(1000);
           //await _socket.SendData(cExecuted);
        }
        public static async Task RemoteMiningStop(string id, string device)
        {
            if (!ConfigManager.GeneralConfig.Allow_remote_management)
            {
                Helpers.ConsolePrint("REMOTE", "Remote management disabled");
                var cExecutedDisabled = "{\"method\":\"executed\",\"params\":[" + id + ",1,\"Remote management disabled\"]}";
                await _socket.SendData(cExecutedDisabled);
                return;
            }
            var cExecuted = "{\"method\":\"executed\",\"params\":[" + id + ",0]}";
            if (!Miner.IsRunningNew)
            {
                await _socket.SendData(cExecuted);
                Helpers.ConsolePrint("REMOTE", "Already stopped");
                return;
            }
            remoteMiningStop = true;
            Thread.Sleep(2000);
            await _socket.SendData(cExecuted);
            Helpers.ConsolePrint("REMOTE", "Mining stop. ID:" + id + " Device:" + device);
            //Thread.Sleep(1000);
            //await _socket.SendData(cExecuted);
        }

        public static bool GetSmaAPICurrentOld()
        {
            Helpers.ConsolePrint("NHM_API_info", "Trying GetSmaAPICurrentOld");

            if (ConfigManager.GeneralConfig.NewPlatform)
            {
                //NHSmaData.Initialized = true;
                //return false;
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
                     //   Helpers.ConsolePrint("NHM_API_info", resp);
                    }

                    dynamic list;
                    if (ConfigManager.GeneralConfig.NewPlatform)
                    {
                        list = JsonConvert.DeserializeObject<Rootobject>(resp);
                    }
                    else
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
                                Helpers.ConsolePrint("SMA-DATA-APICurrentOld: ", Enum.GetName(typeof(AlgorithmType), result.algo) + " - " + result.paying);
                            }
                        }
                        outProf = outProf + "  [\n" + "    " + result.algo + ",\n" + "    " + result.paying + "\n" + "  ],\n";

                    }
                    outProf = outProf.Remove(outProf.Length - 2) + "]";

                  // Helpers.ConsolePrint("SMA-DATA-APICurrentOld: ", outProf);
                    JArray smadata = (JArray.Parse(outProf));

                    NiceHashStats.SetAlgorithmRates(smadata);

                    FileStream fs = new FileStream("configs\\sma.dat", FileMode.Create, FileAccess.Write);
                    StreamWriter w = new StreamWriter(fs);
                    w.Write(smadata);
                    //w.Write(JsonConvert.SerializeObject(message));
                    w.Flush();
                    w.Close();
                    if (!ConfigManager.GeneralConfig.NoShowApiInLog)
                    {
                        Helpers.ConsolePrint("NHM_API_info", "GetSmaAPICurrentOld OK");
                    }
                    return true;
                }
                Helpers.ConsolePrint("NHM_API_info", "GetSmaAPICurrentOld ERROR");
                return false;

            }
            catch (Exception erapi)
            {
                Helpers.ConsolePrint("NHM_API_info", erapi.ToString());
                Helpers.ConsolePrint("NHM_API_info", "GetSmaAPICurrentOld fatal ERROR");
                return false;
            }
            return false;

        }

        public static bool GetSmaAPICurrent()
        {
            Helpers.ConsolePrint("NHM_API_info", "Trying GetSmaAPICurrent");

            if (!ConfigManager.GeneralConfig.NewPlatform)
            {
                return true;
            }

            try
            {
                string resp;
                    resp = NiceHashStats.GetNiceHashApiData("https://api2.nicehash.com/main/api/v2/public/simplemultialgo/info", "x");
                if (resp != null)
                {
                    if (!ConfigManager.GeneralConfig.NoShowApiInLog)
                    {
                       // Helpers.ConsolePrint("NHM_API_info", resp);
                    }

                    dynamic list;
                        list = JsonConvert.DeserializeObject<RootobjectCurrent>(resp);

                    ProfitsSMA profdata = new ProfitsSMA();

                    List<ProfitsSMA> profdata2 = new List<ProfitsSMA>();

                    string outProf = "[\n";

                    var _currentSma = new Dictionary<AlgorithmType, NiceHashSma>();
                    foreach (var miningAlgorithms in list.miningAlgorithms)
                    {
                        int Algo = 0;
                        foreach (AlgorithmType algo in Enum.GetValues(typeof(AlgorithmType)))
                        {
                            if (algo >= 0)
                            {
                               Algo = (int)algo;
                                var AlgorithmName = AlgorithmNiceHashNames.GetName(algo);
                                if (AlgorithmName == miningAlgorithms.title)
                                {
                                    if (!ConfigManager.GeneralConfig.NoShowApiInLog)
                                    {
                                        Helpers.ConsolePrint("SMA-DATA-APICurrent: ", miningAlgorithms.title + " - " + Algo + " - " + miningAlgorithms.paying);
                                    }
                                    outProf = outProf + "  [\n" + "    " + Algo + ",\n" + "    " + miningAlgorithms.paying + "\n" + "  ],\n";
                                    break;
                                }
                            }
                        }
                    }
                    outProf = outProf.Remove(outProf.Length - 2) + "]";

                   // Helpers.ConsolePrint("SMA-DATA-APICurrent: ", outProf);
                    JArray smadata = (JArray.Parse(outProf));

                    NiceHashStats.SetAlgorithmRates(smadata, 10);
                    /*
                    FileStream fs = new FileStream("configs\\sma.dat", FileMode.Create, FileAccess.Write);
                    StreamWriter w = new StreamWriter(fs);
                    w.Write(smadata);
                    //w.Write(JsonConvert.SerializeObject(message));
                    w.Flush();
                    w.Close();
                    */
                    if (!ConfigManager.GeneralConfig.NoShowApiInLog)
                    {
                        Helpers.ConsolePrint("NHM_API_info", "GetSmaAPICurrent OK");
                    }
                    return true;
                }
                Helpers.ConsolePrint("NHM_API_info", "GetSmaAPICurrent ERROR");
                return false;

            }
            catch (Exception erapi)
            {
                Helpers.ConsolePrint("NHM_API_info", erapi.ToString());
                Helpers.ConsolePrint("NHM_API_info", "GetSmaAPICurrent fatal ERROR");
                return false;
            }
            return false;

        }
        public static bool GetSmaAPI5m()
        {
            Helpers.ConsolePrint("NHM_API_info", "Trying GetSmaAPI5m");

            if (!ConfigManager.GeneralConfig.NewPlatform)
            {
                return true;
            }

            try
            {
                string resp;
                resp = NiceHashStats.GetNiceHashApiData("https://api2.nicehash.com/main/api/v2/public/stats/global/current", "x");
                if (resp != null)
                {
                    if (!ConfigManager.GeneralConfig.NoShowApiInLog)
                    {
                      //  Helpers.ConsolePrint("NHM_API_info", resp);
                    }

                    dynamic list;
                    list = JsonConvert.DeserializeObject<Rootobject5m>(resp);

                    ProfitsSMA profdata = new ProfitsSMA();

                    List<ProfitsSMA> profdata2 = new List<ProfitsSMA>();

                    string outProf = "[\n";

                    var _currentSma = new Dictionary<AlgorithmType, NiceHashSma>();
                    foreach (var algos in list.algos)
                    {
                                {
                                    if (!ConfigManager.GeneralConfig.NoShowApiInLog)
                                    {
                                        Helpers.ConsolePrint("SMA-DATA-API5m: ", algos.a + " - " +  algos.p);
                                    }
                                    outProf = outProf + "  [\n" + "    " + algos.a + ",\n" + "    \"" + algos.p + "\"\n" + "  ],\n";
                                }
                    }
                    outProf = outProf.Remove(outProf.Length - 2) + "]";

                   //  Helpers.ConsolePrint("SMA-DATA-APICurrent: ", outProf);
                    JArray smadata = (JArray.Parse(outProf));

                    NiceHashStats.SetAlgorithmRates(smadata, 10);
                    /*
                    FileStream fs = new FileStream("configs\\sma.dat", FileMode.Create, FileAccess.Write);
                    StreamWriter w = new StreamWriter(fs);
                    w.Write(smadata);
                    //w.Write(JsonConvert.SerializeObject(message));
                    w.Flush();
                    w.Close();
                    */
                    if (!ConfigManager.GeneralConfig.NoShowApiInLog)
                    {
                        Helpers.ConsolePrint("NHM_API_info", "GetSmaAPI5m OK");
                    }
                    return true;
                }
                Helpers.ConsolePrint("NHM_API_info", "GetSmaAPI5m ERROR");
                return false;

            }
            catch (Exception erapi)
            {
                Helpers.ConsolePrint("NHM_API_info", erapi.ToString());
                Helpers.ConsolePrint("NHM_API_info", "GetSmaAPI5m fatal ERROR");
                return false;
            }
            return false;

        }

        public static bool GetSmaAPI24h()
        {
            Helpers.ConsolePrint("NHM_API_info", "Trying GetSmaAPI24h");

            if (!ConfigManager.GeneralConfig.NewPlatform)
            {
                return true;
            }

            try
            {
                string resp;
                resp = NiceHashStats.GetNiceHashApiData("https://api2.nicehash.com/main/api/v2/public/stats/global/24h", "x");
                if (resp != null)
                {
                    if (!ConfigManager.GeneralConfig.NoShowApiInLog)
                    {
                      //  Helpers.ConsolePrint("NHM_API_info", resp);
                    }

                    dynamic list;
                    list = JsonConvert.DeserializeObject<Rootobject24h>(resp);

                    ProfitsSMA profdata = new ProfitsSMA();

                    List<ProfitsSMA> profdata2 = new List<ProfitsSMA>();

                    string outProf = "[\n";

                    var _currentSma = new Dictionary<AlgorithmType, NiceHashSma>();
                    foreach (var algos in list.algos)
                    {
                        {
                            if (!ConfigManager.GeneralConfig.NoShowApiInLog)
                            {
                                Helpers.ConsolePrint("SMA-DATA-API24h: ", algos.a + " - " + algos.p);
                            }
                            outProf = outProf + "  [\n" + "    " + algos.a + ",\n" + "    \"" + algos.p + "\"\n" + "  ],\n";
                        }
                    }
                    outProf = outProf.Remove(outProf.Length - 2) + "]";

                 //   Helpers.ConsolePrint("SMA-DATA-API24h: ", outProf);
                    JArray smadata = (JArray.Parse(outProf));

                    NiceHashStats.SetAlgorithmRates(smadata, 10);
                    /*
                    FileStream fs = new FileStream("configs\\sma.dat", FileMode.Create, FileAccess.Write);
                    StreamWriter w = new StreamWriter(fs);
                    w.Write(smadata);
                    //w.Write(JsonConvert.SerializeObject(message));
                    w.Flush();
                    w.Close();
                    */
                    if (!ConfigManager.GeneralConfig.NoShowApiInLog)
                    {
                        Helpers.ConsolePrint("NHM_API_info", "GetSmaAPI24h OK");
                    }
                    return true;
                }
                Helpers.ConsolePrint("NHM_API_info", "GetSmaAPI24h ERROR");
                return false;

            }
            catch (Exception erapi)
            {
                Helpers.ConsolePrint("NHM_API_info", erapi.ToString());
                Helpers.ConsolePrint("NHM_API_info", "GetSmaAPI24h fatal ERROR");
                return false;
            }
            return false;

        }
        public static bool GetSmaAPI()
        {

            if (ConfigManager.GeneralConfig.NewPlatform)
            {
                if (ConfigManager.GeneralConfig.MOPA2)
                {
                    GetSmaAPICurrent(); //bug *10
                }
                if (ConfigManager.GeneralConfig.MOPA3)
                {
                    GetSmaAPI5m(); //bug *10
                }
                if (ConfigManager.GeneralConfig.MOPA4)
                {
                    GetSmaAPI24h(); //bug *10
                }
                if (ConfigManager.GeneralConfig.MOPA5)
                {
                    GetSmaAPICurrent(); //bug *10
                    GetSmaAPI5m(); //bug *10
                    GetSmaAPI24h(); //bug *10
                }
            }
            else
            {
                return GetSmaAPICurrentOld();
            }
            return true;
        }

        public static void LoadSMA()
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
                        dynamic defsma = "[[5,\"5.999963193e-07\"],[36,\"659.5557431\"],[42,\"33.73843367\"],[8,\"0.008899999201\"],[38,\"721.326346\"],[32,\"0.0002005014359\"],[24,\"5.666958028\"],[33,\"0.001290840303\"],[30,\"1.14051987\"],[37,\"100.0002035\"],[7,\"4.999946511e-06\"],[45,\"1324.211344\"],[22,\"0.3053852139\"],[34,\"2.787742099\"],[39,\"47716.02121\"],[44,\"7123.200162\"],[40,\"0.0006006972195\"],[20,\"0.001430123323\"],[23,\"6.561117697e-07\"],[43,\"5731.578685\"],[21,\"9.782149555e-11\"],[14,\"4.644947288e-05\"],[29,\"0.0001507043319\"],[28,\"9.999998266e-08\"],[31,\"0\"]]";
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
                var latest = Array.Find(nhjson, (n) => n.target_commitish == "master-old");
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
                Thread.Sleep(200);
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
           // GetSmaAPICurrent();
            OnConnectionEstablished?.Invoke(null, EventArgs.Empty);
        }

        #endregion

        #region Incoming socket calls
        public static void ClearAlgorithmRates()
        {
            var _currentSma = new Dictionary<AlgorithmType, NiceHashSma>();
            var payingDict = new Dictionary<AlgorithmType, double>();
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
                        payingDict[algo] = paying;
                        //_recentPaying[algo] = new List<double>
                        //{
                        //    0
                        //};
                    }
                }

                //NiceHashSma.Initialized = true;
                NHSmaData.UpdateSmaPaying(payingDict);
                Thread.Sleep(10);
                OnSmaUpdate?.Invoke(null, EventArgs.Empty);

            }
            catch (Exception e)
            {
                Helpers.ConsolePrint("SOCKET", e.ToString());
            }
        }
        private static void SetAlgorithmRates(JArray data, int mult = 1)
        {
            try
            {
                var payingDict = new Dictionary<AlgorithmType, double>();
                if (data != null)
                {
                    foreach (var algo in data)
                    {
                        var algoKey = (AlgorithmType)algo[0].Value<int>();
                        if (!NHSmaData.TryGetPaying(algoKey, out double paying))
                        {
                            Helpers.ConsolePrint("SMA API", "ERROR !NHSmaData.TryGetPaying: Unknown algo: "+ algoKey.ToString());

                            payingDict[algoKey] = algo[1].Value<double>() * mult;
                        }
                        else
                        {
                            if (paying != 0 && (paying * 8 < Math.Abs(algo[1].Value<double>()) * mult || (paying / 8 > Math.Abs(algo[1].Value<double>() * mult))))
                            {
                                Helpers.ConsolePrint("SMA API", "Bug found in: " + algoKey.ToString() + " " + paying.ToString() + "<>" + Math.Abs(algo[1].Value<double>() * mult));
                            }
                            else if (ConfigManager.GeneralConfig.UseNegativeProfit)
                            {
                                if (ConfigManager.GeneralConfig.MOPA5)
                                {
                                    payingDict[algoKey] = Math.Max(Math.Abs(algo[1].Value<double>()) * mult, paying);
                                }
                                else
                                {
                                    payingDict[algoKey] = Math.Abs(algo[1].Value<double>()) * mult;
                                }
                            }
                            else
                            {
                                if (ConfigManager.GeneralConfig.MOPA5)
                                {
                                    payingDict[algoKey] = Math.Max(algo[1].Value<double>() * mult, paying);
                                }
                                else
                                {
                                    payingDict[algoKey] = algo[1].Value<double>() * mult;
                                }
                            }
                        }
                        //DaggerOrderMaxPay = 0
                        //0.001418488464
                        if (ConfigManager.GeneralConfig.DaggerOrderMaxPay > 0 && algoKey == AlgorithmType.DaggerHashimoto && Math.Abs(algo[1].Value<double>()) > ConfigManager.GeneralConfig.DaggerOrderMaxPay)
                        {
                            Helpers.ConsolePrint("SMA", "Sets DaggerHashimoto to 0");
                            payingDict[algoKey] = 0;
                        }
                    }
                }

                NHSmaData.UpdateSmaPaying(payingDict);
                Thread.Sleep(10);
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
           // Helpers.ConsolePrint("SOCKET", "Received5: " + balance);
            try
            {
                if (double.TryParse(balance, NumberStyles.Float, CultureInfo.InvariantCulture, out var bal))
                {
                  //  Helpers.ConsolePrint("SOCKET", "Received6: " + balance);
                    Balance = bal;
                    OnBalanceUpdate?.Invoke(null, EventArgs.Empty);
                }
            }
            catch (Exception e)
            {
                Helpers.ConsolePrint("SOCKET", e.ToString());
            }
         //  Helpers.ConsolePrint("SOCKET", "Received7: " + balance);
        }

        public static void SetVersion(string version)
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

        public static async void DeviceStatus_TickNew(object state)
        {
            var devices = ComputeDeviceManager.Available.Devices;
            var rigStatus = CalcRigStatusString();
            var activeIDs = MinersManager.GetActiveMinersIndexes();
            string type;
            string b64Web;
            string nuuid = "";
            if (!Configs.ConfigManager.GeneralConfig.NewPlatform)
            {
                return;
            }
            if (state != null)
                rigStatus = state.ToString();
            {
            }
            var paramList = new List<JToken>
            {
                rigStatus
            };

            var deviceList = new JArray();
            foreach (var device in devices)
            {
                try
                {
                    /*
                    Helpers.ConsolePrint("DEVICE", device.DeviceType.ToString());
                    Helpers.ConsolePrint("DEVICE", device.Name);
                    Helpers.ConsolePrint("DEVICE", device.Uuid);
                    Helpers.ConsolePrint("DEVICE", device.NewUuid);
                    */
                    int status = 0;
                    if (device.DeviceType == DeviceType.CPU)
                    {
                        type = "1";
                        status = 8;
                        b64Web = UUID.GetB64UUID(device.NewUuid);
                        nuuid = $"{type}-{b64Web}";
                    }
                    if (device.DeviceType == DeviceType.NVIDIA)
                    {
                        type = "2";
                        status = 16;
                        b64Web = UUID.GetB64UUID(device.Uuid);
                        nuuid = $"{type}-{b64Web}";
                    }
                    if (device.DeviceType == DeviceType.AMD)
                    {
                        type = "3";
                        status = 24;
                        var uuidHEX = UUID.GetHexUUID(device.Uuid);//это не правильный uuid, но будет работать
                        var Newuuid = $"AMD-{uuidHEX}";
                        b64Web = UUID.GetB64UUID(Newuuid);
                        nuuid = $"{type}-{b64Web}";
                    }
                    var deviceName = device.Name;
                    /*
                    if (rigStatus != "PENDING")
                    {
                        deviceName = "";
                    }
                    */
                    var array = new JArray
                    {
                        deviceName,
                        nuuid
                    };

                    //var status = DeviceReportStatus(device.DeviceType, device.State);
                    //var status = Convert.ToInt32(activeIDs.Contains(device.Index)) + ((int)device.DeviceType + 1) * 2;

                    //var status = ((int)device.DeviceType + 9) + Convert.ToInt32(Miner.IsRunningNew);
                    //var status =  9;
                    if (device.Enabled)
                    {
                        status = status + Convert.ToInt32(Miner.IsRunningNew) + Convert.ToInt32(device.Enabled);
                    }
                    //var status = 9;
                    array.Add(status);

                    array.Add((int)Math.Round(device.Load));


                    var speedsJson = new JArray();

                    if (rigStatus != "MINING")
                    {
                        speedsJson.Add(new JArray()); // все скорости
                    }
                    else
                    {
                        speedsJson.Add(new JArray()); //  42, 55.0
                    }
                    //    }
                    //}
                    array.Add(speedsJson);

                    // Hardware monitoring
                    array.Add((int)Math.Round(device.Temp));
                    array.Add(device.FanSpeed);
                    array.Add((int)Math.Round(device.PowerUsage));

                    // Power mode
                    array.Add(-1);

                    // Intensity mode
                    array.Add(0);

                    deviceList.Add(array);
                }
                catch (Exception e) { Helpers.ConsolePrint("SOCKET", e.ToString()); }
            }

            try
            {
                paramList.Add(deviceList);

                var data = new MinerStatusMessage
                {
                    param = paramList
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
            catch (Exception e)
            {
                throw new Exception($"DeviceStatus_TickNew: {e.ToString()}");
               // Helpers.ConsolePrint("DeviceStatus_TickNew", e.ToString());
            }
}
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
                    /*
                    Helpers.ConsolePrint("DEVICE", device.DeviceType.ToString());
                    Helpers.ConsolePrint("DEVICE", device.Name);
                    Helpers.ConsolePrint("DEVICE", device.Uuid);
                    Helpers.ConsolePrint("DEVICE", device.NewUuid);
                    */
                    var array = new JArray
                    {
                        device.Index,
                        device.Name
                    };
                    Thread.Sleep(100);
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

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        public static string CalcRigStatusString()
        {
            /*
            var rigState = CalcRigStatus();
            switch (rigState)
            {
                case RigStatus.Offline: return "OFFLINE";
                case RigStatus.Stopped: return "STOPPED";
                case RigStatus.Mining: return "MINING";
                case RigStatus.Benchmarking: return "BENCHMARKING";
                case RigStatus.Error: return "ERROR";
                case RigStatus.Pending: return "PENDING";
                case RigStatus.Disabled: return "DISABLED";
            }
            */
            //return "UNKNOWN";
            //var m = GroupMiner.Miner;

            // skip if not running or if await already in progress

            if (Miner.IsRunningNew)
            {
                return "MINING";
            } else
            {
                return "STOPPED";
            }

        }
        /*
        public string B64Uuid
        {
            get
            {
                //UUIDs
                //RIG - 0
                //CPU - 1
                //GPU - 2 // NVIDIA
                //AMD - 3
                // types

                int type = 1; // assume type is CPU

                if (device. == DeviceType.NVIDIA)
                {
                    type = 2;
                }
                else if (DeviceType == DeviceType.AMD)
                {
                    type = 3;
                }
                var b64Web = UUID.GetB64UUID(Uuid);
                return $"{type}-{b64Web}";
            }
        }
        private string deviceB64Uuid()
        {
            return "";
        }
        private static void SendMinerStatus(bool sendDeviceNames)
        {
            var devices = Available.Devices;
            var rigStatus = CalcRigStatusString();
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
                        //device.B64Uuid  // TODO
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
                    NHM.Common.Logger.Error("SOCKET", e.Message);
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

