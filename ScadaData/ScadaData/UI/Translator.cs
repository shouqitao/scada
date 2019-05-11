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
 * Module   : ScadaData
 * Summary  : User interface translation
 *
 * Author   : Mikhail Shiryaev
 * Created  : 2015
 * Modified : 2018
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using WinForms = System.Windows.Forms;

namespace Scada.UI {
    /// <summary>
    /// User interface translation
    /// <para>UI translation</para>
    /// </summary>
    public static class Translator {
        /// <summary>
        /// Translate form using given dictionary
        /// </summary>
        public static void TranslateForm(WinForms.Form form, string dictName,
            WinForms.ToolTip toolTip = null, params WinForms.ContextMenuStrip[] contextMenus) {
            if (form == null || !Localization.Dictionaries.TryGetValue(dictName, out var dict)) return;

            Dictionary<string, ControlInfo> controlInfoDict = GetControlInfoDict(dict);

            // form header translation
            if (controlInfoDict.TryGetValue("this", out var controlInfo) && controlInfo.Text != null)
                form.Text = controlInfo.Text;

            // translation of controls
            TranslateWinControls(form.Controls, toolTip, controlInfoDict);

            // context menu translation
            if (contextMenus != null)
                TranslateWinControls(contextMenus, null, controlInfoDict);
        }

        /// <summary>
        /// Translate a webpage using a predefined dictionary
        /// </summary>
        public static void TranslatePage(Page page, string dictName) {
            if (page == null || !Localization.Dictionaries.TryGetValue(dictName, out var dict)) return;

            Dictionary<string, ControlInfo> controlInfoDict = GetControlInfoDict(dict);

            // page title translation
            if (controlInfoDict.TryGetValue("this", out var controlInfo) && controlInfo.Text != null)
                page.Title = controlInfo.Text;

            // translation of controls
            TranslateWebControls(page.Controls, controlInfoDict);
        }

        /// <summary>
        /// Get information about controls from the dictionary
        /// </summary>
        private static Dictionary<string, ControlInfo> GetControlInfoDict(Localization.Dict dict) {
            var controlInfoDict = new Dictionary<string, ControlInfo>();

            foreach (string phraseKey in dict.Phrases.Keys) {
                string phraseVal = dict.Phrases[phraseKey];
                int dotPos = phraseKey.IndexOf('.');

                if (dotPos < 0) {
                    // if there is no point in the key of the phrase, then the text property is assigned
                    if (controlInfoDict.ContainsKey(phraseKey))
                        controlInfoDict[phraseKey].Text = phraseVal;
                    else
                        controlInfoDict[phraseKey] = new ControlInfo() {Text = phraseVal};
                } else if (0 < dotPos && dotPos < phraseKey.Length - 1) {
                    // if a point is in the middle of a phrase key, then to the left of the point is the name of the element,
                    // to the right is the property
                    string ctrlName = phraseKey.Substring(0, dotPos);
                    string ctrlProp = phraseKey.Substring(dotPos + 1);
                    var propAssigned = true;

                    if (!controlInfoDict.TryGetValue(ctrlName, out var controlInfo))
                        controlInfo = new ControlInfo();

                    switch (ctrlProp) {
                        case "Text":
                            controlInfo.Text = phraseVal;
                            break;
                        case "ToolTip":
                            controlInfo.ToolTip = phraseVal;
                            break;
                        default: {
                            if (ctrlProp.StartsWith("Items[")) {
                                int pos = ctrlProp.IndexOf(']');
                                if (pos >= 0 && int.TryParse(ctrlProp.Substring(6, pos - 6), out int ind))
                                    controlInfo.SetItem(ind, phraseVal);
                            } else if (ctrlProp != "") {
                                controlInfo.SetProp(ctrlProp, phraseVal);
                            } else {
                                propAssigned = false;
                            }

                            break;
                        }
                    }

                    if (propAssigned)
                        controlInfoDict[ctrlName] = controlInfo;
                }
            }

            return controlInfoDict;
        }

