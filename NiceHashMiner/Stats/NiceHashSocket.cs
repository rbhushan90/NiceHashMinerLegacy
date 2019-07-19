﻿using Newtonsoft.Json;
using NiceHashMiner.Switching;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebSocketSharp;

using NiceHashMinerLegacy.UUID;

namespace NiceHashMiner.Stats
{
    public class NiceHashSocket
    {
        #region JSON Models
#pragma warning disable 649, IDE1006

        private class NicehashLogin
        {
            public string method = "login";
            public string version;
            public int protocol = 1;
        }

        private class NicehashLoginNew
        {
            public string method = "login";
            public string version;
            public int protocol = 1;
            public string btc;
            public string worker;
            public string group;
            public string rig;
        }
#pragma warning restore 649, IDE1006
        #endregion

        public static WebSocket _webSocket;
        public bool IsAlive => _webSocket.ReadyState == WebSocketState.Open;
        public static bool _restartConnection = false;
        private bool _attemptingReconnect;
        public static bool _endConnection = false;
        private bool _connectionAttempted;
        public  static bool _connectionEstablished;
        private readonly Random _random = new Random();
        private readonly string _address;

        public event EventHandler OnConnectionEstablished;
        public event EventHandler<MessageEventArgs> OnDataReceived;
        public event EventHandler OnConnectionLost;
        public static string RigID => UUID.GetDeviceB64UUID();

        public NiceHashSocket(string address)
        {
            _address = address;
        }

        //****************************************************************************************************************
        
        public void StartConnectionNew(string btc = null, string worker = null, string group = null)
        {
            NHSmaData.InitializeIfNeeded();
            _connectionAttempted = true;

            // TESTNET
#if TESTNET || TESTNETDEV || PRODUCTION_NEW
            _login.rig = ApplicationStateManager.RigID;

            if (btc != null) _login.btc = btc;
            if (worker != null) _login.worker = worker;
            if (group != null) _login.group = group;
#endif
            // Helpers.ConsolePrint("rig:", RigID);
            try
            {
                if (_webSocket == null)
                {
                    _webSocket = new WebSocket(_address, true);

                    //_webSocket.OnOpen += Login;
                    }
                else
                {
                    Helpers.ConsolePrint("SOCKET", $"Credentials change reconnecting nhmws");
                    _connectionEstablished = false;
                    _restartConnection = true;
                    //_webSocket?.Close(CloseStatusCode.Normal, $"Credentials change reconnecting {ApplicationStateManager.Title}.");
                    //_webSocket?.Close(CloseStatusCode.Normal, $"Credentials change reconnecting.");
                    _webSocket.Close();
                }
                Helpers.ConsolePrint("SOCKET", "Connecting");
               // _webSocket.OnOpen += Login;
                _webSocket.OnOpen += ConnectCallback;
                _webSocket.OnMessage += ReceiveCallbackNew;
                _webSocket.OnError += ErrorCallbackNew;
                _webSocket.OnClose += CloseCallbackNew;
                _webSocket.Log.Level = LogLevel.Debug;
                _webSocket.Log.Output = (data, s) => Helpers.ConsolePrint("SOCKET", data.ToString());
                _webSocket.EnableRedirection = true;

                _webSocket.Connect();
                Helpers.ConsolePrint("SOCKET", "Connected");
                _connectionEstablished = true;
                _restartConnection = false;
            }
            catch (Exception e)
            {
                Helpers.ConsolePrint("SOCKET", e.ToString());
            }
        }

        public void EndConnectionNew()
        {
            Helpers.ConsolePrint("SOCKET", "End connection to new platform");
            _endConnection = true;
            // TODO client away
            //CloseStatusCode.Away
            _webSocket?.Close(CloseStatusCode.Normal, $"Exiting");
        }

        private void ReceiveCallbackNew(object sender, MessageEventArgs e)
        {
            OnDataReceived?.Invoke(this, e);
        }

        private static void ErrorCallbackNew(object sender, ErrorEventArgs e)
        {
            Helpers.ConsolePrint("NiceHashSocket", $"Error occured: {e.Message}");
        }

        private void CloseCallbackNew(object sender, CloseEventArgs e)
        {
            Helpers.ConsolePrint("NiceHashSocket", $"Connection closed code {e.Code}: {e.Reason}");
            if (!_restartConnection)
            {
                AttemptReconnectNew();
            }
        }
        
