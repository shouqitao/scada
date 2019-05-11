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
 * Module   : ScadaData.Dao
 * Summary  : The base class for database access
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2017
 * Modified : 2018
 */

using System;

namespace Scada.Dao {
    /// <summary>
    /// The base class for database access
    /// <para>Base class for database access</para>
    /// </summary>
    public abstract class BaseDAO {
        /// <summary>
        /// Number of selected entries
        /// </summary>
        protected int selectedCount;


        /// <summary>
        /// Constructor
        /// </summary>
        protected BaseDAO() {
            selectedCount = 0;
        }


        /// <summary>
        /// Get a searchable pattern using the LIKE expression.
        /// </summary>
        protected string BuildLikePattern(string filter) {
            return filter == null
                ? ""
                : "%" + string.Join("%", filter.Split((string[]) null, StringSplitOptions.RemoveEmptyEntries)) + "%";
        }

        /// <summary>
        /// Convert the object obtained from the database into an integer
        /// </summary>
        protected int ConvertToInt(object value) {
            return value == null || value == DBNull.Value ? -1 : (int) value;
        }

        /// <summary>
        /// Convert the object obtained from the database into a real number
        /// </summary>
        protected double ConvertToDouble(object value) {
            return value == null || value == DBNull.Value ? double.NaN : (double) value;
        }

        /// <summary>
        /// Convert the object obtained from the database to the date and time
        /// </summary>
        protected DateTime ConvertToDateTime(object value) {
            return value == null || value == DBNull.Value ? DateTime.MinValue : (DateTime) value;
        }

        /// <summary>
        /// Get string value to write to DB
        /// </summary>
        protected object GetParamValue(string s) {
            return string.IsNullOrEmpty(s) ? DBNull.Value : (object) s.Trim();
        }

        /// <summary>
        /// Get the value of the identifier for writing to the database
        /// </summary>
        protected object GetParamValue(int id) {
            return id <= 0 ? DBNull.Value : (object) id;
        }

        /// <summary>
        /// Get the value of a real number to write to the database
        /// </summary>
        protected object GetParamValue(double value) {
            return double.IsNaN(value) ? DBNull.Value : (object) value;
        }

        /// <summary>
        /// Get the value of the date and time for writing to the database
        /// </summary>
        protected object GetParamValue(DateTime value) {
            return value == DateTime.MinValue ? DBNull.Value : (object) value;
        }
    }
}