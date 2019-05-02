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
 * Summary  : Session manager
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using System;
using System.Text;

namespace Scada.Agent.Engine {
    /// <summary>
    /// Session of communication with the agent
    /// <para>Communication session with the agent</para>
    /// </summary>
    public class Session {
        /// <summary>
        /// Constructor restricting the creation of an object without parameters
        /// </summary>
        private Session() { }

        /// <summary>
        /// Constructor
        /// </summary>
        public Session(long sessionID) {
            ID = sessionID;
            IpAddress = "";
            LoggedOn = false;
            Username = "";
            ScadaInstance = null;
            ActivityDT = DateTime.UtcNow;
        }


        /// <summary>
        /// Get session ID
        /// </summary>
        public long ID { get; private set; }

        /// <summary>
        /// Get the IP address of the connection
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Get or set whether the user of the agent is authorized
        /// </summary>
        public bool LoggedOn { get; set; }

        /// <summary>
        /// Get or set username
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Get or install an instance of the system
        /// </summary>
        public ScadaInstance ScadaInstance { get; set; }

        /// <summary>
        /// Get or set the date and time of the last activity (UTC)
        /// </summary>
        public DateTime ActivityDT { get; private set; }


        /// <summary>
        /// Register activity
        /// </summary>
        public void RegisterActivity() {
            ActivityDT = DateTime.UtcNow;
        }

        /// <summary>
        /// Set authorized user data
        /// </summary>
        public void SetUser(string username, ScadaInstance scadaInstance) {
            LoggedOn = true;
            Username = username;
            ScadaInstance = scadaInstance;
        }

        /// <summary>
        /// Clear user data
        /// </summary>
        public void ClearUser() {
            LoggedOn = false;
            Username = "";
            ScadaInstance = null;
        }

        /// <summary>
        /// Return the string representation of the object
        /// </summary>
        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append("[").Append(ID).Append("] ").Append(IpAddress);

            if (LoggedOn)
                sb.Append("; ").Append(Username);

            sb.Append("; ").Append(ActivityDT.ToLocalTime().ToString("T", Localization.Culture));

            return sb.ToString();
        }
    }
}