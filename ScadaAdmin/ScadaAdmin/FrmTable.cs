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
 * Summary  : Editing configuration table form
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2010
 * Modified : 2018
 */

using Scada;
using Scada.UI;
using System;
using System.Data;
using System.Data.SqlServerCe;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using WinControl;

namespace ScadaAdmin {
    /// <inheritdoc />
    /// <summary>
    /// Editing configuration table form
    /// <para>Configuration table edit form</para>
    /// </summary>
    public partial class FrmTable : Form, IChildForm {
        /// <summary>
        /// Max. text length displayed in the source cell
        /// </summary>
        private const int MaxSourceLength = 50;

        private bool saveOccured; // saving changes happened
        private bool saveOk; // saving changes successful
        private bool pwdColExists; // password column exists
        private bool clrColExists; // there is a color column
        private bool srcColExists; // there is a source code column
        private bool modDTColExists; // there is a time column change


        /// <summary>
        /// Constructor
        /// </summary>
        public FrmTable() {
            InitializeComponent();

            saveOccured = false;
            saveOk = false;
            pwdColExists = false;
            clrColExists = false;
            srcColExists = false;
            modDTColExists = false;

            ChildFormTag = null;
            Table = null;
            GridContextMenu = null;
        }


        #region IChildForm Members

        /// <summary>
        /// Get or set child form information
        /// </summary>
        public ChildFormTag ChildFormTag { get; set; }

        /// <summary>
        /// Save changes to the database
        /// </summary>
        public void Save() {
            string errMsg;
            saveOk = Tables.UpdateData(Table, out errMsg);
            if (saveOk) {
                SetModified(false);
            } else {
                SetModified(true);
                AppUtils.ProcError(errMsg);
            }

            saveOccured = true;
        }

        #endregion


        /// <summary>
        /// Set the sign of data change and the corresponding availability of buttons
        /// </summary>
        private void SetModified(bool value) {
            if (ChildFormTag != null)
                ChildFormTag.Modified = value;

            bindingNavigatorUpdateItem.Enabled = bindingNavigatorCancelItem.Enabled = value;
            bindingNavigatorDeleteItem.Enabled = bindingNavigatorClearItem.Enabled =
                bindingNavigatorAutoResizeItem.Enabled =
                    Table == null ? false : Table.DefaultView.Count > 0;
        }

        /// <summary>
        /// Check the correctness of the cell value
        /// </summary>
        private bool ValidateCell(DataGridViewCell cell, bool showError) {
            return cell == null || !cell.IsInEditMode
                ? true
                : ValidateCell(cell.ColumnIndex, cell.RowIndex, cell.EditedFormattedValue, showError);
        }

