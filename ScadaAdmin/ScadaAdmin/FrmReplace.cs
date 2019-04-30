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
 * Summary  : Replacing table cell data form
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2010
 * Modified : 2016
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Scada;
using Scada.UI;

namespace ScadaAdmin {
    /// <summary>
    /// Replacing table cell data form
    /// <para>Data replacement form in the table cells</para>
    /// </summary>
    public partial class FrmReplace : Form {
        /// <summary>
        /// Table column information
        /// </summary>
        private class ColumnInfo {
            private string header;
            private DataTable dataSource1;
            private DataTable dataSource2;
            private string displayMember;
            private string valueMember;


            /// <summary>
            /// Constructor
            /// </summary>
            public ColumnInfo(DataGridViewColumn column) {
                header = column == null ? "" : column.HeaderText;
                dataSource1 = null;
                dataSource2 = null;
                var cbColumn = column as DataGridViewComboBoxColumn;
                displayMember = cbColumn == null ? "" : cbColumn.DisplayMember;
                valueMember = cbColumn == null ? "" : cbColumn.ValueMember;

                Column = column;
                IsText = column is DataGridViewTextBoxColumn;
            }

            /// <summary>
            /// Get column
            /// </summary>
            public DataGridViewColumn Column { get; private set; }

            /// <summary>
            /// Get column heading
            /// </summary>
            private string Header {
                get { return header; }
            }

            /// <summary>
            /// Get the sign that the column is text
            /// </summary>
            public bool IsText { get; private set; }

            /// <summary>
            /// Get 1st data source with list of column values
            /// </summary>
            public DataTable DataSource1 {
                get {
                    if (dataSource1 == null) {
                        var column = Column as DataGridViewComboBoxColumn;
                        dataSource1 = column == null ? null : CloneTable(column.DataSource as DataTable);
                        return dataSource1;
                    } else {
                        return dataSource1;
                    }
                }
            }

            /// <summary>
            /// Get 2nd data source with list of column values
            /// </summary>
            public DataTable DataSource2 {
                get {
                    if (dataSource2 == null) {
                        var column = Column as DataGridViewComboBoxColumn;
                        dataSource2 = column == null ? null : CloneTable(column.DataSource as DataTable);
                        return dataSource2;
                    } else {
                        return dataSource2;
                    }
                }
            }

            /// <summary>
            /// Get the name of the displayed field if the column values are selected from the list
            /// </summary>
            public string DisplayMember {
                get { return displayMember; }
            }

            /// <summary>
            /// Get the name of the data field if the column values are selected from the list
            /// </summary>
            public string ValueMember {
                get { return valueMember; }
            }

            /// <summary>
            /// Clone table by disabling constraint checking
            /// </summary>
            private DataTable CloneTable(DataTable dataTable) {
                if (dataTable == null) {
                    return null;
                } else {
                    var clone = new DataTable(dataTable.TableName);
                    clone.BeginLoadData();
                    clone.Merge(dataTable, false, MissingSchemaAction.Add);
                    clone.DefaultView.Sort = dataTable.DefaultView.Sort;
                    return clone;
                }
            }

            /// <summary>
            /// Get a string representation of the object
            /// </summary>
            public override string ToString() {
                return Header;
            }
        }


        private string completeMsg; // search completion message
        private FrmTable frmTable; // form of editing the table in which the replacement is made
        private DataGridView gridView; // control of the table in which the replacement is made


        /// <summary>
        /// Constructor
        /// </summary>
        public FrmReplace() {
            InitializeComponent();

            completeMsg = AppPhrases.ValueNotFound;
            frmTable = null;
            gridView = null;
        }


        /// <summary>
        /// Get or set the form for editing the table in which the replacement is made
        /// </summary>
        public FrmTable FrmTable {
            get { return frmTable; }
            set {
                frmTable = value;
                gridView = frmTable == null ? null : frmTable.GridView;
            }
        }


        /// <summary>
        /// Cell satisfies the search conditions.
        /// </summary>
        private bool IsMatched(DataGridViewCell cell, string value, bool ignoreCase, bool wholeCellOnly) {
            if (cell == null) {
                return false;
            } else {
                var comparison =
                    ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture;
                string cellVal = cell.EditedFormattedValue == null ? "" : cell.EditedFormattedValue.ToString();

                return wholeCellOnly && string.Compare(cellVal, value, ignoreCase) == 0 ||
                       !wholeCellOnly && cellVal.IndexOf(value, comparison) >= 0;
            }
        }

        /// <summary>
        /// Cell satisfies the search conditions.
        /// </summary>
        private bool IsMatched(DataGridViewCell cell, object value) {
            if (cell == null) {
                return false;
            } else {
                object cellVal;
                if (cell.IsInEditMode) {
                    var cb = gridView.EditingControl as ComboBox;
                    cellVal = cb == null ? null : cb.SelectedValue;
                } else {
                    cellVal = cell.Value;
                }

                return cellVal == value || (cellVal is int) && (value is int) && ((int) cellVal == (int) value);
            }
        }

