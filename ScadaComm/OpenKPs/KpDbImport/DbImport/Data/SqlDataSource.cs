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
 * Module   : KpDBImport
 * Summary  : Implements a data source for Microsoft SQL Server
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using Scada.Comm.Devices.DbImport.Configuration;
using System;
using System.Data.Common;
using System.Data.SqlClient;

namespace Scada.Comm.Devices.DbImport.Data {
    /// <inheritdoc />
    /// <summary>
    /// Implements a data source for Microsoft SQL Server.
    /// <para>实现Microsoft SQL Server的数据源。</para>
    /// </summary>
    internal class SqlDataSource : DataSource {
        /// <inheritdoc />
        /// <summary>
        /// Creates a database connection.
        /// </summary>
        protected override DbConnection CreateConnection() {
            return new SqlConnection();
        }

        /// <inheritdoc />
        /// <summary>
        /// Creates a command.
        /// </summary>
        protected override DbCommand CreateCommand() {
            return new SqlCommand();
        }

        /// <inheritdoc />
        /// <summary>
        /// Clears the connection pool.
        /// </summary>
        protected override void ClearPool() {
            if (Connection != null)
                SqlConnection.ClearPool((SqlConnection) Connection);
        }


        /// <inheritdoc />
        /// <summary>
        /// Builds a connection string based on the specified connection settings.
        /// </summary>
        public override string BuildConnectionString(DbConnSettings connSettings) {
            return BuildSqlConnectionString(connSettings);
        }

        /// <summary>
        /// Builds a connection string based on the specified connection settings.
        /// </summary>
        public static string BuildSqlConnectionString(DbConnSettings connSettings) {
            if (connSettings == null)
                throw new ArgumentNullException(nameof(connSettings));

            return
                $"Server={connSettings.Server};Database={connSettings.Database};User ID={connSettings.User};Password={connSettings.Password}";
        }
    }
}