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
 * Module   : ScadaData
 * Summary  : Communication with SCADA-Server
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2006
 * Modified : 2017
 */

#undef DETAILED_LOG // output detailed information about data exchange with SCADA-Server

using Scada.Data.Configuration;
using Scada.Data.Models;
using Scada.Data.Tables;
using System;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Utils;

namespace Scada.Client {
    /// <summary>
    /// Communication with SCADA-Server
    /// <para>Data exchange with SCADA-Server</para>
    /// </summary>
    public class ServerComm {
        /// <summary>
        /// State of data exchange with SCADA-Server
        /// </summary>
        public enum CommStates {
            /// <summary>
            /// No connection established
            /// </summary>
            Disconnected,

            /// <summary>
            /// Connection established
            /// </summary>
            Connected,

            /// <summary>
            /// Connection established and program authorized
            /// </summary>
            Authorized,

            /// <summary>
            /// SCADA Server is not ready
            /// </summary>
            NotReady,

            /// <summary>
            /// Data exchange error
            /// </summary>
            Error,

            /// <summary>
            /// Waiting for response to command or request
            /// </summary>
            WaitResponse
        }

        /// <summary>
        /// Directories on SCADA-Server
        /// </summary>
        public enum Dirs {
            /// <summary>
            /// Current slice directory
            /// </summary>
            Cur = 0x01,

            /// <summary>
            /// Watch Slices Directory
            /// </summary>
            Hour = 0x02,

            /// <summary>
            /// Directory of minute slices
            /// </summary>
            Min = 0x03,

            /// <summary>
            /// Event Directory
            /// </summary>
            Events = 0x04,

            /// <summary>
            /// Directory base configuration in DAT format
            /// </summary>
            BaseDAT = 0x05,

            /// <summary>
            /// Interface directory (tables, charts, etc.)
            /// </summary>
            Itf = 0x06
        }


        /// <summary>
        /// Timeout for sending data via TCP, ms
        /// </summary>
        protected const int TcpSendTimeout = 1000;

        /// <summary>
        /// Timeout for receiving data via TCP, ms
        /// </summary>
        protected const int TcpReceiveTimeout = 5000;

        /// <summary>
        /// Connection retry interval
        /// </summary>
        protected readonly TimeSpan ConnectSpan = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Connection check interval by requesting server status
        /// </summary>
        protected readonly TimeSpan PingSpan = TimeSpan.FromSeconds(30);


        /// <summary>
        /// Settings for connecting to the SCADA Server
        /// </summary>
        protected CommSettings commSettings;

        /// <summary>
        /// Work Log
        /// </summary>
        protected ILog log;

        /// <summary>
        /// Job Logging Method
        /// </summary>
        protected Log.WriteLineDelegate writeToLog { get; set; }

        /// <summary>
        /// TCP client for data exchange with SCADA Server
        /// </summary>
        protected TcpClient tcpClient;

        /// <summary>
        /// TCP client data flow
        /// </summary>
        protected NetworkStream netStream;

        /// <summary>
        /// Object for synchronization of data exchange with SCADA-Server
        /// </summary>
        protected object tcpLock;

        /// <summary>
        /// State of data exchange with SCADA-Server
        /// </summary>
        protected CommStates commState;

        /// <summary>
        /// SCADA Server Version
        /// </summary>
        protected string serverVersion;

        /// <summary>
        /// The time to successfully call the reconnect method
        /// </summary>
        protected DateTime restConnSuccDT;

        /// <summary>
        /// Unsuccessful call callback method
        /// </summary>
        protected DateTime restConnErrDT;

        /// <summary>
        /// Error message
        /// </summary>
        protected string errMsg;


        /// <summary>
        /// Constructor restricting the creation of an object without parameters
        /// </summary>
        protected ServerComm() {
            tcpClient = null;
            netStream = null;
            tcpLock = new object();
            commState = CommStates.Disconnected;
            serverVersion = "";
            restConnSuccDT = DateTime.MinValue;
            restConnErrDT = DateTime.MinValue;
            errMsg = "";
        }

        /// <inheritdoc />
        /// <summary>
        /// Constructor with setting up connection settings with SCADA Server and operation log
        /// </summary>
        public ServerComm(CommSettings commSettings, ILog log)
            : this() {
            this.commSettings = commSettings;
            this.log = log;
            this.writeToLog = null;
        }

        /// <summary>
        /// Designer with setting the connection settings with SCADA-Server and the method of recording in the work log
        /// </summary>
        public ServerComm(CommSettings commSettings, Log.WriteLineDelegate writeToLog)
            : this() {
            this.commSettings = commSettings;
            this.log = null;
            this.writeToLog = writeToLog;
        }


        /// <summary>
        /// Get connection settings with SCADA-Server
        /// </summary>
        public CommSettings CommSettings {
            get { return commSettings; }
        }

        /// <summary>
        /// Get the status of data exchange with SCADA-Server
        /// </summary>
        public CommStates CommState {
            get { return commState; }
        }

        /// <summary>
        /// Get a description of the status of data exchange with SCADA-Server
        /// </summary>
        public string CommStateDescr {
            get {
                var stateDescr = new StringBuilder();
                if (serverVersion != "")
                    stateDescr.Append("version ").Append(serverVersion).Append(", ");

                switch (commState) {
                    case CommStates.Disconnected:
                        stateDescr.Append("not connected");
                        break;
                    case CommStates.Connected:
                        stateDescr.Append("connected");
                        break;
                    case CommStates.Authorized:
                        stateDescr.Append("authentication is successful");
                        break;
                    case CommStates.NotReady:
                        stateDescr.Append("SCADA-Server isn't ready");
                        break;
                    case CommStates.Error:
                        stateDescr.Append("communication error");
                        break;
                    case CommStates.WaitResponse:
                        stateDescr.Append("waiting for response");
                        break;
                }

                if (errMsg != "")
                    stateDescr.Append(" - ").Append(errMsg);

                return stateDescr.ToString();
            }
        }

        /// <summary>
        /// Receive an error message
        /// </summary>
        public string ErrMsg {
            get { return errMsg; }
        }


