﻿/*
* This is an open source non-commercial project. Dear PVS-Studio, please check it.
* PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com
*/
using System;
using System.Windows.Forms;

namespace NiceHashMiner
{
    // TODO probably remove
    public partial class MinerFileNotFoundDialog : Form
    {
        public bool DisableDetection;

        public MinerFileNotFoundDialog(string minerDeviceName, string path)
        {
            InitializeComponent();

            DisableDetection = false;
            Text = International.GetText("MinerFileNotFoundDialog_title");
            linkLabelError.Text = string.Format(International.GetText("MinerFileNotFoundDialog_linkLabelError"),
                minerDeviceName, path, International.GetText("MinerFileNotFoundDialog_link"));
            linkLabelError.LinkArea =
                new LinkArea(linkLabelError.Text.IndexOf(International.GetText("MinerFileNotFoundDialog_link")),
                    International.GetText("MinerFileNotFoundDialog_link").Length);
            chkBoxDisableDetection.Text = International.GetText("MinerFileNotFoundDialog_chkBoxDisableDetection");
            buttonOK.Text = International.GetText("Global_OK");
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            if (chkBoxDisableDetection.Checked)
                DisableDetection = true;

            Close();
        }

        private void LinkLabelError_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/nicehash/NiceHashMiner#troubleshooting");
        }
    }
}
