﻿/*
 * WinDivert.cs
 * (C) 2018, all rights reserved,
 *
 * This file is part of WinDivertSharp.
 *
 * WinDivertSharp is free software: you can redistribute it and/or modify it under
 * the terms of the GNU Lesser General Public License as published by the
 * Free Software Foundation, either version 3 of the License, or (at your
 * option) any later version.
 *
 * This program is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 * WinDivertSharp is free software; you can redistribute it and/or modify it under
 * the terms of the GNU General Public License as published by the Free
 * Software Foundation; either version 2 of the License, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
 * for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc., 51
 * Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
 */

using System;
using NUnit.Framework;
using System.Runtime.InteropServices;
using WinDivertSharp;

namespace WinDivertSharpTests
{
    /// <summary>
    /// Holds static payloads used when running various tests. Also uses a static initializer to
    /// start up our primary test WinDivert handle.
    /// </summary>
    internal class TestData : IDisposable
    {
        /// <summary>
        /// Used to stop real traffic from interfering with tests.
        /// </summary>
        public IntPtr UpperWinDivertHandle;

        /// <summary>
        /// Used to stop real traffic from interfering with tests.
        /// </summary>
        public IntPtr LowerWinDivertHandle;

        /// <summary>
        /// Constructs a new TestData object.
        /// </summary>
        /// <remarks>
        /// Opens WinDivert handles that prevent traffic from interfering with tests, closes them
        /// when disposed.
        /// </remarks>
        public TestData()
        {
            // Stop normal traffic from interacting with the tests.
            UpperWinDivertHandle = WinDivert.WinDivertOpen("true", WinDivertLayer.Network, -510, WinDivertOpenFlags.Drop | WinDivertOpenFlags.Debug);

            // Stop test packets escaping to the Internet or TCP/IP stack.
            LowerWinDivertHandle = WinDivert.WinDivertOpen("true", WinDivertLayer.Network, 510, WinDivertOpenFlags.Drop);

            if (UpperWinDivertHandle == IntPtr.Zero || UpperWinDivertHandle == new IntPtr(-1))
            {
                Assert.Fail("Failed to open upper WinDivert handle with Win32 error code {0}.", Marshal.GetLastWin32Error());
            }

            if (LowerWinDivertHandle == IntPtr.Zero || LowerWinDivertHandle == new IntPtr(-1))
            {
                Assert.Fail("Failed to open lower WinDivert handle with Win32 error code {0}.", Marshal.GetLastWin32Error());
            }
        }

        // IPV4 ICMP ECHO REQUEST
        public static WinDivertBuffer EchoRequestData = new WinDivertBuffer(new byte[]
        {   
            0x45, 0x00, 0x00, 0x54, 0x12, 0x34, 0x40, 0x00,
            0x40, 0x01, 0x00, 0x00, 0x0a, 0x00, 0x00, 0x01,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x00, 0x00, 0x00,
            0x0d, 0x56, 0x00, 0x01, 0x8b, 0xa6, 0x60, 0x54,
            0x00, 0x00, 0x00, 0x00, 0xf9, 0x08, 0x0a, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x10, 0x11, 0x12, 0x13,
            0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b,
            0x1c, 0x1d, 0x1e, 0x1f, 0x20, 0x21, 0x22, 0x23,
            0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b,
            0x2c, 0x2d, 0x2e, 0x2f, 0x30, 0x31, 0x32, 0x33,
            0x34, 0x35, 0x36, 0x37
        });

