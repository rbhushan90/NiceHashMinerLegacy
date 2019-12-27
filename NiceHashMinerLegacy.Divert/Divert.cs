using System;
using WinDivertSharp;
using WinDivertSharp.WinAPI;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;

namespace NiceHashMinerLegacy.Divert
{
    public class Divert
    {
       // private static Timer _divertTimer;
        public static volatile bool divert_running = true;
        public static IntPtr DivertHandle;
        private static readonly uint MaxPacket = 2048;

        private static string DevFeeIP = "";
        private static string DevFeeIPName = "";
        private static ushort DevFeePort = 0;
        private static string DivertIP = "";
        private static string DivertIPName = "";
        private static ushort DivertPort = 0;

        private static string NicehashDstIP;
        private static ushort NicehashSrcPort;
        private static ushort NicehashPort;

        private static string filter = "";

        private static string nicehashLogin = "";
        private static string loginJsonStart = "";
        private static string loginJsonEnd = "";

        private static string PacketPayloadData;

        internal static UInt32 SwapByteOrder(UInt32 value)
        {
            return
              ((value & 0xff000000) >> 24) |
              ((value & 0x00ff0000) >> 8) |
              ((value & 0x0000ff00) << 8) |
              ((value & 0x000000ff) << 24);
        }

        internal static UInt64 SwapByteOrder(UInt64 value)
        {
            return
              ((value & 0xff00000000000000L) >> 56) |
              ((value & 0x00ff000000000000L) >> 40) |
              ((value & 0x0000ff0000000000L) >> 24) |
              ((value & 0x000000ff00000000L) >> 8) |
              ((value & 0x00000000ff000000L) << 8) |
              ((value & 0x0000000000ff0000L) << 24) |
              ((value & 0x000000000000ff00L) << 40) |
              ((value & 0x00000000000000ffL) << 56);
        }

        public static ushort SwapOrder(ushort val)
        {
            return (ushort)(((val & 0xFF00) >> 8) | ((val & 0x00FF) << 8));
        }

        public static uint SwapOrder(uint val)
        {
            val = (val >> 16) | (val << 16);
            return ((val & 0xFF00) >> 8) | ((val & 0x00FF) << 8);
        }

        public static ulong SwapOrder(ulong val)
        {
            val = (val >> 32) | (val << 32);
            val = ((val & 0xFFFF0000FFFF0000) >> 16) | ((val & 0x0000FFFF0000FFFF) << 16);
            return ((val & 0xFF00FF00FF00FF00) >> 8) | ((val & 0x00FF00FF00FF00FF) << 8);
        }

        public static string ToString(byte[] bytes)
        {
            string response = string.Empty;

            foreach (byte b in bytes)
                response += (Char)b;

            return response;
        }

      //  public static unsafe string PacketPayloadToString(byte* bytes, uint length)
        public static unsafe string PacketPayloadToString(byte* bytes, uint length)
        {
            string data = "";
            for (int i = 0; i < length; i++)
            {
                if (bytes[i] >= 32)
                    data = data + (char)bytes[i];

            }
            return data;
        }

        public static unsafe byte* StringToPacketPayload(string data, int length)
        {
            byte* bytes = null;
            for (int i = 0; i < length; i++)
            {
                if (data[i] >= 32)
                {
                    bytes = bytes + (char)data[i];
                }
            }
            return bytes;
        }

        private static byte[] StringToByteArray(string str)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            return enc.GetBytes(str);
        }


        public static int RealPacketLength(WinDivertBuffer packet, uint length)
        {
            
            string data = "";
            int len = 0;
            Helpers.ConsolePrint("WinDivertSharp", "!!!!!!!!!!!!!!!!:  " + length.ToString() + " - " + packet.Length.ToString());
            for (int i = 0; i < length + 16; i++)
            {
                data = data + (char)packet[i];
                if (data.Contains("\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0"))
                {
                    Helpers.ConsolePrint("WinDivertSharp", "!!!!!!!!!!!!!!!! i=" + i.ToString());
                }
            }

            len = data.IndexOf("\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0");
            if (len < 0) len = (int)length;
            //data = null;
            //Helpers.ConsolePrint("WinDivertSharp", "!: " + Marshal.PtrToStringAnsi(packet).Length.ToString());
            //return (uint)Marshal.SizeOf(typeof(packet));
            return (int)len;
        }

