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
 * Module   : KpEmail
 * Summary  : Device properties form
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2016
 */

using Scada.Comm.Devices.AB;
using Scada.UI;
using System;
using System.IO;
using System.Windows.Forms;

namespace Scada.Comm.Devices.KpEmail {
    /// <inheritdoc />
    /// <summary>
    /// Device properties form
    /// <para>Form settings properties KP</para>
    /// </summary>
    public partial class FrmConfig : Form {
        private AppDirs appDirs; // application directories
        private int kpNum; // custom control number
        private Config config; // gearbox configuration
        private string configFileName; // KP configuration file name


        /// <inheritdoc />
        /// <summary>
        /// Constructor restricting form creation without parameters
        /// </summary>
        private FrmConfig() {
            InitializeComponent();

            appDirs = null;
            kpNum = 0;
            config = new Config();
            configFileName = "";
        }


        /// <summary>
        /// Install controls according to configuration
        /// </summary>
        private void ConfigToControls() {
            txtHost.Text = config.Host;
            numPort.SetValue(config.Port);
            txtUser.Text = config.User;
            txtPassword.Text = config.Password;
            txtUserDisplayName.Text = config.UserDisplayName;
            chkEnableSsl.Checked = config.EnableSsl;
        }

        /// <summary>
        /// Transfer Control Values to Configuration
        /// </summary>
        private void ControlsToConfig() {
            config.Host = txtHost.Text;
            config.Port = Convert.ToInt32(numPort.Value);
            config.User = txtUser.Text;
            config.Password = txtPassword.Text;
            config.UserDisplayName = txtUserDisplayName.Text;
            config.EnableSsl = chkEnableSsl.Checked;
        }


        /// <summary>
        /// Display the form modally
        /// </summary>
        public static void ShowDialog(AppDirs appDirs, int kpNum) {
            if (appDirs == null)
                throw new ArgumentNullException("appDirs");

            var frmConfig = new FrmConfig {
                appDirs = appDirs,
                kpNum = kpNum
            };
            frmConfig.ShowDialog();
        }


        private void FrmConfig_Load(object sender, EventArgs e) {
            // module localization
            string errMsg;
            if (!Localization.UseRussian) {
                if (Localization.LoadDictionaries(appDirs.LangDir, "KpEmail", out errMsg))
                    Translator.TranslateForm(this, "Scada.Comm.Devices.KpEmail.FrmConfig");
                else
                    ScadaUiUtils.ShowError(errMsg);
            }

            // header output
            Text = string.Format(Text, kpNum);

            // load configuration kp
            configFileName = Config.GetFileName(appDirs.ConfigDir, kpNum);
            if (File.Exists(configFileName) && !config.Load(configFileName, out errMsg))
                ScadaUiUtils.ShowError(errMsg);

            // output configuration kp
            ConfigToControls();
        }

        private void btnEditAddressBook_Click(object sender, EventArgs e) {
            // address book mapping
            FrmAddressBook.ShowDialog(appDirs);
        }

        private void btnOK_Click(object sender, EventArgs e) {
            // receiving configuration changes kp
            ControlsToConfig();

            // save configuration kp
            string errMsg;
            if (config.Save(configFileName, out errMsg))
                DialogResult = DialogResult.OK;
            else
                ScadaUiUtils.ShowError(errMsg);
        }
    }
}