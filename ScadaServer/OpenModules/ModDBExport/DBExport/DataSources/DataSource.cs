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
 * Module   : ModDBExport
 * Summary  : The base class for interacting with database
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2015
 * Modified : 2018
 */

using System;
using System.Data.Common;

namespace Scada.Server.Modules.DBExport {
    /// <inheritdoc />
    /// <summary>
    /// The base class for interacting with database
    /// <para>Base class for interacting with the database</para>
    /// </summary>
    internal abstract class DataSource : IComparable<DataSource> {
        /// <summary>
        /// Constructor
        /// </summary>
        public DataSource() {
            DBType = DBTypes.Undefined;
            Server = "";
            Database = "";
            User = "";
            Password = "";
            ConnectionString = "";

            Connection = null;
            ExportCurDataCmd = null;
            ExportArcDataCmd = null;
            ExportEventCmd = null;
        }


        /// <summary>
        /// Get or set the type of database
        /// </summary>
        public DBTypes DBType { get; set; }

        /// <summary>
        /// Get or set a database server
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Get or set the database name
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Get or set DB user
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Get or set DB user password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Get or set the database connection string
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Get the name of the data source
        /// </summary>
        public string Name {
            get { return DBType + (string.IsNullOrEmpty(Server) ? "" : " - " + Server); }
        }


        /// <summary>
        /// Get database connection
        /// </summary>
        public DbConnection Connection { get; protected set; }

        /// <summary>
        /// Get current data export command
        /// </summary>
        public DbCommand ExportCurDataCmd { get; protected set; }

        /// <summary>
        /// Get the export archive command
        /// </summary>
        public DbCommand ExportArcDataCmd { get; protected set; }

        /// <summary>
        /// Get event export command
        /// </summary>
        public DbCommand ExportEventCmd { get; protected set; }


        /// <summary>
        /// Create a database connection
        /// </summary>
        protected abstract DbConnection CreateConnection();

        /// <summary>
        /// Clear connection pool
        /// </summary>
        protected abstract void ClearPool();

        /// <summary>
        /// Create a team
        /// </summary>
        protected abstract DbCommand CreateCommand(string cmdText);

        /// <summary>
        /// Add command parameter with value
        /// </summary>
        protected abstract void AddCmdParamWithValue(DbCommand cmd, string paramName, object value);

        /// <summary>
        /// Extract host name and port from server name
        /// </summary>
        protected void ExtractHostAndPort(string server, int defaultPort, out string host, out int port) {
            int ind = server.IndexOf(':');

            if (ind >= 0) {
                host = server.Substring(0, ind);
                try {
                    port = int.Parse(server.Substring(ind + 1));
                } catch {
                    port = defaultPort;
                }
            } else {
                host = server;
                port = defaultPort;
            }
        }


        /// <summary>
        /// Build a database connection string based on the remaining connection properties
        /// </summary>
        public abstract string BuildConnectionString();

        /// <summary>
        /// Initialize the connection to the database
        /// </summary>
        public void InitConnection() {
            Connection = CreateConnection();
            if (string.IsNullOrEmpty(ConnectionString))
                ConnectionString = BuildConnectionString();
            Connection.ConnectionString = ConnectionString;
        }

        /// <summary>
        /// Connect to DB
        /// </summary>
        public void Connect() {
            if (Connection == null)
                throw new InvalidOperationException("Connection is not initialized.");

            try {
                Connection.Open();
            } catch {
                Connection.Close();
                ClearPool();
                throw;
            }
        }

        /// <summary>
        /// Disconnect from DB
        /// </summary>
        public void Disconnect() {
            Connection?.Close();
        }

        /// <summary>
        /// Initialize data export commands
        /// </summary>
        public void InitCommands(string exportCurDataQuery, string exportArcDataQuery, string exportEventQuery) {
            ExportCurDataCmd = string.IsNullOrEmpty(exportCurDataQuery) ? null : CreateCommand(exportCurDataQuery);
            ExportArcDataCmd = string.IsNullOrEmpty(exportArcDataQuery) ? null : CreateCommand(exportArcDataQuery);
            ExportEventCmd = string.IsNullOrEmpty(exportEventQuery) ? null : CreateCommand(exportEventQuery);
        }

        /// <summary>
        /// Set the value of the command parameter
        /// </summary>
        public void SetCmdParam(DbCommand cmd, string paramName, object value) {
            if (cmd == null)
                throw new ArgumentNullException(nameof(cmd));

            if (cmd.Parameters.Contains(paramName))
                cmd.Parameters[paramName].Value = value;
            else
                AddCmdParamWithValue(cmd, paramName, value);
        }

        /// <summary>
        /// Clone data source
        /// </summary>
        public virtual DataSource Clone() {
            var dataSourceCopy = (DataSource) Activator.CreateInstance(this.GetType());
            dataSourceCopy.DBType = DBType;
            dataSourceCopy.Server = Server;
            dataSourceCopy.Database = Database;
            dataSourceCopy.User = User;
            dataSourceCopy.Password = Password;
            dataSourceCopy.ConnectionString = ConnectionString;
            return dataSourceCopy;
        }


        /// <summary>
        /// Получить строковое представление объекта
        /// </summary>
        public override string ToString() {
            return Name;
        }

        /// <summary>
        /// Сравнить текущий объект с другим объектом такого же типа
        /// </summary>
        public int CompareTo(DataSource other) {
            int comp = DBType.CompareTo(other.DBType);
            return comp == 0 ? Name.CompareTo(other.Name) : comp;
        }
    }
}