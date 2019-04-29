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
 * Module   : SCADA-Administrator
 * Summary  : Settings of interaction with remote servers
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using Scada;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace ScadaAdmin {
    /// <summary>
    /// Settings of interaction with remote servers
    /// <para>Remote server interaction settings</para>
    /// </summary>
    public class ServersSettings {
        /// <summary>
        /// Default configuration directory
        /// </summary>
        private const string DefConfigDir = @"C:\SCADA\";

        /// <summary>
        /// Default configuration archive
        /// </summary>
        private const string DefConfigArc = @"C:\SCADA\config.zip";

        /// <summary>
        /// Remote server connection settings
        /// <para>Settings for connecting to a remote server</para>
        /// </summary>
        public class ConnectionSettings {
            /// <summary>
            /// Constructor
            /// </summary>
            public ConnectionSettings() {
                SetToDefault();
            }

            /// <summary>
            /// Get or set the name
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Get or set the computer name or IP address
            /// </summary>
            public string Host { get; set; }

            /// <summary>
            /// Get or set TCP port
            /// </summary>
            public int Port { get; set; }

            /// <summary>
            /// Get or set username
            /// </summary>
            public string Username { get; set; }

            /// <summary>
            /// Get or set user password
            /// </summary>
            public string Password { get; set; }

            /// <summary>
            /// Get or set system instance names
            /// </summary>
            public string ScadaInstance { get; set; }

            /// <summary>
            /// Get or set private key
            /// </summary>
            public byte[] SecretKey { get; set; }

            /// <summary>
            /// Set default settings
            /// </summary>
            private void SetToDefault() {
                Name = "";
                Host = "";
                Port = 10002;
                Username = "admin";
                Password = "";
                ScadaInstance = "Default";
                SecretKey = new byte[0];
            }

            /// <summary>
            /// Load settings from XML node
            /// </summary>
            public void LoadFromXml(XmlNode xmlNode) {
                if (xmlNode == null)
                    throw new ArgumentNullException(nameof(xmlNode));

                Name = xmlNode.GetChildAsString("Name");
                Host = xmlNode.GetChildAsString("Host");
                Port = xmlNode.GetChildAsInt("Port", 10002);
                Username = xmlNode.GetChildAsString("Username", "admin");
                Password = xmlNode.GetChildAsString("Password");
                ScadaInstance = xmlNode.GetChildAsString("ScadaInstance");
                SecretKey = ScadaUtils.HexToBytes(xmlNode.GetChildAsString("SecretKey"));
            }

            /// <summary>
            /// Save Settings to XML Node
            /// </summary>
            public void SaveToXml(XmlElement xmlElem) {
                if (xmlElem == null)
                    throw new ArgumentNullException(nameof(xmlElem));

                xmlElem.AppendElem("Name", Name);
                xmlElem.AppendElem("Host", Host);
                xmlElem.AppendElem("Port", Port);
                xmlElem.AppendElem("Username", Username);
                xmlElem.AppendElem("Password", Password);
                xmlElem.AppendElem("ScadaInstance", ScadaInstance);
                xmlElem.AppendElem("SecretKey", ScadaUtils.BytesToHex(SecretKey));
            }
        }

        /// <summary>
        /// Settings of downloading configuration
        /// <para>Configuration Download Settings</para>
        /// </summary>
        public class DownloadSettings {
            /// <summary>
            /// Constructor
            /// </summary>
            public DownloadSettings() {
                SetToDefault();
            }

            /// <summary>
            /// Get or set save sign to directory
            /// </summary>
            public bool SaveToDir { get; set; }

            /// <summary>
            /// Get or set a directory to save the configuration.
            /// </summary>
            public string DestDir { get; set; }

            /// <summary>
            /// Get or set archive file name to save configuration
            /// </summary>
            public string DestFile { get; set; }

            /// <summary>
            /// Get or set the sign of downloading files specific to an instance of the system.
            /// </summary>
            public bool IncludeSpecificFiles { get; set; }

            /// <summary>
            /// Get or set the sign of launching the import of the configuration database after downloading
            /// </summary>
            public bool ImportBase { get; set; }

            /// <summary>
            /// Set default settings
            /// </summary>
            private void SetToDefault() {
                SaveToDir = true;
                DestDir = DefConfigDir;
                DestFile = DefConfigArc;
                IncludeSpecificFiles = true;
                ImportBase = true;
            }

            /// <summary>
            /// Load settings from XML node
            /// </summary>
            public void LoadFromXml(XmlNode xmlNode) {
                if (xmlNode == null)
                    throw new ArgumentNullException(nameof(xmlNode));

                SaveToDir = xmlNode.GetChildAsBool("SaveToDir", true);
                DestDir = ScadaUtils.NormalDir(xmlNode.GetChildAsString("DestDir", DefConfigDir));
                DestFile = xmlNode.GetChildAsString("DestFile", DefConfigArc);
                IncludeSpecificFiles = xmlNode.GetChildAsBool("IncludeSpecificFiles", true);
                ImportBase = xmlNode.GetChildAsBool("ImportBase", true);
            }

            /// <summary>
            /// Save Settings to XML Node
            /// </summary>
            public void SaveToXml(XmlElement xmlElem) {
                if (xmlElem == null)
                    throw new ArgumentNullException(nameof(xmlElem));

                xmlElem.AppendElem("SaveToDir", SaveToDir);
                xmlElem.AppendElem("DestDir", DestDir);
                xmlElem.AppendElem("DestFile", DestFile);
                xmlElem.AppendElem("IncludeSpecificFiles", IncludeSpecificFiles);
                xmlElem.AppendElem("ImportBase", ImportBase);
            }
        }

        /// <summary>
        /// Settings of uploading configuration
        /// <para>Configuration Transfer Settings</para>
        /// </summary>
        public class UploadSettings {
            /// <summary>
            /// Constructor
            /// </summary>
            public UploadSettings() {
                SelectedFiles = new List<string>();
                SetToDefault();
            }

            /// <summary>
            /// Get or set the sign of transfer from the directory
            /// </summary>
            public bool GetFromDir { get; set; }

            /// <summary>
            /// Get or set configuration directory
            /// </summary>
            public string SrcDir { get; set; }

            /// <summary>
            /// Get selected configuration files for transfer
            /// </summary>
            public List<string> SelectedFiles { get; private set; }

            /// <summary>
            /// Get or set archive file name to transfer
            /// </summary>
            public string SrcFile { get; set; }

            /// <summary>
            /// Retrieve or set a cleanup flag for files specific to an instance of the system.
            /// </summary>
            public bool ClearSpecificFiles { get; set; }

            /// <summary>
            /// Set default settings
            /// </summary>
            private void SetToDefault() {
                GetFromDir = true;
                SrcDir = DefConfigDir;
                SelectedFiles.Clear();
                SrcFile = DefConfigArc;
                ClearSpecificFiles = true;
            }

            /// <summary>
            /// Load settings from XML node
            /// </summary>
            public void LoadFromXml(XmlNode xmlNode) {
                if (xmlNode == null)
                    throw new ArgumentNullException(nameof(xmlNode));

                GetFromDir = xmlNode.GetChildAsBool("GetFromDir", true);
                SrcDir = ScadaUtils.NormalDir(xmlNode.GetChildAsString("SrcDir", DefConfigDir));

                SelectedFiles.Clear();
                var selectedFilesNode = xmlNode.SelectSingleNode("SelectedFiles");
                if (selectedFilesNode != null) {
                    var pathNodeList = selectedFilesNode.SelectNodes("Path");
                    foreach (XmlNode pathNode in pathNodeList) {
                        SelectedFiles.Add(pathNode.InnerText);
                    }
                }

                SrcFile = xmlNode.GetChildAsString("SrcFile", DefConfigArc);
                ClearSpecificFiles = xmlNode.GetChildAsBool("ClearSpecificFiles", true);
            }

            /// <summary>
            /// Save Settings to XML Node
            /// </summary>
            public void SaveToXml(XmlElement xmlElem) {
                if (xmlElem == null)
                    throw new ArgumentNullException("xmlElem");

                xmlElem.AppendElem("GetFromDir", GetFromDir);
                xmlElem.AppendElem("SrcDir", SrcDir);

                XmlElement selectedFilesElem = xmlElem.AppendElem("SelectedFiles");
                foreach (string path in SelectedFiles) {
                    selectedFilesElem.AppendElem("Path", path);
                }

                xmlElem.AppendElem("SrcFile", SrcFile);
                xmlElem.AppendElem("ClearSpecificFiles", ClearSpecificFiles);
            }
        }

        /// <summary>
        /// Settings of interaction with a remote server
        /// <para>Remote server interaction settings</para>
        /// </summary>
        public class ServerSettings {
            /// <summary>
            /// Constructor
            /// </summary>
            public ServerSettings() {
                Connection = new ConnectionSettings();
                Download = new DownloadSettings();
                Upload = new UploadSettings();
            }

            /// <summary>
            /// Get server connection settings
            /// </summary>
            public ConnectionSettings Connection { get; private set; }

            /// <summary>
            /// Get configuration download settings
            /// </summary>
            public DownloadSettings Download { get; private set; }

            /// <summary>
            /// Get configuration transfer settings
            /// </summary>
            public UploadSettings Upload { get; private set; }

            /// <summary>
            /// Load settings from XML node
            /// </summary>
            public void LoadFromXml(XmlNode xmlNode) {
                if (xmlNode == null)
                    throw new ArgumentNullException(nameof(xmlNode));

                var connectionNode = xmlNode.SelectSingleNode("Connection");
                if (connectionNode != null)
                    Connection.LoadFromXml(connectionNode);

                var downloadNode = xmlNode.SelectSingleNode("Download");
                if (downloadNode != null)
                    Download.LoadFromXml(downloadNode);

                var uploadNode = xmlNode.SelectSingleNode("Upload");
                if (uploadNode != null)
                    Upload.LoadFromXml(uploadNode);
            }

            /// <summary>
            /// Save Settings to XML Node
            /// </summary>
            public void SaveToXml(XmlElement xmlElem) {
                if (xmlElem == null)
                    throw new ArgumentNullException(nameof(xmlElem));

                Connection.SaveToXml(xmlElem.AppendElem("Connection"));
                Download.SaveToXml(xmlElem.AppendElem("Download"));
                Upload.SaveToXml(xmlElem.AppendElem("Upload"));
            }

            /// <summary>
            /// Get a string representation of the object
            /// </summary>
            public override string ToString() {
                return Connection.Name;
            }
        }


        /// <summary>
        /// Default Settings File Name
        /// </summary>
        public const string DefFileName = "RemoteServers.xml";


        /// <summary>
        /// Constructor
        /// </summary>
        public ServersSettings() {
            Servers = new SortedList<string, ServerSettings>();
        }


        /// <summary>
        /// Get a list of server interaction settings, the key is the name of the connection
        /// </summary>
        public SortedList<string, ServerSettings> Servers { get; private set; }


        /// <summary>
        /// Load settings from file
        /// </summary>
        public bool Load(string fileName, out string errMsg) {
            // setting default values
            Servers.Clear();

            try {
                if (!File.Exists(fileName))
                    throw new FileNotFoundException(string.Format(CommonPhrases.NamedFileNotFound, fileName));

                var xmlDoc = new XmlDocument();
                xmlDoc.Load(fileName);

                var remoteServerNodeList = xmlDoc.DocumentElement.SelectNodes("RemoteServer");
                foreach (XmlNode remoteServerNode in remoteServerNodeList) {
                    var serverSettings = new ServerSettings();
                    serverSettings.LoadFromXml(remoteServerNode);
                    Servers[serverSettings.Connection.Name] = serverSettings;
                }

                errMsg = "";
                return true;
            } catch (Exception ex) {
                errMsg = AppPhrases.LoadServersSettingsError + ":" + Environment.NewLine + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Save settings to file
        /// </summary>
        public bool Save(string fileName, out string errMsg) {
            try {
                var xmlDoc = new XmlDocument();

                var xmlDecl = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
                xmlDoc.AppendChild(xmlDecl);

                var rootElem = xmlDoc.CreateElement("RemoteServers");
                xmlDoc.AppendChild(rootElem);

                foreach (var serverSettings in Servers.Values) {
                    serverSettings.SaveToXml(rootElem.AppendElem("RemoteServer"));
                }

                string bakName = fileName + ".bak";
                File.Copy(fileName, bakName, true);
                xmlDoc.Save(fileName);

                errMsg = "";
                return true;
            } catch (Exception ex) {
                errMsg = AppPhrases.SaveServersSettingsError + ":" + Environment.NewLine + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Get the names of existing connections
        /// </summary>
        public HashSet<string> GetExistingNames(string exceptName = null) {
            var existingNames = new HashSet<string>();

            foreach (var serverSettings in Servers.Values) {
                if (!string.Equals(serverSettings.Connection.Name, exceptName, StringComparison.Ordinal))
                    existingNames.Add(serverSettings.Connection.Name);
            }

            return existingNames;
        }
    }
}