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
 * Module   : ScadaData.Svc
 * Summary  : Windows service properties
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2016
 */

using System;
using System.IO;
using System.Reflection;
using System.Xml;

namespace Scada.Svc {
    /// <summary>
    /// Windows service properties
    /// <para>Windows service properties</para>
    /// </summary>
    public class SvcProps {
        /// <summary>
        /// The name of the file containing the service properties
        /// </summary>
        public const string SvcPropsFileName = "svc_config.xml";

        /// <summary>
        /// Error message that the service name is empty
        /// </summary>
        public static readonly string ServiceNameEmptyError = "Service name must not be empty.";


        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public SvcProps()
            : this("", "") { }

        /// <summary>
        /// Constructor
        /// </summary>
        public SvcProps(string serviceName, string description) {
            ServiceName = serviceName;
            Description = description;
        }


        /// <summary>
        /// Get or set service name
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Get or set the description
        /// </summary>
        public string Description { get; set; }


        /// <summary>
        /// Download service properties
        /// </summary>
        public bool LoadFromFile(string fileName, out string errMsg) {
            ServiceName = "";
            Description = "";

            try {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(fileName);

                var node = xmlDoc.DocumentElement.SelectSingleNode("ServiceName");
                ServiceName = node == null ? "" : node.InnerText;

                if (string.IsNullOrEmpty(ServiceName))
                    throw new Exception(ServiceNameEmptyError);

                node = xmlDoc.DocumentElement.SelectSingleNode("Description");
                Description = node == null ? "" : node.InnerText;

                errMsg = "";
                return true;
            } catch (Exception ex) {
                errMsg = ("Error loading service properties: ") + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Download service properties
        /// </summary>
        public bool LoadFromFile() {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string fileName = path + Path.DirectorySeparatorChar + SvcPropsFileName;

            if (!File.Exists(fileName)) return false;

            if (LoadFromFile(fileName, out string errMsg))
                return true;

            throw new ScadaException(errMsg);
        }
    }
}