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
 * Summary  : Access to the configuration database
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2017
 * Modified : 2017
 */

using Scada.Data.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace Scada.Data.Configuration {
    /// <summary>
    /// Access to the configuration database
    /// <para>Access to configuration database data</para>
    /// </summary>
    public class ConfDAO {
        /// <summary>
        /// Value separator within the table field
        /// </summary>
        protected static readonly char[] FieldSeparator = new char[] {';'};

        /// <summary>
        /// Configuration base tables
        /// </summary>
        protected BaseTables baseTables;


        /// <summary>
        /// Constructor restricting the creation of an object without parameters
        /// </summary>
        protected ConfDAO() { }

        /// <summary>
        /// Constructor
        /// </summary>
        public ConfDAO(BaseTables baseTables) {
            this.baseTables = baseTables ?? throw new ArgumentNullException(nameof(baseTables));
        }


        /// <summary>
        /// Get the names of arbitrary entities, the key - the identifier or number of the entity
        /// </summary>
        protected SortedList<int, string> GetNames(DataTable table) {
            var names = new SortedList<int, string>(table.Rows.Count);

            foreach (DataRow row in table.Rows) {
                names.Add((int) row[0], (string) row["Name"]);
            }

            return names;
        }


        /// <summary>
        /// Get input channel properties in ascending channel numbers.
        /// </summary>
        public List<InCnlProps> GetInCnlProps() {
            var tblInCnl = baseTables.InCnlTable;
            var viewObj = baseTables.ObjTable.DefaultView;
            var viewKP = baseTables.KPTable.DefaultView;
            var viewParam = baseTables.ParamTable.DefaultView;
            var viewFormat = baseTables.FormatTable.DefaultView;
            var viewUnit = baseTables.UnitTable.DefaultView;

            // set sorting for later string retrieval
            viewObj.Sort = "ObjNum";
            viewKP.Sort = "KPNum";
            viewParam.Sort = "ParamID";
            viewFormat.Sort = "FormatID";
            viewUnit.Sort = "UnitID";

            // create and fill list
            var cnlPropsList = new List<InCnlProps>(tblInCnl.Rows.Count);

            foreach (DataRow inCnlRow in tblInCnl.Rows) {
                var cnlProps = new InCnlProps {
                    // defining properties that do not use foreign keys
                    CnlNum = (int) inCnlRow["CnlNum"],
                    CnlName = (string) inCnlRow["Name"],
                    CnlTypeID = (int) inCnlRow["CnlTypeID"],
                    ObjNum = (int) inCnlRow["ObjNum"],
                    KPNum = (int) inCnlRow["KPNum"],
                    Signal = (int) inCnlRow["Signal"],
                    FormulaUsed = (bool) inCnlRow["FormulaUsed"],
                    Formula = (string) inCnlRow["Formula"],
                    Averaging = (bool) inCnlRow["Averaging"],
                    ParamID = (int) inCnlRow["ParamID"],
                    FormatID = (int) inCnlRow["FormatID"],
                    UnitID = (int) inCnlRow["UnitID"],
                    CtrlCnlNum = (int) inCnlRow["CtrlCnlNum"],
                    EvEnabled = (bool) inCnlRow["EvEnabled"],
                    EvSound = (bool) inCnlRow["EvSound"],
                    EvOnChange = (bool) inCnlRow["EvOnChange"],
                    EvOnUndef = (bool) inCnlRow["EvOnUndef"],
                    LimLowCrash = (double) inCnlRow["LimLowCrash"],
                    LimLow = (double) inCnlRow["LimLow"],
                    LimHigh = (double) inCnlRow["LimHigh"],
                    LimHighCrash = (double) inCnlRow["LimHighCrash"]
                };

                // object name definition
                int objRowInd = viewObj.Find(cnlProps.ObjNum);
                if (objRowInd >= 0)
                    cnlProps.ObjName = (string) viewObj[objRowInd]["Name"];

                // name definition
                int kpRowInd = viewKP.Find(cnlProps.KPNum);
                if (kpRowInd >= 0)
                    cnlProps.KPName = (string) viewKP[kpRowInd]["Name"];

                // definition of the parameter name and file name of the icon
                int paramRowInd = viewParam.Find(cnlProps.ParamID);
                if (paramRowInd >= 0) {
                    var paramRowView = viewParam[paramRowInd];
                    cnlProps.ParamName = (string) paramRowView["Name"];
                    cnlProps.IconFileName = (string) paramRowView["IconFileName"];
                }

                // definition of output format
                int formatRowInd = viewFormat.Find(inCnlRow["FormatID"]);
                if (formatRowInd >= 0) {
                    var formatRowView = viewFormat[formatRowInd];
                    cnlProps.ShowNumber = (bool) formatRowView["ShowNumber"];
                    cnlProps.DecDigits = (int) formatRowView["DecDigits"];
                }

                // dimension definition
                int unitRowInd = viewUnit.Find(cnlProps.UnitID);
                if (unitRowInd >= 0) {
                    var unitRowView = viewUnit[unitRowInd];
                    cnlProps.UnitName = (string) unitRowView["Name"];
                    cnlProps.UnitSign = (string) unitRowView["Sign"];
                    string[] unitArr = cnlProps.UnitArr =
                        cnlProps.UnitSign.Split(FieldSeparator, StringSplitOptions.None);
                    for (var j = 0; j < unitArr.Length; j++)
                        unitArr[j] = unitArr[j].Trim();
                    if (unitArr.Length == 1 && unitArr[0] == "")
                        cnlProps.UnitArr = null;
                }

                cnlPropsList.Add(cnlProps);
            }

            return cnlPropsList;
        }

        /// <summary>
        /// Get control channel properties sorted in ascending channel numbers
        /// </summary>
        public List<CtrlCnlProps> GetCtrlCnlProps() {
            var tblCtrlCnl = baseTables.CtrlCnlTable;
            var viewObj = baseTables.ObjTable.DefaultView;
            var viewKP = baseTables.KPTable.DefaultView;
            var viewCmdVal = baseTables.CmdValTable.DefaultView;

            // set sorting for later string retrieval
            viewObj.Sort = "ObjNum";
            viewKP.Sort = "KPNum";
            viewCmdVal.Sort = "CmdValID";

            // create and fill list
            var ctrlCnlPropsList = new List<CtrlCnlProps>(tblCtrlCnl.Rows.Count);

            foreach (DataRow ctrlCnlRow in tblCtrlCnl.Rows) {
                var ctrlCnlProps = new CtrlCnlProps {
                    // defining properties that do not use foreign keys
                    CtrlCnlNum = (int) ctrlCnlRow["CtrlCnlNum"],
                    CtrlCnlName = (string) ctrlCnlRow["Name"],
                    CmdTypeID = (int) ctrlCnlRow["CmdTypeID"],
                    ObjNum = (int) ctrlCnlRow["ObjNum"],
                    KPNum = (int) ctrlCnlRow["KPNum"],
                    CmdNum = (int) ctrlCnlRow["CmdNum"],
                    CmdValID = (int) ctrlCnlRow["CmdValID"],
                    FormulaUsed = (bool) ctrlCnlRow["FormulaUsed"],
                    Formula = (string) ctrlCnlRow["Formula"],
                    EvEnabled = (bool) ctrlCnlRow["EvEnabled"]
                };


                // object name definition
                int objRowInd = viewObj.Find(ctrlCnlProps.ObjNum);
                if (objRowInd >= 0)
                    ctrlCnlProps.ObjName = (string) viewObj[objRowInd]["Name"];

                // name definition
                int kpRowInd = viewKP.Find(ctrlCnlProps.KPNum);
                if (kpRowInd >= 0)
                    ctrlCnlProps.KPName = (string) viewKP[kpRowInd]["Name"];

                // defining team values
                int cmdValInd = viewCmdVal.Find(ctrlCnlProps.CmdValID);
                if (cmdValInd >= 0) {
                    var cmdValRowView = viewCmdVal[cmdValInd];
                    ctrlCnlProps.CmdValName = (string) cmdValRowView["Name"];
                    ctrlCnlProps.CmdVal = (string) cmdValRowView["Val"];
                    string[] cmdValArr = ctrlCnlProps.CmdValArr =
                        ctrlCnlProps.CmdVal.Split(FieldSeparator, StringSplitOptions.None);
                    for (var j = 0; j < cmdValArr.Length; j++)
                        cmdValArr[j] = cmdValArr[j].Trim();
                    if (cmdValArr.Length == 1 && cmdValArr[0] == "")
                        ctrlCnlProps.CmdValArr = null;
                }

                ctrlCnlPropsList.Add(ctrlCnlProps);
            }

            return ctrlCnlPropsList;
        }

        /// <summary>
        /// Get the names of the objects, the key - the number of the object
        /// </summary>
        public SortedList<int, string> GetObjNames() {
            return GetNames(baseTables.ObjTable);
        }

        /// <summary>
        /// Get the names of the KP, the key - the number of KP
        /// </summary>
        public SortedList<int, string> GetKPNames() {
            return GetNames(baseTables.KPTable);
        }

        /// <summary>
        /// Get properties of input channel statuses, key - status
        /// </summary>
        public SortedList<int, CnlStatProps> GetCnlStatProps() {
            var tblEvType = baseTables.EvTypeTable;
            int statusCnt = tblEvType.Rows.Count;
            var cnlStatPropsList = new SortedList<int, CnlStatProps>(tblEvType.Rows.Count);

            foreach (DataRow row in tblEvType.Rows) {
                var cnlStatProps = new CnlStatProps((int) row["CnlStatus"]) {
                    Color = (string) row["Color"],
                    Name = (string) row["Name"]
                };

                cnlStatPropsList.Add(cnlStatProps.Status, cnlStatProps);
            }

            return cnlStatPropsList;
        }
    }
}