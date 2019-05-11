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
 * Summary  : Adapter for reading and writing data tables
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2005
 * Modified : 2017
 * 
 * --------------------------------
 * Table file structure (version 2)
 * --------------------------------
 * File consists of snapshots, each snapshot has the structure:
 * N               - channel number list length          (UInt16)
 * n0..nN-1        - channel number list                 (N * UInt16)
 * CS              - check sum of the previous data      (UInt16)
 * Time            - snapshot time stamp                 (Double)
 * {Value, Status} - snapshot data                       (N * (Double + Byte))
 * 
 * Channel numbers in the list are unique and sorted in ascending order.
 * If the channel number list equals the previous list, it is skipped and N is set to 0.
 * CS = (UInt16)(N + n0 + ... + nN-1 + 1)
 */

using System;
using System.Data;
using System.IO;
using System.Text;

namespace Scada.Data.Tables {
    /// <inheritdoc />
    /// <summary>
    /// Adapter for reading and writing data tables
    /// <para>Adapter to read and write slicing tables</para>
    /// </summary>
    public class SrezAdapter : Adapter {
        /// <summary>
        /// The name of the current slice table
        /// </summary>
        public const string CurTableName = "current.dat";

        /// <summary>
        /// Empty channel list buffer in save format
        /// </summary>
        protected static readonly byte[] EmptyCnlNumsBuf = new byte[] {0x00, 0x00, 0x01, 0x00};


        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public SrezAdapter()
            : base() { }


        /// <summary>
        /// Extract input channel data from binary buffer
        /// </summary>
        protected void ExtractCnlData(byte[] buf, ref int bufInd, out double cnlVal, out byte cnlStat) {
            cnlVal = BitConverter.ToDouble(buf, bufInd);
            cnlStat = buf[bufInd + 8];
            bufInd += 9;
        }

        /// <summary>
        /// Get buffer of description of cut structure in save format
        /// </summary>
        protected byte[] GetSrezDescrBuf(SrezTable.SrezDescr srezDescr) {
            var cnlNumsLen = (ushort) srezDescr.CnlNums.Length;
            var cnlNumsBuf = new byte[cnlNumsLen * 2 + 4];
            cnlNumsBuf[0] = (byte) (cnlNumsLen % 256);
            cnlNumsBuf[1] = (byte) (cnlNumsLen / 256);
            var bufPos = 2;

            for (var i = 0; i < cnlNumsLen; i++) {
                var cnlNum = (ushort) srezDescr.CnlNums[i];
                cnlNumsBuf[bufPos++] = (byte) (cnlNum % 256);
                cnlNumsBuf[bufPos++] = (byte) (cnlNum / 256);
            }

            cnlNumsBuf[bufPos++] = (byte) (srezDescr.CS % 256);
            cnlNumsBuf[bufPos++] = (byte) (srezDescr.CS / 256);

            return cnlNumsBuf;
        }

        /// <summary>
        /// Get slice data buffer in save format
        /// </summary>
        protected byte[] GetCnlDataBuf(SrezTableLight.CnlData[] cnlData) {
            int cnlCnt = cnlData.Length;
            var srezDataBuf = new byte[cnlCnt * 9];

            for (int i = 0,
                k = 0;
                i < cnlCnt;
                i++) {
                var data = cnlData[i];
                BitConverter.GetBytes(data.Val).CopyTo(srezDataBuf, k);
                srezDataBuf[k + 8] = (byte) data.Stat;
                k += 9;
            }

            return srezDataBuf;
        }

