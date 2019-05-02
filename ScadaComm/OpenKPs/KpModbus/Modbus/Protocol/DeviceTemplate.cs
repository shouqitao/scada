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
 * Module   : KpModbus
 * Summary  : Device template
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2012
 * Modified : 2017
 */

using System;
using System.Collections.Generic;
using System.Xml;

namespace Scada.Comm.Devices.Modbus.Protocol {
    /// <summary>
    /// Device template
    /// <para>Device pattern</para>
    /// </summary>
    public class DeviceTemplate {
        /// <summary>
        /// Template settings
        /// </summary>
        public class Settings {
            // The default byte order is 2, 4, and 8 bytes long.
            private int[] defByteOrder2;
            private int[] defByteOrder4;
            private int[] defByteOrder8;

            /// <summary>
            /// Constructor
            /// </summary>
            public Settings() {
                SetToDefault();
            }

            /// <summary>
            /// Get or set the attribute to display addresses starting with 0
            /// </summary>
            public bool ZeroAddr { get; set; }

            /// <summary>
            /// Get or set the indication of the display of addresses in the 10th system
            /// </summary>
            public bool DecAddr { get; set; }

            /// <summary>
            /// Get or set the string byte order by default for values of length 2 bytes
            /// </summary>
            public string DefByteOrder2 { get; set; }

            /// <summary>
            /// Get or set the default string byte order for 4-byte values
            /// </summary>
            public string DefByteOrder4 { get; set; }

            /// <summary>
            /// Get or set the default string record byte order for values of length 8 bytes
            /// </summary>
            public string DefByteOrder8 { get; set; }

            /// <summary>
            /// Set default template settings
            /// </summary>
            public void SetToDefault() {
                defByteOrder2 = null;
                defByteOrder4 = null;
                defByteOrder8 = null;

                ZeroAddr = false;
                DecAddr = true;
                DefByteOrder2 = "";
                DefByteOrder4 = "";
                DefByteOrder8 = "";
            }

            /// <summary>
            /// Load template settings from the XML node
            /// </summary>
            public void LoadFromXml(XmlElement settingsElem) {
                if (settingsElem == null)
                    throw new ArgumentNullException(nameof(settingsElem));

                defByteOrder2 = null;
                defByteOrder4 = null;
                defByteOrder8 = null;

                ZeroAddr = settingsElem.GetChildAsBool("ZeroAddr", false);
                DecAddr = settingsElem.GetChildAsBool("DecAddr", true);
                DefByteOrder2 = settingsElem.GetChildAsString("DefByteOrder2");
                DefByteOrder4 = settingsElem.GetChildAsString("DefByteOrder4");
                DefByteOrder8 = settingsElem.GetChildAsString("DefByteOrder8");
            }

            /// <summary>
            /// Save template settings in XML node
            /// </summary>
            public void SaveToXml(XmlElement settingsElem) {
                if (settingsElem == null)
                    throw new ArgumentNullException(nameof(settingsElem));

                settingsElem.AppendElem("ZeroAddr", ZeroAddr);
                settingsElem.AppendElem("DecAddr", DecAddr);
                settingsElem.AppendElem("DefByteOrder2", DefByteOrder2);
                settingsElem.AppendElem("DefByteOrder4", DefByteOrder4);
                settingsElem.AppendElem("DefByteOrder8", DefByteOrder8);
            }

            /// <summary>
            /// Copy settings from specified
            /// </summary>
            public void CopyFrom(Settings srcSettings) {
                if (srcSettings == null)
                    throw new ArgumentNullException(nameof(srcSettings));

                defByteOrder2 = null;
                defByteOrder4 = null;
                defByteOrder8 = null;

                ZeroAddr = srcSettings.ZeroAddr;
                DecAddr = srcSettings.DecAddr;
                DefByteOrder2 = srcSettings.DefByteOrder2;
                DefByteOrder4 = srcSettings.DefByteOrder4;
                DefByteOrder8 = srcSettings.DefByteOrder8;
            }

            /// <summary>
            /// Get suitable default byte order
            /// </summary>
            public int[] GetDefByteOrder(int elemCnt) {
                switch (elemCnt) {
                    case 1:
                        if (defByteOrder2 == null)
                            defByteOrder2 = ModbusUtils.ParseByteOrder(DefByteOrder2);
                        return defByteOrder2;

                    case 2:
                        if (defByteOrder4 == null)
                            defByteOrder4 = ModbusUtils.ParseByteOrder(DefByteOrder4);
                        return defByteOrder4;

                    case 4:
                        if (defByteOrder8 == null)
                            defByteOrder8 = ModbusUtils.ParseByteOrder(DefByteOrder8);
                        return defByteOrder8;

                    default:
                        return null;
                }
            }
        }


        /// <summary>
        /// Constructor
        /// </summary>
        public DeviceTemplate() {
            Sett = new Settings();
            ElemGroups = new List<ElemGroup>();
            Cmds = new List<ModbusCmd>();
        }


        /// <summary>
        /// Get template settings
        /// </summary>
        public Settings Sett { get; private set; }

        /// <summary>
        /// Get list of item groups
        /// </summary>
        public List<ElemGroup> ElemGroups { get; private set; }

        /// <summary>
        /// Get a list of commands
        /// </summary>
        public List<ModbusCmd> Cmds { get; private set; }


        /// <summary>
        /// Sets the default values.
        /// </summary>
        protected virtual void SetToDefault() {
            Sett.SetToDefault();
            ElemGroups.Clear();
            Cmds.Clear();
        }

