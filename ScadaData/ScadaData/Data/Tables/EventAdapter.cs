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
 * Summary  : Adapter for reading and writing event tables
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2007
 * Modified : 2017
 * 
 * --------------------------------
 * Table file structure (version 3)
 * --------------------------------
 * File consists of a sequence of events, each event has the structure:
 * Time            - event time stamp                        (Double)
 * ObjNum          - object number                           (UInt16)
 * KPNum           - device number                           (UInt16)
 * ParamID         - quantity ID                             (UInt16)
 * CnlNum          - input channel number                    (UInt16)
 * OldCnlVal       - previous channel number                 (Double)
 * OldCnlStat      - previous channel status                 (Byte)
 * NewCnlVal       - new channel value                       (Double)
 * NewCnlStat      - new channel status                      (Byte)
 * Checked         - event is checked                        (Boolean)
 * UserID          - ID of the user checked the event        (UInt16)
 * Descr           - event description                       (Byte + Byte[100])
 * Data            - auxiliary event data                    (Byte + Byte[50])
 *
 * The size of one event equals 189 bytes.
 */

using System;
using System.Data;
using System.IO;
using System.Text;

namespace Scada.Data.Tables {
    /// <inheritdoc />
    /// <summary>
    /// Adapter for reading and writing event tables
    /// <para>Adapter to read and write event tables</para>
    /// </summary>
    public class EventAdapter : Adapter {
        /// <summary>
        /// The size of the event data in the file
        /// </summary>
        public const int EventDataSize = 189;

        /// <summary>
        /// Max. event description length
        /// </summary>
        public const int MaxDescrLen = 100;

        /// <summary>
        /// Max. additional event data length
        /// </summary>
        public const int MaxDataLen = 50;


        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public EventAdapter()
            : base() { }


        /// <summary>
        /// Convert byte array to string, 0th byte - string length
        /// </summary>
        protected string BytesToStr(byte[] bytes, int startIndex) {
            int length = bytes[startIndex];
            startIndex++;
            if (startIndex + length > bytes.Length)
                length = bytes.Length - startIndex;
            return Encoding.Default.GetString(bytes, startIndex, length);
        }

        /// <summary>
        /// Convert an object to an integer
        /// </summary>
        protected int ConvertToInt(object obj) {
            try {
                return Convert.ToInt32(obj);
            } catch {
                return 0;
            }
        }

        /// <summary>
        /// Convert the object to a real number
        /// </summary>
        protected double ConvertToDouble(object obj) {
            try {
                return Convert.ToDouble(obj);
            } catch {
                return 0.0;
            }
        }

