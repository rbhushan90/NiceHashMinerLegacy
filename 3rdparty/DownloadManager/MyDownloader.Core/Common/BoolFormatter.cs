/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
using System;
using System.Collections.Generic;
using System.Text;

namespace MyDownloader.Core.Common
{
    public static class BoolFormatter
    {
        private const string Yes = "Yes";
        private const string No = "No";

        public static bool FromString(string s)
        {
            if (s == Yes) return true;
            return false;
        }

        public static string ToString(bool v)
        {
            if (v) return Yes;
            return No;
        }
    }
}
