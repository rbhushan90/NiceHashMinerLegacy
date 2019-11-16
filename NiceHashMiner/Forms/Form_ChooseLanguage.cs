﻿using NiceHashMiner.Configs;
using System;
using System.Windows.Forms;
using NiceHashMinerLegacy.Common.Enums;

namespace NiceHashMiner.Forms
{
    public partial class Form_ChooseLanguage : Form
    {
        //private static readonly string TOS_TEXT = "NiceHash Miner Legacy Term Of Use\r\n\r\nNiceHash operates as in intermediate service by providing hashing power from hashing power owners to hashing power buyers. NiceHash does not directly provide it's own hashing power. We will do our best to provide stable, secure and feature full service. We do not take responsibility for any kind of hashing power loss or funds loss and do not take any kind of financial, material, legal or other responsibilities for any issues that my arise from using NiceHash Miner Legacy. NiceHash service and it's products (NiceHash Miner Legacy, etc.) is still in development, therefore some bugs or other issues may arise. We will work hard to fix any issues as soon as possible, add new features and overall improve our service. NiceHash reserves the rights to seize any funds of suspicious illegal activity such as mining with botnets, money laundering, hacking attempts, etc.\r\n\r\nAs a user of NiceHash Miner Legacy, you are providing your hashing power (you are a seller) to the hashing power buyers through NiceHash's hashing power marketplace. You earn Bitcoins from selling your hashing power for every valid share your miner generates and is accepted by NiceHash service. In some cases no shares are sent to or are accepted by NiceHash service. This cases are rare and include usage of slower hardware, software or network errors or simmilar. In these cases (no shares generated and accepted by NiceHash service) no reward in form of Bitcoins can be accounted to you. Payouts are automatic and are paid to BTC address, used in NiceHash Miner Legacy. Payment schedule can be found in FAQ on our website and is subject to change. You can always monitor your statistics on this site: https://www.nicehash.com/?p=myminer \r\n\r\nDue to possible software bugs or driver version combination you accept that the information displayed in the NiceHash Miner Legacy may be incorrect and/or that you may not get credited for mining.\r\n\r\nIf you have any questions relating to these Terms of Use, your rights and obligations arising from these Terms and/or your use of the NiceHash service, or any other matter, please contact us at support@nicehash.com.";
        private const string TosText =
                "End-User License Agreement (\"Agreement\")\r\n\r\nLast updated: May 10, 2017\r\n\r\nPlease read this End-User License Agreement (\"Agreement\") carefully before clicking the \"I Agree\" button, downloading or using NiceHash Miner Legacy (\"Application\").\r\n\r\nBy clicking the \"I Agree\" button, downloading or using the Application, you are agreeing to be bound by the terms and conditions of this Agreement.\r\n\r\nThis Agreement is a legal agreement between you (either an individual or a single entity) and NiceHash and it governs your use of the Application made available to you by NiceHash.\r\n\r\nIf you do not agree to the terms of this Agreement, do not click on the \"I Agree\" button and do not download or use the Application.\r\n\r\nThe Application is licensed, not sold, to you by NiceHash for use strictly in accordance with the terms of this Agreement.\r\n\r\nLicense\r\n\r\nNiceHash grants you a revocable, non-exclusive, non-transferable, limited license to download, install and use the Application solely for your personal, non-commercial purposes strictly in accordance with the terms of this Agreement.\r\n\r\nRestrictions\r\n\r\nYou agree not to, and you will not permit others to:\r\n\r\nlicense, sell, rent, lease, assign, distribute, transmit, host, outsource, disclose or otherwise commercially exploit the Application or make the Application available to any third party.\r\n\r\ncopy or use the Application for any purpose other than as permitted under the above section 'License'.\r\n\r\nmodify, make derivative works of, disassemble, decrypt, reverse compile or reverse engineer any part of the Application.\r\n\r\nremove, alter or obscure any proprietary notice (including any notice of copyright or trademark) of NiceHash or its affiliates, partners, suppliers or the licensors of the Application.\r\n\r\nIntellectual Property\r\n\r\nThe Application, including without limitation all copyrights, patents, trademarks, trade secrets and other intellectual property rights are, and shall remain, the sole and exclusive property of NiceHash.\r\n\r\nYour Suggestions\r\n\r\nAny feedback, comments, ideas, improvements or suggestions (collectively, \"Suggestions\") provided by you to NiceHash with respect to the Application shall remain the sole and exclusive property of NiceHash.\r\n\r\nNiceHash shall be free to use, copy, modify, publish, or redistribute the Suggestions for any purpose and in any way without any credit or any compensation to you.\r\n\r\nModifications to Application\r\n\r\nNiceHash reserves the right to modify, suspend or discontinue, temporarily or permanently, the Application or any service to which it connects, with or without notice and without liability to you.\r\n\r\nUpdates to Application\r\n\r\nNiceHash may from time to time provide enhancements or improvements to the features/functionality of the Application, which may include patches, bug fixes, updates, upgrades and other modifications (\"Updates\").\r\n\r\nUpdates may modify or delete certain features and/or functionalities of the Application. You agree that NiceHash has no obligation to (i) provide any Updates, or (ii) continue to provide or enable any particular features and/or functionalities of the Application to you.\r\n\r\nYou further agree that all Updates will be (i) deemed to constitute an integral part of the Application, and (ii) subject to the terms and conditions of this Agreement.\r\n\r\nThird-Party Services and Applications\r\n\r\nThe Application may display, include or make available third-party content (including data, information, applications and other products services) or provide links to third-party websites or services (\"Third-Party Services\").\r\n\r\nYou acknowledge and agree that NiceHash shall not be responsible for any Third-Party Services or Applications, including their accuracy, completeness, timeliness, validity, copyright compliance, legality, decency, quality or any other aspect thereof. NiceHash does not assume and shall not have any liability or responsibility to you or any other person or entity for any Third-Party Services or Applications.\r\n\r\nThird-Party Services, Applications, and links thereto are provided solely as a convenience to you and you access and use them entirely at your own risk and subject to such third parties' terms and conditions.\r\n\r\nPrivacy Policy\r\n\r\nNiceHash collects, stores, maintains, and shares information about you in accordance with its Privacy Policy, which is available at https://www.nicehash.com/?p=privacy. By accepting this Agreement, you acknowledge that you hereby agree and consent to the terms and conditions of our Privacy Policy.\r\n\r\nTerm and Termination\r\n\r\nThis Agreement shall remain in effect until terminated by you or NiceHash.\r\n\r\nNiceHash may, in its sole discretion, at any time and for any or no reason, suspend or terminate this Agreement with or without prior notice.\r\n\r\nThis Agreement will terminate immediately, without prior notice from NiceHash, in the event that you fail to comply with any provision of this Agreement. You may also terminate this Agreement by deleting the Application and all copies thereof from your mobile device or from your computer.\r\n\r\nUpon termination of this Agreement, you shall cease all use of the Application and delete all copies of the Application from your mobile device or from your computer.\r\n\r\nTermination of this Agreement will not limit any of NiceHash's rights or remedies at law or in equity in case of breach by you (during the term of this Agreement) of any of your obligations under the present Agreement.\r\n\r\nIndemnification\r\n\r\nYou agree to indemnify and hold NiceHash and its parents, subsidiaries, affiliates, officers, employees, agents, partners and licensors (if any) harmless from any claim or demand, including reasonable attorneys' fees, due to or arising out of your: (a) use of the Application; (b) violation of this Agreement or any law or regulation; or (c) violation of any right of a third party.\r\n\r\nNo Warranties\r\n\r\nThe Application is provided to you \"AS IS\" and \"AS AVAILABLE\" and with all faults and defects without warranty of any kind. To the maximum extent permitted under applicable law, NiceHash, on its own behalf and on behalf of its affiliates and its and their respective licensors and service providers, expressly disclaims all warranties, whether express, implied, statutory or otherwise, with respect to the Application, including all implied warranties of merchantability, fitness for a particular purpose, title and non-infringement, and warranties that may arise out of course of dealing, course of performance, usage or trade practice. Without limitation to the foregoing, NiceHash provides no warranty or undertaking, and makes no representation of any kind that the Application will meet your requirements, achieve any intended results, be compatible or work with any other software, applications, systems or services, operate without interruption, meet any performance or reliability standards or be error free or that any errors or defects can or will be corrected.\r\n\r\nWithout limiting the foregoing, neither NiceHash nor any NiceHash's provider makes any representation or warranty of any kind, express or implied: (i) as to the operation or availability of the Application, or the information, content, and materials or products included thereon; (ii) that the Application will be uninterrupted or error-free; (iii) as to the accuracy, reliability, or currency of any information or content provided through the Application; or (iv) that the Application, its servers, the content, or e-mails sent from or on behalf of NiceHash are free of viruses, scripts, trojan horses, worms, malware, timebombs or other harmful components.\r\n\r\nSome jurisdictions do not allow the exclusion of or limitations on implied warranties or the limitations on the applicable statutory rights of a consumer, so some or all of the above exclusions and limitations may not apply to you.\r\n\r\nLimitation of Liability\r\n\r\nNotwithstanding any damages that you might incur, the entire liability of NiceHash and any of its suppliers under any provision of this Agreement and your exclusive remedy for all of the foregoing shall be limited to the amount actually paid by you for the Application.\r\n\r\nTo the maximum extent permitted by applicable law, in no event shall NiceHash or its suppliers be liable for any special, incidental, indirect, or consequential damages whatsoever (including, but not limited to, damages for loss of profits, for loss of data or other information, for business interruption, for personal injury, for loss of privacy arising out of or in any way related to the use of or inability to use the Application, third-party software and/or third-party hardware used with the Application, or otherwise in connection with any provision of this Agreement), even if NiceHash or any supplier has been advised of the possibility of such damages and even if the remedy fails of its essential purpose.\r\n\r\nSome states/jurisdictions do not allow the exclusion or limitation of incidental or consequential damages, so the above limitation or exclusion may not apply to you.\r\n\r\nSeverability\r\n\r\nIf any provision of this Agreement is held to be unenforceable or invalid, such provision will be changed and interpreted to accomplish the objectives of such provision to the greatest extent possible under applicable law and the remaining provisions will continue in full force and effect.\r\n\r\nWaiver\r\n\r\nExcept as provided herein, the failure to exercise a right or to require performance of an obligation under this Agreement shall not effect a party's ability to exercise such right or require such performance at any time thereafter nor shall be the waiver of a breach constitute waiver of any subsequent breach.\r\n\r\nAmendments to this Agreement\r\n\r\nNiceHash reserves the right, at its sole discretion, to modify or replace this Agreement at any time. If a revision is material we will provide at least 15 days' notice prior to any new terms taking effect. What constitutes a material change will be determined at our sole discretion.\r\n\r\nBy continuing to access or use our Application after any revisions become effective, you agree to be bound by the revised terms. If you do not agree to the new terms, you are no longer authorized to use the Application.\r\n\r\nGoverning Law\r\n\r\nThe laws of Slovenia, excluding its conflicts of law rules, shall govern this Agreement and your use of the Application. Your use of the Application may also be subject to other local, state, national, or international laws.\r\n\r\nContact Information\r\n\r\nIf you have any questions about this Agreement, please contact us (info@nicehash.com).\r\n\r\nEntire Agreement\r\n\r\nThe Agreement constitutes the entire agreement between you and NiceHash regarding your use of the Application and supersedes all prior and contemporaneous written or oral agreements between you and NiceHash.\r\n\r\nYou may be subject to additional terms and conditions that apply when you use or purchase other NiceHash's services, which NiceHash will provide to you at the time of such use or purchase."
            ;

