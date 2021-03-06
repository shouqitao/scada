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
 * Module   : SCADA-Administrator
 * Summary  : Creating channels according to specified devices
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2015
 * Modified : 2016
 */

using Scada;
using Scada.Comm.Devices;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Drawing;
using System.IO;
using System.Text;

namespace ScadaAdmin {
    /// <summary>
    /// Creating channels according to selected devices
    /// <para>根据指定的设备创建通道</para>
    /// </summary>
    internal static class CreateCnls {
        /// <summary>
        /// The state of the library KP
        /// </summary>
        public enum DllStates {
            NotFound,
            Loaded,
            Error
        }

        /// <summary>
        /// KP information for which channels are created
        /// </summary>
        public class KPInfo {
            /// <summary>
            /// 构造函数
            /// </summary>
            private KPInfo() {
                Enabled = false;
                Color = Color.Gray;
                DefaultCnls = null;

                Selected = false;
                KPNum = 0;
                KPName = "";
                CommLineNum = 0;
                ObjNum = DBNull.Value;
                DllFileName = "";
                DllState = DllStates.NotFound;

                InCnlNumsErr = false;
                FirstInCnlNum = -1;
                LastInCnlNum = -1;

                CtrlCnlNumsErr = false;
                FirstCtrlCnlNum = -1;
                LastCtrlCnlNum = -1;
            }

            /// <summary>
            /// Get or set the flag that selection is allowed
            /// </summary>
            public bool Enabled { get; set; }

            /// <summary>
            /// Get or set the color of the corresponding row in the table
            /// </summary>
            public Color Color { get; set; }

            /// <summary>
            /// Get or set prototypes of default KP channels
            /// </summary>
            public KPView.KPCnlPrototypes DefaultCnls { get; set; }

            /// <summary>
            /// Get or set the flag that KP is selected
            /// </summary>
            public bool Selected { get; set; }

            /// <summary>
            /// Get or set KP number
            /// </summary>
            public int KPNum { get; set; }

            /// <summary>
            /// Get or set the name of KP
            /// </summary>
            public string KPName { get; set; }

            /// <summary>
            /// Get or set the line number
            /// </summary>
            public int CommLineNum { get; set; }

            /// <summary>
            /// Get or set object number
            /// </summary>
            public object ObjNum { get; set; }

            /// <summary>
            /// Get or set the name of the DLL file
            /// </summary>
            public string DllFileName { get; set; }

            /// <summary>
            /// Get or set DLL status
            /// </summary>
            public DllStates DllState { get; set; }

            /// <summary>
            /// Get file name and DLL loading status
            /// </summary>
            public string DllWithState {
                get {
                    string state;
                    switch (DllState) {
                        case DllStates.NotFound:
                            state = AppPhrases.DllNotFound;
                            break;
                        case DllStates.Loaded:
                            state = AppPhrases.DllLoaded;
                            break;
                        default: // DllStates.Error
                            state = AppPhrases.DllError;
                            break;
                    }

                    return DllFileName + " (" + state + ")";
                }
            }

            /// <summary>
            /// Get or set an error sign when calculating input channel numbers
            /// </summary>
            public bool InCnlNumsErr { get; set; }

            /// <summary>
            /// Get or set the number of the first input channel
            /// </summary>
            public int FirstInCnlNum { get; set; }

            /// <summary>
            /// Get or set the last input channel number
            /// </summary>
            public int LastInCnlNum { get; set; }

            /// <summary>
            /// Get input channel numbers
            /// </summary>
            public string InCnls {
                get {
                    return
                        InCnlNumsErr ? AppPhrases.DevCalcError :
                        FirstInCnlNum < 0 ? "" :
                        FirstInCnlNum == 0 ? AppPhrases.DevHasNoCnls :
                        FirstInCnlNum == LastInCnlNum ? FirstInCnlNum.ToString() :
                        FirstInCnlNum + " - " + LastInCnlNum;
                }
            }

            /// <summary>
            /// Get or set an error sign when calculating control channel numbers
            /// </summary>
            public bool CtrlCnlNumsErr { get; set; }

            /// <summary>
            /// Get or set the number of the first control channel
            /// </summary>
            public int FirstCtrlCnlNum { get; set; }

