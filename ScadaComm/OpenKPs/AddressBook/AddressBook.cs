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
 * Summary  : Address book
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2015
 * Modified : 2015
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace Scada.Comm.Devices.AB {
    /// <summary>
    /// Address book
    /// <para>The address book</para>
    /// </summary>
    public class AddressBook : ITreeNode {
        /// <summary>
        /// Base class of reference elements
        /// </summary>
        public abstract class AddressBookItem : IComparable<AddressBookItem>, ITreeNode {
            /// <summary>
            /// Constructor
            /// </summary>
            public AddressBookItem() {
                Value = "";
                Parent = null;
                Children = null;
            }

            /// <summary>
            /// Get item type sorting order
            /// </summary>
            public abstract int Order { get; }

            /// <summary>
            /// Get or set item value
            /// </summary>
            public string Value { get; set; }

            /// <summary>
            /// Get or set parent node
            /// </summary>
            public ITreeNode Parent { get; set; }

            /// <summary>
            /// Get a list of child nodes
            /// </summary>
            public IList Children { get; protected set; }

            /// <summary>
            /// Get a string representation of the object
            /// </summary>
            public override string ToString() {
                return Value;
            }

            /// <summary>
            /// Compare this object with the specified
            /// </summary>
            public int CompareTo(AddressBookItem other) {
                if (other == null) {
                    return 1;
                } else {
                    int comp = Order.CompareTo(other.Order);
                    return comp == 0 ? Value.CompareTo(other.Value) : comp;
                }
            }
        }

        /// <summary>
        /// Contact group
        /// </summary>
        public class ContactGroup : AddressBookItem, ITreeNode {
            /// <summary>
            /// Constructor
            /// </summary>
            public ContactGroup()
                : this("") { }

            /// <summary>
            /// Constructor
            /// </summary>
            public ContactGroup(string name)
                : base() {
                Name = name ?? "";
                Contacts = new List<Contact>();
                Children = Contacts;
            }

            /// <summary>
            /// Get or set group name
            /// </summary>
            public string Name {
                get { return Value; }
                set { Value = value; }
            }

            /// <summary>
            /// Get contacts sorted by name
            /// </summary>
            public List<Contact> Contacts { get; private set; }

            /// <summary>
            /// Get item type sorting order
            /// </summary>
            public override int Order {
                get { return 0; }
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Contact
        /// </summary>
        public class Contact : AddressBookItem, ITreeNode {
            private List<string> phoneNumbers; // phone numbers
            private List<string> emails; // email addresses of mail

            /// <inheritdoc />
            /// <summary>
            /// Constructor
            /// </summary>
            public Contact()
                : this("") { }

            /// <inheritdoc />
            /// <summary>
            /// Constructor
            /// </summary>
            public Contact(string name)
                : base() {
                phoneNumbers = null;
                emails = null;

                Name = name ?? "";
                ContactRecords = new List<ContactRecord>();
                Children = ContactRecords;
            }

            /// <summary>
            /// Get or set contact name
            /// </summary>
            public string Name {
                get { return Value; }
                set { Value = value; }
            }

            /// <summary>
            /// Get contact entries in ascending order
            /// </summary>
            public List<ContactRecord> ContactRecords { get; private set; }

            /// <summary>
            /// Get phone numbers
            /// </summary>
            public List<string> PhoneNumbers {
                get {
                    if (phoneNumbers == null)
                        FillPhoneNumbers();
                    return phoneNumbers;
                }
            }

            /// <summary>
            /// Get email addresses
            /// </summary>
            public List<string> Emails {
                get {
                    if (emails == null)
                        FillEmails();
                    return emails;
                }
            }

            /// <summary>
            /// Get item type sorting order
            /// </summary>
            public override int Order {
                get { return 1; }
            }

            /// <summary>
            /// Fill in the list of phone numbers
            /// </summary>
            public void FillPhoneNumbers() {
                if (phoneNumbers == null)
                    phoneNumbers = new List<string>();
                else
                    phoneNumbers.Clear();

                foreach (var rec in ContactRecords) {
                    if (rec is PhoneNumber)
                        phoneNumbers.Add(rec.Value);
                }
            }

            /// <summary>
            /// Fill in the list of email addresses
            /// </summary>
            public void FillEmails() {
                if (emails == null)
                    emails = new List<string>();
                else
                    emails.Clear();

                foreach (var rec in ContactRecords) {
                    if (rec is Email)
                        emails.Add(rec.Value);
                }
            }
        }

        /// <summary>
        /// Contact record
        /// </summary>
        public abstract class ContactRecord : AddressBookItem, ITreeNode {
            /// <summary>
            /// Constructor
            /// </summary>
            public ContactRecord()
                : base() { }
        }

        /// <inheritdoc />
        /// <summary>
        /// Telephone number
        /// </summary>
        public class PhoneNumber : ContactRecord {
            /// <inheritdoc />
            /// <summary>
            /// Constructor
            /// </summary>
            public PhoneNumber()
                : this("") { }

            /// <inheritdoc />
            /// <summary>
            /// Constructor
            /// </summary>
            public PhoneNumber(string value)
                : base() {
                Value = value;
            }

            /// <inheritdoc />
            /// <summary>
            /// Get sorting field type
            /// </summary>
            public override int Order {
                get { return 2; }
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// E-mail address
        /// </summary>
        public class Email : ContactRecord {
            /// <inheritdoc />
            /// <summary>
            /// Constructor
            /// </summary>
            public Email()
                : this("") { }

            /// <inheritdoc />
            /// <summary>
            /// Constructor
            /// </summary>
            public Email(string value)
                : base() {
                Value = value;
            }

            /// <inheritdoc />
            /// <summary>
            /// Get sorting field type
            /// </summary>
            public override int Order {
                get { return 3; }
            }
        }


        /// <summary>
        /// Default Address Book File Name
        /// </summary>
        public const string DefFileName = "AddressBook.xml";

        private List<Contact> allContacts; // all contacts in ascending order


        /// <summary>
        /// Constructor
        /// </summary>
        public AddressBook() {
            allContacts = null;
            ContactGroups = new List<ContactGroup>();
        }


        /// <summary>
        /// Get contact groups sorted by name
        /// </summary>
        public List<ContactGroup> ContactGroups { get; private set; }

        /// <summary>
        /// Get all contacts sorted by name
        /// </summary>
        public List<Contact> AllContacts {
            get {
                if (allContacts == null)
                    FillAllContacts();
                return allContacts;
            }
        }

        /// <summary>
        /// Get or set parent node - it is always null
        /// </summary>
        ITreeNode ITreeNode.Parent {
            get { return null; }
            set {
                // incorrect call
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Get a list of child nodes
        /// </summary>
        IList ITreeNode.Children {
            get { return ContactGroups; }
        }


        /// <summary>
        /// Download address book from file
        /// </summary>
        public bool Load(string fileName, out string errMsg) {
            try {
                // address book cleaning
                ContactGroups.Clear();

                // loading address book
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(fileName);

                var contactGroupNodes = xmlDoc.DocumentElement.SelectNodes("ContactGroup");

                foreach (XmlElement contactGroupElem in contactGroupNodes) {
                    var contactGroup = new ContactGroup(contactGroupElem.GetAttribute("name")) {Parent = this};

                    var contactNodes = contactGroupElem.SelectNodes("Contact");
                    foreach (XmlElement contactElem in contactNodes) {
                        var contact = new Contact(contactElem.GetAttribute("name")) {Parent = contactGroup};

                        var phoneNumberNodes = contactElem.SelectNodes("PhoneNumber");
                        foreach (XmlElement phoneNumberElem in phoneNumberNodes)
                            contact.ContactRecords.Add(
                                new PhoneNumber(phoneNumberElem.InnerText) {Parent = contact});

                        var emailNodes = contactElem.SelectNodes("Email");
                        foreach (XmlElement emailElem in emailNodes)
                            contact.ContactRecords.Add(
                                new Email(emailElem.InnerText) {Parent = contact});

                        contact.ContactRecords.Sort();
                        contactGroup.Contacts.Add(contact);
                    }

                    contactGroup.Contacts.Sort();
                    ContactGroups.Add(contactGroup);
                }

                ContactGroups.Sort();

                errMsg = "";
                return true;
            } catch (Exception ex) {
                errMsg = AbPhrases.LoadAddressBookError + ":" + Environment.NewLine + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Save Address Book in File
        /// </summary>
        public bool Save(string fileName, out string errMsg) {
            try {
                var xmlDoc = new XmlDocument();

                var xmlDecl = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
                xmlDoc.AppendChild(xmlDecl);

                var rootElem = xmlDoc.CreateElement("AddressBook");
                xmlDoc.AppendChild(rootElem);

                foreach (var contactGroup in ContactGroups) {
                    var contactGroupElem = xmlDoc.CreateElement("ContactGroup");
                    contactGroupElem.SetAttribute("name", contactGroup.Name);
                    rootElem.AppendChild(contactGroupElem);

                    foreach (var contact in contactGroup.Contacts) {
                        var contactElem = xmlDoc.CreateElement("Contact");
                        contactElem.SetAttribute("name", contact.Name);

                        foreach (var contactRecord in contact.ContactRecords) {
                            if (contactRecord is PhoneNumber)
                                contactElem.AppendElem("PhoneNumber", contactRecord);
                            else if (contactRecord is Email)
                                contactElem.AppendElem("Email", contactRecord);
                        }

                        contactGroupElem.AppendChild(contactElem);
                    }
                }

                xmlDoc.Save(fileName);
                errMsg = "";
                return true;
            } catch (Exception ex) {
                errMsg = AbPhrases.SaveAddressBookError + ":" + Environment.NewLine + ex.Message;
                return false;
            }
        }


        /// <summary>
        /// Find a group of contacts
        /// </summary>
        public ContactGroup FindContactGroup(string name) {
            int i = ContactGroups.BinarySearch(new ContactGroup(name));
            return i >= 0 ? ContactGroups[i] : null;
        }

        /// <summary>
        /// Find a contact
        /// </summary>
        public Contact FindContact(string name) {
            int i = AllContacts.BinarySearch(new Contact(name));
            return i >= 0 ? AllContacts[i] : null;
        }

        /// <summary>
        /// Fill in the list of all contacts
        /// </summary>
        public void FillAllContacts() {
            if (allContacts == null)
                allContacts = new List<Contact>();
            else
                allContacts.Clear();

            foreach (var contactGroup in ContactGroups)
            foreach (var contact in contactGroup.Contacts)
                allContacts.Add(contact);

            allContacts.Sort();
        }

        /// <summary>
        /// Get a string representation of the object
        /// </summary>
        public override string ToString() {
            return "Address book";
        }
    }
}