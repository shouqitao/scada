﻿/*
 * Copyright 2016 Mikhail Shiryaev
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
 * Summary  : Input channel status properties
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2016
 */

namespace Scada.Data.Models {
    /// <summary>
    /// Input channel status properties
    /// <para>Input Channel Status Properties</para>
    /// </summary>
    public class CnlStatProps {
        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public CnlStatProps()
            : this(0) { }

        /// <summary>
        /// Constructor
        /// </summary>
        public CnlStatProps(int stat) {
            Status = stat;
            Name = "";
            Color = "";
        }


        /// <summary>
        /// Get or set status value
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Get or set the name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Get or set color
        /// </summary>
        public string Color { get; set; }
    }
}