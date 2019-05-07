﻿/*
 * Copyright 2019 Mikhail Shiryaev
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
 * Module   : Server Shell
 * Summary  : Form for generating Server data and commands
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2019
 * Modified : 2019
 */

using Scada.Client;
using Scada.Server.Shell.Code;
using Scada.UI;
using System;
using System.Windows.Forms;
using Utils;

namespace Scada.Server.Shell.Forms {
    /// <inheritdoc />
    /// <summary>
    /// Form for generating Server data and commands.
    /// <para>Form of data generator and Server commands.</para>
    /// </summary>
    public partial class FrmGenerator : Form {
        private readonly Settings settings; // the application settings
        private readonly ServerEnvironment environment; // the application environment
        private ServerComm serverComm; // the object to communicate with Server
        private FrmGenCommand frmGenCommand; // the form to send command


        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        private FrmGenerator() {
            InitializeComponent();
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public FrmGenerator(Settings settings, ServerEnvironment environment)
            : this() {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
            serverComm = null;
            frmGenCommand = null;
        }


        /// <summary>
        /// Initializes the object to communicate with Server.
        /// </summary>
        private void InitServerComm() {
            if (environment.AgentClient == null) {
                gbGenerator.Enabled = false;
                pnlProfileWarn.Visible = true;
            } else {
                CommSettings commSettings = environment.AgentClient.CreateCommSettings();
                commSettings.ServerPort = settings.TcpPort;
                serverComm = new ServerComm(commSettings, new LogStub());
            }
        }

        /// <summary>
        /// Displays server connection settings.
        /// </summary>
        private void DisplayCommSettings() {
            if (serverComm != null) {
                CommSettings commSettings = serverComm.CommSettings;
                txtServerHost.Text = commSettings.ServerHost;
                txtServerPort.Text = commSettings.ServerPort.ToString();
                txtServerTimeout.Text = commSettings.ServerTimeout.ToString();
                txtServerUser.Text = commSettings.ServerUser;
                txtServerPwd.Text = commSettings.ServerPwd;
            }
        }


        private void FrmGenerator_Load(object sender, EventArgs e) {
            Translator.TranslateForm(this, "Scada.Server.Shell.Forms.FrmGenerator");
            InitServerComm();
            DisplayCommSettings();
        }

        private void btnGenerateData_Click(object sender, EventArgs e) { }

        private void btnGenerateEvent_Click(object sender, EventArgs e) { }

        private void btnGenerateCmd_Click(object sender, EventArgs e) {
            // show the command form
            if (frmGenCommand == null)
                frmGenCommand = new FrmGenCommand(serverComm, environment.ErrLog);

            frmGenCommand.ShowDialog();
        }
    }
}