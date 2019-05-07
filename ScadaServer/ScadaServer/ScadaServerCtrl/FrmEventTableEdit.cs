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
 * Module   : SCADA-Server Control
 * Summary  : Editing or viewing event table form
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2013
 * Modified : 2016
 */

using Scada.Data.Tables;
using Scada.UI;
using System;
using System.Data;
using System.Windows.Forms;
using Utils;

namespace Scada.Server.Ctrl {
    /// <inheritdoc />
    /// <summary>
    /// Editing or viewing event table form
    /// <para>The form of editing or viewing the table of events</para>
    /// </summary>
    public partial class FrmEventTableEdit : Form {
        private Log errLog; // application error log
        private EventAdapter eventAdapter; // event table adapter
        private DataTable dataTable; // event table
        private bool editMode; // edit mode


        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        private FrmEventTableEdit() {
            InitializeComponent();
            errLog = null;
            eventAdapter = null;
            dataTable = null;
            editMode = false;
        }


        /// <summary>
        /// Check the correctness of the real value of the cell
        /// </summary>
        private bool ValidateCell(int colInd, int rowInd, object cellVal) {
            if (0 <= colInd && colInd < dataGridView.ColumnCount && 0 <= rowInd &&
                rowInd < dataGridView.RowCount &&
                cellVal != null) {
                Type valType = dataGridView.Columns[colInd].ValueType;
                string valStr = cellVal.ToString();

                if (valType == typeof(int)) {
                    int intVal;
                    if (!int.TryParse(valStr, out intVal)) {
                        ScadaUiUtils.ShowError(CommonPhrases.IntegerRequired);
                        return false;
                    }
                } else if (valType == typeof(double)) {
                    double doubleVal;
                    if (!double.TryParse(valStr, out doubleVal)) {
                        ScadaUiUtils.ShowError(CommonPhrases.RealRequired);
                        return false;
                    }
                } else if (valType == typeof(DateTime)) {
                    DateTime dtVal;
                    if (!DateTime.TryParse(valStr, out dtVal)) {
                        ScadaUiUtils.ShowError(CommonPhrases.DateTimeRequired);
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Save event table changes
        /// </summary>
        private bool Save() {
            try {
                eventAdapter.Update(dataTable);
                dataGridView.Invalidate();
                return true;
            } catch (Exception ex) {
                string errMsg = AppPhrases.SaveEventTableError + ":\r\n" + ex.Message;
                if (errLog != null)
                    errLog.WriteAction(errMsg, Log.ActTypes.Exception);
                ScadaUiUtils.ShowError(errMsg);
                return false;
            }
        }

        /// <summary>
        /// Download event table
        /// </summary>
        private static bool LoadDataTable(EventAdapter eventAdapter, Log errLog, ref DataTable dataTable) {
            try {
                eventAdapter.Fill(dataTable);
                return true;
            } catch (Exception ex) {
                string errMsg = AppPhrases.LoadEventTableError + ":\r\n" + ex.Message;
                if (errLog != null)
                    errLog.WriteAction(errMsg, Log.ActTypes.Exception);
                ScadaUiUtils.ShowError(errMsg);
                return false;
            }
        }

        /// <summary>
        /// Display the form of editing or viewing the table of slices
        /// </summary>
        public static void Show(string directory, string tableName, bool editMode, Log errLog) {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("directory");
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("tableName");
            if (errLog == null)
                throw new ArgumentNullException("errLog");

            EventAdapter eventAdapter = new EventAdapter();
            eventAdapter.Directory = directory;
            eventAdapter.TableName = tableName;
            var dataTable = new DataTable();

            if (LoadDataTable(eventAdapter, errLog, ref dataTable)) {
                var frmEventTableEdit = new FrmEventTableEdit {
                    errLog = errLog,
                    eventAdapter = eventAdapter,
                    dataTable = dataTable,
                    editMode = editMode
                };
                frmEventTableEdit.ShowDialog();
            }
        }


        private void FrmEventTableEdit_Load(object sender, EventArgs e) {
            // form translation
            Translator.TranslateForm(this, "Scada.Server.Ctrl.FrmEventTableEdit");
            if (lblCount.Text.Contains("{0}"))
                bindingNavigator.CountItemFormat = lblCount.Text;

            // setting controls
            Text = (editMode ? AppPhrases.EditEventTableTitle : AppPhrases.ViewEventTableTitle) +
                   @" - " + eventAdapter.TableName;
            dataTable.DefaultView.AllowNew = editMode;
            dataTable.DefaultView.AllowEdit = editMode;
            bindingSource.DataSource = dataTable;
            btnSave.Visible = editMode;
        }

        private void FrmEventTableEdit_FormClosing(object sender, FormClosingEventArgs e) {
            btnClose.Focus(); // to finish editing the table cell
            var dataView = new DataView(dataTable, "", "",
                DataViewRowState.ModifiedCurrent | DataViewRowState.Added);

            if (dataView.Count > 0) {
                DialogResult dlgRes = MessageBox.Show(AppPhrases.SaveEventTableConfirm,
                    CommonPhrases.QuestionCaption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (dlgRes == DialogResult.Yes)
                    e.Cancel = !Save();
                else if (dlgRes != DialogResult.No)
                    e.Cancel = true;
            }
        }

        private void FrmEventTableEdit_KeyDown(object sender, KeyEventArgs e) {
            // close the form by escape if the table cell is not editable
            if (e.KeyCode == Keys.Escape && dataGridView.CurrentCell != null &&
                !dataGridView.CurrentCell.IsInEditMode)
                DialogResult = DialogResult.Cancel;
        }

        private void btnRefresh_Click(object sender, EventArgs e) {
            // reload table
            var newDataTable = new DataTable();

            if (LoadDataTable(eventAdapter, errLog, ref newDataTable)) {
                dataTable = newDataTable;
                dataTable.DefaultView.AllowNew = editMode;
                dataTable.DefaultView.AllowEdit = editMode;
                try {
                    dataTable.DefaultView.RowFilter = txtFilter.Text;
                } catch {
                    txtFilter.Text = "";
                }

                bindingSource.DataSource = dataTable;
            }
        }

        private void txtFilter_KeyDown(object sender, KeyEventArgs e) {
            // setting table filter
            if (e.KeyCode == Keys.Enter) {
                try {
                    dataTable.DefaultView.RowFilter = txtFilter.Text;
                } catch {
                    ScadaUiUtils.ShowError(AppPhrases.IncorrectFilter);
                }
            }
        }

        private void dataGridView_CellValidating(object sender, DataGridViewCellValidatingEventArgs e) {
            // validation of data entered in the cell
            e.Cancel = dataGridView.CurrentCell != null && dataGridView.CurrentCell.IsInEditMode &&
                       !ValidateCell(e.ColumnIndex, e.RowIndex, e.FormattedValue);
        }

        private void dataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e) {
            ScadaUiUtils.ShowError(CommonPhrases.GridDataError + ":\r\n" + e.Exception.Message);
            e.ThrowException = false;
        }

        private void btnSave_Click(object sender, EventArgs e) {
            // saving changes to the event table
            Save();
        }
    }
}