            /// <summary>
            /// Get or set the last control channel number
            /// </summary>
            public int LastCtrlCnlNum { get; set; }

            /// <summary>
            /// Get control channel numbers
            /// </summary>
            public string CtrlCnls {
                get {
                    return
                        CtrlCnlNumsErr ? AppPhrases.DevCalcError :
                        FirstCtrlCnlNum < 0 ? "" :
                        FirstCtrlCnlNum == 0 ? AppPhrases.DevHasNoCnls :
                        FirstCtrlCnlNum == LastCtrlCnlNum ? FirstCtrlCnlNum.ToString() :
                        FirstCtrlCnlNum + " - " + LastCtrlCnlNum;
                }
            }

            /// <summary>
            /// Create KP information object
            /// </summary>
            public static KPInfo Create(DataRow rowKP, DataTable tblKPType) {
                var kpInfo = new KPInfo {
                    KPNum = (int) rowKP["KPNum"],
                    KPName = (string) rowKP["Name"]
                };
                var commLineNum = rowKP["CommLineNum"];
                kpInfo.CommLineNum = commLineNum == DBNull.Value ? 0 : (int) commLineNum;

                tblKPType.DefaultView.RowFilter = "KPTypeID = " + rowKP["KPTypeID"];
                var dllFileName = tblKPType.DefaultView[0]["DllFileName"];
                kpInfo.DllFileName = dllFileName == null || dllFileName == DBNull.Value ? "" : (string) dllFileName;

                return kpInfo;
            }

            /// <summary>
            /// Set input channel numbers
            /// </summary>
            public void SetInCnlNums(bool error, int firstNum = 0, int lastNum = 0) {
                InCnlNumsErr = error;
                FirstInCnlNum = firstNum;
                LastInCnlNum = lastNum;
            }

            /// <summary>
            /// Set control channel numbers
            /// </summary>
            public void SetCtrlCnlNums(bool error, int firstNum = 0, int lastNum = 0) {
                CtrlCnlNumsErr = error;
                FirstCtrlCnlNum = firstNum;
                LastCtrlCnlNum = lastNum;
            }
        }

        /// <summary>
        /// Channel Numbering Options
        /// </summary>
        public class CnlNumParams {
            /// <summary>
            /// Get or set the starting channel number
            /// </summary>
            public int Start { get; set; }

            /// <summary>
            /// Get or set the multiplicity of the first channel for KP
            /// </summary>
            public int Multiple { get; set; }

            /// <summary>
            /// Get or set the offset of the first channel for KP
            /// </summary>
            public int Shift { get; set; }

            /// <summary>
            /// Get or set the minimum number of free channel numbers between KP
            /// </summary>
            public int Space { get; set; }
        }


        /// <summary>
        /// Calculate the numbers of the first and last channels KP
        /// </summary>
        private static void CalcFirstAndLastNums(int curCnlNum, int curCnlInd, List<int> cnlNums, int cnlNumsCnt,
            int cnlCnt, int cnlsSpace, int cnlsMultiple, out int firstCnlNum, out int lastCnlNum, out int newInCnlInd) {
            firstCnlNum = curCnlNum;
            var defined = false;

            do {
                lastCnlNum = firstCnlNum + cnlCnt - 1;
                // Search for the index of the busy channel whose number is greater than or equal to the first
                while (curCnlInd < cnlNumsCnt && cnlNums[curCnlInd] < firstCnlNum)
                    curCnlInd++;
                // validation of the first channel KP
                if (curCnlInd > 0 && firstCnlNum - cnlNums[curCnlInd - 1] <= cnlsSpace)
                    firstCnlNum += cnlsMultiple;
                // check the validity of the last channel KP
                else if (curCnlInd < cnlNumsCnt && cnlNums[curCnlInd] - lastCnlNum <= cnlsSpace)
                    firstCnlNum += cnlsMultiple;
                else
                    defined = true;
            } while (!defined);

            newInCnlInd = curCnlInd;
        }

