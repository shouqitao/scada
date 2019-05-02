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
 * Module   : AddressBook
 * Summary  : Address book form
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2016
 */

using Scada.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Windows.Forms;

namespace Scada.Comm.Devices.AB {
    /// <inheritdoc />
    /// <summary>
    /// Address book form
    /// <para>Address Book Form</para>
    /// </summary>
    public partial class FrmAddressBook : Form {
        private AppDirs appDirs; // application directories
        private AddressBook addressBook; // The address book
        private bool modified; // sign change address book
        private TreeNode rootNode; // tree root


        /// <inheritdoc />
        /// <summary>
        /// Constructor restricting form creation without parameters
        /// </summary>
        private FrmAddressBook() {
            InitializeComponent();

            appDirs = null;
            addressBook = new AddressBook();
            modified = false;
            rootNode = treeView.Nodes["rootNode"];
            rootNode.Tag = addressBook;
        }


        /// <summary>
        /// Get or set the address book change sign
        /// </summary>
        private bool Modified {
            get { return modified; }
            set {
                modified = value;
                btnSave.Enabled = modified;
            }
        }


        /// <summary>
        /// Build an address book tree
        /// </summary>
        private void BuildTree() {
            try {
                treeView.BeginUpdate();
                rootNode.Nodes.Clear();

                foreach (var contactGroup in addressBook.ContactGroups)
                    rootNode.Nodes.Add(CreateContactGroupNode(contactGroup));

                rootNode.Expand();

                if (rootNode.Nodes.Count > 0)
                    treeView.SelectedNode = rootNode.Nodes[0];
            } finally {
                treeView.EndUpdate();
            }
        }

        /// <summary>
        /// Create a tree node for a group of contacts
        /// </summary>
        private TreeNode CreateContactGroupNode(AddressBook.ContactGroup contactGroup) {
            string imageKey = contactGroup.Contacts.Count > 0 ? "folder_open.png" : "folder_closed.png";
            TreeNode contactGroupNode = TreeViewUtils.CreateNode(contactGroup, imageKey, true);

            foreach (var contact in contactGroup.Contacts)
                contactGroupNode.Nodes.Add(CreateContactNode(contact));

            return contactGroupNode;
        }

        /// <summary>
        /// Create tree node for contact
        /// </summary>
        private TreeNode CreateContactNode(AddressBook.Contact contact) {
            TreeNode contactNode = TreeViewUtils.CreateNode(contact, "contact.png", true);

            foreach (var contactRecord in contact.ContactRecords) {
                if (contactRecord is AddressBook.PhoneNumber)
                    contactNode.Nodes.Add(CreatePhoneNumberNode(contactRecord));
                else if (contactRecord is AddressBook.Email)
                    contactNode.Nodes.Add(CreateEmailNode(contactRecord));
            }

            return contactNode;
        }

        /// <summary>
        /// Create a tree node for a phone number
        /// </summary>
        private TreeNode CreatePhoneNumberNode(AddressBook.ContactRecord phoneNumber) {
            return TreeViewUtils.CreateNode(phoneNumber, "phone.png");
        }

        /// <summary>
        /// Create a tree node for an email address
        /// </summary>
        private TreeNode CreateEmailNode(AddressBook.ContactRecord email) {
            return TreeViewUtils.CreateNode(email, "email.png");
        }

        /// <summary>
        /// Find the index of the insert element to maintain the orderliness of the list
        /// </summary>
        private int FindInsertIndex<T>(List<T> list, int currentIndex, out bool duplicated) {
            if (list.Count < 2) {
                duplicated = false;
                return currentIndex;
            } else {
                var item = list[currentIndex];

                list.RemoveAt(currentIndex);
                int newIndex = list.BinarySearch(item);
                list.Insert(currentIndex, item);

                if (newIndex >= 0) {
                    duplicated = true;
                    return newIndex;
                } else {
                    duplicated = false;
                    return ~newIndex;
                }
            }
        }

        /// <summary>
        /// Set button availability
        /// </summary>
        private void SetButtonsEnabled() {
            object selObj = treeView.GetSelectedObject();
            btnAddContact.Enabled = btnEdit.Enabled = btnDelete.Enabled =
                selObj is AddressBook.AddressBookItem;
            btnAddPhoneNumber.Enabled = btnAddEmail.Enabled =
                selObj is AddressBook.Contact || selObj is AddressBook.ContactRecord;
        }

