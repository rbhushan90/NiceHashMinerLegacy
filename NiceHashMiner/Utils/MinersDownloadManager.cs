namespace NiceHashMiner.Utils
{
    public static class MinersDownloadManager
    {
        public static DownloadSetup StandardDlSetup = new DownloadSetup(
             "https://github.com/angelbbs/NiceHashMinerLegacy/releases/download/Fork_Fix_19.2/bin_ff192.zip",
            "bins.zip",
            "bin");

        public static DownloadSetup ThirdPartyDlSetup = new DownloadSetup(
            "https://github.com/angelbbs/NiceHashMinerLegacy/releases/download/Fork_Fix_19.2/bin_3rdparty_ff192.zip",
            "bins_3rdparty.zip",
            "bin_3rdparty");
    }
}
