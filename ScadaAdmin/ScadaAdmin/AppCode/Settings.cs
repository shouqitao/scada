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
 * Summary  : Application settings
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2010
 * Modified : 2018
 */

using System;
using System.IO;
using System.Xml;
using Scada;
using Utils;

namespace ScadaAdmin {
    /// <summary>
    /// Application settings
    /// <para>Application settings</para>
    /// </summary>
    public class Settings {
        /// <summary>
        /// Application settings
        /// </summary>
        public class AppSettings {
            /// <summary>
            /// Constructor
            /// </summary>
            public AppSettings() {
                SetToDefault();
            }

            /// <summary>
            /// Get or install the SCADA-Administrator configuration database file
            /// </summary>
            public string BaseSDFFile { get; set; }

            /// <summary>
            /// Get or set the SCADA-Server configuration database directory
            /// </summary>
            public string BaseDATDir { get; set; }

            /// <summary>
            /// Get or set the configuration base backup directory
            /// </summary>
            public string BackupDir { get; set; }

            /// <summary>
            /// Get or set the directory SCADA-Communicator
            /// </summary>
            public string CommDir { get; set; }

            /// <summary>
            /// Get or set the sign of automatic backup of the configuration database when sending to the server
            /// </summary>
            public bool AutoBackupBase { get; set; }

            /// <summary>
            /// Set application default settings
            /// </summary>
            public void SetToDefault() {
                BaseSDFFile = @"C:\SCADA\BaseSDF\ScadaBase.sdf";
                BaseDATDir = @"C:\SCADA\BaseDAT\";
                BackupDir = @"C:\SCADA\ScadaAdmin\Backup\";
                CommDir = @"C:\SCADA\ScadaComm\";
                AutoBackupBase = true;
            }
        }

        /// <summary>
        /// The state of the main form
        /// </summary>
        public class FormState {
            /// <summary>
            /// Constructor
            /// </summary>
            public FormState() {
                SetToDefault();
            }

            /// <summary>
            /// Get a sign that the form state is undefined
            /// </summary>
            public bool IsEmpty { get; set; }

            /// <summary>
            /// Get or set horizontal form position
            /// </summary>
            public int Left { get; set; }

            /// <summary>
            /// Get or set the form position vertically
            /// </summary>
            public int Top { get; set; }

            /// <summary>
            /// Get or set the width of the form
            /// </summary>
            public int Width { get; set; }

            /// <summary>
            /// Get or set form height
            /// </summary>
            public int Height { get; set; }

            /// <summary>
            /// Get or set the sign that the form is expanded
            /// </summary>
            public bool Maximized { get; set; }

            /// <summary>
            /// Get or set the width of the explorer tree
            /// </summary>
            public int ExplorerWidth { get; set; }

            /// <summary>
            /// Get or set the name of the connection to the remote server
            /// </summary>
            public string ServerConn { get; set; }

            /// <summary>
            /// Set default main form status
            /// </summary>
            public void SetToDefault() {
                IsEmpty = true;
                Left = 0;
                Top = 0;
                Width = 0;
                Height = 0;
                Maximized = false;
                ExplorerWidth = 0;
                ServerConn = "";
            }
        }


        /// <summary>
        /// Application Settings File Name
        /// </summary>
        private const string AppSettingsFileName = "ScadaAdminConfig.xml";

        /// <summary>
        /// The name of the main form state file
        /// </summary>
        private const string FormStateFileName = "ScadaAdminState.xml";


        /// <summary>
        /// Constructor
        /// </summary>
        public Settings() {
            AppSett = new AppSettings();
            FormSt = new FormState();
        }


        /// <summary>
        /// Get application settings
        /// </summary>
        public AppSettings AppSett { get; private set; }

        /// <summary>
        /// Get the status of the main form
        /// </summary>
        public FormState FormSt { get; private set; }


        /// <summary>
        /// Load application settings from file
        /// </summary>
        public bool LoadAppSettings(out string errMsg) {
            // setting default parameters
            AppSett.SetToDefault();

            // loading from file
            string fileName = AppData.AppDirs.ConfigDir + AppSettingsFileName;

            try {
                if (!File.Exists(fileName))
                    throw new FileNotFoundException(string.Format(CommonPhrases.NamedFileNotFound, fileName));

                var xmlDoc = new XmlDocument();
                xmlDoc.Load(fileName);

                // getting parameter values
                var xmlNodeList = xmlDoc.DocumentElement.SelectNodes("Param");
                foreach (XmlElement xmlElement in xmlNodeList) {
                    string name = xmlElement.GetAttribute("name");
                    string nameL = name.ToLowerInvariant();
                    string val = xmlElement.GetAttribute("value");

                    try {
                        if (nameL == "basesdffile")
                            AppSett.BaseSDFFile = val;
                        else if (nameL == "basedatdir")
                            AppSett.BaseDATDir = ScadaUtils.NormalDir(val);
                        else if (nameL == "backupdir")
                            AppSett.BackupDir = ScadaUtils.NormalDir(val);
                        else if (nameL == "commdir")
                            AppSett.CommDir = ScadaUtils.NormalDir(val);
                        else if (nameL == "backuponpassbase")
                            AppSett.AutoBackupBase = bool.Parse(val);
                    } catch {
                        throw new Exception(string.Format(CommonPhrases.IncorrectXmlParamVal, name));
                    }
                }

                errMsg = "";
                return true;
            } catch (Exception ex) {
                errMsg = CommonPhrases.LoadAppSettingsError + ":\r\n" + ex.Message;
                AppData.ErrLog.WriteAction(errMsg, Log.ActTypes.Exception);
                return false;
            }
        }

