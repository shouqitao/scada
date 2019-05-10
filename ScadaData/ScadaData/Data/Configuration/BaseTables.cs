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
 * Module   : ScadaData
 * Summary  : The tables of the configuration database
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2018
 */

using System;
using System.Data;

namespace Scada.Data.Configuration {
    /// <summary>
    /// The tables of the configuration database
    /// <para>Configuration base tables</para>
    /// </summary>
    /// <remarks>
    /// After using DataTable.DefaultView.RowFilter restore the empty value
    /// <para>After use DataTable.DefaultView.RowFilter need to return empty value</para></remarks>
    public class BaseTables {
        /// <summary>
        /// Constructor
        /// </summary>
        public BaseTables() {
            AllTables = new DataTable[] {
                ObjTable = new DataTable("Obj"), CommLineTable = new DataTable("CommLine"),
                KPTable = new DataTable("KP"), InCnlTable = new DataTable("InCnl"),
                CtrlCnlTable = new DataTable("CtrlCnl"), RoleTable = new DataTable("Role"),
                UserTable = new DataTable("User"), InterfaceTable = new DataTable("Interface"),
                RightTable = new DataTable("Right"), CnlTypeTable = new DataTable("CnlType"),
                CmdTypeTable = new DataTable("CmdType"), EvTypeTable = new DataTable("EvType"),
                KPTypeTable = new DataTable("KPType"), ParamTable = new DataTable("Param"),
                UnitTable = new DataTable("Unit"), CmdValTable = new DataTable("CmdVal"),
                FormatTable = new DataTable("Format"), FormulaTable = new DataTable("Formula")
            };

            BaseAge = DateTime.MinValue;
        }


        /// <summary>
        /// Get a table of objects
        /// </summary>
        public DataTable ObjTable { get; protected set; }

        /// <summary>
        /// Get a table of communication lines
        /// </summary>
        public DataTable CommLineTable { get; protected set; }

        /// <summary>
        /// Get KP table
        /// </summary>
        public DataTable KPTable { get; protected set; }

        /// <summary>
        /// Get a table of input channels
        /// </summary>
        public DataTable InCnlTable { get; protected set; }

        /// <summary>
        /// Get control channel table
        /// </summary>
        public DataTable CtrlCnlTable { get; protected set; }

        /// <summary>
        /// Get the table of roles
        /// </summary>
        public DataTable RoleTable { get; protected set; }

        /// <summary>
        /// Get user table
        /// </summary>
        public DataTable UserTable { get; protected set; }

        /// <summary>
        /// Get interface object table
        /// </summary>
        public DataTable InterfaceTable { get; protected set; }

        /// <summary>
        /// Get a table of rights to interface objects
        /// </summary>
        public DataTable RightTable { get; protected set; }

        /// <summary>
        /// Get a table of input channel types
        /// </summary>
        public DataTable CnlTypeTable { get; protected set; }

        /// <summary>
        /// Get command type table
        /// </summary>
        public DataTable CmdTypeTable { get; protected set; }

        /// <summary>
        /// Get event type table
        /// </summary>
        public DataTable EvTypeTable { get; protected set; }

        /// <summary>
        /// Get KP type table
        /// </summary>
        public DataTable KPTypeTable { get; protected set; }

        /// <summary>
        /// Get a table of values (parameters)
        /// </summary>
        public DataTable ParamTable { get; protected set; }

        /// <summary>
        /// Get dimension table
        /// </summary>
        public DataTable UnitTable { get; protected set; }

        /// <summary>
        /// Get command value table
        /// </summary>
        public DataTable CmdValTable { get; protected set; }

        /// <summary>
        /// Get a table of number formats
        /// </summary>
        public DataTable FormatTable { get; protected set; }

        /// <summary>
        /// Get formula table
        /// </summary>
        public DataTable FormulaTable { get; protected set; }

        /// <summary>
        /// Get an array of links to all configuration database tables
        /// </summary>
        public DataTable[] AllTables { get; protected set; }

        /// <summary>
        /// Get or set the last modified time of successfully read configuration database
        /// </summary>
        public DateTime BaseAge { get; set; }

        /// <summary>
        /// Get object to synchronize access to tables
        /// </summary>
        public object SyncRoot {
            get { return this; }
        }


        /// <summary>
        /// Get the name of the table file without directory
        /// </summary>
        public static string GetFileName(DataTable dataTable) {
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable));

            return dataTable.TableName.ToLowerInvariant() + ".dat";
        }

        /// <summary>
        /// Check that table columns exist
        /// </summary>
        public static bool CheckColumnsExist(DataTable dataTable, bool throwOnFail = false) {
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable));

            if (dataTable.Columns.Count > 0) {
                return true;
            }

            if (throwOnFail) {
                throw new ScadaException($"The table [{dataTable.TableName}] does not contain columns.");
            }

            return false;
        }
    }
}