        /// <summary>
        /// Check the format of the email address
        /// </summary>
        private bool CheckEmail(string email) {
            try {
                var m = new MailAddress(email);
                return true;
            } catch {
                return false;
            }
        }


        /// <summary>
        /// Display the form modally
        /// </summary>
        public static void ShowDialog(AppDirs appDirs) {
            if (appDirs == null)
                throw new ArgumentNullException("appDirs");

            var frmAddressBook = new FrmAddressBook();
            frmAddressBook.appDirs = appDirs;
            frmAddressBook.ShowDialog();
        }


        private void FrmAddressBook_Load(object sender, EventArgs e) {
            // library localization
            string errMsg;
            if (!Localization.UseRussian) {
                if (Localization.LoadDictionaries(appDirs.LangDir, "AddressBook", out errMsg)) {
                    Translator.TranslateForm(this, "Scada.Comm.Devices.AB.FrmAddressBook");
                    AbPhrases.Init();
                    rootNode.Text = AbPhrases.AddressBookNode;
                } else {
                    ScadaUiUtils.ShowError(errMsg);
                }
            }

            // loading address book
            string fileName = appDirs.ConfigDir + AddressBook.DefFileName;
            if (File.Exists(fileName) && !addressBook.Load(fileName, out errMsg))
                ScadaUiUtils.ShowError(errMsg);
            Modified = false;

            // output address book tree
            BuildTree();

            // setting the availability of buttons
            SetButtonsEnabled();
        }

        private void FrmAddressBook_FormClosing(object sender, FormClosingEventArgs e) {
            if (Modified) {
                var result = MessageBox.Show(AbPhrases.SavePhonebookConfirm,
                    CommonPhrases.QuestionCaption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                switch (result) {
                    case DialogResult.Yes:
                        string errMsg;
                        if (!addressBook.Save(appDirs.ConfigDir + AddressBook.DefFileName, out errMsg)) {
                            ScadaUiUtils.ShowError(errMsg);
                            e.Cancel = true;
                        }

                        break;
                    case DialogResult.No:
                        break;
                    default:
                        e.Cancel = true;
                        break;
                }
            }
        }


        private void btnAddContactGroup_Click(object sender, EventArgs e) {
            // add contact group
            var contactGroup = new AddressBook.ContactGroup(AbPhrases.NewContactGroup);
            var contactGroupNode = CreateContactGroupNode(contactGroup);

            treeView.Add(rootNode, contactGroupNode);
            contactGroupNode.BeginEdit();
            Modified = true;
        }

        private void btnAddContact_Click(object sender, EventArgs e) {
            // adding contact
            TreeNode contactGroupNode = treeView.SelectedNode?.FindClosest(typeof(AddressBook.ContactGroup));
            if (contactGroupNode != null) {
                var contact = new AddressBook.Contact(AbPhrases.NewContact);
                var contactNode = CreateContactNode(contact);

                treeView.Add(contactGroupNode, contactNode);
                contactNode.BeginEdit();
                Modified = true;
            }
        }

        private void btnAddPhoneNumber_Click(object sender, EventArgs e) {
            // add phone number
            TreeNode contactNode = treeView.SelectedNode?.FindClosest(typeof(AddressBook.Contact));
            if (contactNode != null) {
                var phoneNumber = new AddressBook.PhoneNumber(AbPhrases.NewPhoneNumber);
                var phoneNumberNode = CreatePhoneNumberNode(phoneNumber);

                treeView.Add(contactNode, phoneNumberNode);
                phoneNumberNode.BeginEdit();
                Modified = true;
            }
        }

        private void btnAddEmail_Click(object sender, EventArgs e) {
            // add email address
            TreeNode contactNode = treeView.SelectedNode?.FindClosest(typeof(AddressBook.Contact));
            if (contactNode != null) {
                var email = new AddressBook.Email(AbPhrases.NewEmail);
                var emailNode = CreateEmailNode(email);

                treeView.Add(contactNode, emailNode);
                emailNode.BeginEdit();
                Modified = true;
            }
        }

        private void btnEdit_Click(object sender, EventArgs e) {
            // switching tree node editing mode
            var selNode = treeView.SelectedNode;
            if (selNode.IsEditing)
                selNode.EndEdit(false);
            else
                selNode.BeginEdit();
        }

        private void btnDelete_Click(object sender, EventArgs e) {
            // delete selected object
            if (treeView.GetSelectedObject() is AddressBook.AddressBookItem) {
                treeView.RemoveSelectedNode();
                Modified = true;
            }
        }


        private void treeView_AfterSelect(object sender, TreeViewEventArgs e) {
            // setting the availability of buttons
            SetButtonsEnabled();
        }

        private void treeView_BeforeExpand(object sender, TreeViewCancelEventArgs e) {
            // set the icon if the group was expanded
            if (e.Node.Tag is AddressBook.ContactGroup)
                e.Node.SetImageKey("folder_open.png");
        }

        private void treeView_BeforeCollapse(object sender, TreeViewCancelEventArgs e) {
            // set icon if group was minimized
            if (e.Node.Tag is AddressBook.ContactGroup)
                e.Node.SetImageKey("folder_closed.png");
        }

        private void treeView_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e) {
            // prohibition of editing the root node of the tree
            if (e.Node == rootNode)
                e.CancelEdit = true;
        }