        /*
        public static void Diversion()
        {
            _divertTimer = new System.Threading.Timer();
            _divertTimer.Tick += DivertTimer_Tick;
            _divertTimer.Interval = 1000;
            _divertTimer.Start();
        }
        */
        //private static async void DivertTimer_Tick(object sender, EventArgs e)
        //private static async void DivertTimer_Tick()
        /*
        [Serializable]
        public class ColorProfilesConfig
        {
            private int id;
            private string jsonrpc;
            private string method;
            private static readonly Color[] DefaultColorProfile = { SystemColors.Control, SystemColors.WindowText, SystemColors.Window, SystemColors.ControlText };
        }
        */

        public static async void DivertStart()
            {
            divert_running = true;
            Helpers.ConsolePrint("WinDivertSharp", "Divert START");
            //   _divertTimer.Stop();
            //   _divertTimer = null;

            
            DevFeeIP = "";
            DevFeeIPName = "pool.supportxmr.com";
            DevFeePort = 3333;

            DivertIP = "";
            DivertIPName = "randomxmonero.in.nicehash.com";
            DivertPort = 3380;
            NicehashPort = 3380; //порт основного соединения
            //DivertIPName = "pool.supportxmr.com";
            //DivertPort = 3333;

            //nicehashLogin = "42fV4v2EC4EALhKWKNCEJsErcdJygynt7RJvFZk8HSeYA9srXdJt58D9fQSwZLqGHbijCSMqSP4mU7inEEWNyer6F7PiqeX.devfee";
            nicehashLogin = "3F2v4K3ExF1tqLLwa6Ac3meimSjV3iUZgQ.devfee";
            //  nicehashLogin = "3F2v4K3ExF1tqLLwa6Ac3meimSjV3iUZgQ.worker20$0-HgaPFxnqIlqsPZDqXC+KyA";
            filter = "ip && tcp && (inbound ? (tcp.SrcPort == 3380) : (tcp.DstPort == 3333) || (tcp.DstPort == 3380))";//xmr
            
            
            /*
            DevFeeIP = "";
            DevFeeIPName = "us1.ethpool.org";
            DevFeePort = 3333;

            DivertIP = "";
            DivertIPName = "us1.ethpool.org";
            DivertPort = 3333;
            NicehashPort = 3353; //порт основного соединения

            filter = "ip && tcp && (inbound ? (tcp.SrcPort == 3353) : (tcp.DstPort == 3333) || (tcp.DstPort == 3353))";//dagger
            */


            uint errorPos = 0;


            if (!WinDivert.WinDivertHelperCheckFilter(filter, WinDivertLayer.Network, out string errorMsg, ref errorPos))
            {
                Helpers.ConsolePrint("WinDivertSharp", "Error in filter string at position: " + errorPos.ToString());
                Helpers.ConsolePrint("WinDivertSharp", "Error: " + errorMsg);
                return;
            }

            DivertHandle = WinDivert.WinDivertOpen(filter, WinDivertLayer.Network, 0, WinDivertOpenFlags.None);

            if (DivertHandle == IntPtr.Zero || DivertHandle == new IntPtr(-1))
            {
                Helpers.ConsolePrint("WinDivertSharp", "Invalid handle. Failed to open. Is run as Administrator?");
                return;
            }

            WinDivert.WinDivertSetParam(DivertHandle, WinDivertParam.QueueLen, 16384);
            WinDivert.WinDivertSetParam(DivertHandle, WinDivertParam.QueueTime, 8000);
            WinDivert.WinDivertSetParam(DivertHandle, WinDivertParam.QueueSize, 33554432);

            await RunDivert(DivertHandle);

            /*
            var threads = new List<Thread>();

            for (int i = 0; i < Environment.ProcessorCount; ++i)
            {
                threads.Add(new Thread(() =>
                {
                    RunDiversion(handle);
                }));

                threads.Last().Start();
            }

            foreach (var dt in threads)
            {
                dt.Join();
            }
            
            WinDivert.WinDivertClose(handle);
            */

            /*
                        while (true)
                        {
                            ProcessTest(s_testData.UpperWinDivertHandle, "(tcp? tcp.DstPort == 80: true) and (udp? udp.DstPort == 80: true)",
             TestData.EchoRequestData, true);
                        }
                        */
        }
        public static void DivertStop()
        {
            if (DivertHandle != IntPtr.Zero || DivertHandle != new IntPtr(-1))
            {
                divert_running = false;
                Thread.Sleep(10);
                WinDivert.WinDivertClose(DivertHandle);
                Helpers.ConsolePrint("WinDivertSharp", "Divert STOP");
            }
        }

