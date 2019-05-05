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
 * Summary  : PostgreSQL interacting traits
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2015
 * Modified : 2015
 */

using Npgsql;
using System;
using System.Data.Common;

namespace Scada.Server.Modules.DBExport {
    /// <summary>
    /// PostgreSQL interacting traits
    /// <para>Features of interaction with PostgreSQL</para>
    /// </summary>
    internal class PgSqlDataSource : DataSource {
        /// <summary>
        /// Default port
        /// </summary>
        private const int DefaultPort = 5432;


        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public PgSqlDataSource()
            : base() {
            DBType = DBTypes.PostgreSQL;
        }


        /// <summary>
        /// Check the existence and type of database connection
        /// </summary>
        private void CheckConnection() {
            if (Connection == null)
                throw new InvalidOperationException("Connection is not inited.");
            if (!(Connection is NpgsqlConnection))
                throw new InvalidOperationException("NpgsqlConnection is required.");
        }


        /// <inheritdoc />
        /// <summary>
        /// Create a database connection
        /// </summary>
        protected override DbConnection CreateConnection() {
            return new NpgsqlConnection();
        }

        /// <inheritdoc />
        /// <summary>
        /// Clear application pool
        /// </summary>
        protected override void ClearPool() {
            NpgsqlConnection.ClearAllPools();
        }

        /// <inheritdoc />
        /// <summary>
        /// Create a Command
        /// </summary>
        protected override DbCommand CreateCommand(string cmdText) {
            CheckConnection();
            return new NpgsqlCommand(cmdText, (NpgsqlConnection) Connection);
        }

        /// <summary>
        /// Add command parameter with value
        /// </summary>
        protected override void AddCmdParamWithValue(DbCommand cmd, string paramName, object value) {
            if (cmd == null)
                throw new ArgumentNullException("cmd");
            if (!(cmd is NpgsqlCommand))
                throw new ArgumentException("NpgsqlCommand is required.", "cmd");

            NpgsqlCommand pgSqlCmd = (NpgsqlCommand) cmd;
            pgSqlCmd.Parameters.AddWithValue(paramName, value);
        }


        /// <inheritdoc />
        /// <summary>
        /// Build a database connection string based on the remaining connection properties
        /// </summary>
        public override string BuildConnectionString() {
            string host;
            int port;
            ExtractHostAndPort(Server, DefaultPort, out host, out port);
            return string.Format("Server={0};Port={1};Database={2};User Id={3};Password={4}",
                host, port, Database, User, Password);
        }
    }
}