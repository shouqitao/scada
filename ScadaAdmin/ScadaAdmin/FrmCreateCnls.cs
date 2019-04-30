/*
 * Copyright 2018 Mikhail Shiryaev
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * 
 * Product  : Rapid SCADA
 * Module   : SCADA-Administrator
 * Summary  : Creating channels form
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2010
 * Modified : 2018
 */

using Scada.Comm.Devices;
using Scada.UI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ScadaAdmin {
    /// <inheritdoc />
    /// <summary>
    /// Creating channels form
    /// <para>Channel creation form</para>
    /// </summary>
    public partial class FrmCreateCnls : Form {
        private static string lastCommDir = ""; // latest used SCADA-Communicator directory
        private static Dictionary<string, Type> kpViewTypes = null; // KP interface type dictionary

        private Scada.Comm.AppDirs commDirs; // directories SCADA-Communicator
        private List<CreateCnls.KPInfo> kpInfoList; // list of information about selectable CP
        private List<int> inCnlNums; // list of input channel numbers
        private List<int> ctrlCnlNums; // control channel number list


        /// <inheritdoc />
        /// <summary>
        /// Constructor restricting form creation without parameters
        /// </summary>
        private FrmCreateCnls() {
            InitializeComponent();

            kpInfoList = new List<CreateCnls.KPInfo>();
            inCnlNums = null;
            ctrlCnlNums = null;
            commDirs = null;
            gvKPSel.AutoGenerateColumns = false;
        }


        /// <summary>
        /// Display the form modally
        /// </summary>
        public static void ShowDialog(string commDir) {
            var frmCreateCnls = new FrmCreateCnls();
            frmCreateCnls.commDirs = new Scada.Comm.AppDirs();
            frmCreateCnls.commDirs.Init(commDir);
            frmCreateCnls.ShowDialog();
        }


        /// <summary>
        /// Download KP libraries
        /// </summary>
        private void LoadKPDlls() {
            if (kpViewTypes == null || lastCommDir != commDirs.ExeDir) {
                lastCommDir = commDirs.ExeDir;
                kpViewTypes = new Dictionary<string, Type>();

                try {
                    var dirInfo = new DirectoryInfo(commDirs.KPDir);
                    FileInfo[] fileInfoAr = dirInfo.GetFiles("kp*.dll", SearchOption.TopDirectoryOnly);

                    foreach (var fileInfo in fileInfoAr) {
                        if (!fileInfo.Name.Equals("kp.dll", StringComparison.OrdinalIgnoreCase)) {
                            Type kpViewType;
                            try {
                                kpViewType = KPFactory.GetKPViewType(fileInfo.FullName);
                            } catch {
                                kpViewType = null;
                            }

                            kpViewTypes.Add(fileInfo.Name, kpViewType);
                        }
                    }
                } catch (Exception ex) {
                    AppUtils.ProcError(AppPhrases.LoadKPDllError + ":\r\n" + ex.Message);
                }
            }
        }

        /// <summary>
        /// Fill filter KP on communication line
        /// </summary>
        private void FillKPFilter() {
            try {
                var tblCommLine = Tables.GetCommLineTable();

                var noFilterRow = tblCommLine.NewRow();
                noFilterRow["CommLineNum"] = 0;
                noFilterRow["Name"] = cbKPFilter.Items[0];
                tblCommLine.Rows.InsertAt(noFilterRow, 0);

                cbKPFilter.DataSource = tblCommLine;
                cbKPFilter.SelectedIndexChanged += cbKPFilter_SelectedIndexChanged;
            } catch (Exception ex) {
                AppUtils.ProcError(AppPhrases.FillKPFilterError + ":\r\n" + ex.Message);
            }
        }

        /// <summary>
        /// Fill table KP
        /// </summary>
        private void FillKPGrid() {
            try {
                var tblObj = Tables.GetObjTable();
                tblObj.Rows.Add(DBNull.Value, AppPhrases.UndefinedItem);
                var colObjNum = (DataGridViewComboBoxColumn) gvKPSel.Columns["colObjNum"];
                colObjNum.DataSource = tblObj;
                colObjNum.DisplayMember = "Name";
                colObjNum.ValueMember = "ObjNum";

                var tblKP = Tables.GetKPTable();
                var tblKPType = Tables.GetKPTypeTable();
                foreach (DataRow rowKP in tblKP.Rows) {
                    var kpInfo = CreateCnls.KPInfo.Create(rowKP, tblKPType);

                    if (kpInfo.DllFileName != "") {
                        Type kpViewType;
                        if (kpViewTypes.TryGetValue(kpInfo.DllFileName, out kpViewType)) {
                            if (kpViewType == null) {
                                kpInfo.Color = Color.Red;
                                kpInfo.DllState = CreateCnls.DllStates.Error;
                            } else {
                                kpInfo.Enabled = true;
                                kpInfo.Color = Color.Black;
                                kpInfo.DllState = CreateCnls.DllStates.Loaded;
                            }
                        } else {
                            kpInfo.DllState = CreateCnls.DllStates.NotFound;
                        }
                    }

                    kpInfoList.Add(kpInfo);
                }

                gvKPSel.DataSource = kpInfoList;
            } catch (Exception ex) {
                AppUtils.ProcError(AppPhrases.FillKPGridError + ":\r\n" + ex.Message);
            }
        }

        /// <summary>
        /// Calculate and display channel numbers
        /// </summary>
        private void CalcAndShowCnlNums(bool showError) {
            // getting numbers of existing channels
            if (inCnlNums == null)
                inCnlNums = Tables.GetInCnlNums();
            if (ctrlCnlNums == null)
                ctrlCnlNums = Tables.GetCtrlCnlNums();

            // getting numbering parameters
            var inCnlNumParams = new CreateCnls.CnlNumParams() {
                Start = decimal.ToInt32(numInCnlsStart.Value),
                Multiple = decimal.ToInt32(numInCnlsMultiple.Value),
                Shift = decimal.ToInt32(numInCnlsShift.Value),
                Space = decimal.ToInt32(numInCnlsSpace.Value)
            };

            var ctrlCnlNumParams = new CreateCnls.CnlNumParams() {
                Start = decimal.ToInt32(numCtrlCnlsStart.Value),
                Multiple = decimal.ToInt32(numCtrlCnlsMultiple.Value),
                Shift = decimal.ToInt32(numCtrlCnlsShift.Value),
                Space = decimal.ToInt32(numCtrlCnlsSpace.Value)
            };

            // calculation of channel numbers
            string errMsg;
            bool calcOk = CreateCnls.CalcCnlNums(kpViewTypes, kpInfoList, commDirs,
                inCnlNums, inCnlNumParams, ctrlCnlNums, ctrlCnlNumParams, out errMsg);

            // output to form
            SwitchCalcCreateEnabled(!calcOk);
            gvKPSel.Invalidate();
            if (showError && errMsg != "")
                AppUtils.ProcError(errMsg);
        }

        /// <summary>
        /// Toggle the availability of buttons for calculating channel numbers and creating channels
        /// </summary>
        private void SwitchCalcCreateEnabled(bool calcEnabled) {
            btnCalc.Enabled = calcEnabled;
            btnCreate.Enabled = !calcEnabled;
        }

        /// <summary>
        /// Allow calculation of channel numbers and prohibit the creation of channels
        /// </summary>
        private void EnableCalc() {
            SwitchCalcCreateEnabled(true);
        }


        private void FrmCreateCnls_Load(object sender, EventArgs e) {
            // form translation
            Translator.TranslateForm(this, "ScadaAdmin.FrmCreateCnls");
        }

        private void FrmCreateCnls_Shown(object sender, EventArgs e) {
            // load libraries KP
            LoadKPDlls();

            // filling the filter KP on the communication line
            FillKPFilter();

            // filling in the KP table
            FillKPGrid();

            // setting the availability of buttons for calculating and creating channels
            EnableCalc();
        }

        private void cbKPFilter_SelectedIndexChanged(object sender, EventArgs e) {
            // link table filtering
            if (cbKPFilter.SelectedIndex > 0) {
                var commLineNum = (int) cbKPFilter.SelectedValue;
                gvKPSel.DataSource = kpInfoList.Where(x => x.CommLineNum == commLineNum)
                    .ToList<CreateCnls.KPInfo>();
            } else {
                gvKPSel.DataSource = kpInfoList;
            }

            // deselect all gearboxes
            btnDeselectAll_Click(null, null);
        }

        private void gvKPSel_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e) {
            // setting cell color
            int rowInd = e.RowIndex;
            if (0 <= rowInd && rowInd < kpInfoList.Count) {
                int colInd = e.ColumnIndex;
                if (colInd == colInCnls.Index && kpInfoList[rowInd].InCnlNumsErr ||
                    colInd == colCtrlCnls.Index && kpInfoList[rowInd].CtrlCnlNumsErr)
                    e.CellStyle.ForeColor = Color.Red;
                else
                    e.CellStyle.ForeColor = kpInfoList[rowInd].Color;
            }
        }

        private void gvKPSel_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e) {
            int rowInd = e.RowIndex;
            if (0 <= rowInd && rowInd < kpInfoList.Count) {
                if (kpInfoList[rowInd].Enabled) {
                    if (e.ColumnIndex == colSelected.Index)
                        EnableCalc();
                } else {
                    // cancel cell editing
                    e.Cancel = true;
                }
            }
        }

        private void numCnls_ValueChanged(object sender, EventArgs e) {
            EnableCalc();
        }

        private void btnSelectAll_Click(object sender, EventArgs e) {
            // selection of all controls that are displayed in the table
            // 选择表中显示的所有控件
            var shownList = gvKPSel.DataSource as List<CreateCnls.KPInfo>;
            if (shownList != null) {
                foreach (var kpInfo in shownList)
                    kpInfo.Selected = kpInfo.Enabled;
                gvKPSel.Invalidate();
                EnableCalc();
            }
        }

        private void btnDeselectAll_Click(object sender, EventArgs e) {
            // Deselecting all controls, including those that are not displayed in the table
            // 取消选择所有控件，包括表中未显示的控件
            foreach (var kpInfo in kpInfoList)
                kpInfo.Selected = false;
            gvKPSel.Invalidate();
            EnableCalc();
        }

        private void btnCalc_Click(object sender, EventArgs e) {
            // calculation and display of channel numbers
            // 计算和显示通道号码
            CalcAndShowCnlNums(true);
        }

        private void btnCreate_Click(object sender, EventArgs e) {
            // channel creation
            string logFileName = AppData.AppDirs.LogDir + "ScadaAdminCreateCnls.txt";
            bool logCreated;
            string msg;

            bool createOK = CreateCnls.CreateChannels(kpInfoList,
                chkInsertKPName.Checked, logFileName, out logCreated, out msg);

            if (msg != "") {
                if (createOK)
                    ScadaUiUtils.ShowInfo(msg);
                else
                    AppUtils.ProcError(msg);
            }

            if (logCreated)
                Process.Start(logFileName);
        }
    }
}