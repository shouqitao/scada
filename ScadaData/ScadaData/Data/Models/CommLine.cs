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
 * Module   : ScadaData
 * Summary  : Communication line properties
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using System;

namespace Scada.Data.Models {
    /// <summary>
    /// Communication line properties
    /// <para>Link Properties</para>
    /// </summary>
    [Obsolete("Use Scada.Data.Entities.*")]
    public class CommLine {
        /// <summary>
        /// Constructor
        /// </summary>
        public CommLine() {
            CommLineNum = 0;
            Name = "";
            Descr = "";
        }


        /// <summary>
        /// Get or set KP number
        /// </summary>
        public int CommLineNum { get; set; }

        /// <summary>
        /// Get or set the name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Get or set the description
        /// </summary>
        public string Descr { get; set; }
    }
}