/*
 * Copyright 2017 Mikhail Shiryaev
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
 * Summary  : Interface that defines methods to send commands
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2017
 * Modified : 2017
 */

using Scada.Data.Models;

namespace Scada.Server.Modules {
    /// <summary>
    /// Interface that defines methods to send commands
    /// <para>Interface defining methods for sending commands</para>
    /// </summary>
    public interface IServerCommands {
        /// <summary>
        /// Send a command TU for a given control channel
        /// </summary>
        void SendCommand(int ctrlCnlNum, Command cmd, int userID);

        /// <summary>
        /// Send a command to the TU for connected clients.
        /// </summary>
        void PassCommand(Command cmd);
    }
}