        /// <summary>
        /// Recursively translate web form controls
        /// </summary>
        private static void TranslateWebControls(ControlCollection controls,
            IDictionary<string, ControlInfo> controlInfoDict) {
            if (controls == null)
                return;

            foreach (Control control in controls) {
                if (!string.IsNullOrEmpty(control.ID) &&
                    controlInfoDict.TryGetValue(control.ID, out var controlInfo)) {
                    switch (control) {
                        case Label label: {
                            if (controlInfo.Text != null)
                                label.Text = controlInfo.Text;
                            if (controlInfo.ToolTip != null)
                                label.ToolTip = controlInfo.ToolTip;
                            break;
                        }
                        case TextBox textBox: {
                            if (controlInfo.Text != null)
                                textBox.Text = controlInfo.Text;
                            if (controlInfo.ToolTip != null)
                                textBox.ToolTip = controlInfo.ToolTip;
                            break;
                        }
                        case CheckBox checkBox: {
                            if (controlInfo.Text != null)
                                checkBox.Text = controlInfo.Text;
                            if (controlInfo.ToolTip != null)
                                checkBox.ToolTip = controlInfo.ToolTip;
                            break;
                        }
                        case HyperLink hyperLink: {
                            if (controlInfo.Text != null)
                                hyperLink.Text = controlInfo.Text;
                            if (controlInfo.Props != null &&
                                controlInfo.Props.TryGetValue("NavigateUrl", out string navigateUrl))
                                hyperLink.NavigateUrl = navigateUrl;
                            break;
                        }
                        case Button button: {
                            if (controlInfo.Text != null)
                                button.Text = controlInfo.Text;
                            if (controlInfo.ToolTip != null)
                                button.ToolTip = controlInfo.ToolTip;
                            break;
                        }
                        case LinkButton linkButton: {
                            if (controlInfo.Text != null)
                                linkButton.Text = controlInfo.Text;
                            if (controlInfo.ToolTip != null)
                                linkButton.ToolTip = controlInfo.ToolTip;
                            break;
                        }
                        case Image image: {
                            if (controlInfo.ToolTip != null)
                                image.ToolTip = controlInfo.ToolTip;
                            break;
                        }
                        case Panel panel: {
                            if (controlInfo.ToolTip != null)
                                panel.ToolTip = controlInfo.ToolTip;
                            break;
                        }
                        case HiddenField hiddenField: {
                            if (controlInfo.Text != null)
                                hiddenField.Value = controlInfo.Text;
                            break;
                        }
                    }
                }

                // start processing child elements
                TranslateWebControls(control.Controls, controlInfoDict);
            }
        }

