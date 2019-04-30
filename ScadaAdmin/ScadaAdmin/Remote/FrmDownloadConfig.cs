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
 * Summary  : Download configuration form
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using Scada;
using Scada.UI;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace ScadaAdmin.Remote {
    /// <summary>
    /// Download configuration form
    /// <para>Configuration download form</para>
    /// </summary>
    public partial class FrmDownloadConfig : Form {
        private ServersSettings serversSettings; // settings of interaction with remote servers
        private bool downloadSettingsModified; // The last selected download settings have been changed.


        /// <summary>
        /// Constructor
        /// </summary>
        public FrmDownloadConfig() {
            InitializeComponent();
            serversSettings = new ServersSettings();
            downloadSettingsModified = false;
        }


        /// <summary>
        /// Display configuration download settings
        /// </summary>
        private void ShowDownloadSettings(ServersSettings.DownloadSettings downloadSettings) {
            if (downloadSettings == null) {
                gbOptions.Enabled = false;
                rbSaveToDir.Checked = true;
                txtDestDir.Text = txtDestFile.Text = "";
                chkIncludeSpecificFiles.Checked = false;
                chkImportBase.Checked = false;
                btnDownload.Enabled = false;
            } else {
                gbOptions.Enabled = true;
                txtDestDir.Text = downloadSettings.DestDir;
                txtDestFile.Text = downloadSettings.DestFile;
                chkIncludeSpecificFiles.Checked = downloadSettings.IncludeSpecificFiles;
                chkImportBase.Checked = downloadSettings.ImportBase;
                btnDownload.Enabled = true;

                if (downloadSettings.SaveToDir)
                    rbSaveToDir.Checked = true;
                else
                    rbSaveToArc.Checked = true;
            }
        }

        /// <summary>
        /// Check configuration download settings
        /// </summary>
        private bool ValidateDownloadSettings() {
            if (rbSaveToDir.Checked) {
                if (string.IsNullOrWhiteSpace(txtDestDir.Text)) {
                    ScadaUiUtils.ShowError(AppPhrases.ConfigDirRequired);
                    return false;
                }
            } else if (string.IsNullOrWhiteSpace(txtDestFile.Text)) {
                ScadaUiUtils.ShowError(AppPhrases.ConfigArcRequired);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Apply configuration download settings
        /// </summary>
        private void ApplyDownloadSettings(ServersSettings.DownloadSettings downloadSettings) {
            downloadSettings.SaveToDir = rbSaveToDir.Checked;
            downloadSettings.DestDir = txtDestDir.Text;
            downloadSettings.DestFile = txtDestFile.Text;
            downloadSettings.IncludeSpecificFiles = chkIncludeSpecificFiles.Checked;
            downloadSettings.ImportBase = chkImportBase.Checked;
        }

        /// <summary>
        /// Save settings for interaction with remote servers
        /// </summary>
        private void SaveServersSettings() {
            if (!serversSettings.Save(AppData.AppDirs.ConfigDir + ServersSettings.DefFileName,
                out string errMsg))
                AppUtils.ProcError(errMsg);
        }

        /// <summary>
        /// Download configuration
        /// </summary>
        private void DownloadConfig(ServersSettings.ServerSettings serverSettings) {
            // download
            Cursor = Cursors.WaitCursor;
            string logFileName = AppData.AppDirs.LogDir + "ScadaAdminDownload.txt";
            bool downloadOK = DownloadUpload.DownloadConfig(serverSettings,
                logFileName, out bool logCreated, out string msg);
            Cursor = Cursors.Default;

            // display of the result message
            if (downloadOK) {
                ScadaUiUtils.ShowInfo(msg);

                // launch import
                ServersSettings.DownloadSettings downloadSettings = serverSettings.Download;
                if (downloadSettings.ImportBase) {
                    var frmImport = new FrmImport();
                    if (downloadSettings.SaveToDir) {
                        frmImport.DefaultSelection = FrmImport.SelectedItem.AllTables;
                        frmImport.DefaultBaseDATDir = Path.Combine(downloadSettings.DestDir, "BaseDAT");
                    } else {
                        frmImport.DefaultSelection = FrmImport.SelectedItem.Archive;
                        frmImport.DefaultArcFileName = downloadSettings.DestFile;
                        frmImport.DefaultBaseDATDir = AppData.Settings.AppSett.BaseDATDir;
                    }

                    frmImport.ShowDialog();
                }
            } else {
                AppUtils.ProcError(msg);

                // log display in notebook
                if (logCreated)
                    Process.Start(logFileName);
            }
        }


        private void FrmDownloadConfig_Load(object sender, EventArgs e) {
            // form translation
            Translator.TranslateForm(this, "ScadaAdmin.Remote.CtrlServerConn");
            Translator.TranslateForm(this, "ScadaAdmin.Remote.FrmDownloadConfig");
            openFileDialog.Title = AppPhrases.ChooseArchiveFile;
            openFileDialog.Filter = AppPhrases.ArchiveFileFilter;
            folderBrowserDialog.Description = AppPhrases.ChooseConfigDir;

            // loading settings
            if (!serversSettings.Load(AppData.AppDirs.ConfigDir + ServersSettings.DefFileName,
                out string errMsg))
                AppUtils.ProcError(errMsg);

            // display settings
            ctrlServerConn.ServersSettings = serversSettings;
        }

        private void ctrlServerConn_SelectedSettingsChanged(object sender, EventArgs e) {
            ShowDownloadSettings(ctrlServerConn.SelectedSettings?.Download);
            downloadSettingsModified = false;
        }

        private void rbSave_CheckedChanged(object sender, EventArgs e) {
            if (((RadioButton) sender).Checked) // to avoid double triggering
            {
                bool saveToDir = rbSaveToDir.Checked;
                txtDestDir.Enabled = saveToDir;
                btnBrowseDestDir.Enabled = saveToDir;
                txtDestFile.Enabled = !saveToDir;
                btnSelectDestFile.Enabled = !saveToDir;
                downloadSettingsModified = true;
            }
        }

        private void downloadControl_Changed(object sender, EventArgs e) {
            downloadSettingsModified = true;
        }

        private void btnBrowseDestDir_Click(object sender, EventArgs e) {
            // select directory to save the configuration
            folderBrowserDialog.SelectedPath = txtDestDir.Text.Trim();

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                txtDestDir.Text = ScadaUtils.NormalDir(folderBrowserDialog.SelectedPath);

            txtDestDir.Focus();
            txtDestDir.DeselectAll();
        }

        private void btnSelectDestFile_Click(object sender, EventArgs e) {
            // select archive file to save configuration
            string fileName = txtDestFile.Text.Trim();
            openFileDialog.FileName = fileName;

            if (fileName != "")
                openFileDialog.InitialDirectory = Path.GetDirectoryName(fileName);

            if (openFileDialog.ShowDialog() == DialogResult.OK)
                txtDestFile.Text = openFileDialog.FileName;

            txtDestFile.Focus();
            txtDestFile.DeselectAll();
        }

        private void btnDownload_Click(object sender, EventArgs e) {
            // checking settings and downloading configuration
            ServersSettings.ServerSettings serverSettings = ctrlServerConn.SelectedSettings;

            if (serverSettings != null && ValidateDownloadSettings()) {
                if (downloadSettingsModified) {
                    ApplyDownloadSettings(serverSettings.Download);
                    SaveServersSettings();
                }

                AppData.Settings.FormSt.ServerConn = serverSettings.Connection.Name;
                DownloadConfig(serverSettings);
            }
        }
    }
}