        /// <summary>
        /// Check the correctness of the cell value
        /// </summary>
        private bool ValidateCell(int colInd, int rowInd, object cellVal, bool showError) {
            var result = true;

            if (0 <= colInd && colInd < dataGridView.ColumnCount && 0 <= rowInd && rowInd < dataGridView.RowCount &&
                cellVal != null) {
                string errMsg;
                result = Tables.ValidateCell(Table, Table.Columns[dataGridView.Columns[colInd].DataPropertyName],
                    cellVal.ToString(), out errMsg);

                if (result) {
                    dataGridView.Rows[rowInd].ErrorText = "";
                } else {
                    dataGridView.Rows[rowInd].ErrorText = errMsg;
                    if (errMsg != "" && showError)
                        ScadaUiUtils.ShowError(errMsg);
                }
            } else {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Convert string to color
        /// </summary>
        private Color StrToColor(string s) {
            try {
                if (s.Length == 7 && s[0] == '#') {
                    int r = int.Parse(s.Substring(1, 2), NumberStyles.HexNumber);
                    int g = int.Parse(s.Substring(3, 2), NumberStyles.HexNumber);
                    int b = int.Parse(s.Substring(5, 2), NumberStyles.HexNumber);
                    return Color.FromArgb(r, g, b);
                } else {
                    return Color.FromName(s);
                }
            } catch {
                return Color.Black;
            }
        }


        /// <summary>
        /// Get or set the table being edited
        /// </summary>
        public DataTable Table { get; set; }

        /// <summary>
        /// Get editable table control
        /// </summary>
        public DataGridView GridView {
            get { return dataGridView; }
        }

        /// <summary>
        /// Get or set the context menu of the table being edited
        /// </summary>
        public ContextMenuStrip GridContextMenu { get; set; }


        /// <summary>
        /// Prepare the form for closure by canceling cell editing if its value is incorrect
        /// </summary>
        public void PrepareClose(bool showError) {
            if (!ValidateCell(dataGridView.CurrentCell, showError))
                dataGridView.CancelEdit();
        }

        /// <summary>
        /// Cut the buffer value of the current cell
        /// </summary>
        public void CellCut() {
            var cell = dataGridView.CurrentCell;
            if (cell != null) {
                var col = dataGridView.Columns[cell.ColumnIndex];
                if (col is DataGridViewTextBoxColumn) {
                    if (cell.IsInEditMode) {
                        var txt = dataGridView.EditingControl as TextBox;
                        if (txt != null)
                            txt.Cut();
                    } else {
                        string val = cell.FormattedValue == null ? "" : cell.FormattedValue.ToString();
                        if (val == "")
                            Clipboard.Clear();
                        else
                            Clipboard.SetText(val);
                        cell.Value = cell.ValueType == typeof(string) ? "" : (object) DBNull.Value;
                    }
                } else if (col is DataGridViewComboBoxColumn) {
                    Clipboard.SetData("ScadaAdminCell", col.Name + ":" + cell.Value);
                    cell.Value = DBNull.Value;
                }
            }
        }

        /// <summary>
        /// Copy to clipboard the current cell value
        /// </summary>
        public void CellCopy() {
            var cell = dataGridView.CurrentCell;
            if (cell != null) {
                var col = dataGridView.Columns[cell.ColumnIndex];
                if (col is DataGridViewTextBoxColumn) {
                    if (cell.IsInEditMode) {
                        var txt = dataGridView.EditingControl as TextBox;
                        if (txt != null)
                            txt.Copy();
                    } else {
                        string val = cell.FormattedValue == null ? "" : cell.FormattedValue.ToString();
                        if (val == "")
                            Clipboard.Clear();
                        else
                            Clipboard.SetText(val);
                    }
                } else if (col is DataGridViewComboBoxColumn) {
                    Clipboard.SetData("ScadaAdminCell", col.Name + ":" + cell.Value);
                }
            }
        }

        /// <summary>
        /// Copy value to buffer to current cell
        /// </summary>
        public void CellPaste() {
            var cell = dataGridView.CurrentCell;
            if (cell != null) {
                var col = dataGridView.Columns[cell.ColumnIndex];
                if (col is DataGridViewTextBoxColumn) {
                    if (dataGridView.BeginEdit(true)) {
                        var txt = dataGridView.EditingControl as TextBox;
                        if (txt != null)
                            txt.Paste();
                    }
                } else if (col is DataGridViewComboBoxColumn) {
                    if (Clipboard.ContainsData("ScadaAdminCell")) {
                        var obj = Clipboard.GetData("ScadaAdminCell");
                        if (obj != null) {
                            string str = obj.ToString();
                            int pos = str.IndexOf(":");
                            if (pos > 0) {
                                string colName = str.Substring(0, pos);
                                if (col.Name == colName) {
                                    string strVal = pos < str.Length - 1
                                        ? str.Substring(pos + 1, str.Length - pos - 1)
                                        : "";
                                    int intVal;
                                    if (int.TryParse(strVal, out intVal)) {
                                        var tbl = (col as DataGridViewComboBoxColumn).DataSource as DataTable;
                                        if (tbl != null && tbl.DefaultView.Find(intVal) >= 0)
                                            cell.Value = intVal;
                                    } else
                                        cell.Value = DBNull.Value;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Save changes to the table in the database
        /// </summary>
        public bool UpdateTable() {
            if (ValidateCell(dataGridView.CurrentCell, true)) {
                // if the cell is in edit mode and its value is changed,
                // then dataTable_RowChanged is called and data is saved
                saveOccured = false;
                dataGridView.EndEdit();
                bindingSource.EndEdit();

                if (!saveOccured)
                    Save();

                dataGridView.Invalidate();
                return saveOk;
            } else
                return false;
        }


        private void FrmTable_Load(object sender, EventArgs e) {
            // form translation
            Translator.TranslateForm(this, "ScadaAdmin.FrmTable");
            if (bindingNavigatorCountItem.Text.Contains("{0}"))
                bindingNavigator.CountItemFormat = bindingNavigatorCountItem.Text;
        }

        private void FrmObj_Shown(object sender, EventArgs e) {
            if (Table == null) {
                bindingNavigatorUpdateItem.Enabled = false;
                bindingNavigatorCancelItem.Enabled = false;
                bindingNavigatorRefreshItem.Enabled = false;
                bindingNavigatorDeleteItem.Enabled = false;
                bindingNavigatorClearItem.Enabled = false;
                bindingNavigatorAutoResizeItem.Enabled = false;
            } else {
                Table.RowChanged += dataTable_RowChanged;
                Table.RowDeleted += dataTable_RowDeleted;

                string tableName = Table.TableName;
                if (tableName == "User")
                    pwdColExists = true;
                else if (tableName == "EvType")
                    clrColExists = true;
                else if (tableName == "Formula")
                    srcColExists = true;
                else if (tableName == "InCnl" || tableName == "CtrlCnl")
                    modDTColExists = true;

                bindingSource.DataSource = Table;
                dataGridView.Columns.AddRange(Table.ExtendedProperties["Columns"] as DataGridViewColumn[]);
                ScadaUiUtils.AutoResizeColumns(dataGridView);

                SetModified(false);
            }
        }


        private void dataTable_RowChanged(object sender, DataRowChangeEventArgs e) {
            if (e.Action == DataRowAction.Add || e.Action == DataRowAction.Change)
                Save();
        }

        private void dataTable_RowDeleted(object sender, DataRowChangeEventArgs e) {
            if (e.Action == DataRowAction.Delete)
                Save();
        }


        private void dataGridView_CellValidating(object sender, DataGridViewCellValidatingEventArgs e) {
            e.Cancel = !ValidateCell(e.ColumnIndex, e.RowIndex, e.FormattedValue, true);
        }

        private void dataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e) {
            AppUtils.ProcError(CommonPhrases.GridDataError + ":\r\n" + e.Exception.Message);
            e.ThrowException = false;
        }

        private void dataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e) {
            if (e.RowIndex >= 0) {
                SetModified(true);

                if (modDTColExists) {
                    var modDTCol = dataGridView.Columns["ModifiedDT"];
                    if (modDTCol != null && modDTCol != dataGridView.Columns[e.ColumnIndex])
                        dataGridView.Rows[e.RowIndex].Cells[modDTCol.Index].Value = DateTime.Now;
                }
            }
        }

        private void dataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e) {
            int colInd = e.ColumnIndex;

            if (0 <= colInd && colInd < dataGridView.Columns.Count && e.Value != null) {
                string colName = dataGridView.Columns[colInd].DataPropertyName;

                // password hiding
                if (pwdColExists && colName == "Password") {
                    string passowrd = e.Value.ToString();
                    if (passowrd.Length > 0)
                        e.Value = new string('●', passowrd.Length);
                }

                // displaying cell text in the appropriate color
                if (clrColExists && colName == "Color")
                    e.CellStyle.ForeColor = StrToColor(e.Value.ToString());

                // limiting the displayed length of the source code
                if (srcColExists && colName == "Source") {
                    string source = e.Value.ToString();
                    if (source.Length > MaxSourceLength)
                        e.Value = source.Substring(0, MaxSourceLength) + "...";
                }
            }
        }

        private void dataGridView_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e) {
            if (dataGridView.CurrentCell != null) {
                int colInd = dataGridView.CurrentCell.ColumnIndex;
                int rowInd = dataGridView.CurrentCell.RowIndex;

                if (0 <= colInd && colInd < dataGridView.Columns.Count) {
                    string colName = dataGridView.Columns[colInd].DataPropertyName;

                    // display password in edit mode
                    if (pwdColExists && colName == "Password") {
                        var txt = e.Control as TextBox;
                        var val = Table.DefaultView[rowInd]["Password"];
                        if (txt != null && val != null && val != DBNull.Value)
                            txt.Text = val.ToString();
                    }

                    // displaying the source code completely in edit mode
                    if (srcColExists && colName == "Source") {
                        var txt = e.Control as TextBox;
                        var val = Table.DefaultView[rowInd]["Source"];
                        if (txt != null && val != null && val != DBNull.Value)
                            txt.Text = val.ToString();
                    }
                }
            }
        }

        private void dataGridView_Leave(object sender, EventArgs e) {
            PrepareClose(true);
        }

        private void dataGridView_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e) {
            // call the context menu of the table being edited
            if (GridContextMenu != null && e.Button == MouseButtons.Right && e.Clicks == 1) {
                try {
                    var cell = dataGridView[e.ColumnIndex, e.RowIndex];
                    if (!cell.IsInEditMode) {
                        dataGridView.CurrentCell = cell;
                        GridContextMenu.Show(MousePosition);
                    }
                } catch {
                    // click on the header or the left margin of the table or 
                    // unable to change current cell
                }
            }
        }

        private void dataGridView_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e) {
            // form call to edit cell value
            if ((clrColExists || srcColExists) && e.Button == MouseButtons.Left) {
                int colInd = e.ColumnIndex;
                int rowInd = e.RowIndex;

                if (0 <= colInd && colInd < dataGridView.Columns.Count && rowInd >= 0) {
                    var cell = dataGridView[colInd, rowInd];
                    string colName = dataGridView.Columns[colInd].DataPropertyName;

                    if (colName == "Color") {
                        // color selection form call
                        dataGridView.EndEdit();
                        var frmSelectColor = new FrmSelectColor {
                            SelectedColor =
                                cell.Value == null ? Color.Empty : StrToColor(cell.Value.ToString())
                        };

                        if (frmSelectColor.ShowDialog() == DialogResult.OK) {
                            cell.Value = frmSelectColor.SelectedColorName;
                            dataGridView.EndEdit();
                        }
                    } else if (colName == "Source") {
                        // call the source code edit form
                        dataGridView.EndEdit();
                        var frmEditSource = new FrmEditSource() {
                            MaxLength = Table.Columns[colName].MaxLength,
                            Source = cell.Value.ToString()
                        };

                        if (frmEditSource.ShowDialog() == DialogResult.OK) {
                            cell.Value = frmEditSource.Source;
                            dataGridView.EndEdit();
                        }
                    }
                }
            }
        }


        private void bindingNavigatorUpdateItem_Click(object sender, EventArgs e) {
            UpdateTable();
        }

        private void bindingNavigatorCancelItem_Click(object sender, EventArgs e) {
            // undo table changes  
            dataGridView.CancelEdit();
            bindingSource.CancelEdit();
            Table.RejectChanges();

            // error cleaning
            if (Table.HasErrors) {
                DataRow[] rowsInError = Table.GetErrors();
                foreach (var row in rowsInError)
                    row.ClearErrors();
            }

            dataGridView.Invalidate();
            bindingSource.ResetBindings(false);
            SetModified(false);
        }

        private void bindingNavigatorRefreshItem_Click(object sender, EventArgs e) {
            if (UpdateTable()) {
                Table.RowChanged -= dataTable_RowChanged;
                Table.RowDeleted -= dataTable_RowDeleted;
                bindingSource.DataSource = null; // to speed up data changes in the table


                try {
                    // update editable table
                    Table.Clear();
                    var adapter = Table.ExtendedProperties["DataAdapter"] as SqlCeDataAdapter;
                    adapter.Fill(Table);
                    Table.BeginLoadData();

                    // updating tables that are data sources for cell values
                    foreach (DataGridViewColumn col in dataGridView.Columns) {
                        var cbCol = col as DataGridViewComboBoxColumn;
                        if (cbCol != null) {
                            var tbl = cbCol.DataSource as DataTable;
                            if (tbl != null) {
                                bool emtyRowExists = tbl.DefaultView.Find(DBNull.Value) >= 0;
                                tbl.Clear();
                                adapter = tbl.ExtendedProperties["DataAdapter"] as SqlCeDataAdapter;
                                adapter.Fill(tbl);
                                if (emtyRowExists) {
                                    tbl.BeginLoadData();
                                    tbl.Rows.Add(DBNull.Value, " ");
                                }
                            }
                        }
                    }
                } catch (Exception ex) {
                    AppUtils.ProcError(AppPhrases.RefreshDataError + ":\r\n" + ex.Message);
                }

                Table.RowChanged += dataTable_RowChanged;
                Table.RowDeleted += dataTable_RowDeleted;
                bindingSource.DataSource = Table;
                bindingSource.ResetBindings(false);

                SetModified(ChildFormTag.Modified); // setting the availability of buttons
            }
        }

        private void bindingNavigatorDeleteItem_Click(object sender, EventArgs e) {
            var selectedRows = dataGridView.SelectedRows;

            if (MessageBox.Show(selectedRows.Count > 1 ? AppPhrases.DeleteRowsConfirm : AppPhrases.DeleteRowConfirm,
                    CommonPhrases.QuestionCaption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) ==
                DialogResult.Yes) {
                Table.RowDeleted -= dataTable_RowDeleted;

                if (selectedRows.Count > 0) {
                    for (int i = selectedRows.Count - 1; i >= 0; i--) {
                        int ind = selectedRows[i].Index;
                        if (0 <= ind && ind < Table.DefaultView.Count)
                            Table.DefaultView.Delete(ind);
                    }
                } else if (dataGridView.CurrentRow != null) {
                    Table.DefaultView.Delete(dataGridView.CurrentRow.Index);
                }

                Save();
                Table.RowDeleted += dataTable_RowDeleted;
                bindingSource.ResetBindings(false);
            }
        }

        private void bindingNavigatorClearItem_Click(object sender, EventArgs e) {
            if (MessageBox.Show(AppPhrases.ClearTableConfirm, CommonPhrases.QuestionCaption,
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == DialogResult.Yes) {
                Table.RowDeleted -= dataTable_RowDeleted;
                bindingSource.DataSource = null; // to speed up data changes in the table

                for (int i = Table.DefaultView.Count - 1; i >= 0; i--)
                    Table.DefaultView.Delete(i);
                Save();

                Table.RowDeleted += dataTable_RowDeleted;
                bindingSource.DataSource = Table;
                bindingSource.ResetBindings(false);
            }
        }

        private void bindingNavigatorAutoResizeItem_Click(object sender, EventArgs e) {
            ScadaUiUtils.AutoResizeColumns(dataGridView);
        }
    }
}