        private void Login(object sender, EventArgs e)
        {
            try
            {
                 //var loginJson = JsonConvert.SerializeObject(_login);
                var loginJson = "{ \"method\":\"login\",\"version\":\"NHML/1.9.2.7\",\"protocol\":3,\"btc\":\"3F2v4K3ExF1tqLLwa6Ac3meimSjV3iUZgQ\",\"worker\":\"worker1\",\"group\":\"\",\"rig\":\"0-AMMDquXCml2iU-g4tcFQEQ\"}";
                //{"method":"login","version":"NHML/1.9.2.7","protocol":3,"btc":"3F2v4K3ExF1tqLLwa6Ac3meimSjV3iUZgQ","worker":"worker1","group":"","rig":""}
                //{ "method":"login","version":"NHML/1.9.2.7","protocol":3,"btc":"3F2v4K3ExF1tqLLwa6Ac3meimSjV3iUZgQ","worker":"worker1","group":"","rig":"0-AMMDquXCml2iU-g4tcFQEQ"}
                SendDataNew(loginJson);

                OnConnectionEstablished?.Invoke(null, EventArgs.Empty);
            }
            catch (Exception er)
            {
                Helpers.ConsolePrint("SOCKET", er.Message);
            }
        }
        
        // Don't call SendData on UI threads, since it will block the thread for a bit if a reconnect is needed
        public bool SendDataNew(string data, bool recurs = false)
        {
            //TESTNET
#if TESTNET || TESTNETDEV || PRODUCTION_NEW
            // skip sending if no btc set send only login
            if (CredentialValidators.ValidateBitcoinAddress(_login.btc) == false && data.Contains("{\"method\":\"login\"") == false)
            {
                NiceHashMinerLegacy.Common.Logger.Info("SOCKET", "Skipping SendData no BTC address");
                return false;
            }
#endif
            try
            {
                // Make sure connection is open
                if (_webSocket != null && IsAlive)
                {
                    Helpers.ConsolePrint("SOCKET", $"Sending data: {data}");
                    _webSocket.Send(data);
                    return true;
                }
                else if (_webSocket != null)
                {
                    if (AttemptReconnectNew() && !recurs)
                    {
                        // Reconnect was successful, send data again (safety to prevent recursion overload)
                        SendDataNew(data, true);
                    }
                    else
                    {
                        Helpers.ConsolePrint("SOCKET", "Socket connection unsuccessfull, will try again on next device update (1min)");
                    }
                }
                else
                {
                    if (!_connectionAttempted)
                    {
                        Helpers.ConsolePrint("SOCKET", "Data sending attempted before socket initialization");
                    }
                    else
                    {
                        Helpers.ConsolePrint("SOCKET", "webSocket not created, retrying");
                        StartConnectionNew();
                    }
                }
            }
            catch (Exception e)
            {
                Helpers.ConsolePrint("NiceHashSocket", $"Error occured while sending data: {e.Message}");
            }
            return false;
        }

        private bool AttemptReconnectNew()
        {
            if (_attemptingReconnect || _endConnection)
            {
                return false;
            }
            if (IsAlive)
            {
                // no reconnect needed
                return true;
            }
            _attemptingReconnect = true;
            var sleep = _connectionEstablished ? 10 + _random.Next(0, 20) : 0;
            Helpers.ConsolePrint("SOCKET", $"Attempting reconnect in {sleep} seconds");
            // More retries on first attempt
            var retries = _connectionEstablished ? 5 : 15;
            if (_connectionEstablished)
            {
                // Don't wait if no connection yet
                Thread.Sleep(sleep * 1000);
            }
            else
            {
                // Don't not wait again
                _connectionEstablished = true;
            }
            for (var i = 0; i < retries; i++)
            {
                try
                {
                    _webSocket.Connect();
                    Thread.Sleep(100);
                    if (IsAlive)
                    {
                        _attemptingReconnect = false;
                        return true;
                    }
                }
                catch (InvalidOperationException e)
                {
                    if (e.Message == "A series of reconnecting has failed.")
                    {
                        // Need to recreate websocket
                        Helpers.ConsolePrint("SOCKET", "Recreating socket");
                        _webSocket = null;
                        StartConnectionNew();
                        break;
                    }
                }
                catch (Exception e)
                {
                    Helpers.ConsolePrint("NiceHashSocketNew", $"Error while attempting reconnect: {e.Message}");
                }
                Thread.Sleep(1000);
            }
            _attemptingReconnect = false;
            OnConnectionLost?.Invoke(null, EventArgs.Empty);
            return false;
        }