        /// <summary>
        /// Identification identifiers in the directory
        /// </summary>
        private static bool FindDictIDs(SortedList<string, int> dictList, DataTable dataTable, string idName,
            StreamWriter writer, string errDescr) {
            dataTable.DefaultView.Sort = "Name";

            for (var i = 0; i < dictList.Count; i++) {
                string key = dictList.Keys[i];
                int ind = dataTable.DefaultView.Find(key);
                if (ind < 0) {
                    writer.WriteLine(errDescr, key);
                    return false;
                } else {
                    dictList[key] = (int) dataTable.DefaultView[ind][idName];
                }
            }

            return true;
        }

        /// <summary>
        /// Create Input Channel Line
        /// </summary>
        private static DataRow CreateInCnlRow(DataTable tblInCnl, DataTable tblFormat,
            SortedList<string, int> paramList, SortedList<string, int> unitList,
            KPView.InCnlPrototype inCnl, object objNum, int kpNum, string kpNameToInsert, StreamWriter writer) {
            var newInCnlRow = tblInCnl.NewRow();
            newInCnlRow["CnlNum"] = inCnl.CnlNum;
            newInCnlRow["Active"] = true;

            int maxCnlNameLen = tblInCnl.Columns["Name"].MaxLength;
            string cnlName = kpNameToInsert + inCnl.CnlName;
            if (cnlName.Length > maxCnlNameLen) {
                cnlName = cnlName.Substring(0, maxCnlNameLen);
                writer.WriteLine(string.Format(AppPhrases.InCnlNameTrancated, inCnl.CnlNum));
            }

            newInCnlRow["Name"] = cnlName;

            newInCnlRow["CnlTypeID"] = inCnl.CnlTypeID;
            newInCnlRow["ObjNum"] = objNum;
            newInCnlRow["KPNum"] = kpNum;
            newInCnlRow["Signal"] = inCnl.Signal;
            newInCnlRow["FormulaUsed"] = inCnl.FormulaUsed;
            newInCnlRow["Formula"] = inCnl.Formula;
            newInCnlRow["Averaging"] = inCnl.Averaging;
            newInCnlRow["ParamID"] = string.IsNullOrEmpty(inCnl.ParamName)
                ? DBNull.Value
                : (object) paramList[inCnl.ParamName];

            newInCnlRow["FormatID"] = DBNull.Value;
            if (inCnl.ShowNumber) {
                int ind = tblFormat.DefaultView.Find(new object[] {true, inCnl.DecDigits});
                if (ind >= 0)
                    newInCnlRow["FormatID"] = tblFormat.DefaultView[ind]["FormatID"];
                else
                    writer.WriteLine(string.Format(AppPhrases.NumFormatNotFound, inCnl.CnlNum, inCnl.DecDigits));
            } else {
                int ind = tblFormat.DefaultView.Find(new object[] {false, DBNull.Value});
                if (ind >= 0)
                    newInCnlRow["FormatID"] = tblFormat.DefaultView[ind]["FormatID"];
                else
                    writer.WriteLine(string.Format(AppPhrases.TextFormatNotFound, inCnl.CnlNum));
            }

            newInCnlRow["UnitID"] =
                string.IsNullOrEmpty(inCnl.UnitName) ? DBNull.Value : (object) unitList[inCnl.UnitName];
            newInCnlRow["CtrlCnlNum"] =
                inCnl.CtrlCnlProps != null && inCnl.CtrlCnlProps.CtrlCnlNum > 0
                    ? (object) inCnl.CtrlCnlProps.CtrlCnlNum
                    : DBNull.Value;
            newInCnlRow["EvEnabled"] = inCnl.EvEnabled;
            newInCnlRow["EvSound"] = inCnl.EvSound;
            newInCnlRow["EvOnChange"] = inCnl.EvOnChange;
            newInCnlRow["EvOnUndef"] = inCnl.EvOnUndef;
            newInCnlRow["LimLowCrash"] = double.IsNaN(inCnl.LimLowCrash) ? DBNull.Value : (object) inCnl.LimLowCrash;
            newInCnlRow["LimLow"] = double.IsNaN(inCnl.LimLow) ? DBNull.Value : (object) inCnl.LimLow;
            newInCnlRow["LimHigh"] = double.IsNaN(inCnl.LimHigh) ? DBNull.Value : (object) inCnl.LimHigh;
            newInCnlRow["LimHighCrash"] = double.IsNaN(inCnl.LimHighCrash) ? DBNull.Value : (object) inCnl.LimHighCrash;
            newInCnlRow["ModifiedDT"] = DateTime.Now;

            return newInCnlRow;
        }

