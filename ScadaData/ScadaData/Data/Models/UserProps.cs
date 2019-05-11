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
 * Summary  : User properties
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2016
 */

using System;
using System.IO;

namespace Scada.Data.Models {
    /// <summary>
    /// User properties
    /// <para>User properties</para>
    /// </summary>
    public class UserProps {
        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public UserProps()
            : this(0) { }

        /// <summary>
        /// Constructor
        /// </summary>
        public UserProps(int userID) {
            UserID = userID;
            UserName = "";
            RoleID = 0;
            RoleName = "";
        }


        /// <summary>
        /// Get or set user ID
        /// </summary>
        public int UserID { get; set; }

        /// <summary>
        /// Get or set username
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Get or set user role Id
        /// </summary>
        public int RoleID { get; set; }

        /// <summary>
        /// Get or set the name of the user role
        /// </summary>
        public string RoleName { get; set; }
    }
}