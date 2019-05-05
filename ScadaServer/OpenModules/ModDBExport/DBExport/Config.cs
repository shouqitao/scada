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
 * Summary  : Module configuration
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2015
 * Modified : 2015
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace Scada.Server.Modules.DBExport {
    /// <summary>
    /// Module configuration
    /// <para>Module configuration</para>
    /// </summary>
    internal class Config {
        /// <summary>
        /// Export options
        /// </summary>
        public class ExportParams {
            /// <summary>
            /// Constructor
            /// </summary>
            public ExportParams() {
                ExportCurData = false;
                ExportCurDataQuery = "";
                ExportArcData = false;
                ExportArcDataQuery = "";
                ExportEvents = false;
                ExportEventQuery = "";
            }

            /// <summary>
            /// Get or set whether to export current data
            /// </summary>
            public bool ExportCurData { get; set; }

            /// <summary>
            /// Get or set SQL query to export current data
            /// </summary>
            public string ExportCurDataQuery { get; set; }

            /// <summary>
            /// Get or set whether to export archived data
            /// </summary>
            public bool ExportArcData { get; set; }

            /// <summary>
            /// Get or set SQL query for exporting archived data
            /// </summary>
            public string ExportArcDataQuery { get; set; }

            /// <summary>
            /// Get or set whether to export events
            /// </summary>
            public bool ExportEvents { get; set; }

            /// <summary>
            /// Get or set SQL query for event export
            /// </summary>
            public string ExportEventQuery { get; set; }

            /// <summary>
            /// Clone export options
            /// </summary>
            public ExportParams Clone() {
                return new ExportParams() {
                    ExportCurData = this.ExportCurData,
                    ExportCurDataQuery = this.ExportCurDataQuery,
                    ExportArcData = this.ExportArcData,
                    ExportArcDataQuery = this.ExportArcDataQuery,
                    ExportEvents = this.ExportEvents,
                    ExportEventQuery = this.ExportEventQuery
                };
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Export destination
        /// </summary>
        public class ExportDestination : IComparable<ExportDestination> {
            /// <summary>
            /// Constructor restricting the creation of an object without parameters
            /// </summary>
            private ExportDestination() { }

            /// <summary>
            /// Constructor
            /// </summary>
            public ExportDestination(DataSource dataSource, ExportParams exportParams) {
                this.DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
                this.ExportParams = exportParams ?? throw new ArgumentNullException(nameof(exportParams));
            }

            /// <summary>
            /// Get data source
            /// </summary>
            public DataSource DataSource { get; private set; }

            /// <summary>
            /// Get export options
            /// </summary>
            public ExportParams ExportParams { get; private set; }

            /// <summary>
            /// Clone export destination
            /// </summary>
            public ExportDestination Clone() {
                return new ExportDestination(DataSource.Clone(), ExportParams.Clone());
            }

            /// <summary>
            /// Compare the current object with another object of the same type.
            /// </summary>
            public int CompareTo(ExportDestination other) {
                return DataSource.CompareTo(other.DataSource);
            }
        }


        /// <summary>
        /// Configuration file name
        /// </summary>
        private const string ConfigFileName = "ModDBExport.xml";


        /// <summary>
        /// Constructor restricting the creation of an object without parameters
        /// </summary>
        private Config() { }

        /// <summary>
        /// Constructor
        /// </summary>
        public Config(string configDir) {
            FileName = ScadaUtils.NormalDir(configDir) + ConfigFileName;
            SetToDefault();
        }


        /// <summary>
        /// Get the full name of the configuration file
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Get export destination
        /// </summary>
        public List<ExportDestination> ExportDestinations { get; private set; }

        /// <summary>
        /// Get or set the control channel number to manually export current data
        /// </summary>
        public int CurDataCtrlCnlNum { get; set; }

        /// <summary>
        /// Get or set the control channel number for manual export of archived data
        /// </summary>
        public int ArcDataCtrlCnlNum { get; set; }

        /// <summary>
        /// Get or set the control channel number for manual event export
        /// </summary>
        public int EventsCtrlCnlNum { get; set; }


        /// <summary>
        /// Set default configuration options
        /// </summary>
        private void SetToDefault() {
            if (ExportDestinations == null)
                ExportDestinations = new List<ExportDestination>();
            else
                ExportDestinations.Clear();

            CurDataCtrlCnlNum = 1;
            ArcDataCtrlCnlNum = 2;
            EventsCtrlCnlNum = 3;
        }

        /// <summary>
        /// Download module configuration
        /// </summary>
        public bool Load(out string errMsg) {
            SetToDefault();

            try {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(FileName);

                // loading export destinations
                var expDestsNode = xmlDoc.DocumentElement.SelectSingleNode("ExportDestinations");
                if (expDestsNode != null) {
                    var expDestNodeList = expDestsNode.SelectNodes("ExportDestination");
                    foreach (XmlElement expDestElem in expDestNodeList) {
                        // loading data source
                        DataSource dataSource = null;
                        var dataSourceNode = expDestElem.SelectSingleNode("DataSource");

                        if (dataSourceNode != null) {
                            // getting data source type
                            DBTypes dbType;
                            if (!Enum.TryParse<DBTypes>(dataSourceNode.GetChildAsString("DBType"), out dbType))
                                dbType = DBTypes.Undefined;

                            // create data source
                            switch (dbType) {
                                case DBTypes.MSSQL:
                                    dataSource = new SqlDataSource();
                                    break;
                                case DBTypes.Oracle:
                                    dataSource = new OraDataSource();
                                    break;
                                case DBTypes.PostgreSQL:
                                    dataSource = new PgSqlDataSource();
                                    break;
                                case DBTypes.MySQL:
                                    dataSource = new MySqlDataSource();
                                    break;
                                case DBTypes.OLEDB:
                                    dataSource = new OleDbDataSource();
                                    break;
                                default:
                                    dataSource = null;
                                    break;
                            }

                            if (dataSource != null) {
                                dataSource.Server = dataSourceNode.GetChildAsString("Server");
                                dataSource.Database = dataSourceNode.GetChildAsString("Database");
                                dataSource.User = dataSourceNode.GetChildAsString("User");
                                dataSource.Password = dataSourceNode.GetChildAsString("Password");
                                dataSource.ConnectionString = dataSourceNode.GetChildAsString("ConnectionString");

                                if (string.IsNullOrEmpty(dataSource.ConnectionString))
                                    dataSource.ConnectionString = dataSource.BuildConnectionString();
                            }
                        }

                        // load export options
                        ExportParams exportParams = null;
                        var exportParamsNode = expDestElem.SelectSingleNode("ExportParams");

                        if (dataSource != null && exportParamsNode != null) {
                            exportParams = new ExportParams {
                                ExportCurDataQuery = exportParamsNode.GetChildAsString("ExportCurDataQuery")
                            };
                            exportParams.ExportCurData = !string.IsNullOrEmpty(exportParams.ExportCurDataQuery) &&
                                                         exportParamsNode.GetChildAsBool("ExportCurData");
                            exportParams.ExportArcDataQuery = exportParamsNode.GetChildAsString("ExportArcDataQuery");
                            exportParams.ExportArcData = !string.IsNullOrEmpty(exportParams.ExportArcDataQuery) &&
                                                         exportParamsNode.GetChildAsBool("ExportArcData");
                            exportParams.ExportEventQuery = exportParamsNode.GetChildAsString("ExportEventQuery");
                            exportParams.ExportEvents = !string.IsNullOrEmpty(exportParams.ExportEventQuery) &&
                                                        exportParamsNode.GetChildAsBool("ExportEvents");
                        }

                        // creating export destination
                        if (dataSource != null && exportParams != null) {
                            var expDest = new ExportDestination(dataSource, exportParams);
                            ExportDestinations.Add(expDest);
                        }
                    }

                    // sort export destinations
                    ExportDestinations.Sort();
                }

                // loading control channel numbers for manual export
                var manExpNode = xmlDoc.DocumentElement.SelectSingleNode("ManualExport");
                if (manExpNode != null) {
                    CurDataCtrlCnlNum = manExpNode.GetChildAsInt("CurDataCtrlCnlNum");
                    ArcDataCtrlCnlNum = manExpNode.GetChildAsInt("ArcDataCtrlCnlNum");
                    EventsCtrlCnlNum = manExpNode.GetChildAsInt("EventsCtrlCnlNum");
                }

                errMsg = "";
                return true;
            } catch (FileNotFoundException ex) {
                errMsg = ModPhrases.LoadModSettingsError + ": " + ex.Message +
                         Environment.NewLine + ModPhrases.ConfigureModule;
                return false;
            } catch (Exception ex) {
                errMsg = ModPhrases.LoadModSettingsError + ": " + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Save Module Configuration
        /// </summary>
        public bool Save(out string errMsg) {
            try {
                var xmlDoc = new XmlDocument();

                var xmlDecl = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
                xmlDoc.AppendChild(xmlDecl);

                var rootElem = xmlDoc.CreateElement("ModDBExport");
                xmlDoc.AppendChild(rootElem);

                // saving export destinations
                var expDestsElem = xmlDoc.CreateElement("ExportDestinations");
                rootElem.AppendChild(expDestsElem);

                foreach (var expDest in ExportDestinations) {
                    var expDestElem = xmlDoc.CreateElement("ExportDestination");
                    expDestsElem.AppendChild(expDestElem);

                    // save data source
                    var dataSource = expDest.DataSource;
                    var dataSourceElem = xmlDoc.CreateElement("DataSource");
                    dataSourceElem.AppendElem("DBType", dataSource.DBType);
                    dataSourceElem.AppendElem("Server", dataSource.Server);
                    dataSourceElem.AppendElem("Database", dataSource.Database);
                    dataSourceElem.AppendElem("User", dataSource.User);
                    dataSourceElem.AppendElem("Password", dataSource.Password);
                    string connStr = dataSource.ConnectionString;
                    string bldConnStr = dataSource.BuildConnectionString();
                    dataSourceElem.AppendElem("ConnectionString",
                        !string.IsNullOrEmpty(bldConnStr) && bldConnStr == connStr ? "" : connStr);
                    expDestElem.AppendChild(dataSourceElem);

                    // saving export settings
                    var exportParams = expDest.ExportParams;
                    var exportParamsElem = xmlDoc.CreateElement("ExportParams");
                    exportParamsElem.AppendElem("ExportCurData", exportParams.ExportCurData);
                    exportParamsElem.AppendElem("ExportCurDataQuery", exportParams.ExportCurDataQuery);
                    exportParamsElem.AppendElem("ExportArcData", exportParams.ExportArcData);
                    exportParamsElem.AppendElem("ExportArcDataQuery", exportParams.ExportArcDataQuery);
                    exportParamsElem.AppendElem("ExportEvents", exportParams.ExportEvents);
                    exportParamsElem.AppendElem("ExportEventQuery", exportParams.ExportEventQuery);
                    expDestElem.AppendChild(exportParamsElem);
                }

                // saving control channel numbers for manual export
                var manExpElem = xmlDoc.CreateElement("ManualExport");
                rootElem.AppendChild(manExpElem);
                manExpElem.AppendElem("CurDataCtrlCnlNum", CurDataCtrlCnlNum);
                manExpElem.AppendElem("ArcDataCtrlCnlNum", ArcDataCtrlCnlNum);
                manExpElem.AppendElem("EventsCtrlCnlNum", EventsCtrlCnlNum);

                xmlDoc.Save(FileName);
                errMsg = "";
                return true;
            } catch (Exception ex) {
                errMsg = ModPhrases.SaveModSettingsError + ": " + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Clone module configuration
        /// </summary>
        public Config Clone() {
            var configCopy = new Config {
                FileName = FileName,
                ExportDestinations = new List<ExportDestination>()
            };

            foreach (var expDest in ExportDestinations)
                configCopy.ExportDestinations.Add(expDest.Clone());

            configCopy.CurDataCtrlCnlNum = CurDataCtrlCnlNum;
            configCopy.ArcDataCtrlCnlNum = ArcDataCtrlCnlNum;
            configCopy.EventsCtrlCnlNum = EventsCtrlCnlNum;

            return configCopy;
        }
    }
}