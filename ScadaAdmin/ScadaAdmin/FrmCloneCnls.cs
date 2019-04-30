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
 * Summary  : Cloning channels form
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2010
 * Modified : 2016
 */

using Scada;
using Scada.UI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Windows.Forms;
using Utils;

namespace ScadaAdmin {
    /// <inheritdoc />
    /// <summary>
    /// Cloning channels form
    /// <para>Channel Cloning Form</para>
    /// </summary>
    public partial class FrmCloneCnls : Form {
        public FrmCloneCnls() {
            InitializeComponent();
        }


        private void FrmCloneCnls_Load(object sender, EventArgs e) {
            // form translation
            Translator.TranslateForm(this, "ScadaAdmin.FrmCloneCnls");
        }

        private void FrmCloneCnls_Shown(object sender, EventArgs e) {
            // filling in the drop-down list of objects
            try {
                var table = Tables.GetObjTable();
                table.Rows.Add(-1, AppPhrases.NotReplace);
                table.Rows.Add(DBNull.Value, AppPhrases.Undefined);

                cbObj.DataSource = table;
                cbObj.DisplayMember = "Name";
                cbObj.ValueMember = "ObjNum";
                cbObj.SelectedValue = -1;
            } catch (Exception ex) {
                AppUtils.ProcError(AppPhrases.FillObjListError + ":\r\n" + ex.Message);
            }

            // filling the drop-down list of KP
            try {
                var table = Tables.GetKPTable();
                table.Rows.Add(-1, AppPhrases.NotReplace);
                table.Rows.Add(DBNull.Value, AppPhrases.Undefined);

                cbKP.DataSource = table;
                cbKP.DisplayMember = "Name";
                cbKP.ValueMember = "KPNum";
                cbKP.SelectedValue = -1;
            } catch (Exception ex) {
                AppUtils.ProcError(AppPhrases.FillKPListError + ":\r\n" + ex.Message);
            }
        }

        private void btnClone_Click(object sender, EventArgs e) {
            // getting the type of data being cloned
            bool cloneInCnls = rbInCnls.Checked;

            try {
                // getting cloned channels
                string fieldName = cloneInCnls ? "CnlNum" : "CtrlCnlNum";
                var table = cloneInCnls ? Tables.GetInCnlTable() : Tables.GetCtrlCnlTable();
                table.DefaultView.RowFilter = numStartNum.Text + " <= " + fieldName + " and " +
                                              fieldName + " <= " + numFinalNum.Text;

                var rows = new List<DataRow>(); // cloning strings
                for (var i = 0; i < table.DefaultView.Count; i++)
                    rows.Add(table.DefaultView[i].Row);

                // cloning
                int shift = Convert.ToInt32(numNewStartNum.Value - numStartNum.Value);
                var newObjNum = cbObj.SelectedValue;
                var newKPNum = cbKP.SelectedValue;
                bool objNumChanged = newObjNum == DBNull.Value || (int) newObjNum > 0;
                bool kpNumChanged = newKPNum == DBNull.Value || (int) newKPNum > 0;

                foreach (var row in rows) {
                    var newRow = table.NewRow();
                    newRow.ItemArray = row.ItemArray;
                    newRow[0] = (int) newRow[0] + shift;

                    if (objNumChanged)
                        newRow["ObjNum"] = newObjNum;
                    if (kpNumChanged)
                        newRow["KPNum"] = newKPNum;

                    table.Rows.Add(newRow);
                }

                // storing information in the database
                var updRows = 0;
                try {
                    var adapter = table.ExtendedProperties["DataAdapter"] as SqlCeDataAdapter;
                    adapter.ContinueUpdateOnError = false;
                    updRows = adapter.Update(table);

                    string infoMsg =
                        (cloneInCnls ? AppPhrases.CloneInCnlsCompleted : AppPhrases.CloneCtrlCnlsCompleted) + "\r\n" +
                        string.Format(AppPhrases.AddedCnlsCount, updRows);
                    ScadaUiUtils.ShowInfo(updRows > 0 ? infoMsg + AppPhrases.RefreshRequired : infoMsg);
                } catch (Exception ex) {
                    // determining the number of the channel on which the error occurred
                    int cnlNum = -1;
                    if (table.HasErrors) {
                        try {
                            cnlNum = (int) table.GetErrors()[0][fieldName] - shift;
                        } catch {
                            // ignored
                        }
                    }

                    // generating and displaying an error message
                    string errMsg =
                        (cloneInCnls ? AppPhrases.CloneInCnlsError : AppPhrases.CloneCtrlCnlsError) + ".\r\n" +
                        string.Format(AppPhrases.AddedCnlsCount, updRows) + "\r\n" +
                        (cnlNum < 0 ? CommonPhrases.ErrorWithColon : string.Format(AppPhrases.CloneCnlError, cnlNum)) +
                        "\r\n" + Tables.TranslateErrorMessage(ex.Message, table);

                    AppData.ErrLog.WriteAction(errMsg, Log.ActTypes.Exception);
                    ScadaUiUtils.ShowError(updRows > 0 ? errMsg + AppPhrases.RefreshRequired : errMsg);
                }
            } catch (Exception ex) {
                AppUtils.ProcError((cloneInCnls ? AppPhrases.CloneInCnlsError : AppPhrases.CloneCtrlCnlsError) +
                                   ":\r\n" + ex.Message);
            }
        }
    }
}