        /// <summary>
        /// Replace part of the string
        /// </summary>
        private string ReplaceStr(string str, string oldStr, string newStr, bool ignoreCase) {
            if (string.IsNullOrEmpty(oldStr))
                return str;

            var comparison =
                ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture;
            var result = new StringBuilder();
            int len = str.Length;
            int oldLen = oldStr.Length;
            var startInd = 0;
            var ind = 0;

            while (ind >= 0 && startInd < len) {
                ind = str.IndexOf(oldStr, startInd, comparison);
                if (ind >= 0) {
                    result.Append(str.Substring(startInd, ind - startInd));
                    result.Append(newStr);
                    startInd = ind + oldLen;
                } else {
                    result.Append(str.Substring(startInd));
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Search next match
        /// </summary>
        private bool FindNext(ColumnInfo columnInfo, bool showMsg) {
            string findStr = columnInfo.IsText ? txtFind.Text : "";
            var findObj = columnInfo.IsText ? null : cbFind.SelectedValue;
            bool ignoreCase = !chkCaseSensitive.Checked;
            bool wholeCellOnly = chkWholeCellOnly.Checked;
            var curCell = gridView.CurrentCell;

            var found = false;
            var cnt = 0;
            int colInd = columnInfo.Column.Index;
            int rowInd = curCell == null ? 0 : curCell.RowIndex;
            if (curCell != null && curCell.ColumnIndex == colInd)
                rowInd++;
            int rowCnt = gridView.RowCount;

            while (cnt < rowCnt && !found) {
                if (rowInd == rowCnt)
                    rowInd = 0;

                var cell = gridView[colInd, rowInd];
                found = columnInfo.IsText
                    ? IsMatched(cell, findStr, ignoreCase, wholeCellOnly)
                    : IsMatched(cell, findObj);

                if (found) {
                    completeMsg = AppPhrases.FindCompleted;
                    try {
                        gridView.CurrentCell = cell;
                    } catch { /* it is impossible to leave the current cell, because its value is incorrect */
                    }
                }

                rowInd++;
                cnt++;
            }

            if (cnt == rowCnt && !found) {
                if (showMsg)
                    ScadaUiUtils.ShowInfo(completeMsg);
                completeMsg = AppPhrases.ValueNotFound;
            }

            return found;
        }

        /// <summary>
        /// Cell Value Replacement
        /// </summary>
        private bool ReplaceCellVal(ColumnInfo columnInfo, bool match, out bool updated) {
            var replaced = false;

            // replacing the value of the current test cell if it satisfies the search conditions
            var curCell = gridView.CurrentCell;
            if (columnInfo.IsText && (!match ||
                                      IsMatched(curCell, txtFind.Text, !chkCaseSensitive.Checked,
                                          chkWholeCellOnly.Checked))) {
                gridView.BeginEdit(false);
                var txt = gridView.EditingControl as TextBox;
                if (txt != null) {
                    replaced = true;
                    txt.Text = chkWholeCellOnly.Checked
                        ? txtReplaceWith.Text
                        : ReplaceStr(txt.Text, txtFind.Text, txtReplaceWith.Text, !chkCaseSensitive.Checked);
                }
            }

            // replacing the value of the current cell with the list if it satisfies the search conditions
            if (!columnInfo.IsText && (!match || IsMatched(curCell, cbFind.SelectedValue))) {
                gridView.BeginEdit(false);
                var cb = gridView.EditingControl as ComboBox;
                if (cb != null) {
                    replaced = true;
                    // if the specified value does not exist, the exception is not raised
                    cb.SelectedValue = cbReplaceWith.SelectedValue;
                }
            }

            if (replaced) {
                completeMsg = AppPhrases.FindCompleted;
                updated = frmTable.UpdateTable();
            } else {
                updated = true;
            }

            return replaced;
        }


        private void FrmReplace_Load(object sender, EventArgs e) {
            // form translation
            Translator.TranslateForm(this, "ScadaAdmin.FrmReplace");

            if (frmTable != null) {
                txtTable.Text = frmTable.Text;

                // filling in the drop-down list of table columns
                var curCell = gridView.CurrentCell;
                int curInd = curCell == null ? -1 : curCell.ColumnIndex;
                var selInd = 0;
                ColumnInfo selColInfo = null;

                for (int i = 0,
                    k = 0;
                    i < gridView.Columns.Count;
                    i++) {
                    var column = gridView.Columns[i];

                    if ((column is DataGridViewTextBoxColumn || column is DataGridViewComboBoxColumn) &&
                        !column.ReadOnly) {
                        var colInfo = new ColumnInfo(column);
                        cbTableColumn.Items.Add(colInfo);

                        if (i == curInd) {
                            selInd = k;
                            selColInfo = colInfo;
                        }

                        k++;
                    }
                }

                if (cbTableColumn.Items.Count > 0)
                    cbTableColumn.SelectedIndex = selInd;

                // setting the default search value
                if (selColInfo != null) // curCell! = null
                {
                    if (selColInfo.IsText) {
                        if (curCell.EditedFormattedValue != null)
                            txtFind.Text = curCell.EditedFormattedValue.ToString();
                    } else {
                        if (curCell.IsInEditMode) {
                            var cb = gridView.EditingControl as ComboBox;
                            if (cb != null)
                                cbFind.SelectedValue = cb.SelectedValue;
                        } else {
                            cbFind.SelectedValue = curCell.Value;
                        }
                    }
                }
            }

            if (cbTableColumn.Items.Count == 0 || txtFind.Visible && txtFind.Text == "")
                btnFindNext.Enabled = btnReplace.Enabled = btnReplaceAll.Enabled = false;
        }

        private void cbColumn_SelectedIndexChanged(object sender, EventArgs e) {
            // customization of form controls depending on the type of column selected
            var columnInfo = cbTableColumn.SelectedItem as ColumnInfo;
            if (columnInfo != null) {
                bool isText = columnInfo.IsText;
                txtFind.Visible = isText;
                txtReplaceWith.Visible = isText;
                cbFind.Visible = !isText;
                cbReplaceWith.Visible = !isText;
                chkCaseSensitive.Enabled = isText;
                chkWholeCellOnly.Enabled = isText;

                if (isText) {
                    btnFindNext.Enabled = btnReplace.Enabled = btnReplaceAll.Enabled = txtFind.Text != "";
                } else {
                    cbFind.DataSource = columnInfo.DataSource1;
                    cbReplaceWith.DataSource = columnInfo.DataSource2;
                    cbFind.DisplayMember = cbReplaceWith.DisplayMember = columnInfo.DisplayMember;
                    cbFind.ValueMember = cbReplaceWith.ValueMember = columnInfo.ValueMember;

                    if (cbFind.Items.Count == 0)
                        btnFindNext.Enabled = btnReplace.Enabled = btnReplaceAll.Enabled = false;
                }
            }
        }

        private void txtFind_TextChanged(object sender, EventArgs e) {
            completeMsg = AppPhrases.ValueNotFound;
            if (txtFind.Visible)
                btnFindNext.Enabled = btnReplace.Enabled = btnReplaceAll.Enabled = txtFind.Text != "";
        }

        private void cbFind_SelectedIndexChanged(object sender, EventArgs e) {
            completeMsg = AppPhrases.ValueNotFound;
        }

        private void btnFindNext_Click(object sender, EventArgs e) {
            var columnInfo = cbTableColumn.SelectedItem as ColumnInfo;
            if (frmTable != null && columnInfo != null)
                FindNext(columnInfo, true);
        }

        private void btnReplace_Click(object sender, EventArgs e) {
            var columnInfo = cbTableColumn.SelectedItem as ColumnInfo;
            if (frmTable != null && columnInfo != null) {
                bool updated;
                ReplaceCellVal(columnInfo, true, out updated);
                if (updated)
                    FindNext(columnInfo, true);
            }
        }

        private void btnReplaceAll_Click(object sender, EventArgs e) {
            // replace all values
            var columnInfo = cbTableColumn.SelectedItem as ColumnInfo;
            if (frmTable != null && columnInfo != null) {
                // list of lines that are replaced
                var replacedRows = new List<DataRowView>();

                var cnt = 0;
                var matchCell = true;
                bool updated;
                bool found;

                do {
                    // replacement
                    bool replaced = ReplaceCellVal(columnInfo, matchCell, out updated);
                    matchCell = false;
                    if (replaced && updated)
                        cnt++;

                    // save string with replaced value
                    var curCell = gridView.CurrentCell;
                    if (replaced) {
                        if (curCell != null) {
                            int rowInd = curCell.RowIndex;
                            if (0 <= rowInd && rowInd < frmTable.Table.DefaultView.Count)
                                replacedRows.Add(frmTable.Table.DefaultView[rowInd]);
                        }
                    }

                    // search for the next replacement value
                    if (updated) {
                        do {
                            found = FindNext(columnInfo, false);
                            if (found) {
                                // check that the value in the found string has not yet been replaced
                                DataRowView rowView = null;
                                if (gridView.CurrentCell != null) {
                                    int rowInd = gridView.CurrentCell.RowIndex;
                                    if (0 <= rowInd && rowInd < frmTable.Table.DefaultView.Count)
                                        rowView = frmTable.Table.DefaultView[rowInd];
                                }

                                if (replacedRows.Contains(rowView))
                                    found = false;
                            }
                        } while (curCell != gridView.CurrentCell && !found);
                    } else {
                        found = false;
                    }
                } while (found);

                if (cnt > 0)
                    ScadaUiUtils.ShowInfo(string.Format(AppPhrases.ReplaceCount, cnt));
                else if (updated)
                    ScadaUiUtils.ShowInfo(completeMsg);
            }
        }

        private void btnClose_Click(object sender, EventArgs e) {
            Close();
        }
    }
}