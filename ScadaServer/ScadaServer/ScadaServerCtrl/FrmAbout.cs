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
 * Module   : SCADA-Server Control
 * Summary  : About form
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2008
 * Modified : 2018
 */

using Scada.UI;
using System;
using System.Diagnostics;
using System.Windows.Forms;
using Utils;

namespace Scada.Server.Ctrl {
    /// <inheritdoc />
    /// <summary>
    /// About form
    /// <para>Форма о программе</para>
    /// </summary>
    public partial class FrmAbout : Form {
        private static FrmAbout frmAbout = null; // form about the program

        private string exeDir; // application executable directory
        private Log errLog; // application error log
        private bool inited; // form initialized
        private string linkUrl; // hyperlink


        /// <inheritdoc />
        /// <summary>
        /// Constructor restricting the creation of an object without parameters
        /// </summary>
        private FrmAbout() {
            InitializeComponent();

            inited = false;
            linkUrl = "";
        }


        /// <summary>
        /// Display a program form
        /// </summary>
        public static void ShowAbout(string exeDir, Log errLog) {
            if (exeDir == null)
                throw new ArgumentNullException("exeDir");
            if (errLog == null)
                throw new ArgumentNullException("errLog");

            if (frmAbout == null) {
                frmAbout = new FrmAbout();
                frmAbout.exeDir = exeDir;
                frmAbout.errLog = errLog;
            }

            frmAbout.Init();
            frmAbout.ShowDialog();
        }


        /// <summary>
        /// Initialize the form
        /// </summary>
        private void Init() {
            if (!inited) {
                inited = true;

                // setting controls depending on localization
                PictureBox activePictureBox;

                if (Localization.UseRussian) {
                    activePictureBox = pbAboutRu;
                    pbAboutEn.Visible = false;
                    lblVersionEn.Visible = false;
                    lblVersionRu.Text = "Версия " + ServerUtils.AppVersion;
                } else {
                    activePictureBox = pbAboutEn;
                    pbAboutRu.Visible = false;
                    lblVersionRu.Visible = false;
                    lblVersionEn.Text = "Version " + ServerUtils.AppVersion;
                }

                // download images and hyperlinks from files if they exist
                bool imgLoaded;
                string errMsg;
                if (ScadaUiUtils.LoadAboutForm(exeDir, this, activePictureBox, lblWebsite,
                    out imgLoaded, out linkUrl, out errMsg)) {
                    if (imgLoaded) {
                        lblVersionRu.Visible = false;
                        lblVersionEn.Visible = false;
                    }
                } else {
                    errLog.WriteAction(errMsg);
                    ScadaUiUtils.ShowError(errMsg);
                }
            }
        }


        private void FrmAbout_Click(object sender, EventArgs e) {
            Close();
        }

        private void FrmAbout_KeyPress(object sender, KeyPressEventArgs e) {
            Close();
        }

        private void lblLink_Click(object sender, EventArgs e) {
            if (ScadaUtils.IsValidUrl(linkUrl)) {
                Process.Start(linkUrl);
                Close();
            }
        }
    }
}