        /// <summary>
        /// Create a control channel string
        /// </summary>
        private static DataRow CreateCtrlCnlRow(DataTable tblCtrlCnl, SortedList<string, int> cmdValList,
            KPView.CtrlCnlPrototype ctrlCnl, object objNum, int kpNum, string kpNameToInsert, StreamWriter writer) {
            var newCtrlCnlRow = tblCtrlCnl.NewRow();
            newCtrlCnlRow["CtrlCnlNum"] = ctrlCnl.CtrlCnlNum;
            newCtrlCnlRow["Active"] = true;

            int maxCtrlCnlNameLen = tblCtrlCnl.Columns["Name"].MaxLength;
            string ctrlCnlName = kpNameToInsert + ctrlCnl.CtrlCnlName;
            if (ctrlCnlName.Length > maxCtrlCnlNameLen) {
                ctrlCnlName = ctrlCnlName.Substring(0, maxCtrlCnlNameLen);
                writer.WriteLine(string.Format(AppPhrases.CtrlCnlNameTrancated, ctrlCnl.CtrlCnlNum));
            }

            newCtrlCnlRow["Name"] = ctrlCnlName;

            newCtrlCnlRow["CmdTypeID"] = ctrlCnl.CmdTypeID;
            newCtrlCnlRow["ObjNum"] = objNum;
            newCtrlCnlRow["KPNum"] = kpNum;
            newCtrlCnlRow["CmdNum"] = ctrlCnl.CmdNum;
            newCtrlCnlRow["CmdValID"] = string.IsNullOrEmpty(ctrlCnl.CmdValName)
                ? DBNull.Value
                : (object) cmdValList[ctrlCnl.CmdValName];
            newCtrlCnlRow["FormulaUsed"] = ctrlCnl.FormulaUsed;
            newCtrlCnlRow["Formula"] = ctrlCnl.Formula;
            newCtrlCnlRow["EvEnabled"] = ctrlCnl.EvEnabled;
            newCtrlCnlRow["ModifiedDT"] = DateTime.Now;

            return newCtrlCnlRow;
        }

        /// <summary>
        /// Save feeds to DB
        /// </summary>
        private static bool UpdateCnls(DataTable dataTable, string descr, StreamWriter writer, out int updRowCnt) {
            updRowCnt = 0;
            var errRowCnt = 0;
            DataRow[] rowsInError = null;

            var sqlAdapter = dataTable.ExtendedProperties["DataAdapter"] as SqlCeDataAdapter;
            updRowCnt = sqlAdapter.Update(dataTable);

            if (dataTable.HasErrors) {
                rowsInError = dataTable.GetErrors();
                errRowCnt = rowsInError.Length;
            }

            if (errRowCnt == 0) {
                writer.WriteLine(string.Format(descr, updRowCnt));
            } else {
                writer.WriteLine(string.Format(descr, updRowCnt) + " " +
                                 string.Format(AppPhrases.ErrorsCount, errRowCnt));
                foreach (var row in rowsInError)
                    writer.WriteLine(string.Format(AppPhrases.CnlError, row[0], row.RowError));
            }

            return errRowCnt == 0;
        }


