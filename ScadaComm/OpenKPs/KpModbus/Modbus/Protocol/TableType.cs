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
 * Module   : KpModbus
 * Summary  : Types of Modbus data tables
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2012
 * Modified : 2018
 */

namespace Scada.Comm.Devices.Modbus.Protocol {
    /// <summary>
    /// Types of Modbus data tables
    /// <para>Types of Modbus Data Tables</para>
    /// </summary>
    public enum TableType {
        /// <summary>
        /// Discrete inputs (1 bit, read only, 1X access)
        /// </summary>
        DiscreteInputs,

        /// <summary>
        /// Flags (1 bit, read and write, 0X access)
        /// </summary>
        Coils,

        /// <summary>
        /// Input registers (16 bits, read only, 3X accesses)
        /// </summary>
        InputRegisters,

        /// <summary>
        /// Storage registers (16 bits, read and write, 4X accesses)
        /// </summary>
        HoldingRegisters
    }
}