/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
using System;
using System.Collections.Generic;
using System.Text;

namespace MyDownloader.Core
{
    [Serializable]
    public class RemoteFileInfo
    {
        private bool acceptRanges;
        private long fileSize;
        private DateTime lastModified = DateTime.MinValue;

        private string mimeType;

        public string MimeType
        {
            get { return mimeType; }
            set { mimeType = value; }
        }

        public bool AcceptRanges
        {
            get { return acceptRanges; }
            set { acceptRanges = value; }
        }

        public long FileSize
        {
            get { return fileSize; }
            set { fileSize = value; }
        }       

        public DateTime LastModified
        {
            get { return lastModified; }
            set { lastModified = value; }
        }

    }
}