        /// <summary>
        /// Save application settings to file
        /// </summary>
        public bool SaveAppSettings(out string errMsg) {
            try {
                // generating an XML document
                var xmlDoc = new XmlDocument();

                var xmlDecl = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
                xmlDoc.AppendChild(xmlDecl);

                var rootElem = xmlDoc.CreateElement("ScadaAdminConfig");
                xmlDoc.AppendChild(rootElem);

                rootElem.AppendParamElem("BaseSDFFile", AppSett.BaseSDFFile,
                    "Файл базы конфигурации в формате SDF", "Configuration database file in SDF format");
                rootElem.AppendParamElem("BaseDATDir", AppSett.BaseDATDir,
                    "Директория базы конфигурации в формате DAT", "Configuration database in DAT format directory");
                rootElem.AppendParamElem("BackupDir", AppSett.BackupDir,
                    "Директория резервного копирования базы конфигурации", "Configuration database backup directory");
                rootElem.AppendParamElem("CommDir", AppSett.CommDir,
                    "Директория SCADA-Коммуникатора", "SCADA-Communicator directory");
                rootElem.AppendParamElem("AutoBackupBase", AppSett.AutoBackupBase,
                    "Автоматически резервировать базу конфигурации", "Automatically backup the configuration database");

                // save to file
                xmlDoc.Save(AppData.AppDirs.ConfigDir + AppSettingsFileName);
                errMsg = "";
                return true;
            } catch (Exception ex) {
                errMsg = CommonPhrases.SaveAppSettingsError + ":\r\n" + ex.Message;
                AppData.ErrLog.WriteAction(errMsg, Log.ActTypes.Exception);
                return false;
            }
        }

        /// <summary>
        /// Load main form state from file
        /// </summary>
        public void LoadFormState() {
            // default state setting
            FormSt.SetToDefault();

            // loading from file
            string fileName = AppData.AppDirs.ConfigDir + FormStateFileName;

            if (File.Exists(fileName)) {
                try {
                    var xmlDoc = new XmlDocument();
                    xmlDoc.Load(fileName);

                    // getting parameter values
                    var xmlNodeList = xmlDoc.DocumentElement.SelectNodes("Param");
                    foreach (XmlElement xmlElement in xmlNodeList) {
                        string name = xmlElement.GetAttribute("name");
                        string nameL = name.ToLowerInvariant();
                        string val = xmlElement.GetAttribute("value");

                        try {
                            if (nameL == "left")
                                FormSt.Left = int.Parse(val);
                            else if (nameL == "top")
                                FormSt.Top = int.Parse(val);
                            else if (nameL == "width")
                                FormSt.Width = int.Parse(val);
                            else if (nameL == "height")
                                FormSt.Height = int.Parse(val);
                            else if (nameL == "maximized")
                                FormSt.Maximized = bool.Parse(val);
                            else if (nameL == "explorerwidth")
                                FormSt.ExplorerWidth = int.Parse(val);
                            else if (nameL == "serverconn")
                                FormSt.ServerConn = val;
                        } catch {
                            throw new Exception(string.Format(CommonPhrases.IncorrectXmlParamVal, name));
                        }
                    }

                    FormSt.IsEmpty = false;
                } catch (Exception ex) {
                    FormSt.IsEmpty = true;
                    AppData.ErrLog.WriteAction(
                        (Localization.UseRussian
                            ? "Ошибка при загрузке состояния главной формы:\r\n"
                            : "Error loading main form state:\r\n") + ex.Message, Log.ActTypes.Exception);
                }
            }
        }

        /// <summary>
        /// Save the state of the main form in the file
        /// </summary>
        public bool SaveFormState(out string errMsg) {
            try {
                // generating an XML document
                var xmlDoc = new XmlDocument();

                var xmlDecl = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
                xmlDoc.AppendChild(xmlDecl);

                var rootElem = xmlDoc.CreateElement("ScadaAdminState");
                xmlDoc.AppendChild(rootElem);

                rootElem.AppendParamElem("Left", FormSt.Left);
                rootElem.AppendParamElem("Top", FormSt.Top);
                rootElem.AppendParamElem("Width", FormSt.Width);
                rootElem.AppendParamElem("Height", FormSt.Height);
                rootElem.AppendParamElem("Maximized", FormSt.Maximized);
                rootElem.AppendParamElem("ExplorerWidth", FormSt.ExplorerWidth);
                rootElem.AppendParamElem("ServerConn", FormSt.ServerConn);

                // save to file
                xmlDoc.Save(AppData.AppDirs.ConfigDir + FormStateFileName);
                errMsg = "";
                return true;
            } catch (Exception ex) {
                errMsg = (Localization.UseRussian
                             ? "Ошибка при сохранении файла состояния главной формы:\r\n"
                             : "Error saving main form state:\r\n") + ex.Message;
                AppData.ErrLog.WriteAction(errMsg, Log.ActTypes.Exception);
                return false;
            }
        }
    }
}