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
 * Summary  : Input channel properties
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2005
 * Modified : 2018
 */

using Scada.Data.Configuration;
using System;
using System.Collections;
using System.Globalization;

namespace Scada.Data.Models {
    /// <inheritdoc />
    /// <summary>
    /// Input channel properties
    /// <para>Input Channel Properties</para>
    /// </summary>
    public class InCnlProps : IComparable<InCnlProps> {
        /// <inheritdoc />
        /// <summary>
        /// A class that allows you to compare the properties of the input channel with an integer
        /// </summary>
        public class IntComparer : IComparer {
            /// <inheritdoc />
            /// <summary>
            /// Compare two objects
            /// </summary>
            public int Compare(object x, object y) {
                int cnlNum1 = ((InCnlProps) x).CnlNum;
                var cnlNum2 = (int) y;
                return cnlNum1.CompareTo(cnlNum2);
            }
        }

        /// <summary>
        /// The object to compare the properties of the input channel with an integer
        /// </summary>
        public static readonly IntComparer IntComp = new IntComparer();


        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public InCnlProps()
            : this(0, "", BaseValues.CnlTypes.TI) { }

        /// <summary>
        /// Constructor
        /// </summary>
        public InCnlProps(int cnlNum, string cnlName, int cnlTypeID) {
            Active = true;
            CnlNum = cnlNum;
            CnlName = cnlName;
            CnlTypeID = cnlTypeID;
            CnlTypeName = "";
            ObjNum = 0;
            ObjName = "";
            KPNum = 0;
            KPName = "";
            Signal = 0;
            FormulaUsed = false;
            Formula = "";
            Averaging = false;
            ParamID = 0;
            ParamName = "";
            IconFileName = "";
            FormatID = 0;
            FormatName = "";
            ShowNumber = true;
            DecDigits = 3;
            FormatInfo = null;
            UnitID = 0;
            UnitName = "";
            UnitSign = "";
            UnitArr = null;
            CtrlCnlNum = 0;
            EvEnabled = false;
            EvSound = false;
            EvOnChange = false;
            EvOnUndef = false;
            LimLowCrash = double.NaN;
            LimLow = double.NaN;
            LimHigh = double.NaN;
            LimHighCrash = double.NaN;
        }


        /// <summary>
        /// Get or set a sign of activity
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Get or set the input channel number
        /// </summary>
        public int CnlNum { get; set; }

        /// <summary>
        /// Get or set the name of the input channel
        /// </summary>
        public string CnlName { get; set; }

        /// <summary>
        /// Get or set the channel type identifier
        /// </summary>
        public int CnlTypeID { get; set; }

        /// <summary>
        /// Get or set channel type name
        /// </summary>
        public string CnlTypeName { get; set; }

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
        /// Get or set a signal (tag number KP)
        /// </summary>
        public int Signal { get; set; }

        /// <summary>
        /// Get or set the sign of using the formula
        /// </summary>
        public bool FormulaUsed { get; set; }

        /// <summary>
        /// Get or set the formula
        /// </summary>
        public string Formula { get; set; }

        /// <summary>
        /// Get or set the averaging attribute
        /// </summary>
        public bool Averaging { get; set; }

        /// <summary>
        /// Get or set the parameter ID
        /// </summary>
        public int ParamID { get; set; }

        /// <summary>
        /// Get or set parameter name
        /// </summary>
        public string ParamName { get; set; }

        /// <summary>
        /// Get or set short icon file name
        /// </summary>
        public string IconFileName { get; set; }

        /// <summary>
        /// Get or set format identifier
        /// </summary>
        public int FormatID { get; set; }

        /// <summary>
        /// Get or set format name
        /// </summary>
        public string FormatName { get; set; }

        /// <summary>
        /// Get or set the sign of the output value of the channel as a number
        /// </summary>
        public bool ShowNumber { get; set; }

        /// <summary>
        /// Get or set the number of fractional characters when displaying the value
        /// </summary>
        public int DecDigits { get; set; }

        /// <summary>
        /// Get or set formatting values
        /// </summary>
        /// <remarks>The property is necessary to optimize the formatting of the channel values.</remarks>
        public NumberFormatInfo FormatInfo { get; set; }

        /// <summary>
        /// Get or set dimension identifier
        /// </summary>
        public int UnitID { get; set; }

        /// <summary>
        /// Get or set the name of the dimension
        /// </summary>
        public string UnitName { get; set; }

        /// <summary>
        /// Get or set dimensions
        /// </summary>
        public string UnitSign { get; set; }

        /// <summary>
        /// Get or set an array of dimensions
        /// </summary>
        public string[] UnitArr { get; set; }

        /// <summary>
        /// Get the dimension of the numeric channel if it is the only one
        /// </summary>
        public string SingleUnit {
            get { return ShowNumber && UnitArr != null && UnitArr.Length == 1 ? UnitArr[0] : ""; }
        }

        /// <summary>
        /// Get or set control channel number
        /// </summary>
        public int CtrlCnlNum { get; set; }

        /// <summary>
        /// Get or set event recording feature
        /// </summary>
        public bool EvEnabled { get; set; }

        /// <summary>
        /// Get or set the event sound event sign
        /// </summary>
        public bool EvSound { get; set; }

        /// <summary>
        /// Get or set the sign of event recording by change
        /// </summary>
        public bool EvOnChange { get; set; }

        /// <summary>
        /// Get or set the sign of recording events for an undefined state
        /// </summary>
        public bool EvOnUndef { get; set; }

        /// <summary>
        /// Get or set lower alarm limit
        /// </summary>
        public double LimLowCrash { get; set; }

        /// <summary>
        /// Get or set lower bound
        /// </summary>
        public double LimLow { get; set; }

        /// <summary>
        /// Get or set upper bound
        /// </summary>
        public double LimHigh { get; set; }

        /// <summary>
        /// Get or set upper alarm limit
        /// </summary>
        public double LimHighCrash { get; set; }


        /// <inheritdoc />
        /// <summary>
        /// Compare the current object with another object of the same type.
        /// </summary>
        public int CompareTo(InCnlProps other) {
            return CnlNum.CompareTo(other.CnlNum);
        }
    }
}