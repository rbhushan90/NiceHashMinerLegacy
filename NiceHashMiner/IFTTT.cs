﻿/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
using NiceHashMiner.Configs;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace NiceHashMiner
{
    internal class Ifttt
    {
        private const string ApiUrl = "https://maker.ifttt.com/trigger/";

        public static void PostToIfttt(string action, string msg)
        {
            try
            {
                var key = ConfigManager.GeneralConfig.IFTTTKey;
                var worker = ConfigManager.GeneralConfig.WorkerName;
                var minProfit = ConfigManager.GeneralConfig.MinimumProfit.ToString("F2").Replace(',', '.');

                using (var client = new WebClient())
                {
                    var postData = new NameValueCollection
                    {
                        ["value1"] = worker,
                        ["value2"] = msg,
                        ["value3"] = minProfit
                    };

                    var response = client.UploadValues(ApiUrl + action + "/with/key/" + key, postData);

                    var responseString = Encoding.Default.GetString(response);
                }
            }
            catch (Exception ex)
            {
                Helpers.ConsolePrint("NICEHASH", ex.Message);
            }
        }
    }
}
