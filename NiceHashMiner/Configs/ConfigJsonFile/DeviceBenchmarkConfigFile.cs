/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
using NiceHashMiner.Configs.Data;

namespace NiceHashMiner.Configs.ConfigJsonFile
{
    public class DeviceBenchmarkConfigFile : ConfigFile<DeviceBenchmarkConfig>
    {
        private const string BenchmarkPrefix = "benchmark_";

        private static string GetName(string deviceUuid, string old = "")
        {
            // make device name
            var invalid = new[] {'<', '>', ':', '"', '/', '\\', '|', '?', '*'};
            var fileName = BenchmarkPrefix + deviceUuid.Replace(' ', '_');
            foreach (var c in invalid)
            {
                fileName = fileName.Replace(c.ToString(), string.Empty);
            }
            const string extension = ".json";
            return fileName + old + extension;
        }

        public DeviceBenchmarkConfigFile(string deviceUuid)
            : base(Folders.Config, GetName(deviceUuid), GetName(deviceUuid, "_OLD"))
        { }
    }
}