        /// <summary>
        /// Loads the template from the XML node.
        /// </summary>
        protected virtual void LoadFromXml(XmlNode rootNode) {
            // loading template settings
            var settingsNode = rootNode.SelectSingleNode("Settings");
            if (settingsNode != null)
                Sett.LoadFromXml((XmlElement) settingsNode);

            // loading of groups of elements
            var elemGroupsNode = rootNode.SelectSingleNode("ElemGroups");
            if (elemGroupsNode != null) {
                var elemGroupNodes = elemGroupsNode.SelectNodes("ElemGroup");
                var kpTagInd = 0;

                foreach (XmlElement elemGroupElem in elemGroupNodes) {
                    var elemGroup = CreateElemGroup(elemGroupElem.GetAttrAsEnum<TableType>("tableType"));
                    elemGroup.StartKPTagInd = kpTagInd;
                    elemGroup.StartSignal = kpTagInd + 1;
                    elemGroup.LoadFromXml(elemGroupElem);

                    if (elemGroup.Elems.Count > 0) {
                        if (elemGroup.ByteOrderEnabled) {
                            foreach (var elem in elemGroup.Elems) {
                                if (elem.ByteOrder == null)
                                    elem.ByteOrder = Sett.GetDefByteOrder(elem.Length);
                            }
                        }

                        ElemGroups.Add(elemGroup);
                        kpTagInd += elemGroup.Elems.Count;
                    }
                }
            }

            // command loading
            var cmdsNode = rootNode.SelectSingleNode("Cmds");
            if (cmdsNode != null) {
                var cmdNodes = cmdsNode.SelectNodes("Cmd");

                foreach (XmlElement cmdElem in cmdNodes) {
                    var cmd = CreateModbusCmd(
                        cmdElem.GetAttrAsEnum<TableType>("tableType"),
                        cmdElem.GetAttrAsBool("multiple"));
                    cmd.LoadFromXml(cmdElem);

                    if (cmd.ByteOrderEnabled && cmd.ByteOrder == null)
                        cmd.ByteOrder = Sett.GetDefByteOrder(cmd.ElemCnt);

                    if (cmd.CmdNum > 0)
                        Cmds.Add(cmd);
                }
            }
        }

        /// <summary>
        /// Saves the template into the XML node.
        /// </summary>
        protected virtual void SaveToXml(XmlElement rootElem) {
            // saving template settings
            Sett.SaveToXml(rootElem.AppendElem("Settings"));

            // saving groups of elements
            XmlElement elemGroupsElem = rootElem.AppendElem("ElemGroups");
            foreach (var elemGroup in ElemGroups) {
                elemGroup.SaveToXml(elemGroupsElem.AppendElem("ElemGroup"));
            }

            // save commands
            XmlElement cmdsElem = rootElem.AppendElem("Cmds");
            foreach (var cmd in Cmds) {
                cmd.SaveToXml(cmdsElem.AppendElem("Cmd"));
            }
        }


        /// <summary>
        /// Найти команду по номеру
        /// </summary>
        public ModbusCmd FindCmd(int cmdNum) {
            foreach (var cmd in Cmds) {
                if (cmd.CmdNum == cmdNum)
                    return cmd;
            }

            return null;
        }

        /// <summary>
        /// Получить активные группы элементов
        /// </summary>
        public List<ElemGroup> GetActiveElemGroups() {
            var activeElemGroups = new List<ElemGroup>();

            foreach (var elemGroup in ElemGroups) {
                if (elemGroup.Active)
                    activeElemGroups.Add(elemGroup);
            }

            return activeElemGroups;
        }

        /// <summary>
        /// Загрузить шаблон устройства
        /// </summary>
        public bool Load(string fileName, out string errMsg) {
            SetToDefault();

            try {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(fileName);
                LoadFromXml(xmlDoc.DocumentElement);
                errMsg = "";
                return true;
            } catch (Exception ex) {
                errMsg = ModbusPhrases.LoadTemplateError + ":" + Environment.NewLine + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Сохранить шаблон устройства
        /// </summary>
        public bool Save(string fileName, out string errMsg) {
            try {
                var xmlDoc = new XmlDocument();
                var xmlDecl = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
                xmlDoc.AppendChild(xmlDecl);

                var rootElem = xmlDoc.CreateElement("DevTemplate");
                xmlDoc.AppendChild(rootElem);
                SaveToXml(rootElem);

                xmlDoc.Save(fileName);
                errMsg = "";
                return true;
            } catch (Exception ex) {
                errMsg = ModbusPhrases.SaveTemplateError + ":" + Environment.NewLine + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Copies the template properties from the source template.
        /// </summary>
        public virtual void CopyFrom(DeviceTemplate srcTemplate) {
            if (srcTemplate == null)
                throw new ArgumentNullException("srcTemplate");

            // копирование настроек шаблона
            Sett.CopyFrom(srcTemplate.Sett);

            // копирование групп элементов
            ElemGroups.Clear();
            foreach (var srcGroup in srcTemplate.ElemGroups) {
                var destGroup = CreateElemGroup(srcGroup.TableType);
                destGroup.CopyFrom(srcGroup);
                ElemGroups.Add(destGroup);
            }

            // копирование команд
            Cmds.Clear();
            foreach (var srcCmd in srcTemplate.Cmds) {
                var destCmd = CreateModbusCmd(srcCmd.TableType, srcCmd.Multiple);
                destCmd.CopyFrom(destCmd);
                Cmds.Add(destCmd);
            }
        }

        /// <summary>
        /// Creates a new group of Modbus elements.
        /// </summary>
        public virtual ElemGroup CreateElemGroup(TableType tableType) {
            return new ElemGroup(tableType);
        }

        /// <summary>
        /// Creates a new Modbus command.
        /// </summary>
        public virtual ModbusCmd CreateModbusCmd(TableType tableType, bool multiple) {
            return new ModbusCmd(tableType, multiple);
        }
    }
}