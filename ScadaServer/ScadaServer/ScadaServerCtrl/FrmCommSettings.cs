﻿/*
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
 * Module   : SCADA-Server Control
 * Summary  : Connection settings form
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2013
 * Modified : 2016
 */

using Scada.Client;
using Scada.UI;
using System;
using System.Windows.Forms;

namespace Scada.Server.Ctrl {
    /// <inheritdoc />
    /// <summary>
    /// Connection settings form
    /// <para>Connection Settings Form</para>
    /// </summary>
    public partial class FrmCommSettings : Form {
        private CommSettings commSettings;


        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public FrmCommSettings() {
            InitializeComponent();
            commSettings = null;
        }

        /// <summary>
        /// Display connection settings form
        /// </summary>
        public static DialogResult Show(CommSettings commSettings) {
            if (commSettings == null)
                throw new ArgumentNullException("commSettings");

            FrmCommSettings frmCommSettings = new FrmCommSettings();
            frmCommSettings.commSettings = commSettings;
            frmCommSettings.txtServerHost.Text = commSettings.ServerHost;
            frmCommSettings.numServerPort.SetValue(commSettings.ServerPort);
            frmCommSettings.numServerTimeout.SetValue(commSettings.ServerTimeout);
            frmCommSettings.txtServerUser.Text = commSettings.ServerUser;
            frmCommSettings.txtServerPwd.Text = commSettings.ServerPwd;
            return frmCommSettings.ShowDialog();
        }


        private void FrmCommSettings_Load(object sender, EventArgs e) {
            // form translation
            Translator.TranslateForm(this, "Scada.Server.Ctrl.FrmCommSettings");
        }

        private void btnOk_Click(object sender, EventArgs e) {
            if (commSettings != null) {
                commSettings.ServerHost = txtServerHost.Text;
                commSettings.ServerPort = decimal.ToInt32(numServerPort.Value);
                commSettings.ServerTimeout = decimal.ToInt32(numServerTimeout.Value);
                commSettings.ServerUser = txtServerUser.Text;
                commSettings.ServerPwd = txtServerPwd.Text;
            }

            DialogResult = DialogResult.OK;
        }
    }
}