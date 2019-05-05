/*
 * Copyright 2015 Mikhail Shiryaev
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
 * Module   : ModDBExport
 * Summary  : Query configuration control
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2015
 * Modified : 2015
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Scada.Server.Modules.DBExport {
    /// <summary>
    /// Query configuration control
    /// <para>Control for query configuration</para>
    /// </summary>
    internal partial class CtrlExportQuery : UserControl {
        /// <summary>
        /// Constructor
        /// </summary>
        public CtrlExportQuery() {
            InitializeComponent();
        }


        /// <summary>
        /// Get or set the sign of export performance
        /// </summary>
        public bool Export {
            get { return chkExport.Checked; }
            set {
                chkExport.CheckedChanged -= chkExport_CheckedChanged;
                chkExport.Checked = value;
                chkExport.CheckedChanged += chkExport_CheckedChanged;
                SetQueryBackColor();
            }
        }

        /// <summary>
        /// Get or set SQL query
        /// </summary>
        public string Query {
            get { return txtQuery.Text; }
            set {
                txtQuery.TextChanged -= txtQuery_TextChanged;
                txtQuery.Text = value;
                txtQuery.TextChanged += txtQuery_TextChanged;
            }
        }


        /// <summary>
        /// Set the background color of the query text field
        /// </summary>
        private void SetQueryBackColor() {
            txtQuery.BackColor = Color.FromKnownColor(chkExport.Checked ? KnownColor.Window : KnownColor.Control);
        }

        /// <summary>
        /// Trigger event TriggerChanged
        /// </summary>
        private void OnPropChanged() {
            PropChanged?.Invoke(this, EventArgs.Empty);
        }


        /// <summary>
        /// The event occurs when changing the properties of the control
        /// </summary>
        [Category("Property Changed")]
        public event EventHandler PropChanged;


        private void CtrlExportQuery_Load(object sender, EventArgs e) {
            SetQueryBackColor();
        }

        private void chkExport_CheckedChanged(object sender, EventArgs e) {
            SetQueryBackColor();
            OnPropChanged();
        }

        private void txtQuery_TextChanged(object sender, EventArgs e) {
            Export = txtQuery.Text != "";
            OnPropChanged();
        }
    }
}