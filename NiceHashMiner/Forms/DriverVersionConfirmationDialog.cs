﻿/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
using System;
using System.Windows.Forms;
using NiceHashMiner.Configs;

namespace NiceHashMiner.Forms
{
    public partial class DriverVersionConfirmationDialog : Form
    {
        public DriverVersionConfirmationDialog()
        {
            InitializeComponent();

            Text = International.GetText("DriverVersionConfirmationDialog_title");
            labelWarning.Text = International.GetText("DriverVersionConfirmationDialog_labelWarning");
            linkToDriverDownloadPage.Text =
                International.GetText("DriverVersionConfirmationDialog_linkToDriverDownloadPage");
            chkBoxDontShowAgain.Text = International.GetText("DriverVersionConfirmationDialog_chkBoxDontShowAgain");
            buttonOK.Text = International.GetText("Global_OK");
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            if (chkBoxDontShowAgain.Checked)
            {
                Helpers.ConsolePrint("NICEHASH", "Setting ShowDriverVersionWarning to false");
                ConfigManager.GeneralConfig.ShowDriverVersionWarning = false;
            }

            Close();
        }

        private void LinkToDriverDownloadPage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(
                "http://support.amd.com/en-us/download/desktop/legacy?product=legacy3&os=Windows+7+-+64");
        }
    }
}
