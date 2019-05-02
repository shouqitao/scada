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
 * Module   : KpEmail
 * Summary  : Mail server connection configuration
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2016
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Scada.Comm.Devices.KpEmail {
    /// <summary>
    /// Mail server connection configuration
    /// <para>Mail Server Connection Configuration</para>
    /// </summary>
    internal class Config {
        /// <summary>
        /// Constructor
        /// </summary>
        public Config() {
            SetToDefault();
        }


        /// <summary>
        /// Get or set server name or IP address
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Get or set the port
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Get or set user
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Get or set the display name of the user
        /// </summary>
        public string UserDisplayName { get; set; }

        /// <summary>
        /// Get or set a password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Get or set SSL sign
        /// </summary>
        public bool EnableSsl { get; set; }


        /// <summary>
        /// Set default configuration options
        /// </summary>
        private void SetToDefault() {
            // Gmail: smtp.gmail.com, 587
            // Yandex: smtp.yandex.ru, 25
            Host = "smtp.gmail.com";
            Port = 587;
            User = "example@gmail.com";
            UserDisplayName = "Rapid SCADA";
            Password = "";
            EnableSsl = true;
        }


        /// <summary>
        /// Load configuration from file
        /// </summary>
        public bool Load(string fileName, out string errMsg) {
            SetToDefault();

            try {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(fileName);

                var rootElem = xmlDoc.DocumentElement;
                Host = rootElem.GetChildAsString("Host");
                Port = rootElem.GetChildAsInt("Port");
                User = rootElem.GetChildAsString("User");
                UserDisplayName = rootElem.GetChildAsString("UserDisplayName");
                Password = rootElem.GetChildAsString("Password");
                EnableSsl = rootElem.GetChildAsBool("EnableSsl");

                errMsg = "";
                return true;
            } catch (Exception ex) {
                errMsg = CommPhrases.LoadKpSettingsError + ":" + Environment.NewLine + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Save configuration to file
        /// </summary>
        public bool Save(string fileName, out string errMsg) {
            try {
                var xmlDoc = new XmlDocument();

                var xmlDecl = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
                xmlDoc.AppendChild(xmlDecl);

                var rootElem = xmlDoc.CreateElement("KpEmailConfig");
                xmlDoc.AppendChild(rootElem);

                rootElem.AppendElem("Host", Host);
                rootElem.AppendElem("Port", Port);
                rootElem.AppendElem("User", User);
                rootElem.AppendElem("UserDisplayName", UserDisplayName);
                rootElem.AppendElem("Password", Password);
                rootElem.AppendElem("EnableSsl", EnableSsl);

                xmlDoc.Save(fileName);
                errMsg = "";
                return true;
            } catch (Exception ex) {
                errMsg = CommPhrases.SaveKpSettingsError + ":" + Environment.NewLine + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Get configuration file name
        /// </summary>
        public static string GetFileName(string configDir, int kpNum) {
            return configDir + "KpEmail_" + CommUtils.AddZeros(kpNum, 3) + ".xml";
        }
    }
}