/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
namespace NiceHashMiner.Utils
{
    public class DownloadSetup
    {
        public DownloadSetup(string url, string dlName, string inFolderName)
        {
            BinsDownloadUrl = url;
            BinsZipLocation = dlName;
            ZipedFolderName = inFolderName;
        }

        public readonly string BinsDownloadUrl;
        public readonly string BinsZipLocation;
        public readonly string ZipedFolderName;
    }
}