        //****************************************************************************************************************

        public void StartConnection()
        {
            Helpers.ConsolePrint("SOCKET", "Start connection to old platform");
            NHSmaData.InitializeIfNeeded();
            _connectionAttempted = true;

            try
            {
                if (_webSocket == null)
                {
                    _webSocket = new WebSocket(_address, true);
                } else
                {
                    _connectionEstablished = false;
                    _restartConnection = true;
                    _webSocket.Close();
                }

                //_webSocket.OnOpen += Login;
                _webSocket.OnOpen += ConnectCallback;
                _webSocket.OnMessage += ReceiveCallback;
                _webSocket.OnError += ErrorCallback;
                _webSocket.OnClose += CloseCallback;
                _webSocket.Log.Level = LogLevel.Debug;
                _webSocket.Log.Output = (data, s) => Helpers.ConsolePrint("SOCKET", data.ToString());
                _webSocket.EnableRedirection = true;
                _webSocket.Connect();
                _connectionEstablished = true;
                _restartConnection = false;
            } catch (Exception e)
            {
                Helpers.ConsolePrint("SOCKET", e.ToString());
            }
        }

        public void StartConnectionold()
        {
            NHSmaData.InitializeIfNeeded();
            _connectionAttempted = true;
            try
            {
                if (_webSocket == null)
                {
                    _webSocket = new WebSocket(Links.NhmSocketAddress_old, true);
                }
                else
                {
                    _webSocket.Close();
                }
                _webSocket.OnOpen += ConnectCallback;
                _webSocket.OnMessage += ReceiveCallback;
                _webSocket.OnError += ErrorCallback;
                _webSocket.OnClose += CloseCallback;
                _webSocket.Log.Level = LogLevel.Debug;
                _webSocket.Log.Output = (data, s) => Helpers.ConsolePrint("SOCKET_OLD", data.ToString());
                _webSocket.EnableRedirection = true;
                _webSocket.Connect();
                _connectionEstablished = true;
                _restartConnection = false;
            }
            catch (Exception e)
            {
                Helpers.ConsolePrint("SOCKET_OLD", e.ToString());
            }
        }

        private void ConnectCallback(object sender, EventArgs e)
        {
            /*
            Sending data: { "method":"login","version":"NHML/1.9.2.7","protocol":3,"btc":"3F2v4K3ExF1tqLLwa6Ac3meimSjV3iUZgQ","worker":"worker1","group":"","rig":"0-AMMDquXCml2iU-g4tcFQEQ"}
[17:02:00] [SOCKET] Sending data: {"method":"miner.status","params":["PENDING",[["Intel(R) Core(TM) i7-3630QM CPU @ 2.40GHz","1-YBxRn6UfL1O7dUk6NNR5EA",9,17,[],-1,-1,-1,-1,0]]]}
*/
            try
            {
                //send login
                int protocol = 1;
                string btc;
                string worker;
                string group = "";
                string rig = RigID;
                //var version = "NHML/" + Application.ProductVersion;
                var version = "NHML/1.9.1.7";
                if (Configs.ConfigManager.GeneralConfig.NewPlatform)
                {
                    protocol = 3;
                    version = "NHML/1.9.2.7";
                    btc = Configs.ConfigManager.GeneralConfig.BitcoinAddressNew;
                    worker = Configs.ConfigManager.GeneralConfig.WorkerName;


                    var login = new NicehashLoginNew
                    {
                        version = version,
                        protocol = protocol,
                        btc = btc,
                        worker = worker,
                        group = group,
                        rig = rig

                    };
                    var loginJson = JsonConvert.SerializeObject(login);
                    //loginJson = loginJson.Replace("{", " { ");
                    SendDataNew(loginJson);
                } else
                {
                    protocol = 1;
                    version = "NHML/1.9.1.7";


                    var login = new NicehashLogin
                    {
                        version = version,
                        protocol = protocol
                    };
                    var loginJson = JsonConvert.SerializeObject(login);
                        SendDataNew(loginJson);
                }
                if (Configs.ConfigManager.GeneralConfig.NewPlatform)
                {
                    NiceHashStats.DeviceStatus_TickNew("PENDING");
                    Thread.Sleep(100);
                    NiceHashStats.DeviceStatus_TickNew("STOPPED");
                }
                /*
               if (Configs.ConfigManager.GeneralConfig.NewPlatform)
                {
                    loginJson = "{ \"method\":\"login\",\"version\":\"NHML/1.9.2.7\",\"protocol\":3,\"btc\":\"3F2v4K3ExF1tqLLwa6Ac3meimSjV3iUZgQ\",\"worker\":\"worker1\",\"group\":\"\",\"rig\":\"0-AMMDquXCml2iU-g4tcFQEQ\"}";
                }
                */

                OnConnectionEstablished?.Invoke(null, EventArgs.Empty);
            } catch (Exception er)
            {
                Helpers.ConsolePrint("SOCKET", er.ToString());
            }
        }