        private void treeView_AfterLabelEdit(object sender, NodeLabelEditEventArgs e) {
            // receiving changes after the node has been edited
            if (e.Label != null /*editing canceled*/) {
                var bookItem = e.Node.Tag as AddressBook.AddressBookItem;

                if (bookItem != null) {
                    string oldVal = bookItem.Value;
                    string newVal = e.Label;

                    if (newVal == "") {
                        e.CancelEdit = true;
                        ScadaUiUtils.ShowError(AbPhrases.EmptyValueNotAllowed);
                        e.Node.BeginEdit();
                    } else if (!oldVal.Equals(newVal, StringComparison.Ordinal)) {
                        // setting a new value
                        bookItem.Value = newVal;

                        // defining a new node index to keep order, and checking the value
                        IList list = bookItem.Parent.Children;
                        int curInd = e.Node.Index;
                        int newInd = curInd;
                        bool duplicated;
                        var errMsg = "";

                        if (bookItem is AddressBook.ContactGroup) {
                            newInd = FindInsertIndex<AddressBook.ContactGroup>(
                                (List<AddressBook.ContactGroup>) list, curInd, out duplicated);
                            if (duplicated)
                                errMsg = AbPhrases.ContactGroupExists;
                        } else if (bookItem is AddressBook.Contact) {
                            newInd = FindInsertIndex<AddressBook.Contact>(
                                (List<AddressBook.Contact>) list, curInd, out duplicated);
                            if (duplicated)
                                errMsg = AbPhrases.ContactExists;
                        } else if (bookItem is AddressBook.ContactRecord) {
                            newInd = FindInsertIndex<AddressBook.ContactRecord>(
                                (List<AddressBook.ContactRecord>) list, curInd, out duplicated);

                            if (bookItem is AddressBook.PhoneNumber) {
                                if (duplicated)
                                    errMsg = AbPhrases.PhoneNumberExists;
                            } else {
                                if (duplicated)
                                    errMsg = AbPhrases.EmailExists;
                                if (!CheckEmail(newVal))
                                    errMsg = AbPhrases.IncorrectEmail;
                            }
                        }

                        if (errMsg != "") {
                            // returning the old value
                            bookItem.Value = newVal;
                            e.CancelEdit = true;
                            ScadaUiUtils.ShowError(errMsg);
                            e.Node.BeginEdit();
                        } else if (newInd != curInd) {
                            // moving the node to keep order
                            BeginInvoke(new Action(() => { treeView.MoveSelectedNode(newInd); }));
                        }

                        Modified = true;
                    }
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e) {
            // save address book
            string errMsg;
            if (addressBook.Save(appDirs.ConfigDir + AddressBook.DefFileName, out errMsg))
                Modified = false;
            else
                ScadaUiUtils.ShowError(errMsg);
        }
    }
}