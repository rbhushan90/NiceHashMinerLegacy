using NiceHashMiner.Devices;
using NiceHashMiner.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using NiceHashMiner.Algorithms;
using NiceHashMiner.Configs;

namespace NiceHashMiner.Forms.Components
{
    public partial class AlgorithmsListView : UserControl
    {
        private const int ENABLED = 0;
        private const int ALGORITHM = 1;
        private const int SPEED = 2;
        private const int SECSPEED = 3;
        private const int RATIO = 4;
        private const int RATE = 5;
        public static bool isListViewEnabled = true;
        public interface IAlgorithmsListView
        {
            void SetCurrentlySelected(ListViewItem lvi, ComputeDevice computeDevice);
            void HandleCheck(ListViewItem lvi);
            void ChangeSpeed(ListViewItem lvi);
        }

        public IAlgorithmsListView ComunicationInterface { get; set; }

        public IBenchmarkCalculation BenchmarkCalculation { get; set; }

        ComputeDevice _computeDevice;

        private class DefaultAlgorithmColorSeter : IListItemCheckColorSetter
        {
            //private static readonly Color DisabledColor = Color.FromArgb(Form_Main._backColor.ToArgb() + 40 * 256 * 256 * 256 + 40 * 256 * 256 + 40 * 256 + 40);
            public static Color DisabledColor = ConfigManager.GeneralConfig.ColorProfileIndex != 0 ? Color.FromArgb(Form_Main._backColor.ToArgb() + 40 * 256 * 256 * 256 + 40 * 256 * 256 + 40 * 256 + 40) : Color.DarkGray;
            //  private static readonly Color DisabledColor = Form_Main._backColor;
            private static readonly Color BenchmarkedColor = Form_Main._backColor;
            private static readonly Color UnbenchmarkedColor = Color.LightBlue;

            public void LviSetColor(ListViewItem lvi)
            {
                if (!isListViewEnabled)
                {
                    return;
                }
                if (lvi.Tag is Algorithm algorithm)
                {
                    if (!algorithm.Enabled && !algorithm.IsBenchmarkPending)
                    {
                        lvi.BackColor = DisabledColor;
                    }
                    else if (!algorithm.BenchmarkNeeded && !algorithm.IsBenchmarkPending)
                    {
                        lvi.BackColor = BenchmarkedColor;
                    }
                    else
                    {
                        lvi.BackColor = UnbenchmarkedColor;
                    }
                }
            }
        }

        private readonly IListItemCheckColorSetter _listItemCheckColorSetter = new DefaultAlgorithmColorSeter();

        // disable checkboxes when in benchmark mode
        private bool _isInBenchmark = false;

        // helper for benchmarking logic
        public bool IsInBenchmark
        {
            get => _isInBenchmark;
            set
            {
                if (value)
                {
                    _isInBenchmark = true;
                    listViewAlgorithms.CheckBoxes = false;
                }
                else
                {
                    _isInBenchmark = false;
                    listViewAlgorithms.CheckBoxes = true;
                }
            }
        }

