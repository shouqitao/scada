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
 * Module   : SCADA-Administrator
 * Summary  : Application directories
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using Scada;
using System.IO;

namespace ScadaAdmin {
    /// <summary>
    /// Application directories
    /// <para>Application Directories</para>
    /// </summary>
    public class AppDirs {
        /// <summary>
        /// Constructor
        /// </summary>
        public AppDirs() {
            ExeDir = "";
            ConfigDir = "";
            LangDir = "";
            LogDir = "";
        }


        /// <summary>
        /// Get the directory of the executable file
        /// </summary>
        public string ExeDir { get; protected set; }

        /// <summary>
        /// Get configuration directory
        /// </summary>
        public string ConfigDir { get; protected set; }

        /// <summary>
        /// Get the language file directory
        /// </summary>
        public string LangDir { get; protected set; }

        /// <summary>
        /// Get a log directory
        /// </summary>
        public string LogDir { get; protected set; }


        /// <summary>
        /// Initialize directories based on the directory of the executable file of the application
        /// </summary>
        public void Init(string exeDir) {
            ExeDir = ScadaUtils.NormalDir(exeDir);
            ConfigDir = ExeDir + "Config" + Path.DirectorySeparatorChar;
            LangDir = ExeDir + "Lang" + Path.DirectorySeparatorChar;
            LogDir = ExeDir + "Log" + Path.DirectorySeparatorChar;
        }
    }
}