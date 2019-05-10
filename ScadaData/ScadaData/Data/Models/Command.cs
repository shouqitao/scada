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
 * Summary  : TeleControl command
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2015
 * Modified : 2017
 */

using Scada.Data.Configuration;
using System;
using System.Text;

namespace Scada.Data.Models {
    /// <summary>
    /// TeleControl command
    /// <para>Command TU</para>
    /// </summary>
    /// <remarks>Serializable attribute required for deep clone of an object
    /// <para>The Serializable attribute is required for deep object cloning</para></remarks>
    [Serializable]
    public class Command {
        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public Command() : this(BaseValues.CmdTypes.Standard) { }

        /// <summary>
        /// Constructor
        /// </summary>
        public Command(int cmdTypeID) {
            CreateDT = DateTime.Now;
            CmdTypeID = cmdTypeID;
            KPNum = 0;
            CmdNum = 0;
            CmdVal = 0.0;
            CmdData = null;
            RecursionLevel = 0;
        }


        /// <summary>
        /// Get the date and time of the team creation
        /// </summary>
        public DateTime CreateDT { get; protected set; }

        /// <summary>
        /// Get or set the command type Id
        /// </summary>
        public int CmdTypeID { get; set; }

        /// <summary>
        /// Get or set KP number
        /// </summary>
        public int KPNum { get; set; }

        /// <summary>
        /// Get or set the command number
        /// </summary>
        public int CmdNum { get; set; }

        /// <summary>
        /// Get or set the command value
        /// </summary>
        public double CmdVal { get; set; }

        /// <summary>
        /// Get or set these commands
        /// </summary>
        public byte[] CmdData { get; set; }

        /// <summary>
        /// Recursion level when sending commands from server modules
        /// </summary>
        public int RecursionLevel { get; set; }


        /// <summary>
        /// Get data commands converted to string
        /// </summary>
        public string GetCmdDataStr() {
            return CmdDataToStr(CmdData);
        }

        /// <summary>
        /// Prepare these commands for transmission to clients over TCP
        /// </summary>
        public void PrepareCmdData() {
            if (CmdTypeID == BaseValues.CmdTypes.Standard)
                CmdData = BitConverter.GetBytes(CmdVal);
            else if (CmdTypeID == BaseValues.CmdTypes.Request)
                CmdData = BitConverter.GetBytes((UInt16) KPNum);
        }

        /// <summary>
        /// Get the code of the command type by ID
        /// </summary>
        public string GetCmdTypeCode() {
            return BaseValues.CmdTypes.GetCmdTypeCode(CmdTypeID);
        }

        /// <summary>
        /// Get command description
        /// </summary>
        public string GetCmdDescr() {
            return GetCmdDescr(0, 0);
        }

        /// <summary>
        /// Get command description with control channel and user
        /// </summary>
        public string GetCmdDescr(int ctrlCnlNum, int userID) {
            const int VisCmdDataLen = 10; // the length of the displayed data command
            var sb = new StringBuilder();

            sb.Append("Command: ");
            if (ctrlCnlNum > 0)
                sb.Append("out ch.=").Append(ctrlCnlNum).Append(", ");
            if (userID > 0)
                sb.Append("user=").Append(userID).Append(", ");
            sb.Append("type=").Append(GetCmdTypeCode());
            if (KPNum > 0)
                sb.Append(", device=").Append(KPNum);
            if (CmdNum > 0)
                sb.Append(", number=").Append(CmdNum);
            if (CmdTypeID == BaseValues.CmdTypes.Standard)
                sb.Append(", value=").AppendFormat(CmdVal.ToString("N3", Localization.Culture));
            if (CmdTypeID == BaseValues.CmdTypes.Binary && CmdData != null)
                sb.Append(", data=")
                    .Append(ScadaUtils.BytesToHex(CmdData, 0, Math.Min(VisCmdDataLen, CmdData.Length)))
                    .Append(VisCmdDataLen < CmdData.Length ? "..." : "");

            return sb.ToString();
        }


        /// <summary>
        /// Convert command data to string
        /// </summary>
        public static string CmdDataToStr(byte[] cmdData) {
            try {
                return cmdData == null ? "" : Encoding.UTF8.GetString(cmdData);
            } catch {
                return "";
            }
        }

        /// <summary>
        /// Convert string to command data.
        /// </summary>
        public static byte[] StrToCmdData(string s) {
            return s == null ? new byte[0] : Encoding.UTF8.GetBytes(s);
        }
    }
}