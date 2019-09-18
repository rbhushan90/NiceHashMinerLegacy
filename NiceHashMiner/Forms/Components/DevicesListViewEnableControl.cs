using NiceHashMiner.Configs;
using NiceHashMiner.Devices;
using NiceHashMiner.Interfaces;
using NiceHashMinerLegacy.Common.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace NiceHashMiner.Forms.Components
{
    public partial class DevicesListViewEnableControl : UserControl
    {
        private const int ENABLED = 0;
        private const int TEMP = 1;
        private const int LOAD = 2;
        private const int FAN = 3;
        private const int POWER = 4;

        private class DefaultDevicesColorSeter : IListItemCheckColorSetter
        {
            private static readonly Color EnabledColor = Color.White;
            private static readonly Color DisabledColor = Color.DarkGray;

            public void LviSetColor(ListViewItem lvi)
            {
                if (lvi.Tag is ComputeDevice cdvo)
                {
                    lvi.BackColor = cdvo.Enabled ? EnabledColor : DisabledColor;
                }
            }
        }

        private IListItemCheckColorSetter _listItemCheckColorSetter = new DefaultDevicesColorSeter();

        public IBenchmarkCalculation BenchmarkCalculation { get; set; }

        private AlgorithmsListView _algorithmsListView;

        // disable checkboxes when in benchmark mode
        private bool _isInBenchmark;

        // helper for benchmarking logic
        public bool IsInBenchmark
        {
            get => _isInBenchmark;
            set
            {
                if (value)
                {
                    _isInBenchmark = true;
                    listViewDevices.CheckBoxes = false;
                }
                else
                {
                    _isInBenchmark = false;
                    listViewDevices.CheckBoxes = true;
                }
            }
        }

        private bool _isMining;

        public bool IsMining
        {
            get => _isMining;
            set
            {
                if (value)
                {
                    _isMining = true;
                    listViewDevices.CheckBoxes = false;
                }
                else
                {
                    _isMining = false;
                    listViewDevices.CheckBoxes = true;
                }
            }
        }

        public bool IsBenchmarkForm = false;
        public bool IsSettingsCopyEnabled = false;

        public string FirstColumnText
        {
            get => listViewDevices.Columns[ENABLED].Text;
            set
            {
                if (value != null) listViewDevices.Columns[ENABLED].Text = value;
            }
        }


        public bool SaveToGeneralConfig { get; set; }

        public DevicesListViewEnableControl()
        {
            InitializeComponent();

            SaveToGeneralConfig = false;
            // intialize ListView callbacks
            listViewDevices.ItemChecked += ListViewDevicesItemChecked;
            //listViewDevices.CheckBoxes = false;
            IsMining = false;
            BenchmarkCalculation = null;
        }

        public void SetIListItemCheckColorSetter(IListItemCheckColorSetter listItemCheckColorSetter)
        {
            _listItemCheckColorSetter = listItemCheckColorSetter;
        }

        public void SetAlgorithmsListView(AlgorithmsListView algorithmsListView)
        {
            _algorithmsListView = algorithmsListView;
        }

        public void ResetListItemColors()
        {
            foreach (ListViewItem lvi in listViewDevices.Items)
            {
                _listItemCheckColorSetter?.LviSetColor(lvi);
            }
        }

        public void SetComputeDevices(List<ComputeDevice> computeDevices)
        {
            // to not run callbacks when setting new
            var tmpSaveToGeneralConfig = SaveToGeneralConfig;
            SaveToGeneralConfig = false;
            listViewDevices.BeginUpdate();
            listViewDevices.Items.Clear();
            string addInfo = "";
            // set devices
            foreach (var computeDevice in computeDevices)
            {
                if (ConfigManager.GeneralConfig.Additional_info_about_device && computeDevice.DeviceType != DeviceType.CPU)
                {
                    addInfo = " (" + computeDevice.GpuRam / 1073741824 + " GB)" + " (" + computeDevice.Uuid.Substring(computeDevice.Uuid.Length-4,4).ToUpper() + ")";
                }
                var lvi = new ListViewItem
                {
                    Checked = computeDevice.Enabled,
                    Text = computeDevice.GetFullName()+addInfo,
                    Tag = computeDevice
                };
                //lvi.SubItems.Add(computeDevice.Name);
                listViewDevices.Items.Add(lvi);
                lvi.SubItems.Add("");
                lvi.SubItems.Add("");
                lvi.SubItems.Add("");
                lvi.SubItems.Add("");
                _listItemCheckColorSetter.LviSetColor(lvi);
            }
            listViewDevices.EndUpdate();
            listViewDevices.Invalidate(true);
            // reset properties
            SaveToGeneralConfig = tmpSaveToGeneralConfig;
        }

        public void SetComputeDevicesStatus(List<ComputeDevice> computeDevices)
        {
            int index = 0;
            foreach (var computeDevice in computeDevices)
            {
                string cTemp = Math.Truncate(computeDevice.Temp).ToString()+ "°C";
                string cLoad = Math.Truncate(computeDevice.Load).ToString()+"%";
                string cFanSpeed = computeDevice.FanSpeed.ToString();
                string cPowerUsage = Math.Truncate(computeDevice.PowerUsage).ToString();
                if (Math.Truncate(computeDevice.PowerUsage) == 0)
                {
                    cPowerUsage = "-1";
                }
                if (ConfigManager.GeneralConfig.Language == LanguageType.Ru)
                {
                    cPowerUsage = cPowerUsage + " Вт";
                } else
                {
                    cPowerUsage = cPowerUsage + " W";
                }

                listViewDevices.Items[index].SubItems[1].Text = cTemp.Contains("-1")?"--":cTemp;
                listViewDevices.Items[index].SubItems[2].Text = cLoad.Contains("-1") ? "--" :cLoad;
                listViewDevices.Items[index].SubItems[3].Text = cFanSpeed.Contains("-1") ? "--" :cFanSpeed;
                listViewDevices.Items[index].SubItems[4].Text = cPowerUsage.Contains("-1") ? "--" :cPowerUsage;
                index++;

            }

        }
        public void ResetComputeDevices(List<ComputeDevice> computeDevices)
        {
            SetComputeDevices(computeDevices);
        }

        public void InitLocale()
        {
            listViewDevices.Columns[ENABLED].Text = International.GetText("ListView_Device");

            if (ConfigManager.GeneralConfig.Language == LanguageType.Ru)
            {
                listViewDevices.Columns[TEMP].Text = "Температура";
                listViewDevices.Columns[LOAD].Text = "Нагрузка";
                listViewDevices.Columns[FAN].Text = "Об/мин";
                listViewDevices.Columns[POWER].Text = "Потребление";
            }
            listViewDevices.Columns[TEMP].Width = 0;
            listViewDevices.Columns[LOAD].Width = 0;
            listViewDevices.Columns[FAN].Width = 0;
            listViewDevices.Columns[POWER].Width = 0;
            //listViewDevices.Columns[0].Width = Width - 4 - SystemInformation.VerticalScrollBarWidth;
            //listViewDevices.Columns[0].Width = Width - SystemInformation.VerticalScrollBarWidth;
            listViewDevices.Columns[0].Width = Width - SystemInformation.VerticalScrollBarWidth - 4;
            
        }

        public void InitLocaleMain()
        {
            listViewDevices.Columns[ENABLED].Text = International.GetText("ListView_Device");
            if (ConfigManager.GeneralConfig.Language == LanguageType.Ru)
            {
                listViewDevices.Columns[TEMP].Text = "Температура";
                listViewDevices.Columns[LOAD].Text = "Нагрузка";
                listViewDevices.Columns[FAN].Text = "Об/мин";
                listViewDevices.Columns[POWER].Text = "Потребление";
            }
            //  listViewDevices.Scrollable = true;
        }

        private void ListViewDevicesItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (e.Item.Tag is ComputeDevice cDevice)
            {
                cDevice.Enabled = e.Item.Checked;

                if (SaveToGeneralConfig)
                {
                    ConfigManager.GeneralConfigFileCommit();
                }
                if (e.Item is ListViewItem lvi) _listItemCheckColorSetter.LviSetColor(lvi);
                _algorithmsListView?.RepaintStatus(cDevice.Enabled, cDevice.Uuid);
            }
            BenchmarkCalculation?.CalcBenchmarkDevicesAlgorithmQueue();
        }

        public void SetDeviceSelectionChangedCallback(ListViewItemSelectionChangedEventHandler callback)
        {
            listViewDevices.ItemSelectionChanged += callback;
        }

        private void ListViewDevices_MouseClick(object sender, MouseEventArgs e)
        {
            if (IsInBenchmark) return;
            if (IsMining) return;
            if (e.Button == MouseButtons.Right)
            {
                if (listViewDevices.FocusedItem.Bounds.Contains(e.Location))
                {
                    contextMenuStrip1.Items.Clear();
                    if (IsSettingsCopyEnabled)
                    {
                        if (listViewDevices.FocusedItem.Tag is ComputeDevice cDevice)
                        {
                            var sameDevTypes =
                                ComputeDeviceManager.Available.GetSameDevicesTypeAsDeviceWithUuid(cDevice.Uuid);
                            if (sameDevTypes.Count > 0)
                            {
                                var copyBenchItem = new ToolStripMenuItem();
                                var copyTuningItem = new ToolStripMenuItem();
                                //copyBenchItem.DropDownItems
                                foreach (var cDev in sameDevTypes)
                                {
                                    if (cDev.Enabled)
                                    {
                                        var copyBenchDropDownItem = new ToolStripMenuItem
                                        {
                                            Text = cDev.Name,
                                            Checked = cDev.Uuid == cDevice.BenchmarkCopyUuid
                                        };
                                        copyBenchDropDownItem.Click += ToolStripMenuItemCopySettings_Click;
                                        copyBenchDropDownItem.Tag = cDev.Uuid;
                                        copyBenchItem.DropDownItems.Add(copyBenchDropDownItem);

                                        var copyTuningDropDownItem = new ToolStripMenuItem {
                                            Text = cDev.Name
                                            //Checked = cDev.UUID == CDevice.TuningCopyUUID
                                        };
                                        copyTuningDropDownItem.Click += ToolStripMenuItemCopyTuning_Click;
                                        copyTuningDropDownItem.Tag = cDev.Uuid;
                                        copyTuningItem.DropDownItems.Add(copyTuningDropDownItem);
                                    }
                                }
                                copyBenchItem.Text = International.GetText("DeviceListView_ContextMenu_CopySettings");
                                copyTuningItem.Text = International.GetText("DeviceListView_ContectMenu_CopyTuning");
                                contextMenuStrip1.Items.Add(copyBenchItem);
                                contextMenuStrip1.Items.Add(copyTuningItem);
                            }
                        }
                    }
                    contextMenuStrip1.Show(Cursor.Position);
                }
            }
        }

        private void ToolStripMenuItem_Click(object sender, bool justTuning) {
            if (sender is ToolStripMenuItem item && item.Tag is string uuid
                && listViewDevices.FocusedItem.Tag is ComputeDevice CDevice) {
                var copyBenchCDev = ComputeDeviceManager.Available.GetDeviceWithUuid(uuid);

                var result = MessageBox.Show(
                    string.Format(
                        International.GetText("DeviceListView_ContextMenu_CopySettings_Confirm_Dialog_Msg"),
                        copyBenchCDev.GetFullName(), CDevice.GetFullName()),
                    International.GetText("DeviceListView_ContextMenu_CopySettings_Confirm_Dialog_Title"),
                    MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    if (justTuning)
                    {
                        CDevice.TuningCopyUuid = uuid;
                        CDevice.CopyTuningSettingsFrom(copyBenchCDev);
                    }
                    else
                    {
                        CDevice.BenchmarkCopyUuid = uuid;
                        CDevice.CopyBenchmarkSettingsFrom(copyBenchCDev);
                    }

                    _algorithmsListView.Update();
                    _algorithmsListView.Refresh();
                    _algorithmsListView?.RepaintStatus(CDevice.Enabled, CDevice.Uuid);
                }
            }
        }

        private void ToolStripMenuItemCopySettings_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem_Click(sender, false);
        }

        private void ToolStripMenuItemCopyTuning_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem_Click(sender, true);
        }

        private void DevicesListViewEnableControl_Resize(object sender, EventArgs e)
        {
            //listViewDevices.Columns[0].Width = Width - SystemInformation.VerticalScrollBarWidth;
            // only one
            foreach (ColumnHeader ch in listViewDevices.Columns)
            {
          //      ch.Width = Width - 10;
            }
        }

        public void SetFirstSelected()
        {
            if (listViewDevices.Items.Count > 0)
            {
                listViewDevices.Items[0].Selected = true;
                listViewDevices.Select();
            }
        }

        private void listViewDevices_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
