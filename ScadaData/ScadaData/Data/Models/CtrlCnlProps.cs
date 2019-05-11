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
 * Module   : ScadaData
 * Summary  : Output channel properties
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2008
 * Modified : 2018
 */

using Scada.Data.Configuration;
using Scada.Data.Tables;
using System;
using System.Collections;

namespace Scada.Data.Models {
    /// <inheritdoc />
    /// <summary>
    /// Output channel properties
    /// <para>Management Channel Properties</para>
    /// </summary>
    public class CtrlCnlProps : IComparable<CtrlCnlProps> {
        /// <inheritdoc />
        /// <summary>
        /// A class that allows you to compare the properties of a control channel with an integer
        /// </summary>
        public class IntComparer : IComparer {
            /// <inheritdoc />
            /// <summary>
            /// Compare two objects
            /// </summary>
            public int Compare(object x, object y) {
                int ctrlCnlNum1 = ((CtrlCnlProps) x).CtrlCnlNum;
                var ctrlCnlNum2 = (int) y;
                return ctrlCnlNum1.CompareTo(ctrlCnlNum2);
            }
        }

        /// <summary>
        /// Object to compare control channel properties with integer
        /// </summary>
        public static readonly IntComparer IntComp = new IntComparer();


        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public CtrlCnlProps()
            : this(0, "", BaseValues.CmdTypes.Standard) { }

        /// <summary>
        /// Constructor
        /// </summary>
        public CtrlCnlProps(int ctrlCnlNum, string ctrlCnlName, int cmdTypeID) {
            Active = true;
            CtrlCnlNum = ctrlCnlNum;
            CtrlCnlName = ctrlCnlName;
            CmdTypeID = cmdTypeID;
            CmdTypeName = "";
            ObjNum = 0;
            ObjName = "";
            KPNum = 0;
            KPName = "";
            CmdNum = 0;
            CmdValID = 0;
            CmdValName = "";
            CmdVal = "";
            CmdValArr = null;
            FormulaUsed = false;
            Formula = "";
            EvEnabled = false;
        }


        /// <summary>
        /// Get or set a sign of activity
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Get or set control channel number
        /// </summary>
        public int CtrlCnlNum { get; set; }

        /// <summary>
        /// Get or set control channel name
        /// </summary>
        public string CtrlCnlName { get; set; }

        /// <summary>
        /// Get or set the command type id
        /// </summary>
        public int CmdTypeID { get; set; }

        /// <summary>
        /// Get or set command type name
        /// </summary>
        public string CmdTypeName { get; set; }

        /// <summary>
        /// Get or set object number
        /// </summary>
        public int ObjNum { get; set; }

        /// <summary>
        /// Get or set the name of the object
        /// </summary>
        public string ObjName { get; set; }

        /// <summary>
        /// Get or set KP number
        /// </summary>
        public int KPNum { get; set; }

        /// <summary>
        /// Get or set the name of KP
        /// </summary>
        public string KPName { get; set; }

        /// <summary>
        /// Get or set the command number
        /// </summary>
        public int CmdNum { get; set; }

        /// <summary>
        /// Get or set the command value ID
        /// </summary>
        public int CmdValID { get; set; }

        /// <summary>
        /// Get or set command name values
        /// </summary>
        public string CmdValName { get; set; }

        /// <summary>
        /// Get or set command values
        /// </summary>
        public string CmdVal { get; set; }

        /// <summary>
        /// Get or set an array of command values
        /// </summary>
        public string[] CmdValArr { get; set; }

        /// <summary>
        /// Get or set the sign of using the formula
        /// </summary>
        public bool FormulaUsed { get; set; }

        /// <summary>
        /// Get or set the formula
        /// </summary>
        public string Formula { get; set; }

        /// <summary>
        /// Get or set event recording feature
        /// </summary>
        public bool EvEnabled { get; set; }


        /// <inheritdoc />
        /// <summary>
        /// Compare the current object with another object of the same type.
        /// </summary>
        public int CompareTo(CtrlCnlProps other) {
            return CtrlCnlNum.CompareTo(other.CtrlCnlNum);
        }
    }
}