        /// <summary>
        /// Populate the dest object from the FileName slice file
        /// </summary>
        protected void FillObj(object dest) {
            Stream stream = null;
            BinaryReader reader = null;
            var fillTime = DateTime.Now;

            var srezTableLight = dest as SrezTableLight;
            var dataTable = dest as DataTable;
            var trend = dest as Trend;

            var srezTable = srezTableLight as SrezTable;
            SrezTableLight.Srez lastStoredSrez = null;

            try {
                if (srezTableLight == null && dataTable == null && trend == null)
                    throw new ScadaException("Destination object is invalid.");

                // storage facility preparation
                if (srezTableLight != null) {
                    srezTableLight.Clear();
                    srezTableLight.TableName = tableName;

                    srezTable?.BeginLoadData();
                } else if (dataTable != null) {
                    // forming the table structure
                    dataTable.BeginLoadData();
                    dataTable.DefaultView.Sort = "";

                    if (dataTable.Columns.Count == 0) {
                        dataTable.Columns.Add("DateTime", typeof(DateTime));
                        dataTable.Columns.Add("CnlNum", typeof(int));
                        dataTable.Columns.Add("Val", typeof(double));
                        dataTable.Columns.Add("Stat", typeof(int));
                        dataTable.DefaultView.AllowNew = false;
                        dataTable.DefaultView.AllowEdit = false;
                        dataTable.DefaultView.AllowDelete = false;
                    } else {
                        dataTable.Rows.Clear();
                    }
                } else // trend != null
                {
                    trend.Clear();
                    trend.TableName = tableName;
                }

                // filling the object with data
                stream = ioStream ?? new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                reader = new BinaryReader(stream);

                var date = ExtractDate(tableName); // slice date determination
                SrezTable.SrezDescr srezDescr = null; // cut description
                int[] cnlNums = null; // link to input channel numbers from slice description
                while (stream.Position < stream.Length) {
                    // reading the list of channels and CS numbers
                    int cnlNumCnt = reader.ReadUInt16();
                    if (cnlNumCnt > 0) {
                        // loading channel numbers to buffer to increase speed
                        int cnlNumSize = cnlNumCnt * 2;
                        var buf = new byte[cnlNumSize];
                        int readSize = reader.Read(buf, 0, cnlNumSize);

                        // creating a description of the slice and filling the numbers of the channels from the buffer 
                        // with checking their uniqueness and orderliness
                        if (readSize == cnlNumSize) {
                            int prevCnlNum = -1;
                            srezDescr = new SrezTable.SrezDescr(cnlNumCnt);
                            cnlNums = srezDescr.CnlNums;
                            for (var i = 0; i < cnlNumCnt; i++) {
                                int cnlNum = BitConverter.ToUInt16(buf, i * 2);
                                if (prevCnlNum >= cnlNum)
                                    throw new ScadaException("Table is incorrect.");
                                cnlNums[i] = prevCnlNum = cnlNum;
                            }

                            srezDescr.CalcCS();
                        }
                    } else if (srezDescr == null) {
                        throw new Exception("Table is incorrect.");
                    }

                    // reading and checking the cops
                    ushort cs = reader.ReadUInt16();
                    bool csOk = cnlNumCnt > 0 ? srezDescr.CS == cs : cs == 1;

                    // read slice data
                    int cnlCnt = cnlNums.Length; // the number of channels in the slice
                    int srezDataSize = cnlCnt * 9; // slice data size
                    if (csOk) {
                        long srezPos = stream.Position;
                        double time = reader.ReadDouble();
                        var srezDT = ScadaUtils.CombineDateTime(date, time);

                        // initialize slice
                        SrezTableLight.Srez srez;
                        if (srezTable != null) {
                            srez = new SrezTable.Srez(srezDT, srezDescr) {
                                State = DataRowState.Unchanged,
                                Position = srezPos
                            };
                        } else if (srezTableLight != null) {
                            srez = new SrezTableLight.Srez(srezDT, cnlCnt);
                            cnlNums.CopyTo(srez.CnlNums, 0);
                        } else { // srezTableLight == null
                            srez = null;
                        }

                        // read input data
                        var bufInd = 0;
                        double val;
                        byte stat;
                        if (trend != null) {
                            // select channel data for trend
                            int index = Array.BinarySearch<int>(cnlNums, trend.CnlNum);
                            if (index >= 0) {
                                stream.Seek(index * 9, SeekOrigin.Current);
                                var buf = new byte[9];
                                int readSize = reader.Read(buf, 0, 9);
                                if (readSize == 9) {
                                    ExtractCnlData(buf, ref bufInd, out val, out stat);
                                    var point = new Trend.Point(srezDT, val, stat);
                                    trend.Points.Add(point);
                                    stream.Seek(srezDataSize - (index + 1) * 9, SeekOrigin.Current);
                                }
                            } else {
                                stream.Seek(srezDataSize, SeekOrigin.Current);
                            }
                        } else {
                            // loading slice data to buffer to increase speed
                            var buf = new byte[srezDataSize];
                            int readSize = reader.Read(buf, 0, srezDataSize);

                            // filling the buffer table
                            if (srezTableLight != null) {
                                for (var i = 0; i < cnlCnt; i++) {
                                    ExtractCnlData(buf, ref bufInd, out val, out stat);

                                    srez.CnlNums[i] = cnlNums[i];
                                    srez.CnlData[i].Val = val;
                                    srez.CnlData[i].Stat = stat;

                                    if (bufInd >= readSize)
                                        break;
                                }

                                srezTableLight.AddSrez(srez);
                                lastStoredSrez = srez;
                            } else { // dataTable != null
                                for (var i = 0; i < cnlCnt; i++) {
                                    ExtractCnlData(buf, ref bufInd, out val, out stat);

                                    var row = dataTable.NewRow();
                                    row["DateTime"] = srezDT;
                                    row["CnlNum"] = cnlNums[i];
                                    row["Val"] = val;
                                    row["Stat"] = stat;
                                    dataTable.Rows.Add(row);

                                    if (bufInd >= readSize)
                                        break;
                                }
                            }
                        }
                    } else {
                        // skip the slice, considering its size as in the case of a repeated list of channel numbers
                        stream.Seek(srezDataSize + 8, SeekOrigin.Current);
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

                if (srezTableLight != null) {
                    srezTableLight.LastFillTime = fillTime;
                    if (srezTable != null) {
                        srezTable.LastStoredSrez = (SrezTable.Srez) lastStoredSrez;
                        srezTable.EndLoadData();
                    }
                }

                if (dataTable != null) {
                    dataTable.EndLoadData();
                    dataTable.AcceptChanges();
                    dataTable.DefaultView.Sort = "DateTime, CnlNum";
                }

                if (trend != null) {
                    trend.LastFillTime = fillTime;
                    trend.Sort();
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
        /// Fill in the srezTableLight table from a file or stream
        /// </summary>
        public void Fill(SrezTableLight srezTableLight) {
            FillObj(srezTableLight);
        }

        /// <summary>
        /// Fill the trend trend of the cnlNum channel from a file or stream
        /// </summary>
        public void Fill(Trend trend) {
            FillObj(trend);
        }

        /// <summary>
        /// Create a single-slice table in a file or stream
        /// </summary>
        /// <remarks>To record the current slice table</remarks>
        public void Create(SrezTable.Srez srez, DateTime srezDT) {
            if (srez == null)
                throw new ArgumentNullException(nameof(srez));

            Stream stream = null;
            BinaryWriter writer = null;

            try {
                stream = ioStream ?? new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write,
                             FileShare.ReadWrite);
                writer = new BinaryWriter(stream);

                writer.Write(GetSrezDescrBuf(srez.SrezDescr));
                writer.Write(ScadaUtils.EncodeDateTime(srezDT));
                writer.Write(GetCnlDataBuf(srez.CnlData));
                stream.SetLength(stream.Position);
            } finally {
                if (fileMode) {
                    writer?.Close();
                    stream?.Close();
                }
            }
        }

        /// <summary>
        /// Write changes to the slicing table to a file or stream.
        /// </summary>
        public void Update(SrezTable srezTable) {
            if (srezTable == null)
                throw new ArgumentNullException(nameof(srezTable));

            Stream stream = null;
            BinaryWriter writer = null;

            try {
                stream = ioStream ?? new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write,
                             FileShare.ReadWrite);
                writer = new BinaryWriter(stream);

                // record modified slices
                foreach (var srez in srezTable.ModifiedSrezList) {
                    stream.Seek(srez.Position + 8, SeekOrigin.Begin);
                    writer.Write(GetCnlDataBuf(srez.CnlData));
                }

                // setting the recording position of the added slices to the stream, 
                // restore slice table if necessary
                var lastSrez = srezTable.LastStoredSrez;

                if (lastSrez == null) {
                    stream.Seek(0, SeekOrigin.Begin);
                } else {
                    stream.Seek(0, SeekOrigin.End);
                    long offset = lastSrez.Position + lastSrez.CnlNums.Length * 9 + 8;

                    if (stream.Position < offset) {
                        var buf = new byte[offset - stream.Position];
                        stream.Write(buf, 0, buf.Length);
                    } else {
                        stream.Seek(offset, SeekOrigin.Begin);
                    }
                }

                // record added slices
                var prevSrezDescr = lastSrez?.SrezDescr;

                foreach (var srez in srezTable.AddedSrezList) {
                    // recording cutoff channel numbers
                    writer.Write(srez.SrezDescr.Equals(prevSrezDescr)
                        ? EmptyCnlNumsBuf
                        : GetSrezDescrBuf(srez.SrezDescr));

                    prevSrezDescr = srez.SrezDescr;

                    // slice data entry
                    srez.Position = stream.Position;
                    writer.Write(ScadaUtils.EncodeDateTime(srez.DateTime));
                    writer.Write(GetCnlDataBuf(srez.CnlData));
                    lastSrez = srez;
                }

                // confirmation of successful saving of changes
                srezTable.AcceptChanges();
                srezTable.LastStoredSrez = lastSrez;
            } finally {
                if (fileMode) {
                    writer?.Close();
                    stream?.Close();
                }
            }
        }


        /// <summary>
        /// Build the name of the table of minute slices based on the date
        /// </summary>
        public static string BuildMinTableName(DateTime date) {
            return (new StringBuilder())
                .Append("m").Append(date.ToString("yyMMdd")).Append(".dat")
                .ToString();
        }

        /// <summary>
        /// Build the name of the table of hourly slices based on the date
        /// </summary>
        public static string BuildHourTableName(DateTime date) {
            return (new StringBuilder())
                .Append("h").Append(date.ToString("yyMMdd")).Append(".dat")
                .ToString();
        }
    }
}