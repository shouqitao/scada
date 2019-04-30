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
 * Summary  : Import configuration table form
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2010
 * Modified : 2018
 */

using Scada;
using Scada.UI;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace ScadaAdmin {
    /// <summary>
    /// Import configuration table form
    /// <para>Import form of the configuration database table</para>
    /// </summary>
    public partial class FrmImport : Form {
        /// <summary>
        /// Drop-down list item for importing all tables
        /// </summary>
        private class ImportAllTablesItem {
            public override string ToString() {
                return AppPhrases.AllTablesItem;
            }
        }

        /// <summary>
        /// Element of the drop-down list for importing from the archive
        /// </summary>
        private class ImportArchiveItem {
            public override string ToString() {
                return AppPhrases.ArchiveItem;
            }
        }

        /// <summary>
        /// Selected item when opening a form
        /// </summary>
        public enum SelectedItem {
            Table,
            AllTables,
            Archive
        };


        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public FrmImport() {
            InitializeComponent();

            DefaultSelection = SelectedItem.Table;
            DefaultTableName = "";
            DefaultArcFileName = "";
            DefaultBaseDATDir = "";
        }


        /// <summary>
        /// Selected default item
        /// </summary>
        public SelectedItem DefaultSelection { get; set; }

        /// <summary>
        /// Get or set the default table name
        /// </summary>
        public string DefaultTableName { get; set; }

        /// <summary>
        /// Get or set the name of the default configuration archive file
        /// </summary>
        public string DefaultArcFileName { get; set; }

        /// <summary>
        /// Get or set the default directory
        /// </summary>
        public string DefaultBaseDATDir { get; set; }


        private void FrmImport_Load(object sender, EventArgs e) {
            // form translation
            Translator.TranslateForm(this, "ScadaAdmin.FrmImport");

            // setting controls
            lblDirectory.Left = lblFileName.Left;

            // defining the default archive file name
            if (string.IsNullOrEmpty(DefaultArcFileName) && !string.IsNullOrEmpty(DefaultBaseDATDir))
                DefaultArcFileName = Path.GetFullPath(DefaultBaseDATDir + @"..\config.zip");

            // filling in the drop-down list of tables
            var tableInd = 0;

            foreach (var tableInfo in Tables.TableInfoList) {
                int ind = cbTable.Items.Add(tableInfo);
                if (tableInfo.Name == DefaultTableName)
                    tableInd = ind;
            }

            int allTablesInd = cbTable.Items.Add(new ImportAllTablesItem());
            int archiveInd = cbTable.Items.Add(new ImportArchiveItem());

            // select list item
            switch (DefaultSelection) {
                case SelectedItem.AllTables:
                    cbTable.SelectedIndex = allTablesInd;
                    break;
                case SelectedItem.Archive:
                    cbTable.SelectedIndex = archiveInd;
                    break;
                default: // SelectedItem.Table
                    cbTable.SelectedIndex = tableInd;
                    break;
            }
        }

        private void cbTable_SelectedIndexChanged(object sender, EventArgs e) {
            // setting table file name
            var selItem = cbTable.SelectedItem;

            if (selItem is ImportAllTablesItem) {
                lblFileName.Visible = false;
                lblDirectory.Visible = true;
                txtFileName.Text = DefaultBaseDATDir;
                gbIDs.Enabled = false;
            } else if (selItem is ImportArchiveItem) {
                lblFileName.Visible = true;
                lblDirectory.Visible = false;
                txtFileName.Text = DefaultArcFileName;
                gbIDs.Enabled = false;
            } else if (selItem is Tables.TableInfo tableInfo) {
                lblFileName.Visible = true;
                lblDirectory.Visible = false;
                txtFileName.Text = DefaultBaseDATDir + tableInfo.FileName;
                gbIDs.Enabled = tableInfo.HasIntID;
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e) {
            if (lblFileName.Visible) {
                // select file to import
                if (cbTable.SelectedItem is ImportArchiveItem) {
                    openFileDialog.Title = AppPhrases.ChooseArchiveFile;
                    openFileDialog.Filter = AppPhrases.ArchiveFileFilter;
                } else {
                    openFileDialog.Title = AppPhrases.ChooseBaseTableFile;
                    openFileDialog.Filter = AppPhrases.BaseTableFileFilter;
                }

                string fileName = txtFileName.Text.Trim();
                openFileDialog.FileName = fileName;

                if (fileName != "")
                    openFileDialog.InitialDirectory = Path.GetDirectoryName(fileName);

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                    txtFileName.Text = openFileDialog.FileName;

                txtFileName.Focus();
                txtFileName.DeselectAll();
            } else {
                // directory selection
                folderBrowserDialog.SelectedPath = txtFileName.Text.Trim();
                folderBrowserDialog.Description = CommonPhrases.ChooseBaseDATDir;

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                    txtFileName.Text = ScadaUtils.NormalDir(folderBrowserDialog.SelectedPath);

                txtFileName.Focus();
                txtFileName.DeselectAll();
            }
        }

        private void chkStartID_CheckedChanged(object sender, EventArgs e) {
            numStartID.Enabled = chkStartID.Checked;
        }

        private void chkFinalID_CheckedChanged(object sender, EventArgs e) {
            numFinalID.Enabled = chkFinalID.Checked;
        }

        private void chkNewStartID_CheckedChanged(object sender, EventArgs e) {
            numNewStartID.Enabled = chkNewStartID.Checked;
        }

        private void btnImport_Click(object sender, EventArgs e) {
            // import selected table from dat format
            if (AppData.Connected) {
                var selItem = cbTable.SelectedItem;
                string logFileName = AppData.AppDirs.LogDir + "ScadaAdminImport.txt";
                bool importOK;
                bool logCreated;
                string msg;

                if (selItem is ImportAllTablesItem) {
                    // import all tables from a directory
                    importOK = ImportExport.ImportAllTables(txtFileName.Text, Tables.TableInfoList,
                        logFileName, out logCreated, out msg);
                } else if (selItem is ImportArchiveItem) {
                    // import archive
                    importOK = ImportExport.ImportArchive(txtFileName.Text, Tables.TableInfoList,
                        logFileName, out logCreated, out msg);
                } else {
                    // import table
                    var tableInfo = (Tables.TableInfo) selItem;
                    int minID = gbIDs.Enabled && chkStartID.Checked ? Convert.ToInt32(numStartID.Value) : 0;
                    int maxID = gbIDs.Enabled && chkFinalID.Checked ? Convert.ToInt32(numFinalID.Value) : int.MaxValue;
                    int newMinID = gbIDs.Enabled && chkNewStartID.Checked ? Convert.ToInt32(numNewStartID.Value) : 0;
                    importOK = ImportExport.ImportTable(txtFileName.Text, tableInfo, minID, maxID, newMinID,
                        logFileName, out logCreated, out msg);
                }

                // display of the result message
                if (importOK) {
                    ScadaUiUtils.ShowInfo(msg);
                } else {
                    AppUtils.ProcError(msg);

                    // log display in notebook
                    if (logCreated)
                        Process.Start(logFileName);
                }
            }
        }
    }
}