        public AlgorithmsListView()
        {
            InitializeComponent();
            System.Reflection.PropertyInfo dbProp = typeof(System.Windows.Forms.Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            dbProp.SetValue(this, true, null);
            AlgorithmsListView.colorListViewHeader(ref listViewAlgorithms, Form_Main._backColor, Form_Main._textColor);

            // callback initializations
            listViewAlgorithms.ItemSelectionChanged += ListViewAlgorithms_ItemSelectionChanged;
            listViewAlgorithms.ItemChecked += (ItemCheckedEventHandler) ListViewAlgorithms_ItemChecked;
            IsInBenchmark = false;
         //   listViewAlgorithms.OwnerDraw = true;
        }
        public static void colorListViewHeader(ref ListView list, Color backColor, Color foreColor)
        {
            list.OwnerDraw = true;
            list.DrawColumnHeader +=
            new DrawListViewColumnHeaderEventHandler
            (
            (sender, e) => headerDraw(sender, e, backColor, foreColor)
            );
            list.DrawItem += new DrawListViewItemEventHandler(bodyDraw);

        }
        private static void headerDraw(object sender, DrawListViewColumnHeaderEventArgs e, Color backColor, Color foreColor)
        {
            using (SolidBrush backBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            using (SolidBrush foreBrush = new SolidBrush(foreColor))
            {
                StringFormat sf = new StringFormat();
                if ((e.ColumnIndex == 0))
                {
                    sf.LineAlignment = StringAlignment.Center;
                    sf.Alignment = StringAlignment.Near;
                }
                else
                {
                    sf.LineAlignment = StringAlignment.Center;
                    sf.Alignment = StringAlignment.Center;
                }
                e.Graphics.DrawString(e.Header.Text, e.Font, foreBrush, e.Bounds, sf);
            }
        }

        private static void bodyDraw(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;

            using (SolidBrush backBrush = new SolidBrush(Form_Main._backColor))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

        }
        public void InitLocale()
        {
            var _backColor = Form_Main._backColor;
            var _foreColor = Form_Main._foreColor;
            var _textColor = Form_Main._textColor;
            //  foreach (var lbl in this.Controls.OfType<ListView>()) lbl.BackColor = _backColor;
            listViewAlgorithms.BackColor = _backColor;
            listViewAlgorithms.ForeColor = _textColor;
            this.BackColor = _backColor;

            listViewAlgorithms.Columns[ENABLED].Text = International.GetText("AlgorithmsListView_Enabled");
            listViewAlgorithms.Columns[ALGORITHM].Text = International.GetText("AlgorithmsListView_Algorithm");
            listViewAlgorithms.Columns[SPEED].Text = International.GetText("AlgorithmsListView_Speed");
            listViewAlgorithms.Columns[SECSPEED].Text = International.GetText("Form_DcriValues_SecondarySpeed");
            listViewAlgorithms.Columns[RATIO].Text = International.GetText("AlgorithmsListView_Ratio");
            listViewAlgorithms.Columns[RATE].Text = International.GetText("AlgorithmsListView_Rate");
            //listViewAlgorithms.Columns[RATE].Width = 0;
        }

        public void SetAlgorithms(ComputeDevice computeDevice, bool isEnabled)
        {
            _computeDevice = computeDevice;
            listViewAlgorithms.BeginUpdate();
            listViewAlgorithms.Items.Clear();
            foreach (var alg in computeDevice.GetAlgorithmSettings())
            {
                if (!alg.Hidden)
                {
                    var lvi = new ListViewItem();

                    var name = "";
                    var secondarySpeed = "";
                    var payingRatio = "";
                    if (alg is DualAlgorithm dualAlg)
                    {
                        name = "  + " + dualAlg.SecondaryAlgorithmName;
                        secondarySpeed = dualAlg.SecondaryBenchmarkSpeedString();
                        payingRatio = dualAlg.SecondaryCurPayingRatio;
                    }
                    else
                    {
                        name = $"{alg.AlgorithmName} ({alg.MinerBaseTypeName})";
                        payingRatio = alg.CurPayingRatio;
                    }

                    lvi.SubItems.Add(name);

                    //sub.Tag = alg.Value;
                    lvi.SubItems.Add(alg.BenchmarkSpeedString());
                    lvi.SubItems.Add(secondarySpeed);
                    lvi.SubItems.Add(payingRatio);
                    lvi.SubItems.Add(alg.CurPayingRate);
                    lvi.Tag = alg;
                    lvi.Checked = alg.Enabled;
                    listViewAlgorithms.Items.Add(lvi);
                }
            }

            listViewAlgorithms.EndUpdate();
            //Enabled = isEnabled;
            if (ConfigManager.GeneralConfig.ColorProfileIndex != 0)
            {
                isListViewEnabled = isEnabled;
                listViewAlgorithms.CheckBoxes = isEnabled;
            }
        }

        public void RepaintStatus(bool isEnabled, string uuid)
        {
            if (_computeDevice != null && _computeDevice.Uuid == uuid)
            {
                foreach (ListViewItem lvi in listViewAlgorithms.Items)
                {
                    var algo = lvi.Tag as Algorithm;
                    lvi.SubItems[SPEED].Text = algo?.BenchmarkSpeedString();
                    lvi.Checked = algo.Enabled;
                    if (algo is DualAlgorithm dualAlg)
                        lvi.SubItems[SECSPEED].Text = dualAlg.SecondaryBenchmarkSpeedString();
                    _listItemCheckColorSetter.LviSetColor(lvi);
                }

                //Visible = isEnabled;
                //Enabled = isEnabled;
                if (ConfigManager.GeneralConfig.ColorProfileIndex != 0)
                {
                    isListViewEnabled = isEnabled;
                    listViewAlgorithms.CheckBoxes = isEnabled;
                }
            }
        }

        #region Callbacks Events

        private void ListViewAlgorithms_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            ComunicationInterface?.SetCurrentlySelected(e.Item, _computeDevice);
        }

        private void ListViewAlgorithms_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (IsInBenchmark)
            {
                e.Item.Checked = !e.Item.Checked;
                return;
            }

            if (e.Item.Tag is Algorithm algo)
            {
                algo.Enabled = e.Item.Checked;
            }

            ComunicationInterface?.HandleCheck(e.Item);
            var lvi = e.Item;
            _listItemCheckColorSetter.LviSetColor(lvi);
            // update benchmark status data
            BenchmarkCalculation?.CalcBenchmarkDevicesAlgorithmQueue();
        }

