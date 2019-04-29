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
 * Module   : SCADA-Administrator
 * Summary  : The common application data
 *
 * Author   : Mikhail Shiryaev
 * Created  : 2010
 * Modified : 2018
 */

using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ScadaAdmin {
    /// <summary>
    /// The common application data
    /// <para>Application General Data</para>
    /// </summary>
    internal static class AppData {
        /// <summary>
        /// Application Error Log File Name
        /// </summary>
        private const string ErrFileName = "ScadaAdmin.err";

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static AppData() {
            AppDirs = new AppDirs();
            AppDirs.Init(Path.GetDirectoryName(Application.ExecutablePath));

            ErrLog = new Log(Log.Formats.Full) {
                FileName = AppDirs.LogDir + ErrFileName,
                Encoding = Encoding.UTF8
            };

            Settings = new Settings();
            Conn = new SqlCeConnection();
        }

        /// <summary>
        /// Get application directories
        /// </summary>
        public static AppDirs AppDirs { get; private set; }

        /// <summary>
        /// Get application error log
        /// </summary>
        public static Log ErrLog { get; private set; }

        /// <summary>
        /// Get app settings
        /// </summary>
        public static Settings Settings { get; private set; }

        /// <summary>
        /// Get database connection
        /// </summary>
        public static SqlCeConnection Conn { get; private set; }

        /// <summary>
        /// Get a sign of whether the connection to the database is established
        /// </summary>
        public static bool Connected {
            get { return Conn.State == ConnectionState.Open; }
        }

        /// <summary>
        /// Connect to the database using the connection string specified in the Web.Config file
        /// </summary>
        public static void Connect() {
            if (Conn.State != ConnectionState.Closed)
                Disconnect();

            string baseSdfFileName = Settings.AppSett.BaseSDFFile;

            if (!File.Exists(baseSdfFileName))
                throw new FileNotFoundException(string.Format(AppPhrases.BaseSDFFileNotFound, baseSdfFileName));

            try {
                string connStr = "Data Source=" + baseSdfFileName;
                if (Conn.ConnectionString != connStr)
                    Conn.ConnectionString = connStr;
                Conn.Open();
            } catch {
                Conn.Close();
                throw;
            }
        }

        /// <summary>
        /// Disconnect from DB
        /// </summary>
        public static void Disconnect() {
            Conn.Close();
        }

        /// <summary>
        /// Pack db
        /// </summary>
        public static bool Compact() {
            if (string.IsNullOrEmpty(Conn.ConnectionString)) {
                return false;
            } else {
                bool wasConnected = Connected;

                try {
                    if (wasConnected)
                        Conn.Close();

                    var engine = new SqlCeEngine(Conn.ConnectionString);
                    engine.Compact(string.Empty);
                } finally {
                    if (wasConnected)
                        Conn.Open();
                }

                return true;
            }
        }
    }
}