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
 * Summary  : Upload configuration form
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using Scada;
using Scada.UI;
using ScadaAdmin.AgentSvcRef;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace ScadaAdmin.Remote {
    /// <summary>
    /// Upload configuration form
    /// <para>Configuration transfer form</para>
    /// </summary>
    public partial class FrmUploadConfig : Form {
        /// <summary>
        /// Tree Node Information
        /// </summary>
        private class NodeInfo {
            /// <summary>
            /// Get or set the path relative to the configuration directory
            /// </summary>
            public string Path { get; set; }

            /// <summary>
            /// Get or set the sign that the node corresponds to the directory
            /// </summary>
            public bool IsDirectory { get; set; }
        }

        // Key tree icon pictograms
        private const string FolderClosedImageKey = "folder_closed2.png";
        private const string FolderOpenImageKey = "folder_open2.png";
        private const string FileImageKey = "file.png";

        private ServersSettings serversSettings; // settings of interaction with remote servers
        private bool uploadSettingsModified; // Last selected transfer settings have been changed
        private TreeNode rootNode; // tree root


        /// <summary>
        /// Constructor
        /// </summary>
        public FrmUploadConfig() {
            InitializeComponent();

            serversSettings = new ServersSettings();
            uploadSettingsModified = false;
            rootNode = null;
        }


        /// <summary>
        /// Display configuration transfer settings
        /// </summary>
        private void ShowUploadSettings(ServersSettings.UploadSettings uploadSettings) {
            if (uploadSettings == null) {
                gbOptions.Enabled = false;
                rbGetFromDir.Checked = true;
                txtSrcDir.Text = txtSrcFile.Text = "";
                tvFiles.Nodes.Clear();
                chkClearSpecificFiles.Checked = false;
                btnUpload.Enabled = false;
            } else {
                gbOptions.Enabled = true;
                txtSrcDir.Text = uploadSettings.SrcDir;
                FillTreeView(uploadSettings.SrcDir, uploadSettings.SelectedFiles);
                txtSrcFile.Text = uploadSettings.SrcFile;
                chkClearSpecificFiles.Checked = uploadSettings.ClearSpecificFiles;
                btnUpload.Enabled = true;

                if (uploadSettings.GetFromDir)
                    rbGetFromDir.Checked = true;
                else
                    rbGetFromArc.Checked = true;
            }
        }

        /// <summary>
        /// Fill the tree of selected files
        /// </summary>
        private void FillTreeView(string srcDir, List<string> selectedFiles) {
            try {
                tvFiles.BeginUpdate();
                tvFiles.Nodes.Clear();

                srcDir = ScadaUtils.NormalDir(srcDir);
                int srcDirLen = srcDir.Length;
                rootNode = tvFiles.Nodes.Add(srcDir.TrimEnd('\\'));

                // adding configuration base node
                var selFiles = new HashSet<string>(selectedFiles);
                AddDirToTreeView(rootNode, srcDir + "BaseDAT\\", srcDirLen, selFiles);

                // add interface node
                AddDirToTreeView(rootNode, srcDir + "Interface\\", srcDirLen, selFiles);

                // adding a Communicator node
                string commDir = srcDir + "ScadaComm\\";
                if (Directory.Exists(commDir)) {
                    var commNode = rootNode.Nodes.Add("ScadaComm");
                    AddDirToTreeView(commNode, commDir + "Config\\", srcDirLen, selFiles);
                }

                // adding a Server node
                string serverDir = srcDir + "ScadaServer\\";
                if (Directory.Exists(serverDir)) {
                    var serverNode = rootNode.Nodes.Add("ScadaServer");
                    AddDirToTreeView(serverNode, serverDir + "Config\\", srcDirLen, selFiles);
                }

                // add a web station node
                string webDir = srcDir + "ScadaWeb\\";
                if (Directory.Exists(webDir)) {
                    var serverNode = rootNode.Nodes.Add("ScadaWeb");
                    AddDirToTreeView(serverNode, webDir + "config\\", srcDirLen, selFiles);
                    AddDirToTreeView(serverNode, webDir + "storage\\", srcDirLen, selFiles);
                }

                rootNode.Expand();
            } finally {
                tvFiles.EndUpdate();
            }
        }

        /// <summary>
        /// Add directory to tree
        /// </summary>
        private void AddDirToTreeView(TreeNode parentNode, string dir, int rootDirLen,
            HashSet<string> selFiles) {
            AddDirToTreeView(parentNode, new DirectoryInfo(dir), rootDirLen, selFiles);
        }

        /// <summary>
        /// Add directory to tree
        /// </summary>
        private void AddDirToTreeView(TreeNode parentNode, DirectoryInfo dirInfo,
            int rootDirLen, HashSet<string> selFiles) {
            if (dirInfo.Exists) {
                var dirNode = parentNode.Nodes.Add(dirInfo.Name);
                dirNode.ImageKey = dirNode.SelectedImageKey = FolderClosedImageKey;
                string dirNodePath = ScadaUtils.NormalDir(dirInfo.FullName.Substring(rootDirLen));
                dirNode.Tag = new NodeInfo() {
                    Path = dirNodePath,
                    IsDirectory = true
                };

                // adding a subdirectory
                DirectoryInfo[] subdirInfoArr = dirInfo.GetDirectories("*", SearchOption.TopDirectoryOnly);

                foreach (var subdirInfo in subdirInfoArr) {
                    AddDirToTreeView(dirNode, subdirInfo, rootDirLen, selFiles);
                }

                // adding files
                FileInfo[] fileInfoArr = dirInfo.GetFiles("*", SearchOption.TopDirectoryOnly);

                foreach (var fileInfo in fileInfoArr) {
                    if (fileInfo.Extension != ".bak") {
                        var fileNode = dirNode.Nodes.Add(fileInfo.Name);
                        fileNode.ImageKey = fileNode.SelectedImageKey = FileImageKey;
                        string fileNodePath = fileInfo.FullName.Substring(rootDirLen);
                        fileNode.Tag = new NodeInfo() {
                            Path = fileNodePath,
                            IsDirectory = false
                        };

                        if (selFiles.Contains(fileNodePath))
                            fileNode.Checked = true;
                    }
                }

                // selecting a node and its child nodes after they are added to the tree
                if (selFiles.Contains(dirNodePath)) {
                    dirNode.Checked = true;
                    CheckAllChildNodes(dirNode, true);
                }
            }
        }

        /// <summary>
        /// Set or deselect child nodes of the tree
        /// </summary>
        private void CheckAllChildNodes(TreeNode treeNode, bool nodeChecked) {
            foreach (TreeNode node in treeNode.Nodes) {
                node.Checked = nodeChecked;
                CheckAllChildNodes(node, nodeChecked);
            }
        }

        /// <summary>
        /// Check configuration transfer settings
        /// </summary>
        private bool ValidateUploadSettings() {
            if (rbGetFromDir.Checked) {
                if (string.IsNullOrWhiteSpace(txtSrcDir.Text)) {
                    ScadaUiUtils.ShowError(AppPhrases.ConfigDirRequired);
                    return false;
                }
            } else if (string.IsNullOrWhiteSpace(txtSrcFile.Text)) {
                ScadaUiUtils.ShowError(AppPhrases.ConfigArcRequired);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Apply configuration transfer settings
        /// </summary>
        private void ApplyUploadSettings(ServersSettings.UploadSettings uploadSettings) {
            uploadSettings.GetFromDir = rbGetFromDir.Checked;
            uploadSettings.SrcDir = txtSrcDir.Text;
            uploadSettings.SrcFile = txtSrcFile.Text;
            uploadSettings.ClearSpecificFiles = chkClearSpecificFiles.Checked;

            uploadSettings.SelectedFiles.Clear();
            TraverseNodes(rootNode);

            // Retrieve selected files based on selected tree nodes
            void TraverseNodes(TreeNode node) {
                if (node.Tag is NodeInfo nodeInfo && node.Checked) {
                    uploadSettings.SelectedFiles.Add(nodeInfo.Path);
                } else {
                    foreach (TreeNode childNode in node.Nodes) {
                        TraverseNodes(childNode);
                    }
                }
            }
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
        /// Convert base configuration to DAT format, if necessary
        /// </summary>
        private void ConvertBaseToDAT(string srcDir) {
            var appSettings = AppData.Settings.AppSett;
            string workBaseDATDir = ScadaUtils.NormalDir(appSettings.BaseDATDir);
            string srcBaseDATDir = Path.Combine(srcDir, "BaseDAT\\");

            if (string.Equals(workBaseDATDir, srcBaseDATDir, StringComparison.OrdinalIgnoreCase)) {
                // backup
                if (appSettings.AutoBackupBase &&
                    !ImportExport.BackupSDF(appSettings.BaseSDFFile, appSettings.BackupDir, out string msg))
                    AppUtils.ProcError(msg);

                // converting
                if (!ImportExport.PassBase(Tables.TableInfoList, workBaseDATDir, out msg))
                    AppUtils.ProcError(msg);
            }
        }

        /// <summary>
        /// Transfer configuration
        /// </summary>
        private void UploadConfig(ServersSettings.ServerSettings serverSettings) {
            // broadcast
            Cursor = Cursors.WaitCursor;
            string logFileName = AppData.AppDirs.LogDir + "ScadaAdminUpload.txt";
            bool uploadOK = DownloadUpload.UploadConfig(serverSettings,
                logFileName, out bool logCreated, out string msg);
            Cursor = Cursors.Default;

            // display of the result message
            if (uploadOK) {
                ScadaUiUtils.ShowInfo(msg);
            } else {
                AppUtils.ProcError(msg);

                // log display in notebook
                if (logCreated)
                    Process.Start(logFileName);
            }
        }


        private void FrmUploadConfig_Load(object sender, EventArgs e) {
            // form translation
            Translator.TranslateForm(this, "ScadaAdmin.Remote.CtrlServerConn");
            Translator.TranslateForm(this, "ScadaAdmin.Remote.FrmUploadConfig");
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
            ShowUploadSettings(ctrlServerConn.SelectedSettings?.Upload);
            uploadSettingsModified = false;
        }

        private void rbGet_CheckedChanged(object sender, EventArgs e) {
            // to avoid double triggering
            if (((RadioButton) sender).Checked) {
                bool getFromDir = rbGetFromDir.Checked;
                txtSrcDir.Enabled = getFromDir;
                btnBrowseSrcDir.Enabled = getFromDir;
                tvFiles.Enabled = getFromDir;
                txtSrcFile.Enabled = !getFromDir;
                btnSelectSrcFile.Enabled = !getFromDir;
                uploadSettingsModified = true;
            }
        }

        private void uploadControl_Changed(object sender, EventArgs e) {
            uploadSettingsModified = true;
        }

        private void tvFiles_AfterCheck(object sender, TreeViewEventArgs e) {
            if (e.Action != TreeViewAction.Unknown) {
                // setting or clearing selections for child nodes
                var node = e.Node;
                CheckAllChildNodes(node, node.Checked);

                // unselecting parent nodes
                if (!node.Checked) {
                    while (node.Parent != null) {
                        node = node.Parent;
                        node.Checked = false;
                    }
                }
            }

            uploadSettingsModified = true;
        }

        private void tvFiles_BeforeCollapse(object sender, TreeViewCancelEventArgs e) {
            // setting the tree node icon
            var node = e.Node;
            if (node == rootNode || node.Tag is NodeInfo nodeInfo && nodeInfo.IsDirectory)
                node.ImageKey = node.SelectedImageKey = FolderClosedImageKey;
        }

        private void tvFiles_BeforeExpand(object sender, TreeViewCancelEventArgs e) {
            // setting the tree node icon
            var node = e.Node;
            if (node == rootNode || node.Tag is NodeInfo nodeInfo && nodeInfo.IsDirectory)
                node.ImageKey = node.SelectedImageKey = FolderOpenImageKey;
        }

        private void btnBrowseSrcDir_Click(object sender, EventArgs e) {
            // configuration directory selection
            folderBrowserDialog.SelectedPath = txtSrcDir.Text.Trim();

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK) {
                txtSrcDir.Text = ScadaUtils.NormalDir(folderBrowserDialog.SelectedPath);
                FillTreeView(txtSrcDir.Text, ctrlServerConn.SelectedSettings.Upload.SelectedFiles);
            }

            txtSrcDir.Focus();
            txtSrcDir.DeselectAll();
        }

        private void btnSelectSrcFile_Click(object sender, EventArgs e) {
            // select configuration archive file
            string fileName = txtSrcFile.Text.Trim();
            openFileDialog.FileName = fileName;

            if (fileName != "")
                openFileDialog.InitialDirectory = Path.GetDirectoryName(fileName);

            if (openFileDialog.ShowDialog() == DialogResult.OK)
                txtSrcFile.Text = openFileDialog.FileName;

            txtSrcFile.Focus();
            txtSrcFile.DeselectAll();
        }

        private void btnUpload_Click(object sender, EventArgs e) {
            // check settings and transfer configuration
            var serverSettings = ctrlServerConn.SelectedSettings;

            if (serverSettings != null && ValidateUploadSettings()) {
                if (uploadSettingsModified) {
                    ApplyUploadSettings(serverSettings.Upload);
                    SaveServersSettings();
                }

                if (serverSettings.Upload.GetFromDir)
                    ConvertBaseToDAT(serverSettings.Upload.SrcDir);

                AppData.Settings.FormSt.ServerConn = serverSettings.Connection.Name;
                UploadConfig(serverSettings);
            }
        }
    }
}