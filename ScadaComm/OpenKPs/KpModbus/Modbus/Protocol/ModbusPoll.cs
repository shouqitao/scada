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
 * Summary  : Polls devices using Modbus protocol
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2012
 * Modified : 2018
 */

using Scada.Comm.Channels;
using System.Globalization;
using Utils;

namespace Scada.Comm.Devices.Modbus.Protocol {
    /// <summary>
    /// Polls devices using Modbus protocol.
    /// <para>Polling devices using Modbus protocol.</para>
    /// </summary>
    public class ModbusPoll {
        /// <summary>
        /// Request Delegate
        /// </summary>
        public delegate bool RequestDelegate(DataUnit dataUnit);

        /// <summary>
        /// Default input buffer size, bytes
        /// </summary>
        private const int DefInBufSize = 300;


        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public ModbusPoll()
            : this(DefInBufSize) { }

        /// <summary>
        /// Constructor
        /// </summary>
        public ModbusPoll(int inBufSize) {
            InBuf = new byte[inBufSize];
            Timeout = 0;
            Connection = null;
            WriteToLog = null;
        }


        /// <summary>
        /// Get input buffer
        /// </summary>
        public byte[] InBuf { get; protected set; }

        /// <summary>
        /// Get or set timeout requests via serial port
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Get or establish a connection with a physical KP
        /// </summary>
        public Connection Connection { get; set; }

        /// <summary>
        /// Get or set logging method
        /// </summary>
        public Log.WriteLineDelegate WriteToLog { get; set; }


        /// <summary>
        /// Call logging method
        /// </summary>
        protected void ExecWriteToLog(string text) {
            WriteToLog?.Invoke(text);
        }

        /// <summary>
        /// Check that the connection is established
        /// </summary>
        protected bool CheckConnection() {
            if (Connection == null || !Connection.Connected) {
                ExecWriteToLog(ModbusPhrases.ConnectionRequired);
                return false;
            } else {
                return true;
            }
        }


        /// <summary>
        /// Run RTU query
        /// </summary>
        public bool RtuRequest(DataUnit dataUnit) {
            if (!CheckConnection())
                return false;

            var result = false;

            // sending request
            ExecWriteToLog(dataUnit.ReqDescr);
            Connection.Write(dataUnit.ReqADU, 0, dataUnit.ReqADU.Length,
                CommUtils.ProtocolLogFormats.Hex, out string logText);
            ExecWriteToLog(logText);

            // reception of the answer
            // reading the start of the response to determine the length of the PDU
            int readCnt = Connection.Read(InBuf, 0, 5, Timeout, CommUtils.ProtocolLogFormats.Hex, out logText);
            ExecWriteToLog(logText);

            if (readCnt == 5) {
                int pduLen;
                int count;

                if (InBuf[0] != dataUnit.ReqADU[0]) // checking device address in response
                {
                    ExecWriteToLog(ModbusPhrases.IncorrectDevAddr);
                } else if (!(InBuf[1] == dataUnit.FuncCode || InBuf[1] == dataUnit.ExcFuncCode)) {
                    ExecWriteToLog(ModbusPhrases.IncorrectPduFuncCode);
                } else {
                    if (InBuf[1] == dataUnit.FuncCode) {
                        // read end of response
                        pduLen = dataUnit.RespPduLen;
                        count = dataUnit.RespAduLen - 5;

                        readCnt = Connection.Read(InBuf, 5, count, Timeout,
                            CommUtils.ProtocolLogFormats.Hex, out logText);
                        ExecWriteToLog(logText);
                    } else // device returned exception
                    {
                        pduLen = 2;
                        count = 0;
                        readCnt = 0;
                    }

                    if (readCnt == count) {
                        if (InBuf[pduLen + 1] + InBuf[pduLen + 2] * 256 ==
                            ModbusUtils.CalcCRC16(InBuf, 0, pduLen + 1)) {
                            // answer decryption
                            string errMsg;

                            if (dataUnit.DecodeRespPDU(InBuf, 1, pduLen, out errMsg)) {
                                ExecWriteToLog(ModbusPhrases.OK);
                                result = true;
                            } else {
                                ExecWriteToLog(errMsg + "!");
                            }
                        } else {
                            ExecWriteToLog(ModbusPhrases.CrcError);
                        }
                    } else {
                        ExecWriteToLog(ModbusPhrases.CommErrorWithExclamation);
                    }
                }
            } else {
                ExecWriteToLog(ModbusPhrases.CommErrorWithExclamation);
            }

            return result;
        }