        /// <summary>
        /// Check the data format for the specified command when communicating with the SCADA Server
        /// </summary>
        protected bool CheckDataFormat(byte[] buffer, int cmdNum) {
            return CheckDataFormat(buffer, cmdNum, buffer.Length);
        }

        /// <summary>
        /// Check the data format for the specified command when communicating with the SCADA Server, specifying the buffer length used
        /// </summary>
        protected bool CheckDataFormat(byte[] buffer, int cmdNum, int bufLen) {
            return bufLen >= 3 && buffer[0] + 256 * buffer[1] == bufLen && buffer[2] == cmdNum;
        }

        /// <summary>
        /// Get the string designation of the directory on the SCADA-Server
        /// </summary>
        protected string DirToString(Dirs directory) {
            switch (directory) {
                case Dirs.Cur:
                    return "[Srez]" + Path.DirectorySeparatorChar;
                case Dirs.Hour:
                    return "[Hr]" + Path.DirectorySeparatorChar;
                case Dirs.Min:
                    return "[Min]" + Path.DirectorySeparatorChar;
                case Dirs.Events:
                    return "[Ev]" + Path.DirectorySeparatorChar;
                case Dirs.BaseDAT:
                    return "[Base]" + Path.DirectorySeparatorChar;
                case Dirs.Itf:
                    return "[Itf]" + Path.DirectorySeparatorChar;
                default:
                    return "";
            }
        }

        /// <summary>
        /// Record the action in the work log
        /// </summary>
        protected void WriteAction(string text, Log.ActTypes actType) {
            if (log != null)
                log.WriteAction(text, actType);
            else writeToLog?.Invoke(text);
        }

