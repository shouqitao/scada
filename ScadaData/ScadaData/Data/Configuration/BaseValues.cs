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
 * Summary  : The main values from the configuration database
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2013
 * Modified : 2017
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace Scada.Data.Configuration {
    /// <summary>
    /// The main values from the configuration database
    /// <para>Basic values from the configuration database</para>
    /// </summary>
    public static class BaseValues {
        /// <summary>
        /// User roles
        /// </summary>
        public static class Roles {
            /// <summary>
            /// Disconnected
            /// </summary>
            public const int Disabled = 0x00;

            /// <summary>
            /// Administrator
            /// </summary>
            public const int Admin = 0x01;

            /// <summary>
            /// Dispatcher
            /// </summary>
            public const int Dispatcher = 0x02;

            /// <summary>
            /// Guest
            /// </summary>
            public const int Guest = 0x03;

            /// <summary>
            /// Application
            /// </summary>
            public const int App = 0x04;

            /// <summary>
            /// Customizable Role
            /// </summary>
            /// <remarks>The minimum ID for a custom role is 0x0B</remarks>
            public const int Custom = 0x0B;

            /// <summary>
            /// Error (invalid username or password)
            /// </summary>
            public const int Err = 0xFF;

            /// <summary>
            /// Get role name by Id
            /// </summary>
            public static string GetRoleName(int roleID) {
                if (roleID == Admin)
                    return "Administrator";
                if (roleID == Dispatcher)
                    return "Dispatcher";
                if (roleID == Guest)
                    return "Guest";
                if (roleID == App)
                    return "Application";
                if (Custom <= roleID && roleID < Err)
                    return "Custom role";

                return roleID == Err ? "Error" : "Disabled";
            }
        }

        /// <summary>
        /// Channel Types
        /// </summary>
        public static class CnlTypes {
            /// <summary>
            /// TeleSignal (TS)
            /// </summary>
            public const int TS = 1;

            /// <summary>
            /// Telemetry (TI)
            /// </summary>
            public const int TI = 2;

            /// <summary>
            /// Pre-calculation ТI
            /// </summary>
            public const int TIDR = 3;

            /// <summary>
            /// Minute TI
            /// </summary>
            public const int TIDRM = 4;

            /// <summary>
            /// Hour TI
            /// </summary>
            public const int TIDRH = 5;

            /// <summary>
            /// Number of switching (pre-calculation)
            /// </summary>
            public const int SWCNT = 6;

            /// <summary>
            /// Pre-calculation vehicle
            /// </summary>
            public const int TSDR = 7;

            /// <summary>
            /// Minute vehicle
            /// </summary>
            public const int TSDRM = 8;

            /// <summary>
            /// Hourly TS
            /// </summary>
            public const int TSDRH = 9;

            /// <summary>
            /// Min channel type identifier
            /// </summary>
            public const int MinCnlTypeID = 1;

            /// <summary>
            /// Max. channel type identifier
            /// </summary>
            public const int MaxCnlTypeID = 9;
        }

        /// <summary>
        /// Types of teams
        /// </summary>
        public static class CmdTypes {
            /// <summary>
            /// Standard Team (TU)
            /// </summary>
            public const int Standard = 0;

            /// <summary>
            /// Binary command
            /// </summary>
            public const int Binary = 1;

            /// <summary>
            /// Extraordinary Survey KP
            /// </summary>
            public const int Request = 2;

            /// <summary>
            /// Get the code of the command type by ID
            /// </summary>
            public static string GetCmdTypeCode(int cmdTypeID) {
                switch (cmdTypeID) {
                    case Standard:
                        return "Standard";
                    case Binary:
                        return "Binary";
                    case Request:
                        return "Request";
                    default:
                        return cmdTypeID.ToString();
                }
            }

            /// <summary>
            /// Recognize the type code of the command
            /// </summary>
            public static int ParseCmdTypeCode(string cmdTypeCode) {
                if (cmdTypeCode.Equals("Standard", StringComparison.OrdinalIgnoreCase))
                    return Standard;
                if (cmdTypeCode.Equals("Binary", StringComparison.OrdinalIgnoreCase))
                    return Binary;
                if (cmdTypeCode.Equals("Request", StringComparison.OrdinalIgnoreCase))
                    return Request;
                return -1;
            }
        }

        /// <summary>
        /// Input channel statuses (event types)
        /// </summary>
        public static class CnlStatuses {
            /// <summary>
            /// Not determined
            /// </summary>
            public const int Undefined = 0;

            /// <summary>
            /// Defined
            /// </summary>
            public const int Defined = 1;

            /// <summary>
            /// Archival
            /// </summary>
            public const int Archival = 2;

            /// <summary>
            /// Error in the formula
            /// </summary>
            public const int FormulaError = 3;

            /// <summary>
            /// Changed
            /// </summary>
            public const int Changed = 4;

            /// <summary>
            /// Unreliable
            /// </summary>
            public const int Unreliable = 5;

            /// <summary>
            /// Emergency understatement
            /// </summary>
            public const int LowCrash = 11;

            /// <summary>
            /// Understating
            /// </summary>
            public const int Low = 12;

            /// <summary>
            /// Normalization
            /// </summary>
            public const int Normal = 13;

            /// <summary>
            /// Overestimate
            /// </summary>
            public const int High = 14;

            /// <summary>
            /// Emergency overestimate
            /// </summary>
            public const int HighCrash = 15;

            /// <summary>
            /// Login allowed
            /// </summary>
            public const int InPermitted = 101;

            /// <summary>
            /// Exit allowed
            /// </summary>
            public const int OutPermitted = 102;

            /// <summary>
            /// Access is denied
            /// </summary>
            public const int AccessDenied = 103;

            /// <summary>
            /// Damage to AL
            /// </summary>
            public const int WireBreak = 111;

            /// <summary>
            /// Disarmed ... 撤防
            /// </summary>
            public const int Disarm = 112;

            /// <summary>
            /// Armed ... 布防
            /// </summary>
            public const int Arm = 113;

            /// <summary>
            /// Alarm
            /// </summary>
            public const int Alarm = 114;
        }

        /// <summary>
        /// Number Formats
        /// </summary>
        public static class Formats {
            /// <summary>
            /// Text from listing
            /// </summary>
            public const int EnumText = 10;

            /// <summary>
            /// ASCII text
            /// </summary>
            public const int AsciiText = 11;

            /// <summary>
            /// Unicode text
            /// </summary>
            public const int UnicodeText = 12;

            /// <summary>
            /// date and time
            /// </summary>
            public const int DateTime = 13;

            /// <summary>
            /// Date
            /// </summary>
            public const int Date = 14;

            /// <summary>
            /// Time
            /// </summary>
            public const int Time = 15;
        }

        /// <summary>
        /// Dimension Names
        /// </summary>
        public static class UnitNames {
            /// <summary>
            /// Off - On
            /// </summary>
            public static string OffOn;

            /// <summary>
            /// No - Yes
            /// </summary>
            public static string NoYes;

            /// <summary>
            /// PC.
            /// </summary>
            public static string Pcs;

            /// <summary>
            /// Static constructor
            /// </summary>
            static UnitNames() {
                OffOn = "Off - On";
                NoYes = "No - Yes";
                Pcs = "pcs.";
            }
        }

        /// <summary>
        /// Command Value Names
        /// </summary>
        public static class CmdValNames {
            /// <summary>
            /// Off
            /// </summary>
            public static string Off;

            /// <summary>
            /// On
            /// </summary>
            public static string On;

            /// <summary>
            /// Off - On
            /// </summary>
            public static string OffOn;

            /// <summary>
            /// Run
            /// </summary>
            public static string Execute;

            /// <summary>
            /// Static constructor
            /// </summary>
            static CmdValNames() {
                Off = "Off";
                On = "On";
                OffOn = "Off - On";
                Execute = "Execute";
            }
        }


        /// <summary>
        /// ID of empty or undefined data
        /// </summary>
        public const int EmptyDataID = 0;
    }
}