        /// <summary>
        /// Calculate the channel numbers and write them to the list of information on KP
        /// </summary>
        public static bool CalcCnlNums(Dictionary<string, Type> kpViewTypes, List<KPInfo> kpInfoList,
            Scada.Comm.AppDirs commDirs, List<int> inCnlNums, CnlNumParams inCnlNumParams,
            List<int> ctrlCnlNums, CnlNumParams ctrlCnlNumParams, out string errMsg) {
            if (kpViewTypes == null)
                throw new ArgumentNullException(nameof(kpViewTypes));
            if (kpInfoList == null)
                throw new ArgumentNullException(nameof(kpInfoList));
            if (inCnlNums == null)
                throw new ArgumentNullException(nameof(inCnlNums));
            if (inCnlNumParams == null)
                throw new ArgumentNullException(nameof(inCnlNumParams));
            if (ctrlCnlNums == null)
                throw new ArgumentNullException(nameof(ctrlCnlNums));
            if (ctrlCnlNumParams == null)
                throw new ArgumentNullException(nameof(ctrlCnlNumParams));

            var hasChannels = false;
            var hasErrors = false;
            errMsg = "";

            try {
                // loading SCADA-Communicator settings
                Scada.Comm.Settings commSett = new Scada.Comm.Settings();
                if (!commSett.Load(commDirs.ConfigDir + Scada.Comm.Settings.DefFileName, out errMsg))
                    throw new Exception(errMsg);

                // filling the directory of KP properties
                Dictionary<int, KPView.KPProperties> kpPropsDict = new Dictionary<int, KPView.KPProperties>();
                foreach (Scada.Comm.Settings.CommLine commLine in commSett.CommLines) {
                    foreach (Scada.Comm.Settings.KP kp in commLine.ReqSequence) {
                        if (!kpPropsDict.ContainsKey(kp.Number))
                            kpPropsDict.Add(kp.Number, new KPView.KPProperties(commLine.CustomParams, kp.CmdLine));
                    }
                }

                // determining the starting number of the input channel
                int inCnlsStart = inCnlNumParams.Start;
                int inCnlsMultiple = inCnlNumParams.Multiple;
                int inCnlsSpace = inCnlNumParams.Space;
                int remainder = inCnlsStart % inCnlsMultiple;
                int curInCnlNum = remainder > 0 ? inCnlsStart - remainder : inCnlsStart;
                curInCnlNum += inCnlNumParams.Shift;
                if (curInCnlNum < inCnlsStart)
                    curInCnlNum += inCnlsMultiple;

                // determination of the control channel start number
                int ctrlCnlsStart = ctrlCnlNumParams.Start;
                int ctrlCnlsMultiple = ctrlCnlNumParams.Multiple;
                int ctrlCnlsSpace = ctrlCnlNumParams.Space;
                remainder = ctrlCnlsStart % ctrlCnlNumParams.Multiple;
                int curCtrlCnlNum = remainder > 0 ? ctrlCnlsStart - remainder : ctrlCnlsStart;
                curCtrlCnlNum += ctrlCnlNumParams.Shift;
                if (curCtrlCnlNum < ctrlCnlNumParams.Start)
                    curCtrlCnlNum += ctrlCnlNumParams.Multiple;

                // calculation channel numbers KP
                var curInCnlInd = 0;
                int inCnlNumsCnt = inCnlNums.Count;
                var curCtrlCnlInd = 0;
                int ctrlCnlNumsCnt = ctrlCnlNums.Count;

                foreach (var kpInfo in kpInfoList) {
                    if (kpInfo.Selected) {
                        // getting interface type kp
                        if (!kpViewTypes.TryGetValue(kpInfo.DllFileName, out Type kpViewType))
                            continue;

                        // instantiating a KP interface class
                        KPView kpView = null;
                        try {
                            kpView = KPFactory.GetKPView(kpViewType, kpInfo.KPNum);
                            KPView.KPProperties kpProps;
                            if (kpPropsDict.TryGetValue(kpInfo.KPNum, out kpProps))
                                kpView.KPProps = kpProps;
                            kpView.AppDirs = commDirs;
                        } catch {
                            kpInfo.SetInCnlNums(true);
                            kpInfo.SetCtrlCnlNums(true);
                            continue;
                        }

                        // default prototype channel acquisition
                        try {
                            kpInfo.DefaultCnls = kpView.DefaultCnls;
                        } catch {
                            kpInfo.SetInCnlNums(true);
                            kpInfo.SetCtrlCnlNums(true);
                            continue;
                        }

                        // definition of numbers of input channels, taking into account the numbers occupied by existing channels
                        if (kpInfo.DefaultCnls != null && kpInfo.DefaultCnls.InCnls.Count > 0) {
                            hasChannels = true;

                            int firstInCnlNum; // first input channel number
                            int lastInCnlNum; // number of last input channel KP
                            int newInCnlInd; // new index of the number of input channels
                            CalcFirstAndLastNums(curInCnlNum, curInCnlInd, inCnlNums, inCnlNumsCnt,
                                kpInfo.DefaultCnls.InCnls.Count, inCnlsSpace, inCnlsMultiple,
                                out firstInCnlNum, out lastInCnlNum, out newInCnlInd);

                            if (lastInCnlNum > ushort.MaxValue) {
                                hasErrors = true;
                                kpInfo.SetInCnlNums(true);
                            } else {
                                kpInfo.SetInCnlNums(false, firstInCnlNum, lastInCnlNum);

                                curInCnlInd = newInCnlInd;
                                curInCnlNum = firstInCnlNum;
                                do {
                                    curInCnlNum += inCnlsMultiple;
                                } while (curInCnlNum - lastInCnlNum <= inCnlsSpace);
                            }
                        } else {
                            // channel numbers are not assigned because KP does not support input channel creation
                            kpInfo.SetInCnlNums(false);
                        }

                        // identification of control channel numbers with regard to numbers occupied by existing channels
                        if (kpInfo.DefaultCnls != null && kpInfo.DefaultCnls.CtrlCnls.Count > 0) {
                            hasChannels = true;

                            int firstCtrlCnlNum; // first control channel number
                            int lastCtrlCnlNum; // control channel last control channel number
                            int newCtrlCnlInd; // new index of the list of control channel numbers
                            CalcFirstAndLastNums(curCtrlCnlNum, curCtrlCnlInd, ctrlCnlNums, ctrlCnlNumsCnt,
                                kpInfo.DefaultCnls.CtrlCnls.Count, ctrlCnlsSpace, ctrlCnlsMultiple,
                                out firstCtrlCnlNum, out lastCtrlCnlNum, out newCtrlCnlInd);

                            if (lastCtrlCnlNum > ushort.MaxValue) {
                                hasErrors = true;
                                kpInfo.SetCtrlCnlNums(true);
                            } else {
                                kpInfo.SetCtrlCnlNums(false, firstCtrlCnlNum, lastCtrlCnlNum);

                                curCtrlCnlInd = newCtrlCnlInd;
                                curCtrlCnlNum = firstCtrlCnlNum;
                                do {
                                    curCtrlCnlNum += ctrlCnlsMultiple;
                                } while (curCtrlCnlNum - lastCtrlCnlNum <= ctrlCnlsSpace);
                            }
                        } else {
                            // channel numbers are not assigned because KP does not support the creation of control channels
                            kpInfo.SetCtrlCnlNums(false);
                        }
                    } else {
                        // channel numbers are not assigned because KP is not selected
                        kpInfo.SetInCnlNums(false, -1, -1);
                        kpInfo.SetCtrlCnlNums(false, -1, -1);
                    }
                }

                if (hasErrors)
                    errMsg = AppPhrases.CalcCnlNumsErrors;
                else if (!hasChannels)
                    errMsg = AppPhrases.CreatedCnlsMissing;
            } catch (Exception ex) {
                hasErrors = true;
                errMsg = AppPhrases.CalcCnlNumsError + ":\r\n" + ex.Message;
            }

            return hasChannels && !hasErrors;
        }

