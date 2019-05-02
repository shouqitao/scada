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
 * Module   : ScadaAgentEngine
 * Summary  : Common data of the agent
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using System.Text;
using Utils;

namespace Scada.Agent.Engine {
    /// <summary>
    /// Common data of the agent
    /// <para>Agent General Information</para>
    /// </summary>
    public sealed class AppData {
        /// <summary>
        /// Application log file name without directory
        /// </summary>
        private const string LogFileName = "ScadaAgent.log";

        /// <summary>
        /// File name of information about the application
        /// </summary>
        public const string InfoFileName = "ScadaAgent.txt";

        private static readonly AppData appDataInstance; // AppData instance
        private int tempFileNameCntr; // temporary files counter


        /// <summary>
        /// Static constructor
        /// </summary>
        static AppData() {
            appDataInstance = new AppData();
        }

        /// <summary>
        /// Constructor restricting the creation of an object from other classes
        /// </summary>
        private AppData() {
            tempFileNameCntr = 0;

            AppDirs = new AppDirs();
            Settings = new Settings();
            Log = new Log(Log.Formats.Full) {Encoding = Encoding.UTF8};
            SessionManager = new SessionManager(Log);
            InstanceManager = new InstanceManager(Settings, Log);
        }


        /// <summary>
        /// Get application directories
        /// </summary>
        public AppDirs AppDirs { get; private set; }

        /// <summary>
        /// Get agent settings
        /// </summary>
        public Settings Settings { get; private set; }

        /// <summary>
        /// Get application log
        /// </summary>
        public Log Log { get; private set; }

        /// <summary>
        /// Get a session manager
        /// </summary>
        public SessionManager SessionManager { get; private set; }

        /// <summary>
        /// Get the system instance manager
        /// </summary>
        public InstanceManager InstanceManager { get; private set; }


        /// <summary>
        /// Initialize general agent data
        /// </summary>
        public void Init(string exeDir) {
            AppDirs.Init(exeDir);
            Log.FileName = AppDirs.LogDir + LogFileName;
        }

        /// <summary>
        /// Get temporary file name
        /// </summary>
        public string GetTempFileName(string prefix = "", string extension = "") {
            return AppDirs.TempDir +
                   (string.IsNullOrEmpty(prefix) ? "temp" : prefix) +
                   "-" + (++tempFileNameCntr) +
                   "." + (string.IsNullOrEmpty(extension) ? "tmp" : extension);
        }

        /// <summary>
        /// Get general agent data
        /// </summary>
        public static AppData GetInstance() {
            return appDataInstance;
        }
    }
}