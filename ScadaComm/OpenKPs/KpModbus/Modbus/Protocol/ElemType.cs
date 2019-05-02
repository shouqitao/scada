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
 * Summary  : Types of Modbus elements
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2012
 * Modified : 2018
 */

namespace Scada.Comm.Devices.Modbus.Protocol {
    /// <summary>
    /// Types of Modbus elements
    /// <para>Types of Modbus Elements</para>
    /// </summary>
    public enum ElemType {
        /// <summary>
        /// Type not defined
        /// </summary>
        Undefined,

        /// <summary>
        /// 2-byte unsigned integer
        /// </summary>
        UShort,

        /// <summary>
        /// 2-byte signed integer
        /// </summary>
        Short,

        /// <summary>
        /// 4-byte unsigned integer
        /// </summary>
        UInt,

        /// <summary>
        /// 4-byte signed integer
        /// </summary>
        Int,

        /// <summary>
        /// 8-byte unsigned integer
        /// </summary>
        ULong,

        /// <summary>
        /// 8-byte signed integer
        /// </summary>
        Long,

        /// <summary>
        /// 4-byte floating-point floating point
        /// </summary>
        Float,

        /// <summary>
        /// 8-byte floating-point floating point
        /// </summary>
        Double,

        /// <summary>
        /// Boolean value
        /// </summary>
        Bool
    }
}