        #endregion //Callbacks Events

        public void ResetListItemColors()
        {
            foreach (ListViewItem lvi in listViewAlgorithms.Items)
            {
                _listItemCheckColorSetter?.LviSetColor(lvi);
            }
        }

        // benchmark settings
        public void SetSpeedStatus(ComputeDevice computeDevice, Algorithm algorithm, string status)
        {
            if (algorithm != null)
            {
                algorithm.BenchmarkStatus = status;
                // gui update only if same as selected
                if (_computeDevice != null && computeDevice.Uuid == _computeDevice.Uuid)
                {
                    foreach (ListViewItem lvi in listViewAlgorithms.Items)
                    {
                        if (lvi.Tag is Algorithm algo && algo.AlgorithmStringID == algorithm.AlgorithmStringID)
                        {
                            // TODO handle numbers
                            lvi.SubItems[SPEED].Text = algorithm.BenchmarkSpeedString();
                            lvi.SubItems[RATE].Text = algorithm.CurPayingRate;
                            
                            if (algorithm is DualAlgorithm dualAlg)
                            {
                                lvi.SubItems[RATIO].Text = dualAlg.SecondaryCurPayingRatio;
                                lvi.SubItems[SECSPEED].Text = dualAlg.SecondaryBenchmarkSpeedString();
                            }
                            else
                            {
                                lvi.SubItems[RATIO].Text = algorithm.CurPayingRatio;
                            }

                            _listItemCheckColorSetter.LviSetColor(lvi);
                            break;
                        }
                    }
                }
            }
        }

