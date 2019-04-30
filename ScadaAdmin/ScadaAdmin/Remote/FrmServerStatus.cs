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
 * Summary  : Form for checking status of a remote server
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using Scada;
using Scada.UI;
using ScadaAdmin.AgentSvcRef;
using System;
using System.Windows.Forms;

namespace ScadaAdmin.Remote {
    /// <summary>
    /// Form for checking status of a remote server
    /// <para>Form to check the status of the remote server</para>
    /// </summary>
    public partial class FrmServerStatus : Form {
        private ServersSettings serversSettings; // settings of interaction with remote servers
        private AgentSvcClient client; // client to communicate with the server
        private long sessionID; // id server interaction sessions


        /// <summary>
        /// Constructor
        /// </summary>
        public FrmServerStatus() {
            InitializeComponent();
            serversSettings = new ServersSettings();
            client = null;
            sessionID = 0;
        }


        /// <summary>
        /// Disconnect from remote server
        /// </summary>
        private void Disconnect() {
            timer.Stop();
            client = null;
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            gbStatus.Enabled = false;
            txtServerStatus.Text = txtCommStatus.Text = txtUpdateTime.Text = "";
        }

        /// <summary>
        /// Get a string representation of the service status
        /// </summary>
        private string StatusToString(ServiceStatus status) {
            switch (status) {
                case ServiceStatus.Normal:
                    return AppPhrases.NormalSvcStatus;
                case ServiceStatus.Stopped:
                    return AppPhrases.StoppedSvcStatus;
                case ServiceStatus.Error:
                    return AppPhrases.ErrorSvcStatus;
                default: // ServiceStatus.Undefined
                    return AppPhrases.UndefinedSvcStatus;
            }
        }


        private void FrmServerStatus_Load(object sender, EventArgs e) {
            // form translation
            Translator.TranslateForm(this, "ScadaAdmin.Remote.CtrlServerConn");
            Translator.TranslateForm(this, "ScadaAdmin.Remote.FrmServerStatus");

            // loading settings
            if (!serversSettings.Load(AppData.AppDirs.ConfigDir + ServersSettings.DefFileName,
                out string errMsg))
                AppUtils.ProcError(errMsg);

            // display settings
            ctrlServerConn.ServersSettings = serversSettings;
        }

        private void FrmServerStatus_FormClosed(object sender, FormClosedEventArgs e) {
            timer.Stop();
        }

        private void ctrlServerConn_SelectedSettingsChanged(object sender, EventArgs e) {
            Disconnect();
        }

        private void btnConnect_Click(object sender, EventArgs e) {
            // connection to a remote server
            if (!timer.Enabled) {
                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;
                gbStatus.Enabled = true;
                AppData.Settings.FormSt.ServerConn = ctrlServerConn.SelectedSettings.Connection.Name;
                timer.Start();
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e) {
            // disconnecting from a remote server
            Disconnect();
        }

        private void btnRestartServer_Click(object sender, EventArgs e) {
            // restart the Server service on the remote server
            if (client != null) {
                if (client.ControlService(sessionID, ServiceApp.Server, ServiceCommand.Restart))
                    ScadaUiUtils.ShowInfo(AppPhrases.ServerRestarted);
                else
                    ScadaUiUtils.ShowError(AppPhrases.UnableRestartServer);
            }
        }

        private void btnRestartComm_Click(object sender, EventArgs e) {
            // restart the Communicator service on the remote server
            if (client != null) {
                if (client.ControlService(sessionID, ServiceApp.Comm, ServiceCommand.Restart))
                    ScadaUiUtils.ShowInfo(AppPhrases.CommRestarted);
                else
                    ScadaUiUtils.ShowError(AppPhrases.UnableRestartComm);
            }
        }

        private void timer_Tick(object sender, EventArgs e) {
            timer.Stop();

            // compound
            if (client == null) {
                if (!DownloadUpload.Connect(ctrlServerConn.SelectedSettings.Connection,
                    out client, out sessionID, out string errMsg)) {
                    Disconnect();
                    ScadaUiUtils.ShowError(errMsg);
                }
            }

            // data request
            if (client != null) {
                ServiceStatus status;

                try {
                    txtServerStatus.Text = client.GetServiceStatus(out status, sessionID, ServiceApp.Server)
                        ? StatusToString(status)
                        : "---";
                } catch (Exception ex) {
                    txtServerStatus.Text = ex.Message;
                }

                try {
                    txtCommStatus.Text = client.GetServiceStatus(out status, sessionID, ServiceApp.Comm)
                        ? StatusToString(status)
                        : "---";
                } catch (Exception ex) {
                    txtCommStatus.Text = ex.Message;
                }

                txtUpdateTime.Text = DateTime.Now.ToLocalizedString();
                timer.Start();
            }
        }
    }
}