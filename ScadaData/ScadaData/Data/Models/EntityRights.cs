/*
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
 * Summary  : Rights to access some entity
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2016
 */

namespace Scada.Data.Models {
    /// <summary>
    /// Rights to access some entity
    /// <para>Rights to access an entity</para>
    /// </summary>
    public struct EntityRights {
        /// <summary>
        /// Lack of rights
        /// </summary>
        public static readonly EntityRights NoRights = new EntityRights(false, false);


        /// <summary>
        /// Constructor
        /// </summary>
        public EntityRights(bool viewRight, bool ctrlRight)
            : this() {
            ViewRight = viewRight;
            ControlRight = ctrlRight;
        }


        /// <summary>
        /// Get or set the right to view
        /// </summary>
        public bool ViewRight { get; set; }

        /// <summary>
        /// Get or set the right to manage
        /// </summary>
        public bool ControlRight { get; set; }
    }
}