        private void ListViewAlgorithms_MouseClick(object sender, MouseEventArgs e)
        {
            if (!isListViewEnabled)
            {
                listViewAlgorithms.SelectedItems.Clear();
                return;
            }
            if (IsInBenchmark) return;
            if (e.Button == MouseButtons.Right)
            {
                contextMenuStrip1.Items.Clear();
                // disable all
                {
                    var disableAllItems = new ToolStripMenuItem
                    {
                        Text = International.GetText("AlgorithmsListView_ContextMenu_DisableAll")
                    };
                    disableAllItems.Click += ToolStripMenuItemDisableAll_Click;
                    contextMenuStrip1.Items.Add(disableAllItems);
                }
                // enable all
                {
                    var enableAllItems = new ToolStripMenuItem
                    {
                        Text = International.GetText("AlgorithmsListView_ContextMenu_EnableAll")
                    };
                    enableAllItems.Click += ToolStripMenuItemEnableAll_Click;
                    contextMenuStrip1.Items.Add(enableAllItems);
                }
                // test this
                {
                    var testItem = new ToolStripMenuItem
                    {
                        Text = International.GetText("AlgorithmsListView_ContextMenu_TestItem")
                    };
                    testItem.Click += ToolStripMenuItemTest_Click;
                    contextMenuStrip1.Items.Add(testItem);
                }
                // enable benchmarked only
                {
                    var enableBenchedItem = new ToolStripMenuItem
                    {
                        Text = International.GetText("AlgorithmsListView_ContextMenu_EnableBenched")
                    };
                    enableBenchedItem.Click += ToolStripMenuItemEnableBenched_Click;
                    contextMenuStrip1.Items.Add(enableBenchedItem);
                }
                // clear item
                {
                    var clearItem = new ToolStripMenuItem
                    {
                        Text = International.GetText("AlgorithmsListView_ContextMenu_ClearItem")
                    };
                    clearItem.Click += ToolStripMenuItemClear_Click;
                    contextMenuStrip1.Items.Add(clearItem);
                }
                // open dcri
                {
                    var dcriMenu = new ToolStripMenuItem
                    {
                        Text = International.GetText("Form_DcriValues_Title")
                    };

                    if (listViewAlgorithms.SelectedItems.Count > 0
                        && listViewAlgorithms.SelectedItems[0].Tag is DualAlgorithm dualAlg)
                    {
                        dcriMenu.Enabled = true;

                        var openDcri = new ToolStripMenuItem
                        {
                            Text = International.GetText("AlgorithmsListView_ContextMenu_OpenDcri")
                        };
                        openDcri.Click += toolStripMenuItemOpenDcri_Click;
                        dcriMenu.DropDownItems.Add(openDcri);

                        var tuningEnabled = new ToolStripMenuItem
                        {
                            Text = International.GetText("Form_DcriValues_TuningEnabled"),
                            CheckOnClick = true,
                            Checked = dualAlg.TuningEnabled
                        };
                        tuningEnabled.CheckedChanged += toolStripMenuItemTuningEnabled_Checked;
                        dcriMenu.DropDownItems.Add(tuningEnabled);
                    }
                    else
                    {
                        dcriMenu.Enabled = false;
                    }

                    contextMenuStrip1.Items.Add(dcriMenu);
                }
                contextMenuStrip1.Show(Cursor.Position);
            }
        }

        private void ToolStripMenuItemEnableAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem lvi in listViewAlgorithms.Items)
            {
                lvi.Checked = true;
            }
        }

        private void ToolStripMenuItemDisableAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem lvi in listViewAlgorithms.Items)
            {
                lvi.Checked = false;
            }
        }

        private void ToolStripMenuItemClear_Click(object sender, EventArgs e)
        {
            if (_computeDevice != null)
            {
                foreach (ListViewItem lvi in listViewAlgorithms.SelectedItems)
                {
                    if (lvi.Tag is Algorithm algorithm)
                    {
                        algorithm.BenchmarkSpeed = 0;
                        if (algorithm is DualAlgorithm dualAlgo)
                        {
                            dualAlgo.SecondaryBenchmarkSpeed = 0;
                            dualAlgo.IntensitySpeeds = new Dictionary<int, double>();
                            dualAlgo.SecondaryIntensitySpeeds = new Dictionary<int, double>();
                            dualAlgo.IntensityUpToDate = false;
                        }

                        RepaintStatus(_computeDevice.Enabled, _computeDevice.Uuid);
                        // update benchmark status data
                        BenchmarkCalculation?.CalcBenchmarkDevicesAlgorithmQueue();
                        // update settings
                        ComunicationInterface?.ChangeSpeed(lvi);
                    }
                }
            }
        }

        private void ToolStripMenuItemTest_Click(object sender, EventArgs e)
        {
            if (_computeDevice != null)
            {
                foreach (ListViewItem lvi in listViewAlgorithms.Items)
                {
                    if (lvi.Tag is Algorithm algorithm)
                    {
                        lvi.Checked = lvi.Selected;
                        if (lvi.Selected && algorithm.BenchmarkSpeed <= 0)
                        {
                            // If it has zero speed, set to 1 so it can be tested
                            algorithm.BenchmarkSpeed = 1;
                            RepaintStatus(_computeDevice.Enabled, _computeDevice.Uuid);
                            ComunicationInterface?.ChangeSpeed(lvi);
                        }
                    }
                }
            }
        }

        private void toolStripMenuItemOpenDcri_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem lvi in listViewAlgorithms.SelectedItems)
            {
                if (lvi.Tag is DualAlgorithm algo)
                {
                    var dcriValues = new FormDcriValues(algo);
                    dcriValues.ShowDialog();
                    RepaintStatus(_computeDevice.Enabled, _computeDevice.Uuid);
                    // update benchmark status data
                    BenchmarkCalculation?.CalcBenchmarkDevicesAlgorithmQueue();
                    // update settings
                    ComunicationInterface?.ChangeSpeed(lvi);
                }
            }
        }

        private void ToolStripMenuItemEnableBenched_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem lvi in listViewAlgorithms.Items)
            {
                if (lvi.Tag is Algorithm algorithm && algorithm.BenchmarkSpeed > 0)
                {
                    lvi.Checked = true;
                }
            }
        }

        private void toolStripMenuItemTuningEnabled_Checked(object sender, EventArgs e)
        {
            foreach (ListViewItem lvi in listViewAlgorithms.SelectedItems)
            {
                if (lvi.Tag is DualAlgorithm algo)
                {
                    algo.TuningEnabled = ((ToolStripMenuItem) sender).Checked;
                    RepaintStatus(_computeDevice.Enabled, _computeDevice.Uuid);
                }
            }
        }

        private void listViewAlgorithms_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listViewAlgorithms_EnabledChanged(object sender, EventArgs e)
        {
          //  AlgorithmsListView.colorListViewHeader(ref listViewAlgorithms, Color.Red, Form_Main._textColor);
        }

        private void listViewAlgorithms_Click(object sender, EventArgs e)
        {
            if (!isListViewEnabled)
            {
                listViewAlgorithms.SelectedItems.Clear();
            }
        }

        private void listViewAlgorithms_ItemChecked_1(object sender, ItemCheckedEventArgs e)
        {
            if (!isListViewEnabled)
            {
                listViewAlgorithms.SelectedItems.Clear();
            }
        }

        private void listViewAlgorithms_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (!isListViewEnabled)
            {
                listViewAlgorithms.SelectedItems.Clear();
            }
        }

        private void listViewAlgorithms_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            listViewAlgorithms.BeginUpdate();

            if (e.ColumnIndex == 4)
            {
                ResizeAutoSizeColumn(listViewAlgorithms, 0);
            }
            if (e.ColumnIndex == 0)
            {
                ResizeAutoSizeColumn(listViewAlgorithms, 4);
            }
            listViewAlgorithms.EndUpdate();
        }
        static private void ResizeAutoSizeColumn(ListView listView, int autoSizeColumnIndex)
        {
            // Do some rudimentary (parameter) validation.
            if (listView == null) throw new ArgumentNullException("listView");
            if (listView.View != View.Details || listView.Columns.Count <= 0 || autoSizeColumnIndex < 0) return;
            if (autoSizeColumnIndex >= listView.Columns.Count)
                throw new IndexOutOfRangeException("Parameter autoSizeColumnIndex is outside the range of column indices in the ListView.");

            // Sum up the width of all columns except the auto-resizing one.
            int otherColumnsWidth = 0;
            foreach (ColumnHeader header in listView.Columns)
                if (header.Index != autoSizeColumnIndex)
                    otherColumnsWidth += header.Width;

            // Calculate the (possibly) new width of the auto-resizable column.
            int autoSizeColumnWidth = listView.ClientRectangle.Width - otherColumnsWidth;

            // Finally set the new width of the auto-resizing column, if it has changed.
            if (listView.Columns[autoSizeColumnIndex].Width != autoSizeColumnWidth)
                listView.Columns[autoSizeColumnIndex].Width = autoSizeColumnWidth;
        }

        private void listViewAlgorithms_Resize(object sender, EventArgs e)
        {
            //ResizeColumn();
            listViewAlgorithms.BeginUpdate();
            ResizeAutoSizeColumn(listViewAlgorithms, 1);
            listViewAlgorithms.EndUpdate();
        }
    }

}
