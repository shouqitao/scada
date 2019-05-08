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
 * Summary  : Formatter for input channel and event data
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2018
 */

using Scada.Data.Configuration;
using Scada.Data.Models;
using Scada.Data.Tables;
using System;
using System.Globalization;
using System.Text;

namespace Scada.Client {
    /// <summary>
    /// Formatter for input channel and event data
    /// <para>Formatting input channel and event data</para>
    /// </summary>
    public class DataFormatter {
        /// <summary>
        /// Delegate receiving input channel status properties
        /// </summary>
        public delegate CnlStatProps GetCnlStatPropsDelegate(int stat);

        /// <summary>
        /// Empty input channel value
        /// </summary>
        public const string EmptyVal = "---";

        /// <summary>
        /// Missing Input Channel Value
        /// </summary>
        protected const string NoVal = "";

        /// <summary>
        /// Input channel value in case of formatting error
        /// </summary>
        protected const string FrmtErrVal = "!";

        /// <summary>
        /// Next hour designation
        /// </summary>
        protected const string NextHourVal = "*";

        /// <summary>
        /// Number of fractional default characters
        /// </summary>
        protected const int DefDecDig = 3;

        /// <summary>
        /// Default color
        /// </summary>
        protected const string DefColor = "black";

        /// <summary>
        /// Color value "On"
        /// </summary>
        protected const string OnColor = "green";

        /// <summary>
        /// Color value "Off"
        /// </summary>
        protected const string OffColor = "red";

        /// <summary>
        /// Current data display time
        /// </summary>
        protected static readonly TimeSpan CurDataVisibleSpan = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Default real number format
        /// </summary>
        protected readonly NumberFormatInfo defNfi;


        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public DataFormatter()
            : this(Localization.Culture) { }

        /// <summary>
        /// Constructor
        /// </summary>
        public DataFormatter(CultureInfo cultureInfo) {
            defNfi = (NumberFormatInfo) cultureInfo.NumberFormat.Clone();
            defNfi.NumberDecimalDigits = DefDecDig;
        }


        /// <summary>
        /// Create a number format based on the default format
        /// </summary>
        protected NumberFormatInfo CreateFormatInfo(int decDig, string decSep, string grSep) {
            var nfi = (NumberFormatInfo) defNfi.Clone();
            nfi.NumberDecimalDigits = decDig;

            if (decSep != null)
                nfi.NumberDecimalSeparator = decSep;

            if (grSep != null)
                nfi.NumberGroupSeparator = grSep;

            return nfi;
        }


        /// <summary>
        /// Format input channel value
        /// </summary>
        public string FormatCnlVal(double val, int stat, InCnlProps cnlProps, bool appendUnit) {
            string text;
            string textWithUnit;
            bool textIsNumber;
            FormatCnlVal(val, stat, cnlProps, null, null, out text, out textWithUnit, out textIsNumber);
            return appendUnit ? textWithUnit : text;
        }

        /// <summary>
        /// Format input channel value
        /// </summary>
        public void FormatCnlVal(double val, int stat, InCnlProps cnlProps,
            out string text, out string textWithUnit) {
            bool textIsNumber;
            FormatCnlVal(val, stat, cnlProps, null, null, out text, out textWithUnit, out textIsNumber);
        }

        /// <summary>
        /// Format input channel value
        /// </summary>
        public void FormatCnlVal(double val, int stat, InCnlProps cnlProps, string decSep, string grSep,
            out string text, out string textWithUnit, out bool textIsNumber, bool throwOnError = false) {
            bool cnlPropsIsNull = cnlProps == null;

            try {
                if (stat <= 0) {
                    text = textWithUnit = EmptyVal;
                    textIsNumber = false;
                } else {
                    text = textWithUnit = NoVal;
                    textIsNumber = false;

                    int formatID = cnlPropsIsNull ? 0 : cnlProps.FormatID;
                    int unitArrLen = cnlPropsIsNull || cnlProps.UnitArr == null ? 0 : cnlProps.UnitArr.Length;

                    if (cnlPropsIsNull || cnlProps.ShowNumber) {
                        // getting dimension
                        string unit = unitArrLen > 0 ? " " + cnlProps.UnitArr[0] : "";

                        // number format definition
                        NumberFormatInfo nfi;
                        bool sepDefined = !(decSep == null && grSep == null);

                        if (cnlPropsIsNull || sepDefined) {
                            nfi = sepDefined ? CreateFormatInfo(DefDecDig, decSep, grSep) : defNfi;
                        } else if (cnlProps.FormatInfo == null) {
                            nfi = cnlProps.FormatInfo = CreateFormatInfo(cnlProps.DecDigits, decSep, grSep);
                        } else {
                            nfi = cnlProps.FormatInfo;
                        }

                        // value formatting
                        text = val.ToString("N", nfi);
                        textWithUnit = text + unit;
                        textIsNumber = true;
                    } else if (formatID == BaseValues.Formats.EnumText) {
                        if (unitArrLen > 0) {
                            var unitInd = (int) val;
                            if (unitInd < 0)
                                unitInd = 0;
                            else if (unitInd >= unitArrLen)
                                unitInd = unitArrLen - 1;
                            text = textWithUnit = cnlProps.UnitArr[unitInd];
                        }
                    } else if (formatID == BaseValues.Formats.AsciiText) {
                        text = textWithUnit = ScadaUtils.DecodeAscii(val);
                    } else if (formatID == BaseValues.Formats.UnicodeText) {
                        text = textWithUnit = ScadaUtils.DecodeUnicode(val);
                    } else if (formatID == BaseValues.Formats.DateTime) {
                        text = textWithUnit = ScadaUtils.DecodeDateTime(val).ToLocalizedString();
                    } else if (formatID == BaseValues.Formats.Date) {
                        text = textWithUnit = ScadaUtils.DecodeDateTime(val).ToLocalizedDateString();
                    } else if (formatID == BaseValues.Formats.Time) {
                        text = textWithUnit = ScadaUtils.DecodeDateTime(val).ToLocalizedTimeString();
                    }
                }
            } catch (Exception ex) {
                if (throwOnError) {
                    string cnlNumStr = cnlPropsIsNull ? "?" : cnlProps.CnlNum.ToString();
                    throw new ScadaException($"Error formatting value of input channel {cnlNumStr}", ex);
                }

                text = textWithUnit = FrmtErrVal;
                textIsNumber = false;
            }
        }

