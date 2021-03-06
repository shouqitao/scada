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
 * Module   : Administrator
 * Summary  : Control for selecting a deployment profile
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using Scada.Admin.App.Code;
using Scada.Admin.App.Forms.Deployment;
using Scada.Admin.Deployment;
using Scada.Admin.Project;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Scada.Admin.App.Controls.Deployment {
    /// <summary>
    /// Control for selecting a deployment profile.
    /// <para>Control for choosing a deployment profile.</para>
    /// </summary>
    public partial class CtrlProfileSelector : UserControl {
        private AppData appData; // the common data of the application
        private DeploymentSettings deploymentSettings; // the deployment settings to select or edit
        private Instance instance; // the instance which profile is selected


        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public CtrlProfileSelector() {
            InitializeComponent();
        }


        /// <summary>
        /// Gets the currently selected profile.
        /// </summary>
        public DeploymentProfile SelectedProfile {
            get { return cbProfile.SelectedItem as DeploymentProfile; }
        }


        /// <summary>
        /// Fills the profiles combo box.
        /// </summary>
        private void FillProfileList() {
            try {
                cbProfile.BeginUpdate();
                cbProfile.Items.Clear();
                cbProfile.Items.Add(AppPhrases.ProfileNotSet);

                var selectedIndex = 0;
                string selectedName = instance.DeploymentProfile;

                foreach (var profile in deploymentSettings.Profiles.Values) {
                    int index = cbProfile.Items.Add(profile);
                    if (profile.Name == selectedName)
                        selectedIndex = index;
                }

                cbProfile.SelectedIndex = selectedIndex;
            } finally {
                cbProfile.EndUpdate();
            }
        }

        /// <summary>
        /// Adds the profile to the deployment settings and combo box.
        /// </summary>
        private void AddProfileToLists(DeploymentProfile profile) {
            // add to the deployment settings
            deploymentSettings.Profiles.Add(profile.Name, profile);

            // add to the combo box
            int index = deploymentSettings.Profiles.IndexOfKey(profile.Name);
            if (index >= 0) {
                cbProfile.Items.Insert(index, profile);
                cbProfile.SelectedIndex = index;
            }
        }

        /// <summary>
        /// Save the deployments settings.
        /// </summary>
        private void SaveDeploymentSettings() {
            if (!deploymentSettings.Save(out string errMsg))
                appData.ProcError(errMsg);
        }

        /// <summary>
        /// Raises a SelectedProfileChanged event.
        /// </summary>
        private void OnSelectedProfileChanged() {
            SelectedProfileChanged?.Invoke(this, EventArgs.Empty);
        }


        /// <summary>
        /// Initializes the control.
        /// </summary>
        public void Init(AppData appData, DeploymentSettings deploymentSettings, Instance instance) {
            this.appData = appData ?? throw new ArgumentNullException(nameof(appData));
            this.deploymentSettings = deploymentSettings ?? throw new ArgumentNullException(nameof(deploymentSettings));
            this.instance = instance ?? throw new ArgumentNullException(nameof(instance));

            txtInstanceName.Text = instance.Name;
            FillProfileList();
        }


        /// <summary>
        /// Occurs when the selected profile changes.
        /// </summary>
        [Category("Property Changed")]
        public event EventHandler SelectedProfileChanged;


        private void cbProfile_SelectedIndexChanged(object sender, EventArgs e) {
            btnEditProfile.Enabled = btnDeleteProfile.Enabled = SelectedProfile != null;
            OnSelectedProfileChanged();
        }

        private void btnCreateProfile_Click(object sender, EventArgs e) {
            // create a new profile
            var profile = new DeploymentProfile();

            var frmConnSettings = new FrmConnSettings() {
                Profile = profile,
                ExistingProfileNames = deploymentSettings.GetExistingProfileNames()
            };

            if (frmConnSettings.ShowDialog() == DialogResult.OK) {
                AddProfileToLists(profile);
                SaveDeploymentSettings();
            }
        }

        private void btnEditProfile_Click(object sender, EventArgs e) {
            // edit the selected profile
            var profile = SelectedProfile;
            string oldName = profile.Name;

            var frmConnSettings = new FrmConnSettings() {
                Profile = profile,
                ExistingProfileNames = deploymentSettings.GetExistingProfileNames(oldName)
            };

            if (frmConnSettings.ShowDialog() == DialogResult.OK) {
                // update the profile name if it changed
                if (oldName != profile.Name) {
                    deploymentSettings.Profiles.Remove(oldName);

                    try {
                        cbProfile.BeginUpdate();
                        cbProfile.Items.RemoveAt(cbProfile.SelectedIndex);
                        AddProfileToLists(profile);
                    } finally {
                        cbProfile.EndUpdate();
                    }
                }

                // save the deployment settings
                SaveDeploymentSettings();
            }
        }

        private void btnDeleteProfile_Click(object sender, EventArgs e) {
            // delete the selected profile
            if (MessageBox.Show(AppPhrases.ConfirmDeleteProfile, CommonPhrases.QuestionCaption,
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == DialogResult.Yes) {
                // remove from the deployment settings
                var selectedProfile = SelectedProfile;
                deploymentSettings.Profiles.Remove(selectedProfile.Name);

                // remove from the combo box
                try {
                    cbProfile.BeginUpdate();
                    int selectedIndex = cbProfile.SelectedIndex;
                    cbProfile.Items.RemoveAt(selectedIndex);

                    if (cbProfile.Items.Count > 0) {
                        cbProfile.SelectedIndex = selectedIndex >= cbProfile.Items.Count
                            ? cbProfile.Items.Count - 1
                            : selectedIndex;
                    }
                } finally {
                    cbProfile.EndUpdate();
                }

                // save the deployment settings
                SaveDeploymentSettings();
            }
        }
    }
}