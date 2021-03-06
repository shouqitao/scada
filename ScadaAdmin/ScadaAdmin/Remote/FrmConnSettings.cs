﻿/*
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
 * Summary  : Remote server connection settings form
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using Scada;
using Scada.UI;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ScadaAdmin.Remote {
    /// <inheritdoc />
    /// <summary>
    /// Remote server connection settings form
    /// <para>The form of settings for connecting to a remote server</para>
    /// </summary>
    public partial class FrmConnSettings : Form {
        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public FrmConnSettings() {
            InitializeComponent();
        }


        /// <summary>
        /// Get or set editable connection settings
        /// </summary>
        public ServersSettings.ConnectionSettings ConnectionSettings { get; set; }

        /// <summary>
        /// Get or set existing settings names
        /// </summary>
        public HashSet<string> ExistingNames { get; set; }


        /// <summary>
        /// Install controls according to settings
        /// </summary>
        private void SettingsToControls() {
            if (ConnectionSettings != null) {
                txtName.Text = ConnectionSettings.Name;
                txtHost.Text = ConnectionSettings.Host;
                numPort.SetValue(ConnectionSettings.Port);
                txtUsername.Text = ConnectionSettings.Username;
                txtPassword.Text = ConnectionSettings.Password;
                txtScadaInstance.Text = ConnectionSettings.ScadaInstance;
                txtSecretKey.Text = ScadaUtils.BytesToHex(ConnectionSettings.SecretKey);
            }
        }

        /// <summary>
        /// Set the settings according to the controls
        /// </summary>
        private void ControlsToSettings() {
            if (ConnectionSettings != null) {
                ConnectionSettings.Name = txtName.Text.Trim();
                ConnectionSettings.Host = txtHost.Text.Trim();
                ConnectionSettings.Port = (int) numPort.Value;
                ConnectionSettings.Username = txtUsername.Text.Trim();
                ConnectionSettings.Password = txtPassword.Text;
                ConnectionSettings.ScadaInstance = txtScadaInstance.Text.Trim();
                ConnectionSettings.SecretKey = ScadaUtils.HexToBytes(txtSecretKey.Text.Trim());
            }
        }

        /// <summary>
        /// Check Control Values
        /// </summary>
        private bool ValidateControls() {
            if (string.IsNullOrWhiteSpace(txtName.Text) ||
                string.IsNullOrWhiteSpace(txtHost.Text) ||
                string.IsNullOrWhiteSpace(txtUsername.Text) ||
                string.IsNullOrWhiteSpace(txtPassword.Text) ||
                string.IsNullOrWhiteSpace(txtScadaInstance.Text) ||
                string.IsNullOrWhiteSpace(txtSecretKey.Text)) {
                ScadaUiUtils.ShowError(AppPhrases.EmptyFieldsNotAllowed);
                return false;
            }

            if (ExistingNames.Contains(txtName.Text.Trim())) {
                ScadaUiUtils.ShowError(AppPhrases.ConnNameDuplicated);
                return false;
            }

            if (!ScadaUtils.HexToBytes(txtSecretKey.Text.Trim(), out byte[] bytes)) {
                ScadaUiUtils.ShowError(AppPhrases.IncorrectSecretKey);
                return false;
            }

            return true;
        }


        private void FrmConnSettings_Load(object sender, EventArgs e) {
            // form translation
            Translator.TranslateForm(this, "ScadaAdmin.Remote.FrmConnSettings", toolTip);

            // display settings
            SettingsToControls();
        }

        private void btnGenSecretKey_Click(object sender, EventArgs e) {
            // private key generation
            txtSecretKey.Text = ScadaUtils.BytesToHex(ScadaUtils.GetRandomBytes(ScadaUtils.SecretKeySize));
            txtSecretKey.Focus();
        }

        private void btnOK_Click(object sender, EventArgs e) {
            if (ValidateControls()) {
                ControlsToSettings();
                DialogResult = DialogResult.OK;
            }
        }
    }
}