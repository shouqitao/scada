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
 * Summary  : Parts of the configuration
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using System;

namespace Scada.Agent {
    /// <summary>
    /// Parts of the configuration
    /// <para>Configuration parts</para>
    /// </summary>
    [Flags]
    public enum ConfigParts {
        /// <summary>
        /// Not specified
        /// </summary>
        None = 0,

        /// <summary>
        /// Configuration database
        /// </summary>
        Base = 1,

        /// <summary>
        /// Interface
        /// </summary>
        Interface = 2,

        /// <summary>
        /// Server
        /// </summary>
        Server = 4,

        /// <summary>
        /// Communicator
        /// </summary>
        Comm = 8,

        /// <summary>
        /// Web station
        /// </summary>
        Web = 16,

        /// <summary>
        /// Whole configuration
        /// </summary>
        All = Base | Interface | Server | Comm | Web
    }
}