        /// <summary>
        /// Create channels in the configuration database using previously calculated channel numbers and prototypes
        /// </summary>
        public static bool CreateChannels(List<KPInfo> kpInfoList, bool insertKPName,
            string logFileName, out bool logCreated, out string msg) {
            logCreated = false;
            msg = "";
            StreamWriter writer = null;

            try {
                // creating a channel creation magazine
                writer = new StreamWriter(logFileName, false, Encoding.UTF8);
                logCreated = true;

                string title = DateTime.Now.ToString("G", Localization.Culture) + " " + AppPhrases.CreateCnlsTitle;
                writer.WriteLine(title);
                writer.WriteLine(new string('-', title.Length));
                writer.WriteLine();

                // generation of lists of identifiers of used values from configuration reference books
                var paramList = new SortedList<string, int>();
                var unitList = new SortedList<string, int>();
                var cmdValList = new SortedList<string, int>();

                foreach (var kpInfo in kpInfoList) {
                    if (kpInfo.DefaultCnls != null) {
                        foreach (KPView.InCnlPrototype inCnl in kpInfo.DefaultCnls.InCnls) {
                            string s = inCnl.ParamName;
                            if (!string.IsNullOrEmpty(s) && !paramList.ContainsKey(s))
                                paramList.Add(s, -1);

                            s = inCnl.UnitName;
                            if (!string.IsNullOrEmpty(s) && !unitList.ContainsKey(s))
                                unitList.Add(s, -1);
                        }

                        foreach (KPView.CtrlCnlPrototype ctrlCnl in kpInfo.DefaultCnls.CtrlCnls) {
                            string s = ctrlCnl.CmdValName;
                            if (!string.IsNullOrEmpty(s) && !cmdValList.ContainsKey(s))
                                cmdValList.Add(s, -1);
                        }
                    }
                }

                // identifying identifiers from configuration reference directories
                writer.WriteLine(AppPhrases.CheckDicts);
                bool paramError = !FindDictIDs(paramList, Tables.GetParamTable(), "ParamID", writer,
                    AppPhrases.ParamNotFound);
                bool unitError = !FindDictIDs(unitList, Tables.GetUnitTable(), "UnitID", writer,
                    AppPhrases.UnitNotFound);
                bool cmdValError = !FindDictIDs(cmdValList, Tables.GetCmdValTable(), "CmdValID", writer,
                    AppPhrases.CmdValsNotFound);

                if (paramError || unitError || cmdValError) {
                    msg = AppPhrases.CreateCnlsImpossible;
                    writer.WriteLine(msg);
                    return false;
                } else {
                    writer.WriteLine(AppPhrases.CreateCnlsStart);

                    // filling in the tables of input channels and control channels
                    var tblInCnl = new DataTable("InCnl");
                    var tblCtrlCnl = new DataTable("CtrlCnl");
                    Tables.FillTableSchema(tblInCnl);
                    Tables.FillTableSchema(tblCtrlCnl);

                    // getting table of number formats
                    var tblFormat = Tables.GetFormatTable();
                    tblFormat.DefaultView.Sort = "ShowNumber, DecDigits";

                    // creation of channels for KP
                    foreach (var kpInfo in kpInfoList) {
                        if (kpInfo.Selected) {
                            int inCnlNum = kpInfo.FirstInCnlNum;
                            int ctrlCnlNum = kpInfo.FirstCtrlCnlNum;
                            var objNum = kpInfo.ObjNum;
                            int kpNum = kpInfo.KPNum;
                            string kpNameToInsert = insertKPName ? kpInfo.KPName + " - " : "";

                            // creation of control channels
                            foreach (KPView.CtrlCnlPrototype ctrlCnl in kpInfo.DefaultCnls.CtrlCnls) {
                                ctrlCnl.CtrlCnlNum = ctrlCnlNum;
                                var newCtrlCnlRow = CreateCtrlCnlRow(tblCtrlCnl, cmdValList,
                                    ctrlCnl, objNum, kpNum, kpNameToInsert, writer);
                                tblCtrlCnl.Rows.Add(newCtrlCnlRow);
                                ctrlCnlNum++;
                            }

                            // creating input channels
                            foreach (KPView.InCnlPrototype inCnl in kpInfo.DefaultCnls.InCnls) {
                                inCnl.CnlNum = inCnlNum;
                                var newInCnlRow = CreateInCnlRow(tblInCnl, tblFormat, paramList, unitList,
                                    inCnl, objNum, kpNum, kpNameToInsert, writer);
                                tblInCnl.Rows.Add(newInCnlRow);
                                inCnlNum++;
                            }
                        }
                    }

                    // saving channels in the database
                    int updRowCnt1,
                        updRowCnt2;
                    bool updateOK = UpdateCnls(tblCtrlCnl, AppPhrases.AddedCtrlCnlsCount, writer, out updRowCnt1);
                    updateOK = UpdateCnls(tblInCnl, AppPhrases.AddedInCnlsCount, writer, out updRowCnt2) && updateOK;
                    msg = updateOK ? AppPhrases.CreateCnlsComplSucc : AppPhrases.CreateCnlsComplWithErr;
                    writer.WriteLine();
                    writer.WriteLine(msg);

                    if (updRowCnt1 + updRowCnt2 > 0)
                        msg += AppPhrases.RefreshRequired;

                    return updateOK;
                }
            } catch (Exception ex) {
                msg = AppPhrases.CreateCnlsError + ":\r\n" + ex.Message;
                try {
                    writer.WriteLine(msg);
                } catch {
                    // ignored
                }

                return false;
            } finally {
                try {
                    writer.Close();
                } catch {
                    // ignored
                }
            }
        }
    }
}