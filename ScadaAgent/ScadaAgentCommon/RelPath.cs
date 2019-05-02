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
 * Module   : ScadaAgentCommon
 * Summary  : Relative path
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

namespace Scada.Agent {
    /// <summary>
    /// Relative path
    /// <para>Relative path</para>
    /// </summary>
    public class RelPath {
        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public RelPath()
            : this(ConfigParts.None, AppFolder.Root, "") { }

        /// <summary>
        /// Constructor
        /// </summary>
        public RelPath(ConfigParts configPart, AppFolder appFolder, string path = "") {
            ConfigPart = configPart;
            AppFolder = appFolder;
            Path = path ?? "";
        }


        /// <summary>
        /// Get or set part of the configuration
        /// </summary>
        public ConfigParts ConfigPart { get; set; }

        /// <summary>
        /// Get or set application folder
        /// </summary>
        public AppFolder AppFolder { get; set; }

        /// <summary>
        /// Get or set the path relative to the application folder
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Get the sign that the path is a file search mask
        /// </summary>
        public bool IsMask {
            get { return Path != null && (Path.IndexOf('*') >= 0 || Path.IndexOf('?') >= 0); }
        }
    }
}