        // IPV4 TCP HTTP GET REQUEST
        public static WinDivertBuffer HttpRequestData = new WinDivertBuffer(new byte[]
        {
            0x45, 0x00, 0x02, 0x09, 0x48, 0x2d, 0x40, 0x00,
            0x40, 0x06, 0x00, 0x00, 0x0a, 0x0a, 0x0a, 0x0a,
            0x5d, 0xb8, 0xd8, 0x77, 0xa3, 0x1a, 0x00, 0x50,
            0x53, 0x38, 0xcc, 0xc2, 0x56, 0x37, 0xb3, 0x55,
            0x80, 0x18, 0x00, 0x73, 0x00, 0x00, 0x00, 0x00,
            0x01, 0x01, 0x08, 0x0a, 0x00, 0x2c, 0x85, 0x1b,
            0x1b, 0x7f, 0x3a, 0x71, 0x47, 0x45, 0x54, 0x20,
            0x2f, 0x20, 0x48, 0x54, 0x54, 0x50, 0x2f, 0x31,
            0x2e, 0x31, 0x0d, 0x0a, 0x48, 0x6f, 0x73, 0x74,
            0x3a, 0x20, 0x77, 0x77, 0x77, 0x2e, 0x65, 0x78,
            0x61, 0x6d, 0x70, 0x6c, 0x65, 0x2e, 0x63, 0x6f,
            0x6d, 0x0d, 0x0a, 0x43, 0x6f, 0x6e, 0x6e, 0x65,
            0x63, 0x74, 0x69, 0x6f, 0x6e, 0x3a, 0x20, 0x6b,
            0x65, 0x65, 0x70, 0x2d, 0x61, 0x6c, 0x69, 0x76,
            0x65, 0x0d, 0x0a, 0x43, 0x61, 0x63, 0x68, 0x65,
            0x2d, 0x43, 0x6f, 0x6e, 0x74, 0x72, 0x6f, 0x6c,
            0x3a, 0x20, 0x6d, 0x61, 0x78, 0x2d, 0x61, 0x67,
            0x65, 0x3d, 0x30, 0x0d, 0x0a, 0x41, 0x63, 0x63,
            0x65, 0x70, 0x74, 0x3a, 0x20, 0x74, 0x65, 0x78,
            0x74, 0x2f, 0x68, 0x74, 0x6d, 0x6c, 0x2c, 0x61,
            0x70, 0x70, 0x6c, 0x69, 0x63, 0x61, 0x74, 0x69,
            0x6f, 0x6e, 0x2f, 0x78, 0x68, 0x74, 0x6d, 0x6c,
            0x2b, 0x78, 0x6d, 0x6c, 0x2c, 0x61, 0x70, 0x70,
            0x6c, 0x69, 0x63, 0x61, 0x74, 0x69, 0x6f, 0x6e,
            0x2f, 0x78, 0x6d, 0x6c, 0x3b, 0x71, 0x3d, 0x30,
            0x2e, 0x39, 0x2c, 0x69, 0x6d, 0x61, 0x67, 0x65,
            0x2f, 0x77, 0x65, 0x62, 0x70, 0x2c, 0x2a, 0x2f,
            0x2a, 0x3b, 0x71, 0x3d, 0x30, 0x2e, 0x38, 0x0d,
            0x0a, 0x55, 0x73, 0x65, 0x72, 0x2d, 0x41, 0x67,
            0x65, 0x6e, 0x74, 0x3a, 0x20, 0x58, 0x58, 0x58,
            0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58,
            0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58,
            0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58,
            0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58,
            0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58,
            0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58,
            0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58,
            0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58,
            0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58,
            0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58,
            0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58,
            0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58,
            0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58,
            0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58,
            0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58,
            0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58,
            0x58, 0x58, 0x0d, 0x0a, 0x41, 0x63, 0x63, 0x65,
            0x70, 0x74, 0x2d, 0x45, 0x6e, 0x63, 0x6f, 0x64,
            0x69, 0x6e, 0x67, 0x3a, 0x20, 0x67, 0x7a, 0x69,
            0x70, 0x2c, 0x64, 0x65, 0x66, 0x6c, 0x61, 0x74,
            0x65, 0x2c, 0x73, 0x64, 0x63, 0x68, 0x0d, 0x0a,
            0x41, 0x63, 0x63, 0x65, 0x70, 0x74, 0x2d, 0x4c,
            0x61, 0x6e, 0x67, 0x75, 0x61, 0x67, 0x65, 0x3a,
            0x20, 0x65, 0x6e, 0x2d, 0x55, 0x53, 0x2c, 0x65,
            0x6e, 0x3b, 0x71, 0x3d, 0x30, 0x2e, 0x38, 0x0d,
            0x0a, 0x49, 0x66, 0x2d, 0x4e, 0x6f, 0x6e, 0x65,
            0x2d, 0x4d, 0x61, 0x74, 0x63, 0x68, 0x3a, 0x20,
            0x22, 0x33, 0x33, 0x33, 0x33, 0x33, 0x33, 0x33,
            0x33, 0x33, 0x22, 0x0d, 0x0a, 0x49, 0x66, 0x2d,
            0x4d, 0x6f, 0x64, 0x69, 0x66, 0x69, 0x65, 0x64,
            0x2d, 0x53, 0x69, 0x6e, 0x63, 0x65, 0x3a, 0x20,
            0x46, 0x72, 0x69, 0x2c, 0x20, 0x30, 0x33, 0x20,
            0x41, 0x75, 0x67, 0x20, 0x32, 0x30, 0x31, 0x34,
            0x20, 0x31, 0x33, 0x3a, 0x33, 0x33, 0x3a, 0x33,
            0x33, 0x20, 0x47, 0x4d, 0x54, 0x0d, 0x0a, 0x0d,
            0x0a
        });

