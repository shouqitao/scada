/*
 * Copyright 2017 Mikhail Shiryaev
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
 * Summary  : SCADA-Server connection settings
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2005
 * Modified : 2017
 */

using System;
using System.IO;
using System.Xml;
using Utils;

namespace Scada.Client {
    /// <inheritdoc />
    /// <summary>
    /// SCADA-Server connection settings
    /// <para>Settings for connecting to the SCADA Server</para>
    /// </summary>
    public class CommSettings : ISettings {
        /// <summary>
        /// Default Settings File Name
        /// </summary>
        public const string DefFileName = "CommSettings.xml";


        /// <summary>
        /// Constructor
        /// </summary>
        public CommSettings() {
            SetToDefault();
        }

        /// <summary>
        /// Constructor with setting communication parameters
        /// </summary>
        public CommSettings(string serverHost, int serverPort, string serverUser, string serverPwd,
            int serverTimeout) {
            ServerHost = serverHost;
            ServerPort = serverPort;
            ServerUser = serverUser;
            ServerPwd = serverPwd;
            ServerTimeout = serverTimeout;
        }


        /// <summary>
        /// Get or set the name of the computer or the IP address of the SCADA Server
        /// </summary>
        public string ServerHost { get; set; }

        /// <summary>
        /// Get or set the TCP port number of SCADA-Server
        /// </summary>
        public int ServerPort { get; set; }

        /// <summary>
        /// Get or set username to connect to SCADA-Server
        /// </summary>
        public string ServerUser { get; set; }

        /// <summary>
        /// Get or set user password to connect to SCADA-Server
        /// </summary>
        public string ServerPwd { get; set; }

        /// <summary>
        /// Get or set SCADA-Server response timeout, ms
        /// </summary>
        public int ServerTimeout { get; set; }


        /// <inheritdoc />
        /// <summary>
        /// Create a new settings object
        /// </summary>
        public ISettings Create() {
            return new CommSettings();
        }

        /// <inheritdoc />
        /// <summary>
        /// Determine whether the specified settings are equal to the current settings
        /// </summary>
        public bool Equals(ISettings settings) {
            return Equals(settings as CommSettings);
        }

        /// <summary>
        /// Determine whether the specified settings are equal to the current settings
        /// </summary>
        public bool Equals(CommSettings commSettings) {
            return commSettings != null && (commSettings == this || ServerHost == commSettings.ServerHost && ServerPort == commSettings.ServerPort &&
                                            ServerUser == commSettings.ServerUser && ServerPwd == commSettings.ServerPwd &&
                                            ServerTimeout == commSettings.ServerTimeout);
        }

        /// <summary>
        /// Set default settings
        /// </summary>
        public void SetToDefault() {
            ServerHost = "localhost";
            ServerPort = 10000;
            ServerUser = "";
            ServerPwd = "12345";
            ServerTimeout = 10000;
        }

        /// <summary>
        /// Create a copy of the settings
        /// </summary>
        public CommSettings Clone() {
            return new CommSettings(ServerHost, ServerPort, ServerUser, ServerPwd, ServerTimeout);
        }

        /// <summary>
        /// Load settings from the specified XML node
        /// </summary>
        public void LoadFromXml(XmlNode commSettingsNode) {
            if (commSettingsNode == null)
                throw new ArgumentNullException(nameof(commSettingsNode));

            var xmlNodeList = commSettingsNode.SelectNodes("Param");

            foreach (XmlElement xmlElement in xmlNodeList) {
                string name = xmlElement.GetAttribute("name").Trim();
                string nameL = name.ToLowerInvariant();
                string val = xmlElement.GetAttribute("value");

                try {
                    if (nameL == "serverhost")
                        ServerHost = val;
                    else if (nameL == "serverport")
                        ServerPort = int.Parse(val);
                    else if (nameL == "serveruser")
                        ServerUser = val;
                    else if (nameL == "serverpwd")
                        ServerPwd = val;
                    else if (nameL == "servertimeout")
                        ServerTimeout = int.Parse(val);
                } catch {
                    throw new ScadaException(string.Format(CommonPhrases.IncorrectXmlParamVal, name));
                }
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Load settings from file
        /// </summary>
        public bool LoadFromFile(string fileName, out string errMsg) {
            // setting default values
            SetToDefault();

            // loading connection settings with SCADA Server
            try {
                if (!File.Exists(fileName))
                    throw new FileNotFoundException(string.Format(CommonPhrases.NamedFileNotFound, fileName));

                var xmlDoc = new XmlDocument();
                xmlDoc.Load(fileName);
                LoadFromXml(xmlDoc.DocumentElement);

                errMsg = "";
                return true;
            } catch (Exception ex) {
                errMsg = CommonPhrases.LoadCommSettingsError + ": " + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Save settings to the specified XML element
        /// </summary>
        public void SaveToXml(XmlElement commSettingsElem) {
            commSettingsElem.AppendParamElem("ServerHost", ServerHost,
                "Computer name or IP-address of SCADA-Server", "SCADA-Server host or IP address");
            commSettingsElem.AppendParamElem("ServerPort", ServerPort,
                "TCP port number of SCADA-Server", "SCADA-Server TCP port number");
            commSettingsElem.AppendParamElem("ServerUser", ServerUser,
                "Username to connect", "User name for the connection");
            commSettingsElem.AppendParamElem("ServerPwd", ServerPwd,
                "User password to connect", "User password for the connection");
            commSettingsElem.AppendParamElem("ServerTimeout", ServerTimeout,
                "Response timeout, ms", "Response timeout, ms");
        }

        /// <inheritdoc />
        /// <summary>
        /// Save settings to file
        /// </summary>
        public bool SaveToFile(string fileName, out string errMsg) {
            try {
                var xmlDoc = new XmlDocument();

                var xmlDecl = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
                xmlDoc.AppendChild(xmlDecl);

                var rootElem = xmlDoc.CreateElement("CommSettings");
                xmlDoc.AppendChild(rootElem);
                SaveToXml(rootElem);

                xmlDoc.Save(fileName);
                errMsg = "";
                return true;
            } catch (Exception ex) {
                errMsg = CommonPhrases.SaveCommSettingsError + ":\n" + ex.Message;
                return false;
            }
        }
    }
}