        /// <summary>
        /// Format command value
        /// </summary>
        public string FormatCmdVal(double cmdVal, CtrlCnlProps ctrlCnlProps) {
            if (ctrlCnlProps == null || ctrlCnlProps.CmdValID <= 0) {
                return cmdVal.ToString("N", defNfi);
            }

            int cmdValArrLen = ctrlCnlProps.CmdValArr?.Length ?? 0;

            if (cmdValArrLen <= 0) return NoVal;

            var cmdInd = (int) cmdVal;
            if (cmdInd < 0)
                cmdInd = 0;
            else if (cmdInd >= cmdValArrLen)
                cmdInd = cmdValArrLen - 1;
            return ctrlCnlProps.CmdValArr[cmdInd];
        }

        /// <summary>
        /// Get event text
        /// </summary>
        public string GetEventText(EventTableLight.Event ev, InCnlProps cnlProps, CnlStatProps cnlStatProps) {
            if (string.IsNullOrEmpty(ev.Descr)) {
                // text in the format "<status>: <value>"
                var sbText =
                    cnlStatProps == null ? new StringBuilder() : new StringBuilder(cnlStatProps.Name);

                if (ev.NewCnlStat > BaseValues.CnlStatuses.Undefined) {
                    if (sbText.Length > 0)
                        sbText.Append(": ");
                    sbText.Append(FormatCnlVal(ev.NewCnlVal, ev.NewCnlStat, cnlProps, true));
                }

                return sbText.ToString();
            }

            // description of the event only
            return ev.Descr;
        }

        /// <summary>
        /// Get the color of the input channel value
        /// </summary>
        public string GetCnlValColor(double val, int stat, InCnlProps cnlProps, CnlStatProps cnlStatProps) {
            try {
                if (cnlProps == null) {
                    return DefColor;
                }

                if (cnlProps.ShowNumber ||
                    cnlProps.UnitArr == null || cnlProps.UnitArr.Length != 2 ||
                    stat == BaseValues.CnlStatuses.Undefined ||
                    stat == BaseValues.CnlStatuses.FormulaError ||
                    stat == BaseValues.CnlStatuses.Unreliable) {
                    return cnlStatProps == null || string.IsNullOrEmpty(cnlStatProps.Color)
                        ? DefColor
                        : cnlStatProps.Color;
                }

                return val > 0 ? OnColor : OffColor;
            } catch (Exception ex) {
                string cnlNumStr = cnlProps == null ? cnlProps.CnlNum.ToString() : "?";
                throw new ScadaException($"Error getting color of input channel {cnlNumStr}", ex);
            }
        }

        /// <summary>
        /// Determine whether to display current data
        /// </summary>
        public bool CurDataVisible(DateTime dataAge, DateTime nowDT, out string emptyVal) {
            emptyVal = NoVal;
            return nowDT - dataAge <= CurDataVisibleSpan;
        }

        /// <summary>
        /// Determine the need to display hourly data
        /// </summary>
        public bool HourDataVisible(DateTime dataAge, DateTime nowDT, bool snapshotExists, out string emptyVal) {
            if (snapshotExists || dataAge.Date < nowDT.Date) {
                emptyVal = EmptyVal;
                return snapshotExists;
            }

            if (dataAge.Date > nowDT.Date) {
                emptyVal = NoVal;
                return false;
            }

            if (dataAge.Hour > nowDT.Hour + 1) {
                emptyVal = NoVal;
                return false;
            }

            if (dataAge.Hour == nowDT.Hour + 1) {
                emptyVal = NextHourVal;
                return false;
            }

            emptyVal = EmptyVal;

            return false;
        }
    }
}