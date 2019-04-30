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
 * Summary  : Control for selecting a connection to a remote server
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using Scada;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace ScadaAdmin.Remote {
    /// <summary>
    /// Control for selecting a connection to a remote server
    /// <para>The control to select a connection to a remote server</para>
    /// </summary>
    public partial class CtrlServerConn : UserControl {
        private ServersSettings serversSettings; // settings of interaction with remote servers


        /// <summary>
        /// Constructor
        /// </summary>
        public CtrlServerConn() {
            InitializeComponent();
            serversSettings = null;
        }


        /// <summary>
        /// Get or set settings for interaction with remote servers
        /// </summary>
        public ServersSettings ServersSettings {
            get { return serversSettings; }
            set {
                serversSettings = value;
                FillConnList();
            }
        }

        /// <summary>
        /// Get selected settings
        /// </summary>
        public ServersSettings.ServerSettings SelectedSettings {
            get { return cbConnection.SelectedItem as ServersSettings.ServerSettings; }
        }


        /// <summary>
        /// Fill in the list of connections
        /// </summary>
        private void FillConnList() {
            try {
                cbConnection.BeginUpdate();
                cbConnection.Items.Clear();
                var selInd = 0;

                if (serversSettings != null) {
                    string defConnName = AppData.Settings.FormSt.ServerConn;

                    foreach (ServersSettings.ServerSettings serverSettings in serversSettings.Servers.Values
                    ) {
                        int ind = cbConnection.Items.Add(serverSettings);
                        if (string.Equals(serverSettings.Connection.Name, defConnName))
                            selInd = ind;
                    }
                }

                if (cbConnection.Items.Count > 0)
                    cbConnection.SelectedIndex = selInd;
            } finally {
                cbConnection.EndUpdate();
            }
        }

        /// <summary>
        /// Add server settings to general settings and drop-down list
        /// </summary>
        private void AddToLists(ServersSettings.ServerSettings serverSettings) {
            // adding to general settings
            string connName = serverSettings.Connection.Name;
            serversSettings.Servers.Add(connName, serverSettings);

            // add to dropdown list
            int ind = serversSettings.Servers.IndexOfKey(connName);
            if (ind >= 0) {
                cbConnection.Items.Insert(ind, serverSettings);
                cbConnection.SelectedIndex = ind;
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
        /// Raise the SelectedSettingsChanged event
        /// </summary>
        private void OnSelectedSettingsChanged() {
            SelectedSettingsChanged?.Invoke(this, EventArgs.Empty);
        }


        /// <summary>
        /// Event occurring when changing the selected connection
        /// </summary>
        [Category("Property Changed")]
        public event EventHandler SelectedSettingsChanged;


        private void cbConnection_SelectedIndexChanged(object sender, EventArgs e) {
            btnCreateConn.Enabled = serversSettings != null;
            btnEditConn.Enabled = btnRemoveConn.Enabled = SelectedSettings != null;
            OnSelectedSettingsChanged();
        }

        private void btnCreateConn_Click(object sender, EventArgs e) {
            // creating new settings
            var serverSettings = new ServersSettings.ServerSettings();
            var frmConnSettings = new FrmConnSettings() {
                ConnectionSettings = serverSettings.Connection,
                ExistingNames = ServersSettings.GetExistingNames()
            };

            if (frmConnSettings.ShowDialog() == DialogResult.OK) {
                AddToLists(serverSettings);
                SaveServersSettings();
            }
        }

        private void btnEditConn_Click(object sender, EventArgs e) {
            // editing settings
            ServersSettings.ServerSettings serverSettings = SelectedSettings;
            string oldName = serverSettings.Connection.Name;

            var frmConnSettings = new FrmConnSettings() {
                ConnectionSettings = serverSettings.Connection,
                ExistingNames = ServersSettings.GetExistingNames(oldName)
            };

            if (frmConnSettings.ShowDialog() == DialogResult.OK) {
                // name update if it has changed
                if (!string.Equals(oldName, serverSettings.Connection.Name, StringComparison.Ordinal)) {
                    serversSettings.Servers.Remove(oldName);
                    cbConnection.BeginUpdate();
                    cbConnection.Items.RemoveAt(cbConnection.SelectedIndex);
                    AddToLists(serverSettings);
                    cbConnection.EndUpdate();
                }

                // saving settings
                SaveServersSettings();
            }
        }

        private void btnRemoveConn_Click(object sender, EventArgs e) {
            // delete settings
            if (MessageBox.Show(AppPhrases.DeleteConnConfirm, CommonPhrases.QuestionCaption,
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == DialogResult.Yes) {
                // remove from general settings
                ServersSettings.ServerSettings serverSettings = SelectedSettings;
                serversSettings.Servers.Remove(serverSettings.Connection.Name);

                // remove from dropdown list
                cbConnection.BeginUpdate();
                int selInd = cbConnection.SelectedIndex;
                cbConnection.Items.RemoveAt(selInd);

                if (cbConnection.Items.Count > 0) {
                    cbConnection.SelectedIndex = selInd >= cbConnection.Items.Count
                        ? cbConnection.Items.Count - 1
                        : selInd;
                }

                cbConnection.EndUpdate();

                // saving settings
                SaveServersSettings();
            }
        }
    }
}