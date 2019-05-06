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
 * Module   : ScadaServerCommon
 * Summary  : The base class for server module logic
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2013
 * Modified : 2018
 */

using Scada.Data.Configuration;
using Scada.Data.Models;
using Scada.Data.Tables;
using System;
using Utils;

namespace Scada.Server.Modules {
    /// <summary>
    /// The base class for server module logic
    /// <para>Parent class of server module logic</para>
    /// </summary>
    public abstract class ModLogic {
        /// <summary>
        /// Module operation stop time, ms
        /// </summary>
        public const int WaitForStop = 7000;

        private AppDirs appDirs; // application directories


        /// <summary>
        /// Constructor
        /// </summary>
        protected ModLogic() {
            appDirs = new AppDirs();
            Settings = null;
            WriteToLog = null;
            ServerData = null;
            ServerCommands = null;
        }


        /// <summary>
        /// Get the module name
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Get or set application directories
        /// </summary>
        public AppDirs AppDirs {
            get { return appDirs; }
            set {
                appDirs = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// Get or set SCADA-Server settings
        /// </summary>
        public Settings Settings { get; set; }

        /// <summary>
        /// Get or set application logging method
        /// </summary>
        public Log.WriteActionDelegate WriteToLog { get; set; }

        /// <summary>
        /// Get or set an object to access server data
        /// </summary>
        public IServerData ServerData { get; set; }

        /// <summary>
        /// Get or set an object to send commands
        /// </summary>
        public IServerCommands ServerCommands { get; set; }


        /// <summary>
        /// Perform actions at server startup
        /// </summary>
        public virtual void OnServerStart() {
            if (WriteToLog != null)
                WriteToLog(
                    string.Format(Localization.UseRussian ? "Запуск работы модуля {0}" : "Start {0} module", Name),
                    Log.ActTypes.Action);
        }

        /// <summary>
        /// Perform actions when shutting down the server
        /// </summary>
        public virtual void OnServerStop() {
            if (WriteToLog != null)
                WriteToLog(
                    string.Format(Localization.UseRussian ? "Завершение работы модуля {0}" : "Stop {0} module", Name),
                    Log.ActTypes.Action);
        }

        /// <summary>
        /// Perform actions after processing new current data
        /// </summary>
        /// <remarks>Channel numbers are sorted in ascending order.
        /// Calculation of additional calculation channels of the current slice at the time of calling the method failed</remarks>
        public virtual void OnCurDataProcessed(int[] cnlNums, SrezTableLight.Srez curSrez) { }

        /// <summary>
        /// Perform actions after calculating the calculation channels for the current slice
        /// </summary>
        /// <remarks>Channel numbers are sorted in ascending order.</remarks>
        public virtual void OnCurDataCalculated(int[] cnlNums, SrezTableLight.Srez curSrez) { }

        /// <summary>
        /// Perform actions after processing new archived data
        /// </summary>
        /// <remarks>
        /// Channel numbers are sorted in ascending order.
        /// Calculation of additional calculation channels of the archive slice at the time of calling the method is completed.
        /// The arcSrez parameter is null if the recording of archive slices is disabled.
        /// </remarks>
        public virtual void OnArcDataProcessed(int[] cnlNums, SrezTableLight.Srez arcSrez) { }

        /// <summary>
        /// Perform actions when creating an event
        /// </summary>
        /// <remarks>The method is called before the event is written to the disk,
        ///          so the properties of the event can be changed.</remarks>
        public virtual void OnEventCreating(EventTableLight.Event ev) { }

        /// <summary>
        /// Perform actions after creating an event and writing to disk
        /// </summary>
        /// <remarks>The method is called after writing to the event disk.</remarks>
        public virtual void OnEventCreated(EventTableLight.Event ev) { }

        /// <summary>
        /// Perform actions after acknowledging the event
        /// </summary>
        public virtual void OnEventChecked(DateTime date, int evNum, int userID) { }

        /// <summary>
        /// Perform actions after receiving the command TU
        /// </summary>
        /// <remarks>The method is invoked after receiving the TU command from the connected
        /// clients and is not invoked after the TU command is sent by the server modules</remarks>
        public virtual void OnCommandReceived(int ctrlCnlNum, Command cmd, int userID, ref bool passToClients) { }

        /// <summary>
        /// Check user name and password, get his role
        /// </summary>
        /// <remarks>If the password is empty, it is not checked.</remarks>
        public virtual bool ValidateUser(string username, string password, out int roleID, out bool handled) {
            roleID = BaseValues.Roles.Err;
            handled = false;
            return false;
        }
    }
}