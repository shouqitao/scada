﻿/*
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
 * Summary  : The utility methods for the application
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2010
 * Modified : 2018
 */

using Scada;
using Scada.UI;
using System.IO;
using Utils;

namespace ScadaAdmin {
    /// <summary>
    /// The utility methods for the application
    /// <para>Application helper methods</para>
    /// </summary>
    internal static class AppUtils {
        /// <summary>
        /// Handle an error by logging information and displaying a message.
        /// </summary>
        public static void ProcError(string message) {
            AppData.ErrLog.WriteAction(message, Log.ActTypes.Exception);
            ScadaUiUtils.ShowError(message);
        }


        /// <summary>
        /// Check if the integer value is correct
        /// </summary>
        public static bool ValidateInt(string val, int minVal, int maxVal, out string errMsg) {
            int intVal;
            if (int.TryParse(val, out intVal) && minVal <= intVal && intVal <= maxVal) {
                errMsg = "";
                return true;
            } else {
                errMsg = string.Format(CommonPhrases.IntegerRangingRequired, minVal, maxVal);
                return false;
            }
        }

        /// <summary>
        /// Check if the real value is correct
        /// </summary>
        public static bool ValidateDouble(string val, out string errMsg) {
            if (double.TryParse(val, out double doubleVal)) {
                errMsg = "";
                return true;
            } else {
                errMsg = CommonPhrases.RealRequired;
                return false;
            }
        }

        /// <summary>
        /// Verify the string value
        /// </summary>
        public static bool ValidateStr(string val, int maxLen, out string errMsg) {
            if (val == null || maxLen <= 0 || val.Length <= maxLen) {
                errMsg = "";
                return true;
            } else {
                errMsg = string.Format(CommonPhrases.LineLengthLimit, maxLen);
                return false;
            }
        }


        /// <summary>
        /// Print header with underscore
        /// </summary>
        public static void WriteTitle(StreamWriter writer, string title) {
            writer.WriteLine(title);
            writer.WriteLine(new string('-', title.Length));
        }
    }
}