        // IPV4 DNS REQUEST
        public static WinDivertBuffer DnsRequestData = new WinDivertBuffer(new byte[]
        {
            0x45, 0x00, 0x00, 0x39, 0x20, 0x90, 0x00, 0x00,
            0x49, 0x11, 0x00, 0x00, 0x0a, 0x00, 0x00, 0x01,
            0x08, 0x08, 0x04, 0x04, 0xe0, 0x45, 0x00, 0x35,
            0x00, 0x25, 0x00, 0x00, 0x17, 0x08, 0x01, 0x00,
            0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x07, 0x65, 0x78, 0x61, 0x6d, 0x70, 0x6c, 0x65,
            0x03, 0x63, 0x6f, 0x6d, 0x00, 0x00, 0x01, 0x00,
            0x01
        });

        // IPV6 TCP SYN
        public static WinDivertBuffer Ipv6TcpSynData = new WinDivertBuffer(new byte[]
        {
            0x60, 0x00, 0x00, 0x00, 0x00, 0x28, 0x06, 0x40,
            0x12, 0x34, 0x56, 0x78, 0x00, 0x01, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0xaa, 0xbb, 0xcc, 0xdd,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
            0xc3, 0x7e, 0x00, 0x17, 0xe1, 0xd7, 0xc8, 0xaa,
            0x00, 0x00, 0x00, 0x00, 0xa0, 0x02, 0xaa, 0xaa,
            0x00, 0x00, 0x00, 0x00, 0x02, 0x04, 0xff, 0xc4,
            0x04, 0x02, 0x08, 0x0a, 0xff, 0xff, 0x91, 0x86,
            0x00, 0x00, 0x00, 0x00, 0x01, 0x03, 0x03, 0x07
        });

        // IPV6 ICMPV6 ECHO REPLY
        public static WinDivertBuffer Ipv6EchoReplyData = new WinDivertBuffer(new byte[]
        {
            0x60, 0x00, 0x00, 0x00, 0x00, 0x40, 0x3a, 0x1f,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
            0x81, 0x00, 0x00, 0x00, 0x10, 0x72, 0x00, 0x03,
            0xa4, 0xd5, 0x69, 0x54, 0x00, 0x00, 0x00, 0x00,
            0xab, 0x75, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
            0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff,
            0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
            0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff,
            0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77
        });

        // IPV6 EXTENSION HEADERS UDP
        public static WinDivertBuffer Ipv6ExtHdrsUdpData = new WinDivertBuffer(new byte[]
        {
            0x60, 0x00, 0x00, 0x00, 0x00, 0x2d, 0x00, 0x64,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
            0x3c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x3c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x11, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x12, 0x34, 0xaa, 0xaa, 0x00, 0x15, 0x00, 0x00,
            0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x20, 0x57, 0x6f,
            0x72, 0x6c, 0x64, 0x21, 0x01
        });

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).

                    WinDivert.WinDivertClose(UpperWinDivertHandle);
                    WinDivert.WinDivertClose(LowerWinDivertHandle);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~TestData() { // Do not change this code. Put cleanup code in Dispose(bool disposing)
        // above. Dispose(false); }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above. GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}