        public Form_ChooseLanguage()
        {
            InitializeComponent();

            // Add language selections list
            var lang = International.GetAvailableLanguages();
            MessageBox.Show(lang.Count.ToString());
            comboBox_Languages.Items.Clear();
            for (var i = 0; i < lang.Count; i++)
            {
                comboBox_Languages.Items.Add(lang[(LanguageType) i]);
            }

            comboBox_Languages.SelectedIndex = 0;

            //label_Instruction.Location = new Point((this.Width - label_Instruction.Size.Width) / 2, label_Instruction.Location.Y);
            //button_OK.Location = new Point((this.Width - button_OK.Size.Width) / 2, button_OK.Location.Y);
            //comboBox_Languages.Location = new Point((this.Width - comboBox_Languages.Size.Width) / 2, comboBox_Languages.Location.Y);
            textBox_TOS.Text = TosText;
            textBox_TOS.ScrollBars = ScrollBars.Vertical;
        }

        private void Button_OK_Click(object sender, EventArgs e)
        {
            ConfigManager.GeneralConfig.Language = (LanguageType) comboBox_Languages.SelectedIndex;
            ConfigManager.GeneralConfigFileCommit();
            Close();
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_TOS.Checked)
            {
                ConfigManager.GeneralConfig.agreedWithTOS = Globals.CurrentTosVer;
                comboBox_Languages.Enabled = true;
                button_OK.Enabled = true;
            }
            else
            {
                ConfigManager.GeneralConfig.agreedWithTOS = 0;
                comboBox_Languages.Enabled = false;
                button_OK.Enabled = false;
            }
        }
    }
}