        /// <summary>
        /// Recursively translate Windows form controls
        /// </summary>
        private static void TranslateWinControls(IEnumerable controls, WinForms.ToolTip toolTip,
            IDictionary<string, ControlInfo> controlInfoDict) {
            if (controls == null)
                return;

            foreach (var elem in controls) {
                ControlInfo controlInfo;

                if (elem is WinForms.Control control) {
                    // normal control handling
                    if (!string.IsNullOrEmpty(control.Name) /*for example, input field and buttons NumericUpDown*/ &&
                        controlInfoDict.TryGetValue(control.Name, out controlInfo)) {
                        if (controlInfo.Text != null)
                            control.Text = controlInfo.Text;

                        if (controlInfo.ToolTip != null)
                            toolTip?.SetToolTip(control, controlInfo.ToolTip);

                        if (controlInfo.Items != null) {
                            if (elem is WinForms.ComboBox comboBox) {
                                for (int i = 0,
                                    cnt = Math.Min(comboBox.Items.Count, controlInfo.Items.Count);
                                    i < cnt;
                                    i++) {
                                    string itemText = controlInfo.Items[i];
                                    if (itemText != null)
                                        comboBox.Items[i] = itemText;
                                }
                            } else if (elem is WinForms.ListBox listBox) {
                                for (int i = 0,
                                    cnt = Math.Min(listBox.Items.Count, controlInfo.Items.Count);
                                    i < cnt;
                                    i++) {
                                    string itemText = controlInfo.Items[i];
                                    if (itemText != null)
                                        listBox.Items[i] = itemText;
                                }
                            } else if (elem is WinForms.ListView listView) {
                                for (int i = 0,
                                    cnt = Math.Min(listView.Items.Count, controlInfo.Items.Count);
                                    i < cnt;
                                    i++) {
                                    string itemText = controlInfo.Items[i];
                                    if (itemText != null)
                                        listView.Items[i].Text = itemText;
                                }
                            }
                        }
                    }

                    // start processing nested items
                    if (elem is WinForms.MenuStrip menuStrip) {
                        // start processing menu items
                        TranslateWinControls(menuStrip.Items, toolTip, controlInfoDict);
                    } else if (elem is WinForms.ToolStrip toolStrip) {
                        // start processing toolbar items
                        TranslateWinControls(toolStrip.Items, toolTip, controlInfoDict);
                    } else if (elem is WinForms.DataGridView dataGridView) {
                        // start processing table columns
                        TranslateWinControls(dataGridView.Columns, toolTip, controlInfoDict);
                    } else if (elem is WinForms.ListView listView) {
                        // start processing of columns and list groups
                        TranslateWinControls(listView.Columns, toolTip, controlInfoDict);
                        TranslateWinControls(listView.Groups, toolTip, controlInfoDict);
                    }

                    // start processing child elements
                    if (control.HasChildren)
                        TranslateWinControls(control.Controls, toolTip, controlInfoDict);
                } else if (elem is WinForms.ToolStripItem toolStripItem) {
                    // processing a menu item or toolbar item
                    if (controlInfoDict.TryGetValue(toolStripItem.Name, out controlInfo)) {
                        if (controlInfo.Text != null)
                            toolStripItem.Text = controlInfo.Text;
                        if (controlInfo.ToolTip != null)
                            toolStripItem.ToolTipText = controlInfo.ToolTip;
                    }

                    // start processing submenu items
                    if (elem is WinForms.ToolStripDropDownItem dropDownItem && dropDownItem.HasDropDownItems)
                        TranslateWinControls(dropDownItem.DropDownItems, toolTip, controlInfoDict);
                } else if (elem is WinForms.DataGridViewColumn column) {
                    // table column processing
                    if (controlInfoDict.TryGetValue(column.Name, out controlInfo) && controlInfo.Text != null)
                        column.HeaderText = controlInfo.Text;
                } else if (elem is WinForms.ColumnHeader columnHeader) {
                    // list column processing
                    if (controlInfoDict.TryGetValue(columnHeader.Name, out controlInfo) && controlInfo.Text != null)
                        columnHeader.Text = controlInfo.Text;
                } else if (elem is WinForms.ListViewGroup listViewGroup) {
                    // list group processing
                    if (controlInfoDict.TryGetValue(listViewGroup.Name, out controlInfo) && controlInfo.Text != null)
                        listViewGroup.Header = controlInfo.Text;
                }
            }
        }

        /// <summary>
        /// Control information
        /// </summary>
        private class ControlInfo {
            /// <summary>
            /// Constructor
            /// </summary>
            public ControlInfo() {
                Text = null;
                ToolTip = null;
                Props = null;
                Items = null;
            }

            /// <summary>
            /// Get a list of items
            /// </summary>
            public List<string> Items { get; private set; }

            /// <summary>
            /// Get property dictionary, excluding text and hint
            /// </summary>
            public Dictionary<string, string> Props { get; private set; }

            /// <summary>
            /// Get or set text
            /// </summary>
            public string Text { get; set; }

            /// <summary>
            /// Get or set tooltip
            /// </summary>
            public string ToolTip { get; set; }

            /// <summary>
            /// Set the value of the list item, initializing the list if necessary
            /// </summary>
            public void SetItem(int index, string val) {
                if (Items == null)
                    Items = new List<string>();

                if (index < Items.Count) {
                    Items[index] = val;
                } else {
                    while (Items.Count < index)
                        Items.Add(null);
                    Items.Add(val);
                }
            }

            /// <summary>
            /// Set the value of the property, initializing the dictionary if necessary.
            /// </summary>
            public void SetProp(string name, string val) {
                if (Props == null)
                    Props = new Dictionary<string, string>();
                Props[name] = val;
            }
        }
    }
}