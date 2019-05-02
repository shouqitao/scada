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
 * Summary  : Unit of Modbus data
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2012
 * Modified : 2018
 */

using System.Text;

namespace Scada.Comm.Devices.Modbus.Protocol {
    /// <summary>
    /// Unit of Modbus data
    /// <para>Modbus data block</para>
    /// </summary>
    public abstract class DataUnit {
        /// <summary>
        /// Constructor restricting the creation of an object without parameters
        /// </summary>
        protected DataUnit()
            : this(TableType.DiscreteInputs) { }

        /// <summary>
        /// Constructor
        /// </summary>
        protected DataUnit(TableType tableType) {
            Name = "";
            TableType = tableType;
            Address = 0;

            FuncCode = 0;
            ExcFuncCode = 0;
            ReqPDU = null;
            RespPduLen = 0;
            ReqADU = null;
            ReqStr = "";
            RespByteCnt = 0;
        }


        /// <summary>
        /// Get or set the name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Get or set data table type
        /// </summary>
        public TableType TableType { get; set; }

        /// <summary>
        /// Get or set the address of the starting element
        /// </summary>
        public ushort Address { get; set; }

        /// <summary>
        /// Get query description to get item values
        /// </summary>
        public abstract string ReqDescr { get; }


        /// <summary>
        /// Request Function Code
        /// </summary>
        public byte FuncCode { get; protected set; }

        /// <summary>
        /// Function code denoting the exception
        /// </summary>
        public byte ExcFuncCode { get; protected set; }

        /// <summary>
        /// Get request PDU
        /// </summary>
        public byte[] ReqPDU { get; protected set; }

        /// <summary>
        /// Get the length of the request response PDU
        /// </summary>
        public int RespPduLen { get; protected set; }

        /// <summary>
        /// Get ADU request
        /// </summary>
        public byte[] ReqADU { get; protected set; }

        /// <summary>
        /// Get query string in ASCII mode
        /// </summary>
        public string ReqStr { get; protected set; }

        /// <summary>
        /// Get the length of the ADU response to the request
        /// </summary>
        public int RespAduLen { get; protected set; }

        /// <summary>
        /// Get the number of bytes, which is indicated in the response
        /// </summary>
        public byte RespByteCnt { get; protected set; }

        /// <summary>
        /// Gets the maximum number of elements.
        /// </summary>
        public int MaxElemCnt {
            get { return GetMaxElemCnt(TableType); }
        }

        /// <summary>
        /// Gets the default element type.
        /// </summary>
        public ElemType DefElemType {
            get { return GetDefElemType(TableType); }
        }

        /// <summary>
        /// To get the indication that the use of types is allowed for a data block or its elements
        /// </summary>
        public virtual bool ElemTypeEnabled {
            get { return TableType == TableType.InputRegisters || TableType == TableType.HoldingRegisters; }
        }

        /// <summary>
        /// Get the indication that byte order is allowed for a data block or its elements
        /// </summary>
        public virtual bool ByteOrderEnabled {
            get { return TableType == TableType.InputRegisters || TableType == TableType.HoldingRegisters; }
        }


        /// <summary>
        /// Initialize the request PDU, calculate the answer length
        /// </summary>
        public abstract void InitReqPDU();

        /// <summary>
        /// Initialize the ADU request and calculate the length of the response
        /// </summary>
        public virtual void InitReqADU(byte devAddr, TransMode transMode) {
            if (ReqPDU != null) {
                int pduLen = ReqPDU.Length;

                switch (transMode) {
                    case TransMode.RTU:
                        ReqADU = new byte[pduLen + 3];
                        ReqADU[0] = devAddr;
                        ReqPDU.CopyTo(ReqADU, 1);
                        ushort crc = ModbusUtils.CalcCRC16(ReqADU, 0, pduLen + 1);
                        ReqADU[pduLen + 1] = (byte) (crc % 256);
                        ReqADU[pduLen + 2] = (byte) (crc / 256);
                        RespAduLen = RespPduLen + 3;
                        break;
                    case TransMode.ASCII:
                        var aduBuf = new byte[pduLen + 2];
                        aduBuf[0] = devAddr;
                        ReqPDU.CopyTo(aduBuf, 1);
                        aduBuf[pduLen + 1] = ModbusUtils.CalcLRC(aduBuf, 0, pduLen + 1);

                        var sbADU = new StringBuilder();
                        foreach (byte b in aduBuf)
                            sbADU.Append(b.ToString("X2"));

                        ReqADU = Encoding.Default.GetBytes(sbADU.ToString());
                        ReqStr = ModbusUtils.Colon + sbADU;
                        RespAduLen = RespPduLen + 2;
                        break;
                    default: // TransModes.TCP
                        ReqADU = new byte[pduLen + 7];
                        ReqADU[0] = 0;
                        ReqADU[1] = 0;
                        ReqADU[2] = 0;
                        ReqADU[3] = 0;
                        ReqADU[4] = (byte) ((pduLen + 1) / 256);
                        ReqADU[5] = (byte) ((pduLen + 1) % 256);
                        ReqADU[6] = devAddr;
                        ReqPDU.CopyTo(ReqADU, 7);
                        RespAduLen = RespPduLen + 7;
                        break;
                }
            }
        }

        /// <summary>
        /// Decrypt Response PDU
        /// </summary>
        public virtual bool DecodeRespPDU(byte[] buffer, int offset, int length, out string errMsg) {
            errMsg = "";
            var result = false;
            byte respFuncCode = buffer[offset];

            if (respFuncCode == FuncCode) {
                if (length == RespPduLen)
                    result = true;
                else
                    errMsg = ModbusPhrases.IncorrectPduLength;
            } else if (respFuncCode == ExcFuncCode) {
                errMsg = length == 2
                    ? ModbusPhrases.DeviceError + ": " + ModbusUtils.GetExcDescr(buffer[offset + 1])
                    : ModbusPhrases.IncorrectPduLength;
            } else {
                errMsg = ModbusPhrases.IncorrectPduFuncCode;
            }

            return result;
        }

        /// <summary>
        /// Gets the maximum number of elements depending on the data table type.
        /// </summary>
        public virtual int GetMaxElemCnt(TableType tableType) {
            return tableType == TableType.DiscreteInputs || tableType == TableType.Coils ? 2000 : 125;
        }

        /// <summary>
        /// Gets the element type depending on the data table type.
        /// </summary>
        public virtual ElemType GetDefElemType(TableType tableType) {
            return tableType == TableType.DiscreteInputs || tableType == TableType.Coils
                ? ElemType.Bool
                : ElemType.UShort;
        }
    }
}