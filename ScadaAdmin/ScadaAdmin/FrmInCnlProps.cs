/*
 * Copyright 2016 Mikhail Shiryaev
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
 * Summary  : Editing input channel properties form
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2010
 * Modified : 2016
 */

using Scada;
using Scada.UI;
using System;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace ScadaAdmin {
    /// <inheritdoc />
    /// <summary>
    /// Editing input channel properties form
    /// <para>Input channel properties edit form</para>
    /// </summary>
    public partial class FrmInCnlProps : Form {
        private FrmTable frmTable;
        private DataGridView gridView;
        private DataGridViewRow row;


        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public FrmInCnlProps() {
            InitializeComponent();
        }


        /// <summary>
        /// Fill in the list of values of the drop-down list and set the selected value.
        /// </summary>
        private void SetComboBoxVal(ComboBox comboBox, string columnName) {
            var col = gridView.Columns[columnName] as DataGridViewComboBoxColumn;
            comboBox.DataSource = col.DataSource;
            comboBox.DisplayMember = col.DisplayMember;
            comboBox.ValueMember = col.ValueMember;
            comboBox.SelectedValue = row.Cells[columnName].Value;
        }

        /// <summary>
        /// Convert object to bool
        /// </summary>
        private bool ObjToBool(object obj) {
            try {
                return (bool) obj;
            } catch {
                return false;
            }
        }

        /// <summary>
        /// Display Input Channel Properties
        /// </summary>
        public DialogResult ShowInCnlProps(FrmTable frmTable) {
            // getting editable table and row
            this.frmTable = frmTable;
            try {
                gridView = frmTable.GridView;
                row = gridView.Rows[gridView.CurrentCell.RowIndex];
            } catch {
                gridView = null;
                row = null;
            }

            // display properties of the selected input channel
            try {
                if (row == null)
                    throw new Exception(CommonPhrases.NoData);

                chkActive.Checked = ObjToBool(row.Cells["Active"].Value);
                txtModifiedDT.Text = row.Cells["ModifiedDT"].FormattedValue.ToString();
                txtCnlNum.Text = row.Cells["CnlNum"].FormattedValue.ToString();
                txtName.Text = row.Cells["Name"].FormattedValue.ToString();

                SetComboBoxVal(cbCnlType, "CnlTypeID");
                SetComboBoxVal(cbObj, "ObjNum");
                if (cbObj.SelectedValue != null)
                    txtObjNum.Text = cbObj.SelectedValue.ToString();
                SetComboBoxVal(cbKP, "KPNum");
                if (cbKP.SelectedValue != null)
                    txtKPNum.Text = cbKP.SelectedValue.ToString();

                txtSignal.Text = row.Cells["Signal"].FormattedValue.ToString();
                chkFormulaUsed.Checked = ObjToBool(row.Cells["FormulaUsed"].Value);
                txtFormula.Text = row.Cells["Formula"].FormattedValue.ToString();

                SetComboBoxVal(cbParam, "ParamID");
                SetComboBoxVal(cbFormat, "FormatID");
                SetComboBoxVal(cbUnit, "UnitID");

                var numObj = row.Cells["CtrlCnlNum"].Value;
                if (numObj is int) {
                    var num = (int) numObj;
                    txtCtrlCnlNum.Text = num.ToString();
                    txtCtrlCnlName.Text = Tables.GetCtrlCnlName(num);
                }

                chkEvEnabled.Checked = ObjToBool(row.Cells["EvEnabled"].Value);
                chkEvSound.Checked = ObjToBool(row.Cells["EvSound"].Value);
                chkEvOnChange.Checked = ObjToBool(row.Cells["EvOnChange"].Value);
                chkEvOnUndef.Checked = ObjToBool(row.Cells["EvOnUndef"].Value);

                txtLimLowCrash.Text = row.Cells["LimLowCrash"].FormattedValue.ToString();
                txtLimLow.Text = row.Cells["LimLow"].FormattedValue.ToString();
                txtLimHigh.Text = row.Cells["LimHigh"].FormattedValue.ToString();
                txtLimHighCrash.Text = row.Cells["LimHighCrash"].FormattedValue.ToString();

                return ShowDialog();
            } catch (Exception ex) {
                AppUtils.ProcError(AppPhrases.ShowInCnlPropsError + ":\r\n" + ex.Message);
                return DialogResult.Cancel;
            }
        }


        private void FrmInCnlProps_Load(object sender, EventArgs e) {
            // form translation
            Translator.TranslateForm(this, "ScadaAdmin.FrmInCnlProps");
        }

        private void txtCtrlCnlNum_TextChanged(object sender, EventArgs e) {
            txtCtrlCnlName.Text = "";
        }

        private void btnOk_Click(object sender, EventArgs e) {
            // validation of entered data
            var errors = new StringBuilder();
            string errMsg;

            if (!AppUtils.ValidateInt(txtCnlNum.Text, 1, ushort.MaxValue, out errMsg))
                errors.AppendLine(AppPhrases.IncorrectInCnlNum).AppendLine(errMsg);
            if (txtName.Text == "")
                errors.AppendLine(AppPhrases.IncorrectInCnlName).AppendLine(CommonPhrases.NonemptyRequired);
            if (cbCnlType.SelectedValue == null)
                errors.AppendLine(AppPhrases.IncorrectCnlType).AppendLine(CommonPhrases.NonemptyRequired);
            if (txtSignal.Text != "" && !AppUtils.ValidateInt(txtSignal.Text, 1, int.MaxValue, out errMsg))
                errors.AppendLine(AppPhrases.IncorrectSignal).AppendLine(errMsg);
            string ctrlCnlNum = txtCtrlCnlNum.Text;
            if (ctrlCnlNum != "") {
                if (AppUtils.ValidateInt(ctrlCnlNum, 1, ushort.MaxValue, out errMsg)) {
                    if (Tables.GetCtrlCnlName(int.Parse(ctrlCnlNum)) == "")
                        errors.AppendLine(AppPhrases.IncorrectCtrlCnlNum)
                            .AppendLine(string.Format(AppPhrases.CtrlCnlNotExists, ctrlCnlNum));
                } else {
                    errors.AppendLine(AppPhrases.IncorrectCtrlCnlNum).AppendLine(errMsg);
                }
            }

            if (txtLimLowCrash.Text != "" && !AppUtils.ValidateDouble(txtLimLowCrash.Text, out errMsg))
                errors.AppendLine(AppPhrases.IncorrectLimLowCrash).AppendLine(errMsg);
            if (txtLimLow.Text != "" && !AppUtils.ValidateDouble(txtLimLow.Text, out errMsg))
                errors.AppendLine(AppPhrases.IncorrectLimLow).AppendLine(errMsg);
            if (txtLimHigh.Text != "" && !AppUtils.ValidateDouble(txtLimHigh.Text, out errMsg))
                errors.AppendLine(AppPhrases.IncorrectLimHigh).AppendLine(errMsg);
            if (txtLimHighCrash.Text != "" && !AppUtils.ValidateDouble(txtLimHighCrash.Text, out errMsg))
                errors.AppendLine(AppPhrases.IncorrectLimHighCrash).AppendLine(errMsg);

            errMsg = errors.ToString().TrimEnd();

            if (errMsg == "") {
                // passing input channel properties to an editable table
                try {
                    var dataRow = frmTable.Table.DefaultView[row.Index];
                    dataRow["Active"] = chkActive.Checked;
                    dataRow["CnlNum"] = txtCnlNum.Text;
                    dataRow["Name"] = txtName.Text;
                    dataRow["CnlTypeID"] = cbCnlType.SelectedValue;
                    dataRow["ModifiedDT"] = DateTime.Now;
                    dataRow["ObjNum"] = cbObj.SelectedValue;
                    dataRow["KPNum"] = cbKP.SelectedValue;
                    dataRow["Signal"] = txtSignal.Text == "" ? DBNull.Value : (object) txtSignal.Text;
                    dataRow["FormulaUsed"] = chkFormulaUsed.Checked;
                    dataRow["Formula"] = txtFormula.Text == "" ? DBNull.Value : (object) txtFormula.Text;
                    dataRow["Averaging"] = chkAveraging.Checked;
                    dataRow["ParamID"] = cbParam.SelectedValue;
                    dataRow["FormatID"] = cbFormat.SelectedValue;
                    dataRow["UnitID"] = cbUnit.SelectedValue;
                    dataRow["CtrlCnlNum"] = txtCtrlCnlNum.Text == "" ? DBNull.Value : (object) txtCtrlCnlNum.Text;
                    dataRow["EvEnabled"] = chkEvEnabled.Checked;
                    dataRow["EvSound"] = chkEvSound.Checked;
                    dataRow["EvOnChange"] = chkEvOnChange.Checked;
                    dataRow["EvOnUndef"] = chkEvOnUndef.Checked;
                    dataRow["LimLowCrash"] = txtLimLowCrash.Text == "" ? DBNull.Value : (object) txtLimLowCrash.Text;
                    dataRow["LimLow"] = txtLimLow.Text == "" ? DBNull.Value : (object) txtLimLow.Text;
                    dataRow["LimHigh"] = txtLimHigh.Text == "" ? DBNull.Value : (object) txtLimHigh.Text;
                    dataRow["LimHighCrash"] = txtLimHighCrash.Text == "" ? DBNull.Value : (object) txtLimHighCrash.Text;
                    DialogResult = DialogResult.OK;
                } catch (Exception ex) {
                    AppUtils.ProcError(AppPhrases.WriteInCnlPropsError + ":\r\n" + ex.Message);
                    DialogResult = DialogResult.Cancel;
                }
            } else {
                ScadaUiUtils.ShowError(errMsg);
            }
        }
    }
}