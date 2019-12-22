/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
using System;
using System.Collections.Generic;
using System.Text;

namespace MyDownloader.Core.Common
{
    public static class TimeSpanFormatter
    {
        public static string ToString(TimeSpan ts)
        {
            if (ts == TimeSpan.MaxValue)
            {
                return "?";
            }

            string str = ts.ToString();
            int index = str.LastIndexOf('.');
            if (index > 0)
            {
                return str.Remove(index);
            }
            else
            {
                return str;
            }
        }
    }
}
