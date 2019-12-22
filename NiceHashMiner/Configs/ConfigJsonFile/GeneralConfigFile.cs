/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
using NiceHashMiner.Configs.Data;

namespace NiceHashMiner.Configs.ConfigJsonFile
{
    public class GeneralConfigFile : ConfigFile<GeneralConfig>
    {
        public GeneralConfigFile()
            : base(Folders.Config, "General.json", "General_old.json")
        { }
    }
}
