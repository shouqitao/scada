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
 * Module   : ModDBExport
 * Summary  : Module configuration form
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2015
 * Modified : 2016
 */

using Scada.Client;
using Scada.UI;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Scada.Server.Modules.DBExport {
    /// <inheritdoc />
    /// <summary>
    /// Module configuration form
    /// <para>KP configuration form</para>
    /// </summary>
    internal partial class FrmDBExportConfig : Form {
        private AppDirs appDirs; // application directories
        private ServerComm serverComm; // object for data exchange with SCADA-Server

        private Config config; // module configuration
        private Config configCopy; // copy of the module configuration to implement the cancellation of changes
        private bool modified; // sign of configuration change
        private bool changing; // control values change
        private Config.ExportDestination selExpDest; // selected export destination
        private TreeNode selExpDestNode; // tree node of the selected export destination


        /// <inheritdoc />
        /// <summary>
        /// Constructor restricting form creation without parameters
        /// </summary>
        private FrmDBExportConfig() {
            InitializeComponent();

            config = null;
            configCopy = null;
            modified = false;
            changing = false;
            selExpDest = null;
            selExpDestNode = null;
        }


        /// <summary>
        /// Get or set configuration change flag
        /// </summary>
        private bool Modified {
            get { return modified; }
            set {
                modified = value;
                btnSave.Enabled = modified;
                btnCancel.Enabled = modified;
            }
        }


        /// <summary>
        /// Display the form modally
        /// </summary>
        public static void ShowDialog(AppDirs appDirs, ServerComm serverComm) {
            var frmDBExportConfig = new FrmDBExportConfig {
                appDirs = appDirs,
                serverComm = serverComm
            };
            frmDBExportConfig.ShowDialog();
        }


        /// <summary>
        /// Create a tree node corresponding to the export destination
        /// </summary>
        private TreeNode NewExpDestNode(Config.ExportDestination expDest) {
            var node = new TreeNode(expDest.DataSource.Name) {
                Tag = expDest
            };

            string imageKey;
            switch (expDest.DataSource.DBType) {
                case DBTypes.MSSQL:
                    imageKey = "mssql.png";
                    break;
                case DBTypes.Oracle:
                    imageKey = "oracle.png";
                    break;
                case DBTypes.PostgreSQL:
                    imageKey = "postgresql.png";
                    break;
                case DBTypes.MySQL:
                    imageKey = "mysql.png";
                    break;
                case DBTypes.OLEDB:
                    imageKey = "oledb.png";
                    break;
                default:
                    imageKey = "";
                    break;
            }

            node.ImageKey = node.SelectedImageKey = imageKey;
            return node;
        }

        /// <summary>
        /// Display configuration
        /// </summary>
        private void ConfigToControls() {
            // resetting the selected object
            selExpDest = null;
            selExpDestNode = null;

            // cleaning and filling wood
            treeView.BeginUpdate();
            treeView.Nodes.Clear();

            foreach (var expDest in config.ExportDestinations)
                treeView.Nodes.Add(NewExpDestNode(expDest));

            treeView.ExpandAll();
            treeView.EndUpdate();

            // selection of the first tree node
            if (treeView.Nodes.Count > 0)
                treeView.SelectedNode = treeView.Nodes[0];

            SetControlsEnabled();
        }

        /// <summary>
        /// Display export options for selected destination
        /// </summary>
        private void ShowSelectedExportParams() {
            if (selExpDest != null) {
                changing = true;

                // output of database connection parameters
                tabControl.SelectedIndex = 0;
                var dataSource = selExpDest.DataSource;
                txtServer.Text = dataSource.Server;
                txtDatabase.Text = dataSource.Database;
                txtUser.Text = dataSource.User;
                txtPassword.Text = dataSource.Password;
                txtConnectionString.Text = dataSource.ConnectionString;

                // setting the background of controls corresponding to the database connection parameters
                string bldConnStr = dataSource.BuildConnectionString();
                KnownColor connParamsColor;
                KnownColor connStrColor;

                if (!string.IsNullOrEmpty(bldConnStr) && bldConnStr == dataSource.ConnectionString) {
                    connParamsColor = KnownColor.Window;
                    connStrColor = KnownColor.Control;
                } else {
                    connParamsColor = KnownColor.Control;
                    connStrColor = KnownColor.Window;
                }

                SetConnControlsBackColor(connParamsColor, connStrColor);

                // output export options
                var expParams = selExpDest.ExportParams;
                ctrlExportCurDataQuery.Export = expParams.ExportCurData;
                ctrlExportCurDataQuery.Query = expParams.ExportCurDataQuery;
                ctrlExportArcDataQuery.Export = expParams.ExportArcData;
                ctrlExportArcDataQuery.Query = expParams.ExportArcDataQuery;
                ctrlExportEventQuery.Export = expParams.ExportEvents;
                ctrlExportEventQuery.Query = expParams.ExportEventQuery;
                changing = false;
            }
        }

        /// <summary>
        /// Set the background color of the control parameters of the database connection
        /// </summary>
        private void SetConnControlsBackColor(KnownColor connParamsColor, KnownColor connStrColor) {
            txtServer.BackColor = txtDatabase.BackColor = txtUser.BackColor = txtPassword.BackColor =
                Color.FromKnownColor(connParamsColor);
            txtConnectionString.BackColor = Color.FromKnownColor(connStrColor);
        }

        /// <summary>
        /// Install and display an automatically constructed connection string.
        /// </summary>
        private void SetConnectionString() {
            if (selExpDest != null) {
                string bldConnStr = selExpDest.DataSource.BuildConnectionString();
                if (!string.IsNullOrEmpty(bldConnStr)) {
                    selExpDest.DataSource.ConnectionString = bldConnStr;
                    changing = true;
                    txtConnectionString.Text = bldConnStr;
                    changing = false;
                    SetConnControlsBackColor(KnownColor.Window, KnownColor.Control);
                }
            }
        }

        /// <summary>
        /// Set accessibility and visibility of buttons and export options
        /// </summary>
        private void SetControlsEnabled() {
            if (selExpDest == null) // configuration is empty
            {
                btnDelDataSource.Enabled = false;
                btnManualExport.Enabled = false;
                lblInstruction.Visible = true;
                tabControl.Visible = false;
            } else {
                btnDelDataSource.Enabled = true;
                btnManualExport.Enabled = true;
                lblInstruction.Visible = false;
                tabControl.Visible = true;
            }
        }

        /// <summary>
        /// Save Module Configuration
        /// </summary>
        private bool SaveConfig() {
            if (Modified) {
                string errMsg;
                if (config.Save(out errMsg)) {
                    Modified = false;
                    return true;
                } else {
                    ScadaUiUtils.ShowError(errMsg);
                    return false;
                }
            } else {
                return true;
            }
        }


        private void FrmDBExportConfig_Load(object sender, EventArgs e) {
            // module localization
            string errMsg;
            if (!Localization.UseRussian) {
                if (Localization.LoadDictionaries(appDirs.LangDir, "ModDBExport", out errMsg))
                    Translator.TranslateForm(this, "Scada.Server.Modules.DBExport.FrmDBExportConfig");
                else
                    ScadaUiUtils.ShowError(errMsg);
            }

            // setting controls
            lblInstruction.Top = treeView.Top;

            // configuration download
            config = new Config(appDirs.ConfigDir);
            if (File.Exists(config.FileName) && !config.Load(out errMsg))
                ScadaUiUtils.ShowError(errMsg);

            // creating a copy of the configuration
            configCopy = config.Clone();

            // configuration display
            ConfigToControls();

            // removing the sign of the configuration change
            Modified = false;
        }

        private void FrmDBExportConfig_FormClosing(object sender, FormClosingEventArgs e) {
            if (Modified) {
                var result = MessageBox.Show(ModPhrases.SaveModSettingsConfirm,
                    CommonPhrases.QuestionCaption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                switch (result) {
                    case DialogResult.Yes:
                        if (!SaveConfig())
                            e.Cancel = true;
                        break;
                    case DialogResult.No:
                        break;
                    default:
                        e.Cancel = true;
                        break;
                }
            }
        }


        private void treeView_AfterSelect(object sender, TreeViewEventArgs e) {
            // definition and display of properties of the selected object
            var selNode = e.Node;
            selExpDest = selNode.Tag as Config.ExportDestination;
            selExpDestNode = selExpDest == null ? null : selNode;
            ShowSelectedExportParams();
        }

        private void miAddDataSource_Click(object sender, EventArgs e) {
            // adding export destination
            DataSource dataSource = null;

            if (sender == miAddSqlDataSource)
                dataSource = new SqlDataSource();
            else if (sender == miAddOraDataSource)
                dataSource = new OraDataSource();
            else if (sender == miAddPgSqlDataSource)
                dataSource = new PgSqlDataSource();
            else if (sender == miAddMySqlDataSource)
                dataSource = new MySqlDataSource();
            else if (sender == miAddOleDbDataSource)
                dataSource = new OleDbDataSource();

            if (dataSource != null) {
                var expDest = new Config.ExportDestination(dataSource, new Config.ExportParams());
                var treeNode = NewExpDestNode(expDest);

                int ind = config.ExportDestinations.BinarySearch(expDest);
                if (ind >= 0)
                    ind++;
                else
                    ind = ~ind;

                config.ExportDestinations.Insert(ind, expDest);
                treeView.Nodes.Insert(ind, treeNode);
                treeView.SelectedNode = treeNode;

                SetConnectionString();
                SetControlsEnabled();
                Modified = true;
            }
        }

        private void btnDelDataSource_Click(object sender, EventArgs e) {
            // delete export destination
            if (selExpDestNode != null) {
                var prevNode = selExpDestNode.PrevNode;
                var nextNode = selExpDestNode.NextNode;

                int ind = selExpDestNode.Index;
                config.ExportDestinations.RemoveAt(ind);
                treeView.Nodes.RemoveAt(ind);

                treeView.SelectedNode = nextNode ?? prevNode;
                if (treeView.SelectedNode == null) {
                    selExpDest = null;
                    selExpDestNode = null;
                }

                SetControlsEnabled();
                Modified = true;
            }
        }

        private void btnManualExport_Click(object sender, EventArgs e) {
            // manual export form display
            int curDataCtrlCnlNum = config.CurDataCtrlCnlNum;
            int arcDataCtrlCnlNum = config.ArcDataCtrlCnlNum;
            int eventsCtrlCnlNum = config.EventsCtrlCnlNum;

            if (FrmManualExport.ShowDialog(serverComm, config.ExportDestinations, selExpDest,
                    ref curDataCtrlCnlNum, ref arcDataCtrlCnlNum, ref eventsCtrlCnlNum) &&
                (config.CurDataCtrlCnlNum != curDataCtrlCnlNum ||
                 config.ArcDataCtrlCnlNum != arcDataCtrlCnlNum ||
                 config.EventsCtrlCnlNum != eventsCtrlCnlNum)) {
                // setting changed control channel numbers
                config.CurDataCtrlCnlNum = curDataCtrlCnlNum;
                config.ArcDataCtrlCnlNum = arcDataCtrlCnlNum;
                config.EventsCtrlCnlNum = eventsCtrlCnlNum;
                Modified = true;
            }
        }


        private void txtServer_TextChanged(object sender, EventArgs e) {
            if (!changing && selExpDest != null && selExpDestNode != null) {
                selExpDest.DataSource.Server = txtServer.Text;
                selExpDestNode.Text = selExpDest.DataSource.Name;
                SetConnectionString();
                Modified = true;
            }
        }

        private void txtDatabase_TextChanged(object sender, EventArgs e) {
            if (!changing && selExpDest != null) {
                selExpDest.DataSource.Database = txtDatabase.Text;
                SetConnectionString();
                Modified = true;
            }
        }

        private void txtUser_TextChanged(object sender, EventArgs e) {
            if (!changing && selExpDest != null) {
                selExpDest.DataSource.User = txtUser.Text;
                SetConnectionString();
                Modified = true;
            }
        }

        private void txtPassword_TextChanged(object sender, EventArgs e) {
            if (!changing && selExpDest != null) {
                selExpDest.DataSource.Password = txtPassword.Text;
                SetConnectionString();
                Modified = true;
            }
        }

        private void txtConnectionString_TextChanged(object sender, EventArgs e) {
            if (!changing && selExpDest != null) {
                selExpDest.DataSource.ConnectionString = txtConnectionString.Text;
                SetConnControlsBackColor(KnownColor.Control, KnownColor.Window);
                Modified = true;
            }
        }

        private void ctrlExportCurDataQuery_PropChanged(object sender, EventArgs e) {
            if (!changing && selExpDest != null) {
                selExpDest.ExportParams.ExportCurData = ctrlExportCurDataQuery.Export;
                selExpDest.ExportParams.ExportCurDataQuery = ctrlExportCurDataQuery.Query;
                Modified = true;
            }
        }

        private void ctrlExportArcDataQuery_PropChanged(object sender, EventArgs e) {
            if (!changing && selExpDest != null) {
                selExpDest.ExportParams.ExportArcData = ctrlExportArcDataQuery.Export;
                selExpDest.ExportParams.ExportArcDataQuery = ctrlExportArcDataQuery.Query;
                Modified = true;
            }
        }

        private void ctrlExportEventQuery_PropChanged(object sender, EventArgs e) {
            if (!changing && selExpDest != null) {
                selExpDest.ExportParams.ExportEvents = ctrlExportEventQuery.Export;
                selExpDest.ExportParams.ExportEventQuery = ctrlExportEventQuery.Query;
                Modified = true;
            }
        }


        private void btnSave_Click(object sender, EventArgs e) {
            // saving module configuration
            SaveConfig();
        }

        private void btnCancel_Click(object sender, EventArgs e) {
            // cancel configuration changes
            config = configCopy;
            configCopy = config.Clone();
            ConfigToControls();
            Modified = false;
        }
    }
}