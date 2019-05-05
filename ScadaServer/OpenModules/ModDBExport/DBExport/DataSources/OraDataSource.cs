/*
 * Copyright 2015 Mikhail Shiryaev
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
 * Module   : ModDBExport
 * Summary  : Oracle interacting traits
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2015
 * Modified : 2015
 */

using System;
using System.Data.Common;
using System.Data.OracleClient;
using System.Data.SqlClient;

#pragma warning disable 618 // turn off obsolete Oracle classes warning

namespace Scada.Server.Modules.DBExport {
    /// <inheritdoc />
    /// <summary>
    /// Oracle interacting traits
    /// <para>Features of interaction with Oracle</para>
    /// </summary>
    internal class OraDataSource : DataSource {
        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public OraDataSource()
            : base() {
            DBType = DBTypes.Oracle;
        }


        /// <summary>
        /// Check the existence and type of database connection
        /// </summary>
        private void CheckConnection() {
            if (Connection == null)
                throw new InvalidOperationException("Connection is not inited.");
            if (!(Connection is OracleConnection))
                throw new InvalidOperationException("OracleConnection is required.");
        }


        /// <inheritdoc />
        /// <summary>
        /// Create a database connection
        /// </summary>
        protected override DbConnection CreateConnection() {
            return new OracleConnection();
        }

        /// <inheritdoc />
        /// <summary>
        /// Clear application pool
        /// </summary>
        protected override void ClearPool() {
            CheckConnection();
            OracleConnection.ClearPool((OracleConnection) Connection);
        }

        /// <inheritdoc />
        /// <summary>
        /// Create a Command
        /// </summary>
        protected override DbCommand CreateCommand(string cmdText) {
            CheckConnection();
            return new OracleCommand(cmdText, (OracleConnection) Connection);
        }

        /// <inheritdoc />
        /// <summary>
        /// Add command parameter with value
        /// </summary>
        protected override void AddCmdParamWithValue(DbCommand cmd, string paramName, object value) {
            if (cmd == null)
                throw new ArgumentNullException(nameof(cmd));
            if (!(cmd is OracleCommand))
                throw new ArgumentException("OracleCommand is required.", "cmd");

            OracleCommand oraCmd = (OracleCommand) cmd;
            oraCmd.Parameters.AddWithValue(paramName, value);
        }


        /// <inheritdoc />
        /// <summary>
        /// Build a database connection string based on the remaining connection properties
        /// </summary>
        public override string BuildConnectionString() {
            return
                $"Server={Server}{(string.IsNullOrEmpty(Database) ? "" : "/" + Database)};User ID={User};Password={Password}";
        }
    }
}