        private void ReceiveCallback(object sender, MessageEventArgs e)
        {
            OnDataReceived?.Invoke(this, e);
        }

        private static void ErrorCallback(object sender, ErrorEventArgs e)
        {
            Helpers.ConsolePrint("SOCKET", e.ToString());
        }

        private void CloseCallback(object sender, CloseEventArgs e)
        {
            if (!_restartConnection)
            {
                Helpers.ConsolePrint("SOCKET", $"Connection closed code {e.Code}: {e.Reason}");
                AttemptReconnect();
            }
        }
        private Task<bool> SendAsync(string data)
        {
            return Task.Run(() =>
            {
                var t = new TaskCompletionSource<bool>();
                _webSocket.SendAsync(data, b => t.TrySetResult(b));
                return t.Task;
            });
        }
        // Don't call SendData on UI threads, since it will block the thread for a bit if a reconnect is needed
        // public bool SendData(string data, bool recurs = false)
        public async Task<bool> SendData(string data, bool recurs = false)
        {
            try
            {
                if (_webSocket != null && IsAlive)
                {
                    // Make sure connection is open
                    // Verify valid JSON and method
                    dynamic dataJson = JsonConvert.DeserializeObject(data);
                    if (dataJson.method == "credentials.set" || dataJson.method == "devices.status" || dataJson.method == "miner.status" || dataJson.method == "login" || dataJson.method == "executed")
                    {
                        Helpers.ConsolePrint("SOCKET", "Sending data: " + data);
                        _webSocket.Send(data);
                        //return true;
                        return await SendAsync(data);
                    }
                } else if (_webSocket != null)
                {
                    if (AttemptReconnect() && !recurs)
                    {
                        // Reconnect was successful, send data again (safety to prevent recursion overload)
                        //SendData(data, true);
                        await SendData(data, true);
                    } else
                    {
                        Helpers.ConsolePrint("SOCKET", "Socket connection unsuccessfull, will try again on next device update (1min)");
                    }
                } else
                {
                    if (!_connectionAttempted)
                    {
                        Helpers.ConsolePrint("SOCKET", "Data sending attempted before socket initialization");
                    } else
                    {
                        Helpers.ConsolePrint("SOCKET", "webSocket not created, retrying");
                        StartConnection();
                    }
                }
            } catch (Exception e)
            {
                Helpers.ConsolePrint("SOCKET", e.ToString());
            }
            return false;
        }

        private bool AttemptReconnect()
        {
            if (_attemptingReconnect)
            {
                return false;
            }
            if (IsAlive)
            {
                // no reconnect needed
                return true;
            }
            _attemptingReconnect = true;
            var sleep = _connectionEstablished ? 10 + _random.Next(0, 20) : 0;
            Helpers.ConsolePrint("SOCKET", "Attempting reconnect in " + sleep + " seconds");
            // More retries on first attempt
            var retries = _connectionEstablished ? 5 : 25;
            if (_connectionEstablished)
            {
                // Don't wait if no connection yet
                Thread.Sleep(sleep * 1000);
            } else
            {
                // Don't not wait again
                _connectionEstablished = true;
            }
            for (var i = 0; i < retries; i++)
            {
                try
                {
                    _webSocket.Connect();
                    Thread.Sleep(100);
                    if (IsAlive)
                    {
                        _attemptingReconnect = false;
                        return true;
                    }
                }
                catch (InvalidOperationException e)
                {
                    if (e.Message == "A series of reconnecting has failed.")
                    {
                        // Need to recreate websocket
                        Helpers.ConsolePrint("SOCKET", "Try old method");
                        _webSocket = null;
                        StartConnectionold();
                        break;
                    }
                }
                catch (Exception e)
                {
                    Helpers.ConsolePrint("SOCKET", $"Error while attempting reconnect: {e}");
                }
                Thread.Sleep(1000);
            }
            _attemptingReconnect = false;
            OnConnectionLost?.Invoke(null, EventArgs.Empty);
            return false;
        }
    }
}