        /// <summary>
        /// Run ASCII query
        /// </summary>
        public bool AsciiRequest(DataUnit dataUnit) {
            if (!CheckConnection())
                return false;

            var result = false;

            // sending request
            ExecWriteToLog(dataUnit.ReqDescr);
            Connection.WriteLine(dataUnit.ReqStr, out string logText);
            ExecWriteToLog(logText);

            // reception of the answer
            string line = Connection.ReadLine(Timeout, out logText);
            ExecWriteToLog(logText);
            int lineLen = line == null ? 0 : line.Length;

            if (lineLen >= 3) {
                int aduLen = (lineLen - 1) / 2;

                if (aduLen == dataUnit.RespAduLen && lineLen % 2 == 1) {
                    // getting ADU response
                    var aduBuf = new byte[aduLen];
                    var parseOK = true;

                    for (int i = 0,
                        j = 1;
                        i < aduLen && parseOK;
                        i++, j += 2) {
                        try {
                            aduBuf[i] = byte.Parse(line.Substring(j, 2), NumberStyles.HexNumber);
                        } catch {
                            ExecWriteToLog(ModbusPhrases.IncorrectSymbol);
                            parseOK = false;
                        }
                    }

                    if (parseOK) {
                        if (aduBuf[aduLen - 1] == ModbusUtils.CalcLRC(aduBuf, 0, aduLen - 1)) {
                            // answer decryption
                            string errMsg;

                            if (dataUnit.DecodeRespPDU(aduBuf, 1, aduLen - 2, out errMsg)) {
                                ExecWriteToLog(ModbusPhrases.OK);
                                result = true;
                            } else {
                                ExecWriteToLog(errMsg + "!");
                            }
                        } else {
                            ExecWriteToLog(ModbusPhrases.LrcError);
                        }
                    }
                } else {
                    ExecWriteToLog(ModbusPhrases.IncorrectAduLength);
                }
            } else {
                ExecWriteToLog(ModbusPhrases.CommErrorWithExclamation);
            }

            return result;
        }

        /// <summary>
        /// Run a TCP request
        /// </summary>
        public bool TcpRequest(DataUnit dataUnit) {
            if (!CheckConnection())
                return false;

            var result = false;

            // sending request
            WriteToLog(dataUnit.ReqDescr);
            Connection.Write(dataUnit.ReqADU, 0, dataUnit.ReqADU.Length,
                CommUtils.ProtocolLogFormats.Hex, out string logText);
            ExecWriteToLog(logText);

            // reception of the answer
            // read MBAP Header
            int readCnt = Connection.Read(InBuf, 0, 7, Timeout, CommUtils.ProtocolLogFormats.Hex, out logText);
            ExecWriteToLog(logText);

            if (readCnt == 7) {
                int pduLen = InBuf[4] * 256 + InBuf[5] - 1;

                if (InBuf[0] == 0 && InBuf[1] == 0 && InBuf[2] == 0 && InBuf[3] == 0 && pduLen > 0 &&
                    InBuf[6] == dataUnit.ReqADU[6]) {
                    // PDU reading
                    readCnt = Connection.Read(InBuf, 7, pduLen, Timeout,
                        CommUtils.ProtocolLogFormats.Hex, out logText);
                    ExecWriteToLog(logText);

                    if (readCnt == pduLen) {
                        // answer decryption
                        string errMsg;

                        if (dataUnit.DecodeRespPDU(InBuf, 7, pduLen, out errMsg)) {
                            ExecWriteToLog(ModbusPhrases.OK);
                            result = true;
                        } else {
                            ExecWriteToLog(errMsg + "!");
                        }
                    } else {
                        WriteToLog(ModbusPhrases.CommErrorWithExclamation);
                    }
                } else {
                    WriteToLog(ModbusPhrases.IncorrectMbap);
                }
            } else {
                WriteToLog(ModbusPhrases.CommErrorWithExclamation);
            }

            return result;
        }

        /// <summary>
        /// Get the request method corresponding to the data transfer mode.
        /// </summary>
        public RequestDelegate GetRequestMethod(TransMode transMode) {
            switch (transMode) {
                case TransMode.RTU:
                    return RtuRequest;
                case TransMode.ASCII:
                    return AsciiRequest;
                default: // TransMode.TCP
                    return TcpRequest;
            }
        }
    }
}