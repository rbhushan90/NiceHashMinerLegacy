/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
using System;
using System.Collections.Generic;
using System.Text;
using MyDownloader.Core.Extensions;

namespace MyDownloader.Core.UI
{
    public class CoreExtention : IExtension
    {
        #region IExtension Members

        public string Name
        {
            get { return "Core"; }
        }

        public IUIExtension UIExtension
        {
            get { return new CoreUIExtention(); }
        }

        #endregion
    }
}