        /// <summary>
        /// Convert object to date and time
        /// </summary>
        protected DateTime ConvertToDateTime(object obj) {
            try {
                return Convert.ToDateTime(obj);
            } catch {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Convert object to boolean
        /// </summary>
        protected bool ConvertToBoolean(object obj) {
            try {
                return Convert.ToBoolean(obj);
            } catch {
                return false;
            }
        }

        /// <summary>
        /// Create event data based on table row
        /// </summary>
        protected EventTableLight.Event CreateEvent(DataRowView rowView) {
            var ev = new EventTableLight.Event {
                Number = ConvertToInt(rowView["Number"]),
                DateTime = ConvertToDateTime(rowView["DateTime"]),
                ObjNum = ConvertToInt(rowView["ObjNum"]),
                KPNum = ConvertToInt(rowView["KPNum"]),
                ParamID = ConvertToInt(rowView["ParamID"]),
                CnlNum = ConvertToInt(rowView["CnlNum"]),
                OldCnlVal = ConvertToDouble(rowView["OldCnlVal"]),
                OldCnlStat = ConvertToInt(rowView["OldCnlStat"]),
                NewCnlVal = ConvertToDouble(rowView["NewCnlVal"]),
                NewCnlStat = ConvertToInt(rowView["NewCnlStat"]),
                Checked = ConvertToBoolean(rowView["Checked"]),
                UserID = ConvertToInt(rowView["UserID"]),
                Descr = Convert.ToString(rowView["Descr"]),
                Data = Convert.ToString(rowView["Data"])
            };
            return ev;
        }

        /// <summary>
        /// Create buffer for event recording
        /// </summary>
        protected byte[] CreateEventBuffer(EventTableLight.Event ev) {
            var evBuf = new byte[EventDataSize];
            Array.Copy(BitConverter.GetBytes(ScadaUtils.EncodeDateTime(ev.DateTime)), 0, evBuf, 0, 8);
            evBuf[8] = (byte) (ev.ObjNum % 256);
            evBuf[9] = (byte) (ev.ObjNum / 256);
            evBuf[10] = (byte) (ev.KPNum % 256);
            evBuf[11] = (byte) (ev.KPNum / 256);
            evBuf[12] = (byte) (ev.ParamID % 256);
            evBuf[13] = (byte) (ev.ParamID / 256);
            evBuf[14] = (byte) (ev.CnlNum % 256);
            evBuf[15] = (byte) (ev.CnlNum / 256);
            Array.Copy(BitConverter.GetBytes(ev.OldCnlVal), 0, evBuf, 16, 8);
            evBuf[24] = (byte) ev.OldCnlStat;
            Array.Copy(BitConverter.GetBytes(ev.NewCnlVal), 0, evBuf, 25, 8);
            evBuf[33] = (byte) ev.NewCnlStat;
            evBuf[34] = ev.Checked ? (byte) 1 : (byte) 0;
            evBuf[35] = (byte) (ev.UserID % 256);
            evBuf[36] = (byte) (ev.UserID / 256);
            string descr = ev.Descr ?? "";
            if (descr.Length > MaxDescrLen)
                descr = descr.Substring(0, MaxDescrLen);
            evBuf[37] = (byte) descr.Length;
            Array.Copy(Encoding.Default.GetBytes(descr), 0, evBuf, 38, descr.Length);
            string data = ev.Data ?? "";
            if (data.Length > MaxDataLen)
                data = data.Substring(0, MaxDataLen);
            evBuf[138] = (byte) data.Length;
            Array.Copy(Encoding.Default.GetBytes(data), 0, evBuf, 139, data.Length);
            return evBuf;
        }

        /// <summary>
        /// Populate the dest object from the FileName event file.
        /// </summary>
        protected void FillObj(object dest) {
            Stream stream = null;
            BinaryReader reader = null;
            var fillTime = DateTime.Now;

            EventTableLight eventTableLight = null;
            DataTable dataTable = null;

            try {
                if (dest is EventTableLight)
                    eventTableLight = dest as EventTableLight;
                else if (dest is DataTable)
                    dataTable = dest as DataTable;
                else
                    throw new ScadaException("Destination object is invalid.");

                // determining the date of events in the table
                var date = ExtractDate(tableName);

                // storage facility preparation
                if (eventTableLight != null) {
                    eventTableLight.Clear();
                    eventTableLight.TableName = tableName;
                } else { // dataTable != null
                    // forming the table structure
                    dataTable.BeginLoadData();
                    dataTable.DefaultView.Sort = "";

                    if (dataTable.Columns.Count == 0) {
                        dataTable.Columns.Add("Number", typeof(int));
                        dataTable.Columns.Add("DateTime", typeof(DateTime)).DefaultValue = date;
                        dataTable.Columns.Add("ObjNum", typeof(int)).DefaultValue = 0;
                        dataTable.Columns.Add("KPNum", typeof(int)).DefaultValue = 0;
                        dataTable.Columns.Add("ParamID", typeof(int)).DefaultValue = 0;
                        dataTable.Columns.Add("CnlNum", typeof(int)).DefaultValue = 0;
                        dataTable.Columns.Add("OldCnlVal", typeof(double)).DefaultValue = 0.0;
                        dataTable.Columns.Add("OldCnlStat", typeof(int)).DefaultValue = 0;
                        dataTable.Columns.Add("NewCnlVal", typeof(double)).DefaultValue = 0.0;
                        dataTable.Columns.Add("NewCnlStat", typeof(int)).DefaultValue = 0;
                        dataTable.Columns.Add("Checked", typeof(bool)).DefaultValue = false;
                        dataTable.Columns.Add("UserID", typeof(int)).DefaultValue = 0;
                        dataTable.Columns.Add("Descr", typeof(string));
                        dataTable.Columns.Add("Data", typeof(string));
                        dataTable.DefaultView.AllowNew = false;
                        dataTable.DefaultView.AllowEdit = false;
                        dataTable.DefaultView.AllowDelete = false;
                    } else {
                        var colDateTime = dataTable.Columns["DateTime"];
                        if (colDateTime != null)
                            colDateTime.DefaultValue = date;
                        dataTable.Rows.Clear();
                    }
                }

                // filling a table from a file
                stream = ioStream ?? new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                reader = new BinaryReader(stream);

                var eventBuf = new byte[EventDataSize]; // event data buffer
                var evNum = 1; // event sequence number

                while (stream.Position < stream.Length) {
                    int readSize = reader.Read(eventBuf, 0, EventDataSize);
                    if (readSize == EventDataSize) {
                        // create event based on read data
                        var ev = new EventTableLight.Event {Number = evNum};
                        evNum++;

                        double time = BitConverter.ToDouble(eventBuf, 0);
                        ev.DateTime = ScadaUtils.CombineDateTime(date, time);

                        ev.ObjNum = BitConverter.ToUInt16(eventBuf, 8);
                        ev.KPNum = BitConverter.ToUInt16(eventBuf, 10);
                        ev.ParamID = BitConverter.ToUInt16(eventBuf, 12);
                        ev.CnlNum = BitConverter.ToUInt16(eventBuf, 14);
                        ev.OldCnlVal = BitConverter.ToDouble(eventBuf, 16);
                        ev.OldCnlStat = eventBuf[24];
                        ev.NewCnlVal = BitConverter.ToDouble(eventBuf, 25);
                        ev.NewCnlStat = eventBuf[33];
                        ev.Checked = eventBuf[34] > 0;
                        ev.UserID = BitConverter.ToUInt16(eventBuf, 35);
                        ev.Descr = BytesToStr(eventBuf, 37);
                        ev.Data = BytesToStr(eventBuf, 138);

                        // create row of filled table
                        if (eventTableLight != null) {
                            eventTableLight.AllEvents.Add(ev); // faster than eventTableLight.AddEvent (ev)
                        } else { // dataTable != null
                            var row = dataTable.NewRow();
                            row["Number"] = ev.Number;
                            row["DateTime"] = ev.DateTime;
                            row["ObjNum"] = ev.ObjNum;
                            row["KPNum"] = ev.KPNum;
                            row["ParamID"] = ev.ParamID;
                            row["CnlNum"] = ev.CnlNum;
                            row["OldCnlVal"] = ev.OldCnlVal;
                            row["OldCnlStat"] = ev.OldCnlStat;
                            row["NewCnlVal"] = ev.NewCnlVal;
                            row["NewCnlStat"] = ev.NewCnlStat;
                            row["Checked"] = ev.Checked;
                            row["UserID"] = ev.UserID;
                            row["Descr"] = ev.Descr;
                            row["Data"] = ev.Data;
                            dataTable.Rows.Add(row);
                        }
                    }
                }
            } catch (EndOfStreamException) {
                // normal file end situation
            } catch {
                fillTime = DateTime.MinValue;
                throw;
            } finally {
                if (fileMode) {
                    reader?.Close();
                    stream?.Close();
                }

                if (eventTableLight != null) {
                    eventTableLight.LastFillTime = fillTime;
                }

                if (dataTable != null) {
                    dataTable.EndLoadData();
                    dataTable.AcceptChanges();
                    dataTable.DefaultView.Sort = "Number";
                }
            }
        }


        /// <summary>
        /// Populate the dataTable table from a file or stream
        /// </summary>
        public void Fill(DataTable dataTable) {
            FillObj(dataTable);
        }

        /// <summary>
        /// Populate the eventTableLight table from a file or stream
        /// </summary>
        public void Fill(EventTableLight eventTableLight) {
            FillObj(eventTableLight);
        }

        /// <summary>
        /// Write changes to the dataTable table to a file or stream.
        /// </summary>
        public void Update(DataTable dataTable) {
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable));

            Stream stream = null;
            BinaryWriter writer = null;

            try {
                stream = ioStream ?? new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                writer = new BinaryWriter(stream);

                // recording changed events
                var dataView = new DataView(dataTable, "", "", DataViewRowState.ModifiedCurrent);

                foreach (DataRowView rowView in dataView) {
                    var ev = CreateEvent(rowView);

                    if (ev.Number > 0) {
                        stream.Seek((ev.Number - 1) * EventDataSize, SeekOrigin.Begin);
                        writer.Write(CreateEventBuffer(ev));
                    }
                }

                // recording of added events
                dataView = new DataView(dataTable, "", "", DataViewRowState.Added);

                if (dataView.Count > 0) {
                    // setting the recording position to a multiple of the size of the event data
                    stream.Seek(0, SeekOrigin.End);
                    var evInd = (int) (stream.Position / EventDataSize);
                    int evNum = evInd + 1;
                    stream.Seek(evInd * EventDataSize, SeekOrigin.Begin);

                    // event recording and setting event numbers
                    foreach (DataRowView rowView in dataView) {
                        var ev = CreateEvent(rowView);
                        writer.Write(CreateEventBuffer(ev));
                        rowView["Number"] = evNum++;
                    }
                }

                // confirmation of successful saving of changes
                dataTable.AcceptChanges();
            } finally {
                if (fileMode) {
                    writer?.Close();
                    stream?.Close();
                }
            }
        }