        /// <summary>
        /// Connect to SCADA-Server and authorize
        /// </summary>
        protected bool Connect() {
            try {
                commState = CommStates.Disconnected;
                WriteAction($"Connect to SCADA-Server \"{commSettings.ServerHost}\"", Log.ActTypes.Action);

                // determining the IP address if it is specified in the program configuration
                IPAddress ipAddress = null;
                try {
                    ipAddress = IPAddress.Parse(commSettings.ServerHost);
                } catch {
                    // ignored
                }

                // create, configure and attempt to establish a connection
                tcpClient = new TcpClient {
                    NoDelay = true, // sends data immediately upon calling NetworkStream.Write
                    ReceiveBufferSize = 16384, // 16 kB
                    SendBufferSize = 8192, // 8 kB, default size
                    SendTimeout = TcpSendTimeout,
                    ReceiveTimeout = TcpReceiveTimeout
                };

                if (ipAddress == null)
                    tcpClient.Connect(commSettings.ServerHost, commSettings.ServerPort);
                else
                    tcpClient.Connect(ipAddress, commSettings.ServerPort);

                netStream = tcpClient.GetStream();

                // getting version of SCADA-Server
                var buf = new byte[5];
                int bytesRead = netStream.Read(buf, 0, 5);

                // processing read version data
                if (bytesRead == buf.Length && CheckDataFormat(buf, 0x00)) {
                    commState = CommStates.Connected;
                    serverVersion = buf[4] + "." + buf[3];

                    // request for correct user name and password, his role
                    var userLen = (byte) commSettings.ServerUser.Length;
                    var pwdLen = (byte) commSettings.ServerPwd.Length;
                    buf = new byte[5 + userLen + pwdLen];

                    buf[0] = (byte) (buf.Length % 256);
                    buf[1] = (byte) (buf.Length / 256);
                    buf[2] = 0x01;
                    buf[3] = userLen;
                    Array.Copy(Encoding.Default.GetBytes(commSettings.ServerUser), 0, buf, 4, userLen);
                    buf[4 + userLen] = pwdLen;
                    Array.Copy(Encoding.Default.GetBytes(commSettings.ServerPwd), 0, buf, 5 + userLen, pwdLen);

                    netStream.Write(buf, 0, buf.Length);

                    // receiving result
                    buf = new byte[4];
                    bytesRead = netStream.Read(buf, 0, 4);

                    // read data processing
                    if (bytesRead == buf.Length && CheckDataFormat(buf, 0x01)) {
                        int roleID = buf[3];

                        if (roleID == BaseValues.Roles.App) {
                            commState = CommStates.Authorized;
                        } else if (roleID < BaseValues.Roles.Err) {
                            errMsg = "Insufficient rights to connect to SCADA-Server";
                            WriteAction(errMsg, Log.ActTypes.Error);
                            commState = CommStates.Error;
                        } else // roleID == BaseValues.Roles.Err
                        {
                            errMsg = "User name or password is incorrect";
                            WriteAction(errMsg, Log.ActTypes.Error);
                            commState = CommStates.Error;
                        }
                    } else {
                        errMsg = "Incorrect SCADA-Server response to check user name and password request";
                        WriteAction(errMsg, Log.ActTypes.Error);
                        commState = CommStates.Error;
                    }
                } else {
                    errMsg = "Incorrect SCADA-Server response to version request";
                    WriteAction(errMsg, Log.ActTypes.Error);
                    commState = CommStates.Error;
                }
            } catch (Exception ex) {
                errMsg = ("Error connecting to SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
            }

            // return result
            if (commState == CommStates.Authorized) {
                return true;
            }

            Disconnect();
            return false;
        }

        /// <summary>
        /// Break connection with SCADA-Server
        /// </summary>
        protected void Disconnect() {
            try {
                commState = CommStates.Disconnected;
                serverVersion = "";

                if (tcpClient != null) {
                    WriteAction("Disconnect from SCADA-Server", Log.ActTypes.Action);

                    if (netStream != null) {
                        // clearing (for proper disconnection) and closing the TCP client data stream
                        ClearNetStream();
                        netStream.Close();
                        netStream = null;
                    }

                    tcpClient.Close();
                    tcpClient = null;
                }
            } catch (Exception ex) {
                errMsg = ("Error disconnecting from SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
            }
        }

        /// <summary>
        /// Read information from a TCP client data stream
        /// </summary>
        /// <remarks>The method is used to read the "large" amount of data.</remarks>
        protected int ReadNetStream(byte[] buffer, int offset, int size) {
            var bytesRead = 0;
            var startReadDT = DateTime.Now;

            do {
                bytesRead += netStream.Read(buffer, bytesRead + offset, size - bytesRead);
            } while (bytesRead < size && (DateTime.Now - startReadDT).TotalMilliseconds <= TcpReceiveTimeout);

            return bytesRead;
        }

        /// <summary>
        /// Clear TCP client data stream
        /// </summary>
        protected void ClearNetStream() {
            try {
                if (netStream != null && netStream.DataAvailable) {
                    // reading the remaining data from the stream, but not more than 100 kB
                    var buf = new byte[1024];
                    var n = 0;
                    while (netStream.DataAvailable && ++n <= 100)
                        try {
                            netStream.Read(buf, 0, 1024);
                        } catch {
                            // ignored
                        }
                }
            } catch (Exception ex) {
                errMsg = ("Error clear network stream: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
            }
        }

        /// <summary>
        /// Reconnect to the SCADA Server and authorize if necessary
        /// </summary>
        protected bool RestoreConnection() {
            try {
                var connectNeeded = false; // re-connection required
                var now = DateTime.Now;

                if (commState >= CommStates.Authorized) {
                    if (now - restConnSuccDT > PingSpan) {
                        // connection check
                        try {
                            WriteAction("Request SCADA-Server state", Log.ActTypes.Action);
                            commState = CommStates.WaitResponse;

                            // SCADA Server Status Request (ping)
                            var buf = new byte[3];
                            buf[0] = 0x03;
                            buf[1] = 0x00;
                            buf[2] = 0x02;
                            netStream.Write(buf, 0, 3);

                            // receiving result
                            buf = new byte[4];
                            netStream.Read(buf, 0, 4);

                            // result processing
                            if (CheckDataFormat(buf, 0x02)) {
                                commState = buf[3] > 0 ? CommStates.Authorized : CommStates.NotReady;
                            } else {
                                errMsg = "Incorrect SCADA-Server response to state request";
                                WriteAction(errMsg, Log.ActTypes.Error);
                                commState = CommStates.Error;
                                connectNeeded = true;
                            }
                        } catch {
                            connectNeeded = true;
                        }
                    }
                } else if (now - restConnErrDT > ConnectSpan) {
                    connectNeeded = true;
                }

                // connection if necessary
                if (connectNeeded) {
                    if (tcpClient != null)
                        Disconnect();

                    if (Connect()) {
                        restConnSuccDT = now;
                        restConnErrDT = DateTime.MinValue;
                        return true;
                    }

                    restConnSuccDT = DateTime.MinValue;
                    restConnErrDT = now;
                    return false;
                }

                ClearNetStream(); // clear TCP client data stream

                if (commState >= CommStates.Authorized) {
                    restConnSuccDT = now;
                    return true;
                }

                errMsg = "Unable to connect to SCADA-Server. Try again.";
                WriteAction(errMsg, Log.ActTypes.Error);
                return false;
            } catch (Exception ex) {
                errMsg = ("Error restoring connection with SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
                commState = CommStates.Error;
                return false;
            }
        }

        /// <summary>
        /// Restore TCP Receive Timeout Value to Default
        /// </summary>
        protected void RestoreReceiveTimeout() {
            try {
                if (tcpClient.ReceiveTimeout != TcpReceiveTimeout)
                    tcpClient.ReceiveTimeout = TcpReceiveTimeout;
            } catch {
                // ignored
            }
        }

        /// <summary>
        /// Receive file from SCADA-Server
        /// </summary>
        protected bool ReceiveFileToStream(Dirs dir, string fileName, Stream inStream) {
            var result = false;
            string filePath = DirToString(dir) + fileName;

            try {
#if DETAILED_LOG
                WriteAction(string.Format(Localization.UseRussian ? 
                    "Приём файла {0} от SCADA-Сервера" : 
                    "Receive file {0} from SCADA-Server", filePath), Log.ActTypes.Action);
#endif

                commState = CommStates.WaitResponse;
                tcpClient.ReceiveTimeout = commSettings.ServerTimeout;

                const int DataSize = 50 * 1024; // requested data size 50 KB
                const byte DataSizeL = DataSize % 256;
                const byte DataSizeH = DataSize / 256;

                var buf = new byte[6 + DataSize]; // send and receive data buffer
                var open = true; // open file is running
                var stop = false; // sign of completion of data reception

                while (!stop) {
                    if (open) {
                        // sending a command to open a file and read data
                        var fileNameLen = (byte) fileName.Length;
                        int cmdLen = 7 + fileNameLen;
                        buf[0] = (byte) (cmdLen % 256);
                        buf[1] = (byte) (cmdLen / 256);
                        buf[2] = 0x08;
                        buf[3] = (byte) dir;
                        buf[4] = fileNameLen;
                        Array.Copy(Encoding.Default.GetBytes(fileName), 0, buf, 5, fileNameLen);
                        buf[cmdLen - 2] = DataSizeL;
                        buf[cmdLen - 1] = DataSizeH;
                        netStream.Write(buf, 0, cmdLen);
                    } else {
                        // sending a command to read data from a file
                        buf[0] = 0x05;
                        buf[1] = 0x00;
                        buf[2] = 0x0A;
                        buf[3] = DataSizeL;
                        buf[4] = DataSizeH;
                        netStream.Write(buf, 0, 5);
                    }

                    // receiving the result of opening the file and the read data
                    byte cmdNum = buf[2];
                    int headerLen = open ? 6 : 5;
                    int bytesRead = netStream.Read(buf, 0, headerLen);
                    var dataSizeRead = 0; // size of data read from file                    

                    if (bytesRead == headerLen) {
                        dataSizeRead = buf[headerLen - 2] + 256 * buf[headerLen - 1];
                        if (0 < dataSizeRead && dataSizeRead <= DataSize)
                            bytesRead += ReadNetStream(buf, headerLen, dataSizeRead);
                    }

                    if (CheckDataFormat(buf, cmdNum, bytesRead) && bytesRead == dataSizeRead + headerLen) {
                        if (open) {
                            open = false;

                            if (buf[3] > 0) { // file open
                                inStream.Write(buf, 6, dataSizeRead);
                                commState = CommStates.Authorized;
                                stop = dataSizeRead < DataSize;
                            } else {
                                errMsg = $"SCADA-Server unable to open file {filePath}";
                                WriteAction(errMsg, Log.ActTypes.Action);
                                commState = CommStates.NotReady;
                                stop = true;
                            }
                        } else {
                            inStream.Write(buf, 5, dataSizeRead);
                            commState = CommStates.Authorized;
                            stop = dataSizeRead < DataSize;
                        }
                    } else {
                        errMsg = $"Incorrect SCADA-Server response to open file or read from file {filePath} command ";
                        WriteAction(errMsg, Log.ActTypes.Error);
                        commState = CommStates.Error;
                        stop = true;
                    }
                }

                // result determination
                if (commState == CommStates.Authorized) {
                    if (inStream.Length > 0)
                        inStream.Position = 0;
                    result = true;
                }
            } catch (Exception ex) {
                errMsg = $"Error receiving file {filePath} from SCADA-Server: " + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
                Disconnect();
            } finally {
                RestoreReceiveTimeout();
            }

            return result;
        }

        /// <summary>
        /// Send command to SCADA-Server
        /// </summary>
        protected bool SendCommand(int userID, int ctrlCnl, double cmdVal, byte[] cmdData, int kpNum, out bool result) {
            Monitor.Enter(tcpLock);
            var complete = false;
            result = false;
            errMsg = "";

            try {
                if (RestoreConnection()) {
                    WriteAction("Send telecommand to SCADA-Server", Log.ActTypes.Action);

                    commState = CommStates.WaitResponse;
                    tcpClient.ReceiveTimeout = commSettings.ServerTimeout;

                    // sending team
                    int cmdLen = double.IsNaN(cmdVal) ? 10 + cmdData?.Length ?? 12 : 18;
                    var buf = new byte[cmdLen];
                    buf[0] = (byte) (cmdLen % 256);
                    buf[1] = (byte) (cmdLen / 256);
                    buf[2] = 0x06;
                    buf[3] = (byte) (userID % 256);
                    buf[4] = (byte) (userID / 256);
                    buf[6] = (byte) (ctrlCnl % 256);
                    buf[7] = (byte) (ctrlCnl / 256);

                    if (!double.IsNaN(cmdVal)) { // standard team
                        buf[5] = 0x00;
                        buf[8] = 0x08;
                        buf[9] = 0x00;
                        byte[] bytes = BitConverter.GetBytes(cmdVal);
                        Array.Copy(bytes, 0, buf, 10, 8);
                    } else if (cmdData != null) { // binary command
                        buf[5] = 0x01;
                        int cmdDataLen = cmdData.Length;
                        buf[8] = (byte) (cmdDataLen % 256);
                        buf[9] = (byte) (cmdDataLen / 256);
                        Array.Copy(cmdData, 0, buf, 10, cmdDataLen);
                    } else { // poll KP
                        buf[5] = 0x02;
                        buf[8] = 0x02;
                        buf[9] = 0x00;
                        buf[10] = (byte) (kpNum % 256);
                        buf[11] = (byte) (kpNum / 256);
                    }

                    netStream.Write(buf, 0, cmdLen);

                    // receiving result
                    buf = new byte[4];
                    int bytesRead = netStream.Read(buf, 0, 4);

                    // data processing
                    if (bytesRead == buf.Length && CheckDataFormat(buf, 0x06)) {
                        result = buf[3] > 0;
                        commState = result ? CommStates.Authorized : CommStates.NotReady;
                        complete = true;
                    } else {
                        errMsg = "Incorrect SCADA-Server response to telecommand";
                        WriteAction(errMsg, Log.ActTypes.Error);
                        commState = CommStates.Error;
                    }
                }
            } catch (Exception ex) {
                errMsg = ("Error sending telecommand to SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
                Disconnect();
            } finally {
                RestoreReceiveTimeout();
                Monitor.Exit(tcpLock);
            }

            return complete;
        }


        /// <summary>
        /// Request the correct user name and password from the SCADA-Server, get its role.
        /// Returns the success of the query.
        /// </summary>
        public bool CheckUser(string username, string password, out int roleID) {
            Monitor.Enter(tcpLock);
            var result = false;
            roleID = BaseValues.Roles.Disabled;
            errMsg = "";

            try {
                if (RestoreConnection()) {
                    commState = CommStates.WaitResponse;
                    tcpClient.ReceiveTimeout = commSettings.ServerTimeout;

                    // request for correct user name and password, his role
                    byte userLen = username == null ? (byte) 0 : (byte) username.Length;
                    byte pwdLen = password == null ? (byte) 0 : (byte) password.Length;
                    var buf = new byte[5 + userLen + pwdLen];

                    buf[0] = (byte) (buf.Length % 256);
                    buf[1] = (byte) (buf.Length / 256);
                    buf[2] = 0x01;
                    buf[3] = userLen;
                    if (userLen > 0)
                        Array.Copy(Encoding.Default.GetBytes(username), 0, buf, 4, userLen);
                    buf[4 + userLen] = pwdLen;
                    if (pwdLen > 0)
                        Array.Copy(Encoding.Default.GetBytes(password), 0, buf, 5 + userLen, pwdLen);

                    netStream.Write(buf, 0, buf.Length);

                    // receiving result
                    buf = new byte[4];
                    int bytesRead = netStream.Read(buf, 0, 4);

                    // data processing
                    if (bytesRead == buf.Length && CheckDataFormat(buf, 0x01)) {
                        roleID = buf[3];
                        result = true;
                        commState = CommStates.Authorized;
                    } else {
                        errMsg = "Incorrect SCADA-Server response to check user name and password request";
                        WriteAction(errMsg, Log.ActTypes.Error);
                        commState = CommStates.Error;
                    }
                }
            } catch (Exception ex) {
                errMsg = ("Error requesting check user name and password to SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
                Disconnect();
            } finally {
                RestoreReceiveTimeout();
                Monitor.Exit(tcpLock);
            }

            return result;
        }

        /// <summary>
        /// Accept configuration database table from SCADA-Server
        /// </summary>
        public bool ReceiveBaseTable(string tableName, DataTable dataTable) {
            Monitor.Enter(tcpLock);
            var result = false;
            errMsg = "";

            try {
                try {
                    if (RestoreConnection()) {
                        using (var memStream = new MemoryStream()) {
                            if (ReceiveFileToStream(Dirs.BaseDAT, tableName, memStream)) {
                                var adapter = new BaseAdapter();
                                adapter.Stream = memStream;
                                adapter.TableName = tableName;
                                adapter.Fill(dataTable, false);
                                result = true;
                            }
                        }
                    }
                } finally {
                    // clearing the table if new data could not be obtained
                    if (!result)
                        dataTable.Rows.Clear();
                }
            } catch (Exception ex) {
                errMsg = ("Error receiving configuration database table from SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
            } finally {
                Monitor.Exit(tcpLock);
            }

            return result;
        }

        /// <summary>
        /// Accept slice table from SCADA-Server
        /// </summary>
        public bool ReceiveSrezTable(string tableName, SrezTableLight srezTableLight) {
            Monitor.Enter(tcpLock);
            var result = false;
            errMsg = "";

            try {
                try {
                    if (RestoreConnection()) {
                        // table directory definition
                        var dir = Dirs.Cur;
                        if (tableName.Length > 0) {
                            if (tableName[0] == 'h')
                                dir = Dirs.Hour;
                            else if (tableName[0] == 'm')
                                dir = Dirs.Min;
                        }

                        // receiving data
                        using (var memStream = new MemoryStream()) {
                            if (ReceiveFileToStream(dir, tableName, memStream)) {
                                var adapter = new SrezAdapter {
                                    Stream = memStream,
                                    TableName = tableName
                                };
                                adapter.Fill(srezTableLight);
                                result = true;
                            }
                        }
                    }
                } finally {
                    // clearing the table if new data could not be obtained
                    if (!result) {
                        srezTableLight.Clear();
                        srezTableLight.TableName = tableName;
                    }
                }
            } catch (Exception ex) {
                errMsg = ("Error receiving data table from SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
            } finally {
                Monitor.Exit(tcpLock);
            }

            return result;
        }

        /// <summary>
        /// Accept input channel trend from SCADA Server
        /// </summary>
        public bool ReceiveTrend(string tableName, DateTime date, Trend trend) {
            Monitor.Enter(tcpLock);
            var result = false;
            errMsg = "";

            try {
                try {
                    if (RestoreConnection()) {
                        WriteAction($"Receive input channel {trend.CnlNum} trend from SCADA-Server. File: {tableName}",
                            Log.ActTypes.Action);

                        commState = CommStates.WaitResponse;
                        tcpClient.ReceiveTimeout = commSettings.ServerTimeout;

                        byte tableType; // table type: current, hour or minute
                        byte year,
                            month,
                            day; // date of requested data

                        if (tableName == SrezAdapter.CurTableName) {
                            tableType = 0x01;
                            year = month = day = 0;
                        } else {
                            tableType = tableName.Length > 0 && tableName[0] == 'h' ? (byte) 0x02 : (byte) 0x03;
                            year = (byte) (date.Year % 100);
                            month = (byte) date.Month;
                            day = (byte) date.Day;
                        }

                        // send input channel trend request
                        var buf = new byte[13];
                        buf[0] = 0x0D;
                        buf[1] = 0x00;
                        buf[2] = 0x0D;
                        buf[3] = tableType;
                        buf[4] = year;
                        buf[5] = month;
                        buf[6] = day;
                        buf[7] = 0x01;
                        buf[8] = 0x00;
                        byte[] bytes = BitConverter.GetBytes(trend.CnlNum);
                        Array.Copy(bytes, 0, buf, 9, 4);
                        netStream.Write(buf, 0, 13);

                        // receiving input channel trend data
                        buf = new byte[7];
                        int bytesRead = netStream.Read(buf, 0, 7);
                        var pointCnt = 0;

                        if (bytesRead == 7) {
                            pointCnt = buf[5] + buf[6] * 256;

                            if (pointCnt > 0) {
                                Array.Resize<byte>(ref buf, 7 + pointCnt * 18);
                                bytesRead += ReadNetStream(buf, 7, buf.Length - 7);
                            }
                        }

                        // fill the trend of the input channel from the data
                        if (bytesRead == buf.Length && buf[4] == 0x0D) {
                            for (var i = 0; i < pointCnt; i++) {
                                Trend.Point point;
                                int pos = i * 18 + 7;
                                point.DateTime = ScadaUtils.DecodeDateTime(BitConverter.ToDouble(buf, pos));
                                point.Val = BitConverter.ToDouble(buf, pos + 8);
                                point.Stat = BitConverter.ToUInt16(buf, pos + 16);

                                trend.Points.Add(point);
                            }

                            trend.Sort();
                            result = true;
                            commState = CommStates.Authorized;
                        } else {
                            errMsg = "Incorrect SCADA-Server response to input channel trend request";
                            WriteAction(errMsg, Log.ActTypes.Error);
                            commState = CommStates.Error;
                        }
                    }
                } finally {
                    // clear trend if unable to get new data
                    if (!result) {
                        trend.Clear();
                        trend.TableName = tableName;
                    }
                }
            } catch (Exception ex) {
                errMsg = ("Error receiving input channel trend from SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
                Disconnect();
            } finally {
                RestoreReceiveTimeout();
                Monitor.Exit(tcpLock);
            }

            return result;
        }

        /// <summary>
        /// Receive event table from SCADA-Server
        /// </summary>
        public bool ReceiveEventTable(string tableName, EventTableLight eventTableLight) {
            Monitor.Enter(tcpLock);
            var result = false;
            errMsg = "";

            try {
                try {
                    if (RestoreConnection()) {
                        using (var memStream = new MemoryStream()) {
                            if (ReceiveFileToStream(Dirs.Events, tableName, memStream)) {
                                var adapter = new EventAdapter();
                                adapter.Stream = memStream;
                                adapter.TableName = tableName;
                                adapter.Fill(eventTableLight);
                                result = true;
                            }
                        }
                    }
                } finally {
                    // clearing the table if new data could not be obtained
                    if (!result) {
                        eventTableLight.Clear();
                        eventTableLight.TableName = tableName;
                    }
                }
            } catch (Exception ex) {
                errMsg = ("Error receiving event table from SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
            } finally {
                Monitor.Exit(tcpLock);
            }

            return result;
        }

        /// <summary>
        /// Accept submission from SCADA Server
        /// </summary>
        public bool ReceiveView(string fileName, BaseView view) {
            bool result = ReceiveUiObj(fileName, view);
            if (result && view != null)
                view.Path = fileName;
            return result;
        }

        /// <summary>
        /// Accept user interface object from SCADA Server
        /// </summary>
        public bool ReceiveUiObj(string fileName, ISupportLoading uiObj) {
            Monitor.Enter(tcpLock);
            var result = false;
            errMsg = "";

            try {
                try {
                    if (RestoreConnection()) {
                        using (var memStream = new MemoryStream()) {
                            if (ReceiveFileToStream(Dirs.Itf, fileName, memStream)) {
                                uiObj.LoadFromStream(memStream);
                                result = true;
                            }
                        }
                    }
                } finally {
                    if (!result)
                        uiObj.Clear();
                }
            } catch (Exception ex) {
                errMsg = ("Error receiving user interface object from SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
            } finally {
                Monitor.Exit(tcpLock);
            }

            return result;
        }

        /// <summary>
        /// Receive file from SCADA-Server
        /// </summary>
        public bool ReceiveFile(Dirs dir, string fileName, Stream stream) {
            lock (tcpLock) {
                return RestoreConnection() && ReceiveFileToStream(dir, fileName, stream);
            }
        }

        /// <summary>
        /// Accept date and time of file change from SCADA-Server.
        /// If there is no file, the minimum date is returned.
        /// </summary>
        public DateTime ReceiveFileAge(Dirs dir, string fileName) {
            Monitor.Enter(tcpLock);
            var result = DateTime.MinValue;
            string filePath = DirToString(dir) + fileName;
            errMsg = "";

            try {
                if (RestoreConnection()) {
#if DETAILED_LOG
                    WriteAction(string.Format(Localization.UseRussian ? 
                        "Приём даты и времени изменения файла {0} от SCADA-Сервера" :
                        "Receive date and time of file {0} modification from SCADA-Server", filePath), 
                        Log.ActTypes.Action);
#endif

                    commState = CommStates.WaitResponse;
                    tcpClient.ReceiveTimeout = commSettings.ServerTimeout;

                    // send request date and time of file change
                    int cmdLen = 6 + fileName.Length;
                    var buf = new byte[cmdLen];
                    buf[0] = (byte) (cmdLen % 256);
                    buf[1] = (byte) (cmdLen / 256);
                    buf[2] = 0x0C;
                    buf[3] = 0x01;
                    buf[4] = (byte) dir;
                    buf[5] = (byte) fileName.Length;
                    Array.Copy(Encoding.Default.GetBytes(fileName), 0, buf, 6, fileName.Length);
                    netStream.Write(buf, 0, cmdLen);

                    // receiving date and time of file change
                    buf = new byte[12];
                    netStream.Read(buf, 0, 12);

                    // handle file date and time
                    if (CheckDataFormat(buf, 0x0C)) {
                        double dt = BitConverter.ToDouble(buf, 4);
                        result = dt == 0.0 ? DateTime.MinValue : ScadaUtils.DecodeDateTime(dt);
                        commState = CommStates.Authorized;
                    } else {
                        errMsg =
                            $"Incorrect SCADA-Server response to file {filePath} modification date and time request";
                        WriteAction(errMsg, Log.ActTypes.Error);
                        commState = CommStates.Error;
                    }
                }
            } catch (Exception ex) {
                errMsg = $"Error receiving date and time of file {filePath} modification from SCADA-Server: " +
                         ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
                Disconnect();
            } finally {
                RestoreReceiveTimeout();
                Monitor.Exit(tcpLock);
            }

            return result;
        }


        /// <summary>
        /// Send the standard command TU SCADA-Server
        /// </summary>
        public bool SendStandardCommand(int userID, int ctrlCnl, double cmdVal, out bool result) {
            return SendCommand(userID, ctrlCnl, cmdVal, null, 0, out result);
        }

        /// <summary>
        /// Send binary command TU SCADA-Server
        /// </summary>
        public bool SendBinaryCommand(int userID, int ctrlCnl, byte[] cmdData, out bool result) {
            return SendCommand(userID, ctrlCnl, double.NaN, cmdData, 0, out result);
        }

        /// <summary>
        /// Send a command for extraordinary polling of SCADA-Server
        /// </summary>
        public bool SendRequestCommand(int userID, int ctrlCnl, int kpNum, out bool result) {
            return SendCommand(userID, ctrlCnl, double.NaN, null, kpNum, out result);
        }

        /// <summary>
        /// Accept TU command from SCADA-Server
        /// </summary>
        /// <remarks>
        /// For a standard command, the data returned by the command is null.
        /// For a binary command, the return value of the command is double.NaN.
        /// For a polling command KP, the return value of the command is double.NaN and these commands are null.</remarks>
        public bool ReceiveCommand(out int kpNum, out int cmdNum, out double cmdVal, out byte[] cmdData) {
            Command cmd;
            bool result = ReceiveCommand(out cmd);

            if (result) {
                kpNum = cmd.KPNum;
                cmdNum = cmd.CmdNum;
                cmdVal = cmd.CmdTypeID == BaseValues.CmdTypes.Standard ? cmd.CmdVal : double.NaN;
                cmdData = cmd.CmdTypeID == BaseValues.CmdTypes.Binary ? cmd.CmdData : null;
            } else {
                kpNum = 0;
                cmdNum = 0;
                cmdVal = double.NaN;
                cmdData = null;
            }

            return result;
        }

        /// <summary>
        /// Take command from SCADA-Server
        /// </summary>
        public bool ReceiveCommand(out Command cmd) {
            Monitor.Enter(tcpLock);
            var result = false;
            cmd = null;
            errMsg = "";

            try {
                if (RestoreConnection()) {
                    commState = CommStates.WaitResponse;
                    tcpClient.ReceiveTimeout = commSettings.ServerTimeout;

                    // command request
                    var buf = new byte[3];
                    buf[0] = 0x03;
                    buf[1] = 0x00;
                    buf[2] = 0x07;
                    netStream.Write(buf, 0, 3);

                    // team reception
                    buf = new byte[5];
                    int bytesRead = netStream.Read(buf, 0, 5);
                    var cmdDataLen = 0;

                    if (bytesRead == 5) {
                        cmdDataLen = buf[3] + buf[4] * 256;

                        if (cmdDataLen > 0) {
                            Array.Resize<byte>(ref buf, 10 + cmdDataLen);
                            bytesRead += netStream.Read(buf, 5, 5 + cmdDataLen);
                        }
                    }

                    // data processing
                    if (CheckDataFormat(buf, 0x07) && bytesRead == buf.Length) {
                        if (cmdDataLen > 0) {
                            byte cmdType = buf[5];
                            cmd = new Command(cmdType);

                            if (cmdType == 0) {
                                cmd.CmdVal = BitConverter.ToDouble(buf, 10);
                            } else if (cmdType == 1) {
                                var cmdData = new byte[cmdDataLen];
                                Array.Copy(buf, 10, cmdData, 0, cmdDataLen);
                                cmd.CmdData = cmdData;
                            }

                            cmd.KPNum = buf[6] + buf[7] * 256;
                            cmd.CmdNum = buf[8] + buf[9] * 256;

                            commState = CommStates.Authorized;
                            result = true;
                        } else { // there are no teams in the queue
                            commState = CommStates.Authorized;
                        }
                    } else {
                        errMsg = "Incorrect SCADA-Server response to telecommand request";
                        WriteAction(errMsg, Log.ActTypes.Error);
                        commState = CommStates.Error;
                    }
                }
            } catch (Exception ex) {
                errMsg = ("Error requesting telecommand from SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
                Disconnect();
            } finally {
                RestoreReceiveTimeout();
                Monitor.Exit(tcpLock);
            }

            return result;
        }


        /// <summary>
        /// Send the current cut SCADA-Server
        /// </summary>
        public bool SendSrez(SrezTableLight.Srez curSrez, out bool result) {
            Monitor.Enter(tcpLock);
            var complete = false;
            result = false;
            errMsg = "";

            try {
                if (RestoreConnection()) {
                    commState = CommStates.WaitResponse;
                    tcpClient.ReceiveTimeout = commSettings.ServerTimeout;

                    // sending a command to write current slice
                    int cnlCnt = curSrez.CnlNums.Length;
                    int cmdLen = cnlCnt * 14 + 5;

                    var buf = new byte[cmdLen];
                    buf[0] = (byte) (cmdLen % 256);
                    buf[1] = (byte) (cmdLen / 256);
                    buf[2] = 0x03;
                    buf[3] = (byte) (cnlCnt % 256);
                    buf[4] = (byte) (cnlCnt / 256);

                    for (var i = 0; i < cnlCnt; i++) {
                        byte[] bytes = BitConverter.GetBytes((uint) curSrez.CnlNums[i]);
                        Array.Copy(bytes, 0, buf, i * 14 + 5, 4);

                        var data = curSrez.CnlData[i];
                        bytes = BitConverter.GetBytes(data.Val);
                        Array.Copy(bytes, 0, buf, i * 14 + 9, 8);

                        bytes = BitConverter.GetBytes((ushort) data.Stat);
                        Array.Copy(bytes, 0, buf, i * 14 + 17, 2);
                    }

                    netStream.Write(buf, 0, cmdLen);

                    // receiving result
                    buf = new byte[4];
                    int bytesRead = netStream.Read(buf, 0, 4);

                    // data processing
                    if (bytesRead == buf.Length && CheckDataFormat(buf, 0x03)) {
                        result = buf[3] > 0;
                        commState = result ? CommStates.Authorized : CommStates.NotReady;
                        complete = true;
                    } else {
                        errMsg = "Incorrect SCADA-Server response to sending current data command";
                        WriteAction(errMsg, Log.ActTypes.Exception);
                        commState = CommStates.Error;
                    }
                }
            } catch (Exception ex) {
                errMsg = ("Error sending current data to SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
                Disconnect();
            } finally {
                RestoreReceiveTimeout();
                Monitor.Exit(tcpLock);
            }

            return complete;
        }

        /// <summary>
        /// Send archival cut SCADA-Server
        /// </summary>
        public bool SendArchive(SrezTableLight.Srez arcSrez, out bool result) {
            Monitor.Enter(tcpLock);
            var complete = false;
            result = false;
            errMsg = "";

            try {
                if (RestoreConnection()) {
                    commState = CommStates.WaitResponse;
                    tcpClient.ReceiveTimeout = commSettings.ServerTimeout;

                    // send command to write archive slice
                    int cnlCnt = arcSrez.CnlNums.Length;
                    int cmdLen = cnlCnt * 14 + 13;

                    var buf = new byte[cmdLen];
                    buf[0] = (byte) (cmdLen % 256);
                    buf[1] = (byte) (cmdLen / 256);
                    buf[2] = 0x04;

                    double arcDT = ScadaUtils.EncodeDateTime(arcSrez.DateTime);
                    byte[] bytes = BitConverter.GetBytes(arcDT);
                    Array.Copy(bytes, 0, buf, 3, 8);

                    buf[11] = (byte) (cnlCnt % 256);
                    buf[12] = (byte) (cnlCnt / 256);

                    for (var i = 0; i < cnlCnt; i++) {
                        bytes = BitConverter.GetBytes((uint) arcSrez.CnlNums[i]);
                        Array.Copy(bytes, 0, buf, i * 14 + 13, 4);

                        var data = arcSrez.CnlData[i];
                        bytes = BitConverter.GetBytes(data.Val);
                        Array.Copy(bytes, 0, buf, i * 14 + 17, 8);

                        bytes = BitConverter.GetBytes((ushort) data.Stat);
                        Array.Copy(bytes, 0, buf, i * 14 + 25, 2);
                    }

                    netStream.Write(buf, 0, cmdLen);

                    // receiving result
                    buf = new byte[4];
                    int bytesRead = netStream.Read(buf, 0, 4);

                    // data processing
                    if (bytesRead == buf.Length && CheckDataFormat(buf, 0x04)) {
                        result = buf[3] > 0;
                        commState = result ? CommStates.Authorized : CommStates.NotReady;
                        complete = true;
                    } else {
                        errMsg = "Incorrect SCADA-Server response to sending archive data command";
                        WriteAction(errMsg, Log.ActTypes.Exception);
                        commState = CommStates.Error;
                    }
                }
            } catch (Exception ex) {
                errMsg = ("Error sending archive data to SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
                Disconnect();
            } finally {
                RestoreReceiveTimeout();
                Monitor.Exit(tcpLock);
            }

            return complete;
        }

        /// <summary>
        /// Send event SCADA-Server
        /// </summary>
        public bool SendEvent(EventTableLight.Event aEvent, out bool result) {
            Monitor.Enter(tcpLock);
            var complete = false;
            result = false;
            errMsg = "";

            try {
                if (RestoreConnection()) {
                    commState = CommStates.WaitResponse;
                    tcpClient.ReceiveTimeout = commSettings.ServerTimeout;

                    // send event recording command
                    var descrLen = (byte) aEvent.Descr.Length;
                    var dataLen = (byte) aEvent.Data.Length;
                    int cmdLen = 46 + descrLen + dataLen;
                    var buf = new byte[cmdLen];
                    buf[0] = (byte) (cmdLen % 256);
                    buf[1] = (byte) (cmdLen / 256);
                    buf[2] = 0x05;

                    double evDT = ScadaUtils.EncodeDateTime(aEvent.DateTime);
                    byte[] bytes = BitConverter.GetBytes(evDT);
                    Array.Copy(bytes, 0, buf, 3, 8);

                    buf[11] = (byte) (aEvent.ObjNum % 256);
                    buf[12] = (byte) (aEvent.ObjNum / 256);
                    buf[13] = (byte) (aEvent.KPNum % 256);
                    buf[14] = (byte) (aEvent.KPNum / 256);
                    buf[15] = (byte) (aEvent.ParamID % 256);
                    buf[16] = (byte) (aEvent.ParamID / 256);

                    bytes = BitConverter.GetBytes(aEvent.CnlNum);
                    Array.Copy(bytes, 0, buf, 17, 4);
                    bytes = BitConverter.GetBytes(aEvent.OldCnlVal);
                    Array.Copy(bytes, 0, buf, 21, 8);
                    bytes = BitConverter.GetBytes(aEvent.OldCnlStat);
                    Array.Copy(bytes, 0, buf, 29, 2);
                    bytes = BitConverter.GetBytes(aEvent.NewCnlVal);
                    Array.Copy(bytes, 0, buf, 31, 8);
                    bytes = BitConverter.GetBytes(aEvent.NewCnlStat);
                    Array.Copy(bytes, 0, buf, 39, 2);

                    buf[41] = aEvent.Checked ? (byte) 0x01 : (byte) 0x00;
                    buf[42] = (byte) (aEvent.UserID % 256);
                    buf[43] = (byte) (aEvent.UserID / 256);

                    buf[44] = descrLen;
                    Array.Copy(Encoding.Default.GetBytes(aEvent.Descr), 0, buf, 45, descrLen);
                    buf[45 + descrLen] = dataLen;
                    Array.Copy(Encoding.Default.GetBytes(aEvent.Data), 0, buf, 46 + descrLen, dataLen);

                    netStream.Write(buf, 0, cmdLen);

                    // receiving result
                    buf = new byte[4];
                    int bytesRead = netStream.Read(buf, 0, 4);

                    // data processing
                    if (bytesRead == buf.Length && CheckDataFormat(buf, 0x05)) {
                        result = buf[3] > 0;
                        commState = result ? CommStates.Authorized : CommStates.NotReady;
                        complete = true;
                    } else {
                        errMsg = "Incorrect SCADA-Server response to sending event command";
                        WriteAction(errMsg, Log.ActTypes.Error);
                        commState = CommStates.Error;
                    }
                }
            } catch (Exception ex) {
                errMsg = ("Error sending event to SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
                Disconnect();
            } finally {
                RestoreReceiveTimeout();
                Monitor.Exit(tcpLock);
            }

            return complete;
        }

        /// <summary>
        /// Send an event acknowledgment command to the SCADA-Server
        /// </summary>
        public bool CheckEvent(int userID, DateTime date, int evNum, out bool result) {
            Monitor.Enter(tcpLock);
            var complete = false;
            result = false;
            errMsg = "";

            try {
                if (RestoreConnection()) {
                    WriteAction("Send check event command to SCADA-Server", Log.ActTypes.Action);

                    commState = CommStates.WaitResponse;
                    tcpClient.ReceiveTimeout = commSettings.ServerTimeout;

                    // sending team
                    var buf = new byte[10];
                    buf[0] = 0x0A;
                    buf[1] = 0x00;
                    buf[2] = 0x0E;
                    buf[3] = (byte) (userID % 256);
                    buf[4] = (byte) (userID / 256);
                    buf[5] = (byte) (date.Year % 100);
                    buf[6] = (byte) date.Month;
                    buf[7] = (byte) date.Day;
                    buf[8] = (byte) (evNum % 256);
                    buf[9] = (byte) (evNum / 256);
                    netStream.Write(buf, 0, 10);

                    // receiving result
                    buf = new byte[4];
                    int bytesRead = netStream.Read(buf, 0, 4);

                    // data processing
                    if (bytesRead == buf.Length && CheckDataFormat(buf, 0x0E)) {
                        result = buf[3] > 0;
                        commState = result ? CommStates.Authorized : CommStates.NotReady;
                        complete = true;
                    } else {
                        errMsg = "Incorrect SCADA-Server response to check event command";
                        WriteAction(errMsg, Log.ActTypes.Error);
                        commState = CommStates.Error;
                    }
                }
            } catch (Exception ex) {
                errMsg = ("Error sending check event command to SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
                Disconnect();
            } finally {
                RestoreReceiveTimeout();
                Monitor.Exit(tcpLock);
            }

            return complete;
        }

        /// <summary>
        /// Shut down the SCADA Server and free up resources
        /// </summary>
        public void Close() {
            Disconnect();
        }
    }
}