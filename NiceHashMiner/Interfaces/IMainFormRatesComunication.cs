﻿/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
namespace NiceHashMiner.Interfaces
{
    public interface IMainFormRatesComunication
    {
        void ClearRatesAll();

        void AddRateInfo(string groupName, string deviceStringInfo, ApiData iApiData, double paying, double power,
            bool isApiGetException);
        //void RaiseAlertSharesNotAccepted(string algoName);

        // The following four must use an invoker since they may be called from non-UI thread

        void ShowNotProfitable(string msg);

        void HideNotProfitable();

        void ForceMinerStatsUpdate();

        void ClearRates(int groupCount);
    }
}
