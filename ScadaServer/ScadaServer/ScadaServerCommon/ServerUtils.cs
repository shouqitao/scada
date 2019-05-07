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
 * Module   : ScadaServerCommon
 * Summary  : The class contains utility methods for SCADA-Server and its libraries
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2015
 * Modified : 2018
 */

using Scada.Data.Tables;
using System;
using System.IO;
using System.Text;

namespace Scada.Server {
    /// <summary>
    /// The class contains utility methods for SCADA-Server and its libraries
    /// <para>A class containing helper methods for SCADA-Server and its libraries</para>
    /// </summary>
    public static class ServerUtils {
        /// <summary>
        /// Server version
        /// </summary>
        public const string AppVersion = "5.1.1.0";

        /// <summary>
        /// Server byte of the version number
        /// </summary>
        public const byte AppVersionHi = 5;

        /// <summary>
        /// Server low byte
        /// </summary>
        public const byte AppVersionLo = 1;

        /// <summary>
        /// Application log file name
        /// </summary>
        public const string AppLogFileName = "ScadaServerSvc.log";

        /// <summary>
        /// Application Status File Name
        /// </summary>
        public const string AppStateFileName = "ScadaServerSvc.txt";


        /// <summary>
        /// Build the full file name of the current slice
        /// </summary>
        public static string BuildCurFileName(string arcDir) {
            return (new StringBuilder())
                .Append(arcDir).Append("Cur").Append(Path.DirectorySeparatorChar)
                .Append(SrezAdapter.CurTableName)
                .ToString();
        }

        /// <summary>
        /// Build the full file name of the table of minute slices based on the date
        /// </summary>
        public static string BuildMinFileName(string arcDir, DateTime date) {
            return (new StringBuilder())
                .Append(arcDir).Append("Min").Append(Path.DirectorySeparatorChar)
                .Append(SrezAdapter.BuildMinTableName(date))
                .ToString();
        }

        /// <summary>
        /// Build the full file name of the hourly slice table based on the date
        /// </summary>
        public static string BuildHourFileName(string arcDir, DateTime date) {
            return (new StringBuilder())
                .Append(arcDir).Append("Hour").Append(Path.DirectorySeparatorChar)
                .Append(SrezAdapter.BuildHourTableName(date))
                .ToString();
        }

        /// <summary>
        /// Build the full name of the event table file based on the date
        /// </summary>
        public static string BuildEvFileName(string arcDir, DateTime date) {
            return (new StringBuilder())
                .Append(arcDir).Append("Events").Append(Path.DirectorySeparatorChar)
                .Append(EventAdapter.BuildEvTableName(date))
                .ToString();
        }
    }
}