        /// <summary>
        /// Add event to file or stream
        /// </summary>
        public void AppendEvent(EventTableLight.Event ev) {
            if (ev == null)
                throw new ArgumentNullException(nameof(ev));

            Stream stream = null;
            BinaryWriter writer = null;

            try {
                stream = ioStream ?? new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                writer = new BinaryWriter(stream);

                // setting the recording position to a multiple of the size of the event data
                stream.Seek(0, SeekOrigin.End);
                long evInd = stream.Position / EventDataSize;
                long offset = evInd * EventDataSize;
                stream.Seek(offset, SeekOrigin.Begin);

                // event recording
                writer.Write(CreateEventBuffer(ev));
                ev.Number = (int) evInd + 1;
            } finally {
                if (fileMode) {
                    writer?.Close();
                    stream?.Close();
                }
            }
        }

        /// <summary>
        /// Acknowledge event in file or stream
        /// </summary>
        public void CheckEvent(int evNum, int userID) {
            Stream stream = null;
            BinaryWriter writer = null;

            try {
                stream = ioStream ?? new FileStream(fileName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
                writer = new BinaryWriter(stream);

                stream.Seek(0, SeekOrigin.End);
                long size = stream.Position;
                long offset = (evNum - 1) * EventDataSize + 34;

                if (0 <= offset && offset + 2 < size) {
                    stream.Seek(offset, SeekOrigin.Begin);
                    writer.Write(userID > 0 ? (byte) 1 : (byte) 0);
                    writer.Write((ushort) userID);
                }
            } finally {
                if (fileMode) {
                    writer?.Close();
                    stream?.Close();
                }
            }
        }


        /// <summary>
        /// Build event table name based on date
        /// </summary>
        public static string BuildEvTableName(DateTime date) {
            return (new StringBuilder())
                .Append("e").Append(date.ToString("yyMMdd")).Append(".dat")
                .ToString();
        }
    }
}