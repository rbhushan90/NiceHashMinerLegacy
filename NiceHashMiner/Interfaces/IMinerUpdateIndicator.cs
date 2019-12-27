/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
namespace NiceHashMiner.Interfaces
{
    public interface IMinerUpdateIndicator
    {
        void SetMaxProgressValue(int max);
        void SetProgressValueAndMsg(int value, string msg);
        void SetTitle(string title);
        void FinishMsg(bool success);
    }
}
