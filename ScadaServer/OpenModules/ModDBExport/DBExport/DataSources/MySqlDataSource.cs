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
 * Summary  : MySQL interacting traits
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2015
 * Modified : 2015
 */

using MySql.Data.MySqlClient;
using System;
using System.Data.Common;

namespace Scada.Server.Modules.DBExport {
    /// <inheritdoc />
    /// <summary>
    /// MySQL interacting traits
    /// <para>Features of interaction with MySQL</para>
    /// </summary>
    internal class MySqlDataSource : DataSource {
        /// <summary>
        /// Default port
        /// </summary>
        private const int DefaultPort = 3306;


        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public MySqlDataSource()
            : base() {
            DBType = DBTypes.MySQL;
        }


        /// <summary>
        /// Check the existence and type of database connection
        /// </summary>
        private void CheckConnection() {
            if (Connection == null)
                throw new InvalidOperationException("Connection is not inited.");
            if (!(Connection is MySqlConnection))
                throw new InvalidOperationException("MySqlConnection is required.");
        }


        /// <inheritdoc />
        /// <summary>
        /// Create a database connection
        /// </summary>
        protected override DbConnection CreateConnection() {
            return new MySqlConnection();
        }

        /// <inheritdoc />
        /// <summary>
        /// Clear application pool
        /// </summary>
        protected override void ClearPool() {
            CheckConnection();
            MySqlConnection.ClearPool((MySqlConnection) Connection);
        }

        /// <inheritdoc />
        /// <summary>
        /// Create a team
        /// </summary>
        protected override DbCommand CreateCommand(string cmdText) {
            CheckConnection();
            return new MySqlCommand(cmdText, (MySqlConnection) Connection);
        }

        /// <inheritdoc />
        /// <summary>
        /// Add command parameter with value
        /// </summary>
        protected override void AddCmdParamWithValue(DbCommand cmd, string paramName, object value) {
            if (cmd == null)
                throw new ArgumentNullException("cmd");
            if (!(cmd is MySqlCommand))
                throw new ArgumentException("MySqlCommand is required.", "cmd");

            var mySqlCmd = (MySqlCommand) cmd;
            mySqlCmd.Parameters.AddWithValue(paramName, value);
        }


        /// <inheritdoc />
        /// <summary>
        /// Build a database connection string based on the remaining connection properties
        /// </summary>
        public override string BuildConnectionString() {
            string host;
            int port;
            ExtractHostAndPort(Server, DefaultPort, out host, out port);

            var csb = new MySqlConnectionStringBuilder {
                Server = host,
                Port = (uint)port,
                Database = Database,
                UserID = User,
                Password = Password
            };

            return csb.ToString();
        }
    }
}