        private static Task<bool> RunDivert(IntPtr handle)
        {

            return Task.Run(() =>
            {
                var t = new TaskCompletionSource<bool>();
                RunDivert1(handle);
                return t.Task;
            });
        }

       // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static async Task RunDivert1(IntPtr handle)
        {
            var packet = new WinDivertBuffer();
            IPAddress oldDstAddress = null;

            var addr = new WinDivertAddress();
            int np = 1;
            uint readLen = 0;

            IPv4Header? ipv4Header = null;
            IPv6Header? ipv6Header = null;
            IcmpV4Header? icmpV4Header = null;
            IcmpV6Header? icmpV6Header = null;
            TcpHeader? tcpHeader = null;
            UdpHeader? udpHeader = null;

            //Span<byte> packetData = null;
            byte[] packetData = null;
            NativeOverlapped recvOverlapped;

            IntPtr recvEvent = IntPtr.Zero;
            uint recvAsyncIoLen = 0;
            bool modified = false;

            do
            {
                try
                {
                    // Thread.Sleep(10);
                    if (divert_running)
                    {
                        ipv4Header = null;
                        ipv6Header = null;
                        icmpV4Header = null;
                        icmpV6Header = null;
                        tcpHeader = null;
                        udpHeader = null;
                        packetData = null;
                        readLen = 0;

                        recvAsyncIoLen = 0;
                        recvOverlapped = new NativeOverlapped();

                        recvEvent = Kernel32.CreateEvent(IntPtr.Zero, false, false, IntPtr.Zero);
                        //recvOverlapped.EventHandle = recvEvent;

                        if (recvEvent == IntPtr.Zero)
                        {
                            Helpers.ConsolePrint("WinDivertSharp", "Failed to initialize receive IO event.");
                            //continue;
                        }
                        addr.Reset();
                        recvOverlapped.EventHandle = recvEvent;

                        var result = WinDivert.WinDivertRecvEx(handle, packet, 0, ref addr, ref readLen, ref recvOverlapped);

                        if (!result)
                        {
                            var error = Marshal.GetLastWin32Error();
                            //Helpers.ConsolePrint("WinDivertSharp", "No error code: " + error.ToString());
                            // 997 == ERROR_IO_PENDING
                            if (error != 997)
                            {
                                Helpers.ConsolePrint($"WinDivertSharp", "Unknown IO error ID {0} while awaiting overlapped result.", error.ToString());
                                Kernel32.CloseHandle(recvEvent);
                                continue;
                            }

                            while (Kernel32.WaitForSingleObject(recvEvent, 1000) == (uint)WaitForSingleObjectResult.WaitTimeout) ;

                            if (!Kernel32.GetOverlappedResult(handle, ref recvOverlapped, ref recvAsyncIoLen, false))
                            {
                                Helpers.ConsolePrint("WinDivertSharp", "Failed to get overlapped result.");
                                Kernel32.CloseHandle(recvEvent);
                                continue;
                            }

                            readLen = recvAsyncIoLen;
                        }

                        Kernel32.CloseHandle(recvEvent);
                        //Helpers.ConsolePrint("WinDivertSharp", "Read packet lenght: " + readLen.ToString());
                        np++;
                        string cpacket0 = "";
                        for (int i = 0; i < readLen; i++)
                        {
                           // if (packet[i] >= 32)
                                cpacket0 = cpacket0 + (char)packet[i];

                        }
                        if (cpacket0.Length > 100)
                        File.WriteAllText(np.ToString()+"o.pkt", cpacket0);
                       
                        
                       
                        //stratum+tcp://daggerhashimoto.eu.nicehash.com:3353
                        
                        
                        try
                        {
                            System.Text.ASCIIEncoding ASCII = new System.Text.ASCIIEncoding();
                            IPHostEntry heserver = Dns.GetHostEntry(DevFeeIPName);
                            foreach (IPAddress curAdd in heserver.AddressList)
                            {
                                if (curAdd.AddressFamily.ToString() == ProtocolFamily.InterNetwork.ToString())
                                {
                                    DevFeeIP = curAdd.ToString();
                                    break; //only 1st IP
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Helpers.ConsolePrint("WinDivertSharp", "Exception: " + e.ToString());
                        }

                        try
                        {
                            System.Text.ASCIIEncoding ASCII = new System.Text.ASCIIEncoding();
                            IPHostEntry heserver = Dns.GetHostEntry(DivertIPName);
                            foreach (IPAddress curAdd in heserver.AddressList)
                            {
                                if (curAdd.AddressFamily.ToString() == ProtocolFamily.InterNetwork.ToString())
                                {
                                    DivertIP = curAdd.ToString();
                                    break; //only 1st IP
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Helpers.ConsolePrint("WinDivertSharp", "Exception: " + e.ToString());
                        }

                        //*************************************************************************************************************
                        var parse_result = WinDivert.WinDivertHelperParsePacket(packet, readLen); //**+++++++++++++++++++++++++


                        if (parse_result.IPv4Header != null)
                        {
                            Helpers.ConsolePrint("WinDivertSharp", "SrcAdr: " + parse_result.IPv4Header->SrcAddr.ToString() + ":" + SwapOrder(parse_result.TcpHeader->SrcPort).ToString());
                            Helpers.ConsolePrint("WinDivertSharp", "DstAdr: " + parse_result.IPv4Header->DstAddr.ToString() + ":" + SwapOrder(parse_result.TcpHeader->DstPort).ToString());
                        } else
                        {
                            modified = true;// not send
                            goto sendPacket;//hmm...
                        }

                        if (parse_result.TcpHeader->DstPort == SwapOrder(DevFeePort) && 
                            //addr.Direction == WinDivertDirection.Outbound &&
                                parse_result.IPv4Header->DstAddr.ToString() == DevFeeIP)// to/from devfee
                        {
                            Helpers.ConsolePrint("WinDivertSharp", "DEVFEE SESSION: " + addr.Direction.ToString());
                            if (parse_result.PacketPayloadLength > 0)
                            {
                                PacketPayloadData = PacketPayloadToString(parse_result.PacketPayload, parse_result.PacketPayloadLength);
                                Helpers.ConsolePrint("WinDivertSharp", PacketPayloadData);
                            }
                            modified = false;
                            goto Divert;//меняем данные в пакете
                        }
                        if (parse_result.TcpHeader->SrcPort == SwapOrder(NicehashSrcPort) || parse_result.TcpHeader->DstPort == SwapOrder(NicehashSrcPort))
                        {
                            //оригинальные пакеты с найсом не трогаем
                            Helpers.ConsolePrint("WinDivertSharp", "NICEHASH SESSION: " + addr.Direction.ToString());
                            if (parse_result.PacketPayloadLength > 0)
                            {
                                PacketPayloadData = PacketPayloadToString(parse_result.PacketPayload, parse_result.PacketPayloadLength);
                                Helpers.ConsolePrint("WinDivertSharp", PacketPayloadData);
                            }
                            modified = false;
                            goto sendPacket;
                        }

            //локальный порт не установлен или отличается от devfee сессии или от установленной сессии с найсом
            // (был дисконнект или failover)
            //тут надо еще определять и пропускать divert src порт?
                        if (parse_result.TcpHeader->DstPort == SwapOrder(NicehashPort) && addr.Direction == WinDivertDirection.Outbound)
                        {
                            //оригинальные пакеты с найсом не трогаем
                            Helpers.ConsolePrint("WinDivertSharp", "Store connections to nicehash:");
                            //Helpers.ConsolePrint("WinDivertSharp", "proccess: " + parse_result.TcpHeader->);
                            Helpers.ConsolePrint("WinDivertSharp", "Nicehash SrcAdr: " + parse_result.IPv4Header->SrcAddr.ToString() + ":" + SwapOrder(parse_result.TcpHeader->SrcPort).ToString());
                            Helpers.ConsolePrint("WinDivertSharp", "Nicehash DstAdr: " + parse_result.IPv4Header->DstAddr.ToString() + ":" + SwapOrder(parse_result.TcpHeader->DstPort).ToString());
                            Helpers.ConsolePrint("WinDivertSharp", "PacketPayloadLength: " + parse_result.PacketPayloadLength.ToString());
                            NicehashDstIP = parse_result.IPv4Header->DstAddr.ToString();//nicehash ip before devfee session
                            //сохраним локальный порт для определения назначения пакета(оригинальное соединение или divert)
                            NicehashSrcPort = SwapOrder(parse_result.TcpHeader->SrcPort);
                            modified = false;
                            goto sendPacket;
                        }

                       //********************************перехват
Divert:                        
                        if (parse_result.TcpHeader->DstPort == SwapOrder(DevFeePort) &&
                                addr.Direction == WinDivertDirection.Outbound &&
                                parse_result.IPv4Header->DstAddr.ToString() == DevFeeIP)//out to devfee
                        {
                                Helpers.ConsolePrint("WinDivertSharp", "OUTBOUND TO DEVFEE");

                                Helpers.ConsolePrint("WinDivertSharp", "PacketPayloadLength: " + parse_result.PacketPayloadLength.ToString());
                                Helpers.ConsolePrint("WinDivertSharp", "DevFee SrcAdr: " + parse_result.IPv4Header->SrcAddr.ToString() + ":" + SwapOrder(parse_result.TcpHeader->SrcPort).ToString());
                                Helpers.ConsolePrint("WinDivertSharp", "DevFee DstAdr: " + parse_result.IPv4Header->DstAddr.ToString() + ":" + SwapOrder(parse_result.TcpHeader->DstPort).ToString());

                                parse_result.IPv4Header->DstAddr = IPAddress.Parse(DivertIP);
                                parse_result.TcpHeader->DstPort = SwapOrder(DivertPort);
                                Helpers.ConsolePrint("WinDivertSharp", "New DevFee DstAdr: " + parse_result.IPv4Header->DstAddr.ToString() + ":" + SwapOrder(parse_result.TcpHeader->DstPort).ToString());

                            if (parse_result.PacketPayloadLength > 0)
                            {
                                PacketPayloadData = PacketPayloadToString(parse_result.PacketPayload, parse_result.PacketPayloadLength);
                                Helpers.ConsolePrint("WinDivertSharp", "packet: " + PacketPayloadData);
                            }
                        }

                        if (addr.Direction == WinDivertDirection.Inbound)
                        {
                            Helpers.ConsolePrint("WinDivertSharp", "INBOUND FROM DEVFEE");
                            Helpers.ConsolePrint("WinDivertSharp", "DevFee SrcAdr: " + parse_result.IPv4Header->SrcAddr.ToString() + ":" + SwapOrder(parse_result.TcpHeader->SrcPort).ToString());
                            Helpers.ConsolePrint("WinDivertSharp", "DevFee DstAdr: " + parse_result.IPv4Header->DstAddr.ToString() + ":" + SwapOrder(parse_result.TcpHeader->DstPort).ToString());

                            parse_result.IPv4Header->SrcAddr = IPAddress.Parse(DevFeeIP); ;
                            parse_result.TcpHeader->SrcPort = SwapOrder(DevFeePort);
                            Helpers.ConsolePrint("WinDivertSharp", "New DevFee DstAdr: " + parse_result.IPv4Header->SrcAddr.ToString() + ":" + SwapOrder(parse_result.TcpHeader->SrcPort).ToString());
                            
                            if (parse_result.PacketPayloadLength > 0)
                            {
                                PacketPayloadData = PacketPayloadToString(parse_result.PacketPayload, parse_result.PacketPayloadLength);
                                Helpers.ConsolePrint("WinDivertSharp", "packet: " + PacketPayloadData);
                            }
                        }

                        parse_result = WinDivert.WinDivertHelperParsePacket(packet, readLen);
                        modified = false;


                        string cpacket3 = "";
                        for (int i = 0; i < readLen; i++)
                        {
                            // if (packet[i] >= 32)
                            cpacket3 = cpacket3 + (char)packet[i];

                        }
                        if (cpacket0.Length > 100)
                            File.WriteAllText(np.ToString() + "di.pkt", cpacket3);

                        /*
                    if (parse_result.PacketPayloadLength > 10)
                    {
                        int i1 = PacketPayloadToString(parse_result.PacketPayload, parse_result.PacketPayloadLength).IndexOf("\", \"pass\":");
                        string s1 = PacketPayloadToString(parse_result.PacketPayload, parse_result.PacketPayloadLength).Substring(i1, (int)parse_result.PacketPayloadLength - i1);
                        Helpers.ConsolePrint("WinDivertSharp", s1);
                    }
                    */
                        //{"worker": "eth1.0", "jsonrpc": "2.0", "params": ["0xB9cF2dA90Bdff1BC014720Cc84F5Ab99d7974EbA", "x"], "id": 2, "method": "eth_submitLogin"}
                        //                                                   3F2v4K3ExF1tqLLwa6Ac3meimSjV3iUZgQ.devfee20$0-HgaPFxnqIlqsPZDqXC+KyA

                        //{"id": 11, "method": "mining.submit", "params": ["3F2v4K3ExF1tqLLwa6Ac3meimSjV3iUZgQ.Farm1$0-2t3LAymH0Ve-dEwJZ-UEcw","0000000023fe40fd","4005697fcd"]}
                        //if (PacketPayload.Length > 0 & PacketPayload.Contains("id") & PacketPayload.Contains("jsonrpc") & PacketPayload.Contains("method") & PacketPayload.Contains("params") & PacketPayload.Contains("login"))
                        /*
                        PacketPayload = PacketPayloadToString(parse_result.PacketPayload, parse_result.PacketPayloadLength);
                        if (PacketPayload.Length > 0)
                        {
                          //  Helpers.ConsolePrint("WinDivertSharp", "packet: " + PacketPayload);
                        }
                        */
                        //NICEHASH SESSION: Outbound
                        //[22:20:27][WinDivertSharp] { "id":2,"jsonrpc":"2.0","method":"submit","params":{ "id":"2f0ea7863fa02870920164926ebe734c","job_id":"00000000b432d15e","nonce":"862401eb","result":"3869a1930edf01d8e13062f6fff7413840dc223382eddbc342757498552c0000"} }
                        //DEVFEE SESSION: Outbound
                        //[22:22:03][WinDivertSharp] { "id":4,"jsonrpc":"2.0","method":"submit","params":{ "id":"9e7d3e25ee827c62c53152c94c671b70","job_id":"00000000f3d8ccd7","nonce":"08230171","result":"0faaf46c0510d2dbecb917989c8c61b03671e7bfbc5d2199799904804a300000"} }

                        // parse_result = WinDivert.WinDivertHelperParsePacket(packet, packet.Length);

                        
                        
                        if (parse_result.PacketPayloadLength > 10)
                        {
                            PacketPayloadData = PacketPayloadToString(parse_result.PacketPayload, parse_result.PacketPayloadLength);
                            dynamic json = JsonConvert.DeserializeObject(PacketPayloadData);

                            if (PacketPayloadData.Contains("jsonrpc") & PacketPayloadData.Contains("params") & PacketPayloadData.Contains("method") & PacketPayloadData.ToLower().Contains("login"))
                            {
                                //dynamic jsonLogin = JsonConvert.DeserializeObject(PacketPayloadToString(parse_result.PacketPayload, parse_result.PacketPayloadLength));
                                //{"id":1,"jsonrpc":"2.0","method":"login","params":{"login":"42fV4v2EC4EALhKWKNCEJsErcdJygynt7RJvFZk8HSeYA9srXdJt58D9fQSwZLqGHbijCSMqSP4mU7inEEWNyer6F7PiqeX.donate","pass":"x","agent":"XMRig/5.2.0 (Windows NT 6.1; Win64; x64) libuv/1.31.0 msvc/2017","algo":["rx/0","cn/1","cn/2","cn/r","cn/fast","cn/half","cn/xao","cn/rto","cn/rwz","cn/zls","cn/double","cn/gpu","cn-lite/0","cn-lite/1","cn-heavy/0","cn-heavy/tube","cn-heavy/xhv","cn-pico","cn/0","rx/wow","rx/loki","rx/arq","argon2/chukwa","argon2/wrkz"]}}
                                //Helpers.ConsolePrint("WinDivertSharp", "id: " + resp.id);
                                //Helpers.ConsolePrint("WinDivertSharp", "jsonrpc: " + resp.jsonrpc);
                                //Helpers.ConsolePrint("WinDivertSharp", "method: " + resp.method);
                                Helpers.ConsolePrint("WinDivertSharp", "algo: " + json.@params.algo[0]);
                                Helpers.ConsolePrint("WinDivertSharp", "old login: " + json.@params.login);

                                json.@params.login = nicehashLogin;
                                Helpers.ConsolePrint("WinDivertSharp", "new login: " + json.@params.login);
                                // resp.@params.login = "42fV4v2EC4EALhKWKNCEJsErcdJygynt7RJvFZk8HSeYA9srXdJt58D9fQSwZLqGHbij";
                                //resp.@params.login = "42fV4v2EC4EALhKWKNCEJsErcdJygynt7RJvFZk8HSeYA9srXdJt58D9fQSwZLqGHbijCSMqSP4mU7inEEWNyer6F7PiqeX.devfee";
                                // resp.@params.login = "3F2v4K3ExF1tqLLwa6Ac3meimSjV3iUZgQ.devfee21";
                                // resp.@params.login = "3F2v4K3ExF1tqLLwa6Ac3meimSjV3iUZgQ.devfee20$0-HgaPFxnqIlqsPZDqXC+KyA";
                                //пакет нельзя изменять в размере. Можно дополнять кодом 10 до нужного размера
                                //string newjson = loginJsonStart + nicehashLogin + loginJsonEnd + (char)10;
                            }
                            //eth
                            if (PacketPayloadData.Contains("worker") & PacketPayloadData.Contains("jsonrpc") & PacketPayloadData.Contains("params") & PacketPayloadData.Contains("eth_submitLogin"))
                            {
                                //{"worker": "eth1.0", "jsonrpc": "2.0", "params": ["0xB9cF2dA90Bdff1BC014720Cc84F5Ab99d7974EbA", "x"], "id": 2, "method": "eth_submitLogin"}
                                //                                                   3F2v4K3ExF1tqLLwa6Ac3meimSjV3iUZgQ.devfee20$0-HgaPFxnqIlqsPZDqXC+KyA

                                //{"id": 11, "method": "mining.submit", "params": ["3F2v4K3ExF1tqLLwa6Ac3meimSjV3iUZgQ.Farm1$0-2t3LAymH0Ve-dEwJZ-UEcw","0000000023fe40fd","4005697fcd"]}

                                Helpers.ConsolePrint("WinDivertSharp", "old login: " + json.@params[0]);
                                json.@params[0] = nicehashLogin;
                                Helpers.ConsolePrint("WinDivertSharp", "new login: " + json.@params.login);
                            }
                            if (PacketPayloadData.Contains("method") & PacketPayloadData.Contains("mining.submit") & PacketPayloadData.Contains("params") )
                            {
                                //{"worker": "eth1.0", "jsonrpc": "2.0", "params": ["0xB9cF2dA90Bdff1BC014720Cc84F5Ab99d7974EbA", "x"], "id": 2, "method": "eth_submitLogin"}
                                //                                                   3F2v4K3ExF1tqLLwa6Ac3meimSjV3iUZgQ.devfee20$0-HgaPFxnqIlqsPZDqXC+KyA

                                //{"id": 11, "method": "mining.submit", "params": ["3F2v4K3ExF1tqLLwa6Ac3meimSjV3iUZgQ.Farm1$0-2t3LAymH0Ve-dEwJZ-UEcw","0000000023fe40fd","4005697fcd"]}

                                Helpers.ConsolePrint("WinDivertSharp", "old subit: " + json.@params[0]);
                                json.@params[0] = nicehashLogin;
                                Helpers.ConsolePrint("WinDivertSharp", "new submit: " + json.@params.login);
                            }

                            string newjson = JsonConvert.SerializeObject(json) + (char)10;//magic
                                                                                              // string newjson = JsonConvert.SerializeObject(loginJson) + (char)10;

                                //   Helpers.ConsolePrint("WinDivertSharp", JsonConvert.DeserializeObject(XMRLogin));
                                Helpers.ConsolePrint("WinDivertSharp", newjson);

                                //40byte is tcp/ip head
                                byte[] head = new byte[40];
                                for (int i = 0; i < 40; i++)
                                {
                                    head[i] = (byte)packet[i];
                                }

                                byte[] newPayload = new byte[newjson.Length];
                                for (int i = 0; i < newjson.Length; i++)
                                {
                                    newPayload[i] = (byte)newjson[i];
                                }

                                //byte[] newpacket = new byte[newjson.Length + 40];
                                byte[] newpacket = new byte[readLen];
                                for (int i = 0; i < 40; i++)
                                {
                                    newpacket[i] = (byte)head[i];
                                }
                                for (int i = 0; i < newPayload.Length; i++)
                                {
                                    newpacket[i + 40] = (byte)newPayload[i];
                                }

                                var buffer = new WinDivertBuffer(newpacket);

                                parse_result = WinDivert.WinDivertHelperParsePacket(buffer, buffer.Length);
                                //parse_result.PacketPayloadLength = buffer.Length - 40;

                                WinDivert.WinDivertHelperCalcChecksums(buffer, buffer.Length, ref addr, WinDivertChecksumHelperParam.All);
                                Helpers.ConsolePrint("WinDivertSharp", "Modified packet length: " + buffer.Length.ToString() + " buffer.Length:" + buffer.Length.ToString());
                                if (!WinDivert.WinDivertSend(handle, buffer, buffer.Length, ref addr))
                                {
                                    Helpers.ConsolePrint("WinDivertSharp", "Write Err: {0}", Marshal.GetLastWin32Error());
                                }

                                Helpers.ConsolePrint("WinDivertSharp", "Original packet length: " + readLen.ToString() + " Sended buffer length:" + RealPacketLength(packet, readLen).ToString());
                                string cpacket2 = "";
                                for (int i = 0; i < buffer.Length; i++)
                                {
                                    // if (packet[i] >= 32)
                                    cpacket2 = cpacket2 + (char)buffer[i];

                                }
                                if (cpacket0.Length > 100)
                                File.WriteAllText(np.ToString() + "d.pkt", cpacket2);

                                Helpers.ConsolePrint("WinDivertSharp", "++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
                                modified = true;//packet already send
                            
                        }

                        //pool.supportxmr.com:3333
                        //   Helpers.ConsolePrint("src - ", parse_result.IPv4Header->SrcAddr.ToString());
                        //pdmIpv4.UpdateCalculatedValues();
                        //pdmIpv4.CalculateIPChecksum();

                        //pdmTcp.UpdateCalculatedValues();

                        /*
                        if (addr.Direction == WinDivertDirection.Inbound)
                        {
                            Helpers.ConsolePrint("WinDivertSharp", "inbound!");
                        }
                        */
                        /*
                        if (packetData != null)
                        {
                            Helpers.ConsolePrint("WinDivertSharp", "Packet has n byte payload: " + packetData.Length);
                            //Console.WriteLine("Packet has {0} byte payload.", packetData.Length);
                        }
                        */

                        /*
                        packet.CalculateTCPChecksum();
                        tcpPacket.UpdateCalculatedValues();
                        tcpPacket.UpdateTCPChecksum();

                        ipPacket.CalculateIPChecksum();
                        ipPacket.UpdateCalculatedValues();
                        ipPacket.UpdateIPChecksum();
                        */

sendPacket:
                        
                        if (!modified)
                        {
                            //readLen = RealPacketLength(packet);
                            WinDivert.WinDivertHelperCalcChecksums(packet, readLen, ref addr, WinDivertChecksumHelperParam.All);
                            Helpers.ConsolePrint("WinDivertSharp", "Original packet length: " + readLen.ToString() + " Sended packet length:" + RealPacketLength(packet, readLen).ToString());

                            Helpers.ConsolePrint("WinDivertSharp", "IPv4Header length: " + parse_result.IPv4Header->Length.ToString());
                            //Helpers.ConsolePrint("WinDivertSharp", "IPv6Header length: " + parse_result.IPv6Header->Length.ToString());
                            Helpers.ConsolePrint("WinDivertSharp", "TcpHeader length: " + parse_result.TcpHeader->HdrLength.ToString());
                            //Helpers.ConsolePrint("WinDivertSharp", "UdpHeader length: " + parse_result.UdpHeader->Length.ToString());


                            if (!WinDivert.WinDivertSend(handle, packet, readLen, ref addr))
                            {
                                Helpers.ConsolePrint("WinDivertSharp", "Write Err: {0}", Marshal.GetLastWin32Error());
                            }

                            string cpacket1 = "";
                            for (int i = 0; i < readLen; i++)
                            {
                                // if (packet[i] >= 32)
                                cpacket1 = cpacket1 + (char)packet[i];

                            }
                            if (cpacket0.Length > 100)
                                File.WriteAllText(np.ToString() + "n.pkt", cpacket1);
                            

                            Helpers.ConsolePrint("WinDivertSharp", "--------------------------------------------------------------------");
                        }

                      
                    }
                } catch (Exception e)
                {
                    Helpers.ConsolePrint("WinDivertSharp error: ", e.ToString());
                }
                finally
                {
                    /*
                    if (handle != IntPtr.Zero)
                    {
                        WinDivert.WinDivertClose(handle);
                    }
                    
                    if (recvEvent != IntPtr.Zero)
                    {
                        Kernel32.CloseHandle(recvEvent);
                    }

                    packet.Dispose();
                    packet = null;
                    */
                }

            }
            while (divert_running);

        }
    }
}
