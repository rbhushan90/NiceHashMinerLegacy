using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinDivertSharp;
using NiceHashMiner.Stats;
using System.Threading;

using SystemTimer = System.Timers.Timer;
using Timer = System.Windows.Forms.Timer;
using WinDivertSharp.WinAPI;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace NiceHashMiner.Miners
{
    class Divert
    {
        private static Timer _divertTimer;
        public static volatile bool divert_running = true;
        public static IntPtr DivertHandle;
        private static readonly uint MaxPacket = 2048;

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

        public static unsafe string PacketPayloadToString(byte *bytes, uint length)
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
            byte *bytes = null;
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

        public static void Diversion()
        {
            _divertTimer = new Timer();
            _divertTimer.Tick += DivertTimer_Tick;
            _divertTimer.Interval = 1000;
            _divertTimer.Start();
        }
        private static async void DivertTimer_Tick(object sender, EventArgs e)
        {
            Helpers.ConsolePrint("WinDivertSharp", "start: ");
            _divertTimer.Stop();
            _divertTimer = null;

           // string filter = "ip && tcp && (inbound ? (tcp.SrcPort == 3333) : (tcp.DstPort == 3380))"; //test xmr nicehash
     //       string filter = "ip && tcp && (inbound ? (tcp.SrcPort == 3333) : (tcp.DstPort == 3333))";
            string filter = "ip && tcp && (inbound ? (tcp.SrcPort == 3333) || (tcp.SrcPort == 8008) : (tcp.DstPort == 3333) || (tcp.DstPort == 8008))";
           

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

            await RunDiversion(DivertHandle);
            
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
        public static void StopDiversion()
        {
            if (DivertHandle != IntPtr.Zero || DivertHandle != new IntPtr(-1))
            {
                divert_running = false;
                WinDivert.WinDivertClose(DivertHandle);
            }
        }

        private static Task<bool> RunDiversion(IntPtr handle)
        {

            return Task.Run(() =>
            {
                var t = new TaskCompletionSource<bool>();
                RunDiversion1(handle);
                return t.Task;
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static async Task RunDiversion1(IntPtr handle)
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

                        Helpers.ConsolePrint("WinDivertSharp", "Read");
                        var result = WinDivert.WinDivertRecvEx(handle, packet, 0, ref addr, ref readLen, ref recvOverlapped);

                        if (!result)
                        {
                            var error = Marshal.GetLastWin32Error();
                            Helpers.ConsolePrint("WinDivertSharp", "No error code: " + error.ToString());
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
                        Helpers.ConsolePrint("WinDivertSharp", "Read packet lenght: " + readLen.ToString());
                        /*
                        string cpacket = "";
                        for (int i = 0; i < readLen; i++)
                        {
                           // if (packet[i] >= 32)
                                cpacket = cpacket + (char)packet[i];

                        }
                        File.WriteAllText(np.ToString()+"o.pkt", cpacket);
                        */
                        var parse_result = WinDivert.WinDivertHelperParsePacket(packet, readLen);

                        string data = "";
                        for (int i = 0; i < parse_result.PacketPayloadLength; i++)
                        {
                            if (parse_result.PacketPayload[i] >= 32)
                                data = data + (char)parse_result.PacketPayload[i];

                        }

                        if (parse_result.IPv4Header != null)
                        {
                            Helpers.ConsolePrint("WinDivertSharp", "SrcAdr: " + parse_result.IPv4Header->SrcAddr.ToString());
                            Helpers.ConsolePrint("WinDivertSharp", "DstAdr: " + parse_result.IPv4Header->DstAddr.ToString());
                            Helpers.ConsolePrint("WinDivertSharp", "SrcPort: " + SwapOrder(parse_result.TcpHeader->SrcPort).ToString());
                            Helpers.ConsolePrint("WinDivertSharp", "DstPort: " + SwapOrder(parse_result.TcpHeader->DstPort).ToString());
                            Helpers.ConsolePrint("WinDivertSharp", "TCPChecksum: " + parse_result.TcpHeader->Checksum.ToString());
                            Helpers.ConsolePrint("WinDivertSharp", "IPChecksum: " + parse_result.IPv4Header->Checksum.ToString());
                        }

                        if (oldDstAddress == null)
                        {
                            oldDstAddress = parse_result.IPv4Header->DstAddr;
                        }
                        string servIP = "";
                        try
                        {
                            System.Text.ASCIIEncoding ASCII = new System.Text.ASCIIEncoding();

                            // Get server related information.
                            IPHostEntry heserver = Dns.GetHostEntry("pool.supportxmr.com");

                            // Loop on the AddressList
                            foreach (IPAddress curAdd in heserver.AddressList)
                            {
                                if (curAdd.AddressFamily.ToString() == ProtocolFamily.InterNetwork.ToString())
                                {
                                    servIP = curAdd.ToString();
                                }
                            }

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("[DoResolve] Exception: " + e.ToString());
                        }

                        if (servIP.Length != 0)
                        {
                            var DivertIP = IPAddress.Parse(servIP);
                            if (addr.Direction == WinDivertDirection.Outbound)
                            {
                                Helpers.ConsolePrint("WinDivertSharp", "Outbound");
                             //   parse_result.IPv4Header->DstAddr = IPAddress.Parse(servIP);
                             //   parse_result.TcpHeader->DstPort = SwapOrder(3333); 
                            }
                            else //inbound
                            {
                                Helpers.ConsolePrint("WinDivertSharp", "Inbound");
                              //  parse_result.IPv4Header->SrcAddr = oldDstAddress;
                              //  parse_result.TcpHeader->SrcPort = SwapOrder(3380); 
                            }
                        }

                        parse_result = WinDivert.WinDivertHelperParsePacket(packet, readLen);

                        Helpers.ConsolePrint("WinDivertSharp", "New SrcAdr: " + parse_result.IPv4Header->SrcAddr.ToString());
                        Helpers.ConsolePrint("WinDivertSharp", "New DstAdr: " + parse_result.IPv4Header->DstAddr.ToString());
                        Helpers.ConsolePrint("WinDivertSharp", "New SrcPort: " + SwapOrder(parse_result.TcpHeader->SrcPort).ToString());
                        Helpers.ConsolePrint("WinDivertSharp", "New DstPort: " + SwapOrder(parse_result.TcpHeader->DstPort).ToString());
                        Helpers.ConsolePrint("WinDivertSharp", "TCPChecksum: " + parse_result.TcpHeader->Checksum.ToString());
                        Helpers.ConsolePrint("WinDivertSharp", "IPChecksum: " + parse_result.IPv4Header->Checksum.ToString());
                        Helpers.ConsolePrint("WinDivertSharp", "PacketPayloadLength: " + parse_result.PacketPayloadLength.ToString());

                        modified = false;

                        if (parse_result.PacketPayloadLength > 10 & data.Contains("id") & data.Contains("jsonrpc") & data.Contains("method") & data.Contains("params") & data.Contains("login"))
                        {
                            dynamic resp = JsonConvert.DeserializeObject(PacketPayloadToString(parse_result.PacketPayload, parse_result.PacketPayloadLength));

                            Helpers.ConsolePrint("WinDivertSharp", "id: " + resp.id);
                            Helpers.ConsolePrint("WinDivertSharp", "jsonrpc: " + resp.jsonrpc);
                            Helpers.ConsolePrint("WinDivertSharp", "method: " + resp.method);
                           // Helpers.ConsolePrint("WinDivertSharp", "params: " + resp.@params);
                            Helpers.ConsolePrint("WinDivertSharp", "login: " + resp.@params.login);
                           // resp.@params.login = "42fV4v2EC4EALhKWKNCEJsErcdJygynt7RJvFZk8HSeYA9srXdJt58D9fQSwZLqGHbij";
                            //resp.@params.login = "42fV4v2EC4EALhKWKNCEJsErcdJygynt7RJvFZk8HSeYA9srXdJt58D9fQSwZLqGHbijCSMqSP4mU7inEEWNyer6F7PiqeX.devfee";
                            //resp.@params.login = "3F2v4K3ExF1tqLLwa6Ac3meimSjV3iUZgQ.worker20$0-HgaPFxnqIlqsPZDqXC+KyA";
                            //пакет нельзя изменять в размере. Можно дополнять кодом 10 до нужного размера
                            Helpers.ConsolePrint("WinDivertSharp", "new login: " + resp.@params.login);
                            //string newjson = JsonConvert.SerializeObject(resp);
                            string newjson = JsonConvert.SerializeObject(resp) + (char)10;
                            /*
                            cpacket = "";
                            for (int i = 0; i < readLen; i++)
                            {
                                // if (packet[i] >= 32)
                                cpacket = cpacket + (char)packet[i];

                            }
                            File.WriteAllText("old_packet.pkt", cpacket);
                            */

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

                            byte[] newpacket = new byte[newjson.Length + 40];
                            for (int i = 0; i < 40; i++)
                            {
                                newpacket[i] = (byte)head[i];
                            }
                            for (int i = 0; i < newPayload.Length; i++)
                            {
                                newpacket[i + 40] = (byte)newPayload[i];
                            }

                            var buffer = new WinDivertBuffer(newpacket);
                            /*
                            cpacket = "";
                            for (int i = 0; i < buffer.Length; i++)
                            {
                                // if (packet[i] >= 32)
                                cpacket = cpacket + (char)buffer[i];

                            }
                            File.WriteAllText("new_packet.pkt", cpacket);
                            */
                            /*
                            string cpacket3 = "";
                            for (int i = 0; i < buffer.Length; i++)
                            {
                                // if (packet[i] >= 32)
                                cpacket3 = cpacket3 + (char)buffer[i];

                            }
                            File.WriteAllText("new.pkt", cpacket3);
                            */
                            parse_result = WinDivert.WinDivertHelperParsePacket(buffer, buffer.Length);
                            parse_result.PacketPayloadLength = buffer.Length - 40;

                            WinDivert.WinDivertHelperCalcChecksums(buffer, buffer.Length, ref addr, WinDivertChecksumHelperParam.All);
                            Helpers.ConsolePrint("WinDivertSharp", "readLen: " + readLen.ToString() + " buffer.Length:" + buffer.Length.ToString());
                            if (!WinDivert.WinDivertSend(handle, buffer, buffer.Length, ref addr))
                            {
                                Helpers.ConsolePrint("WinDivertSharp", "Write Err: {0}", Marshal.GetLastWin32Error());
                            }

                            modified = true;
                        }
                        
                        string cpacket2 = "";
                        for (int i = 0; i < readLen; i++)
                        {
                            // if (packet[i] >= 32)
                            cpacket2 = cpacket2 + (char)packet[i];

                        }
                        File.WriteAllText(np.ToString() + "n.pkt", cpacket2);
                        np++;
                        

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
                        //WinDivert.WinDivertHelperCalcChecksums(packet, packet.Length, modified ? 0 : WINDIVERT_HELPER_NO_REPLACE);
                        //WinDivert.WinDivertHelperCalcChecksums(packet, packet.Length, ref addr, WinDivertChecksumHelperParam.All);

                        if (!modified)
                        {
                            WinDivert.WinDivertHelperCalcChecksums(packet, packet.Length, ref addr, WinDivertChecksumHelperParam.All);
                            Helpers.ConsolePrint("WinDivertSharp", "readLen: " + readLen.ToString() + " packet.Length:" + packet.Length.ToString());

                            if (!WinDivert.WinDivertSend(handle, packet, readLen, ref addr))
                            {
                                Helpers.ConsolePrint("WinDivertSharp", "Write Err: {0}", Marshal.GetLastWin32Error());
                            }
                        }
                        
                    }
                }
                catch (Exception e)
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
