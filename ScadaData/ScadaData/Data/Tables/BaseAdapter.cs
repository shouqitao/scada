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
 * Module   : ScadaData
 * Summary  : Adapter for reading and writing configuration database tables
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2010
 * Modified : 2018
 * 
 * --------------------------------
 * Table file structure (version 3)
 * --------------------------------
 * 
 * Header (3 bytes):
 * FieldCnt  - field count         (Byte)
 * Reserve1  - reserve             (UInt16)
 * 
 * Field definition (110 bytes):
 * DataType  - data type           (Byte)
 * DataSize  - data size           (UInt16)
 * MaxStrLen - max. string length  (UInt16)
 * AllowNull - NULL allowed        (Byte)
 * NameLen   - name length <= 100  (UInt16)
 * Name      - name, ASCII string  (Byte[100])
 * Reserve2  - reserve             (UInt16)
 * 
 * Table row:
 * Reserve3     - reserve          (UInt16)
 * Data1..DataN - row data
 * If a field allows NULL, a flag (1 byte) is written before the field data.
 * If a field value equals NULL, the appropriate data block is filled by the default.
 * 
 * Data types and data representation:
 * type 0: integer                 (Int32)
 * type 1: float                   (Double)
 * type 2: boolean                 (Byte)
 *   0 - false, otherwise true
 * type 3: date and time           (Double)
 *   Delphi data format is used
 * type 4: UTF-8 string with maximum data size UInt16.MaxValue - 2
 *   String data size              (UInt16)
 *   String data                   (Byte[])
 */

using System;
using System.IO;
using System.Data;
using System.Text;
using System.ComponentModel;

namespace Scada.Data.Tables {
    /// <inheritdoc />
    /// <summary>
    /// Adapter for reading and writing configuration database tables
    /// <para>Adapter to read and write configuration database tables</para>
    /// </summary>
    public class BaseAdapter : Adapter {
        /// <summary>
        /// Table field definition
        /// </summary>
        protected struct FieldDef {
            /// <summary>
            /// Get or set the field name
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Get or set data type
            /// </summary>
            public int DataType { get; set; }

            /// <summary>
            /// Get or set data size
            /// </summary>
            public int DataSize { get; set; }

            /// <summary>
            /// Get or set the maximum string length for string type
            /// </summary>
            public int MaxStrLen { get; set; }

            /// <summary>
            /// Get or set NULL validity flag
            /// </summary>
            public bool AllowNull { get; set; }
        }

        /// <summary>
        /// Types of table field data
        /// </summary>
        protected struct DataTypes {
            /// <summary>
            /// Whole
            /// </summary>
            public const int Integer = 0;

            /// <summary>
            /// Real
            /// </summary>
            public const int Double = 1;

            /// <summary>
            /// Logical
            /// </summary>
            public const int Boolean = 2;

            /// <summary>
            /// date and time
            /// </summary>
            public const int DateTime = 3;

            /// <summary>
            /// String
            /// </summary>
            public const int String = 4;
        }

        /// <summary>
        /// Field definition length
        /// </summary>
        protected const int FieldDefSize = 110;

        /// <summary>
        /// Maximum allowed length (number of characters) of the field name
        /// </summary>
        protected const int MaxFieldNameLen = 100;

        /// <summary>
        /// Maximum allowed data length for storing field name
        /// </summary>
        protected const int MaxFieldNameDataSize = MaxFieldNameLen + 2 /*length record*/;

        /// <summary>
        /// Maximum allowed data length for storing field string value
        /// </summary>
        protected const int MaxStringDataSize = ushort.MaxValue - 2 /*length record*/;

        /// <summary>
        /// Maximum allowed length (number of characters) of string field value
        /// </summary>
        protected static readonly int MaxStringLen = Encoding.UTF8.GetMaxCharCount(MaxStringDataSize);


        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public BaseAdapter()
            : base() { }


        /// <summary>
        /// Convert an array of bytes to a value whose type is given by dataType
        /// </summary>
        protected static object BytesToObj(byte[] bytes, int index, int dataType) {
            switch (dataType) {
                case DataTypes.Integer:
                    return BitConverter.ToInt32(bytes, index);
                case DataTypes.Double:
                    return BitConverter.ToDouble(bytes, index);
                case DataTypes.Boolean:
                    return bytes[index] > 0;
                case DataTypes.DateTime:
                    return ScadaUtils.DecodeDateTime(BitConverter.ToDouble(bytes, index));
                case DataTypes.String:
                    int strDataSize = BitConverter.ToUInt16(bytes, index);
                    index += 2;
                    if (index + strDataSize > bytes.Length)
                        strDataSize = bytes.Length - index;
                    // for ASCII-encoded data, the Encoding.UTF8.GetString () method will also work correctly
                    return strDataSize > 0 ? Encoding.UTF8.GetString(bytes, index, strDataSize) : "";
                default:
                    return DBNull.Value;
            }
        }

        /// <summary>
        /// Convert and write to a byte array a string in the given encoding
        /// </summary>
        protected static void ConvertStr(string s, int maxLen, int maxDataSize,
            byte[] buffer, int index, Encoding encoding) {
            byte[] strData;
            int strDataSize;

            if (string.IsNullOrEmpty(s)) {
                strData = null;
                strDataSize = 0;
            } else {
                if (s.Length > maxLen)
                    s = s.Substring(0, maxLen);

                strData = encoding.GetBytes(s);
                strDataSize = strData.Length;
            }

            Array.Copy(BitConverter.GetBytes((ushort) strDataSize), 0, buffer, index, 2);
            if (strData != null)
                Array.Copy(strData, 0, buffer, index + 2, strDataSize);
            int totalStrDataSize = strDataSize + 2; // the size of the data line, taking into account the length record
            Array.Clear(buffer, index + totalStrDataSize, maxDataSize - totalStrDataSize);
        }

        /// <summary>
        /// Read field definitions
        /// </summary>
        protected FieldDef[] ReadFieldDefs(Stream stream, BinaryReader reader, out int recSize) {
            // header read
            byte fieldCnt = reader.ReadByte(); // number of fields
            stream.Seek(2, SeekOrigin.Current);

            var fieldDefs = new FieldDef[fieldCnt];
            recSize = 2; // line size in file

            if (fieldCnt > 0) {
                // reading field definitions
                var fieldDefBuf = new byte[FieldDefSize];

                for (var i = 0; i < fieldCnt; i++) {
                    // loading field definition data to buffer for increased speed
                    int readSize = reader.Read(fieldDefBuf, 0, FieldDefSize);

                    // filling the field definition from the buffer
                    if (readSize == FieldDefSize) {
                        var fieldDef = new FieldDef {
                            DataType = fieldDefBuf[0],
                            DataSize = BitConverter.ToUInt16(fieldDefBuf, 1),
                            MaxStrLen = BitConverter.ToUInt16(fieldDefBuf, 3),
                            AllowNull = fieldDefBuf[5] > 0,
                            Name = (string) BytesToObj(fieldDefBuf, 6, DataTypes.String)
                        };

                        if (string.IsNullOrEmpty(fieldDef.Name))
                            throw new ScadaException("Field name must not be empty.");

                        fieldDefs[i] = fieldDef;
                        recSize += fieldDef.DataSize;

                        if (fieldDef.AllowNull)
                            recSize++;
                    }
                }
            }

            return fieldDefs;
        }

        /// <summary>
        /// Create field definition
        /// </summary>
        protected FieldDef CreateFieldDef(string name, Type type, int maxLength, bool allowNull, ref int recSize) {
            var fieldDef = new FieldDef();

            if (type == typeof(int)) {
                fieldDef.DataType = DataTypes.Integer;
                fieldDef.DataSize = sizeof(int);
                fieldDef.MaxStrLen = 0;
            } else if (type == typeof(double)) {
                fieldDef.DataType = DataTypes.Double;
                fieldDef.DataSize = sizeof(double);
                fieldDef.MaxStrLen = 0;
            } else if (type == typeof(bool)) {
                fieldDef.DataType = DataTypes.Boolean;
                fieldDef.DataSize = 1;
                fieldDef.MaxStrLen = 0;
            } else if (type == typeof(DateTime)) {
                fieldDef.DataType = DataTypes.DateTime;
                fieldDef.DataSize = sizeof(double);
                fieldDef.MaxStrLen = 0;
            } else // String
            {
                fieldDef.DataType = DataTypes.String;
                int maxLen = Math.Min(maxLength, MaxStringLen);
                fieldDef.DataSize = 2 /*length record*/ + Encoding.UTF8.GetMaxByteCount(maxLen);
                fieldDef.MaxStrLen = maxLen;
            }

            fieldDef.Name = name;
            fieldDef.AllowNull = allowNull;

            recSize += fieldDef.DataSize;
            if (fieldDef.AllowNull)
                recSize++;

            return fieldDef;
        }

        /// <summary>
        /// Write field definition
        /// </summary>
        protected void WriteFieldDef(FieldDef fieldDef, BinaryWriter writer) {
            var fieldDefBuf = new byte[FieldDefSize];
            fieldDefBuf[0] = (byte) fieldDef.DataType;
            Array.Copy(BitConverter.GetBytes((ushort) fieldDef.DataSize), 0, fieldDefBuf, 1, 2);
            Array.Copy(BitConverter.GetBytes((ushort) fieldDef.MaxStrLen), 0, fieldDefBuf, 3, 2);
            fieldDefBuf[5] = fieldDef.AllowNull ? (byte) 1 : (byte) 0;
            ConvertStr(fieldDef.Name, MaxFieldNameLen, MaxFieldNameDataSize, fieldDefBuf, 6, Encoding.ASCII);
            fieldDefBuf[FieldDefSize - 2] = fieldDefBuf[FieldDefSize - 1] = 0; // reserve

            writer.Write(fieldDefBuf);
        }

        /// <summary>
        /// Write value to table row buffer
        /// </summary>
        protected void WriteValueToRowBuffer(FieldDef fieldDef, object val, byte[] rowBuf, ref int bufInd) {
            bool isNull = val == null || val == DBNull.Value;

            if (fieldDef.AllowNull)
                rowBuf[bufInd++] = isNull ? (byte) 1 : (byte) 0;

            switch (fieldDef.DataType) {
                case DataTypes.Integer:
                    int intVal = isNull ? 0 : (int) val;
                    Array.Copy(BitConverter.GetBytes(intVal), 0, rowBuf, bufInd, fieldDef.DataSize);
                    break;
                case DataTypes.Double:
                    double dblVal = isNull ? 0.0 : (double) val;
                    Array.Copy(BitConverter.GetBytes(dblVal), 0, rowBuf, bufInd, fieldDef.DataSize);
                    break;
                case DataTypes.Boolean:
                    rowBuf[bufInd] = (byte) (isNull ? 0 : (bool) val ? 1 : 0);
                    break;
                case DataTypes.DateTime:
                    double dtVal = isNull ? 0.0 : ScadaUtils.EncodeDateTime((DateTime) val);
                    Array.Copy(BitConverter.GetBytes(dtVal), 0, rowBuf, bufInd, fieldDef.DataSize);
                    break;
                default:
                    string strVal = isNull ? "" : val.ToString();
                    ConvertStr(strVal, fieldDef.MaxStrLen, fieldDef.DataSize,
                        rowBuf, bufInd, Encoding.UTF8);
                    break;
            }

            bufInd += fieldDef.DataSize;
        }


        /// <summary>
        /// Populate the dataTable table from a file or stream
        /// </summary>
        public void Fill(DataTable dataTable, bool allowNulls) {
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable));

            Stream stream = null;
            BinaryReader reader = null;
            dataTable.Rows.Clear();

            try {
                stream = ioStream ?? new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                reader = new BinaryReader(stream);
                FieldDef[] fieldDefs = ReadFieldDefs(stream, reader, out int recSize);

                if (fieldDefs.Length > 0) {
                    // forming the table structure
                    dataTable.BeginLoadData();
                    dataTable.DefaultView.Sort = "";

                    if (dataTable.Columns.Count == 0) {
                        foreach (var fieldDef in fieldDefs) {
                            var column = new DataColumn(fieldDef.Name) {AllowDBNull = fieldDef.AllowNull};

                            switch (fieldDef.DataType) {
                                case DataTypes.Integer:
                                    column.DataType = typeof(int);
                                    break;
                                case DataTypes.Double:
                                    column.DataType = typeof(double);
                                    break;
                                case DataTypes.Boolean:
                                    column.DataType = typeof(bool);
                                    break;
                                case DataTypes.DateTime:
                                    column.DataType = typeof(DateTime);
                                    break;
                                default:
                                    column.DataType = typeof(string);
                                    column.MaxLength = fieldDef.MaxStrLen;
                                    break;
                            }

                            dataTable.Columns.Add(column);
                        }

                        dataTable.DefaultView.AllowNew = false;
                        dataTable.DefaultView.AllowEdit = false;
                        dataTable.DefaultView.AllowDelete = false;
                    }

                    // read lines
                    var rowBuf = new byte[recSize];
                    while (stream.Position < stream.Length) {
                        // load table row data to buffer to increase speed
                        int readSize = reader.Read(rowBuf, 0, recSize);

                        // filling the row of the table from the buffer
                        if (readSize == recSize) {
                            var row = dataTable.NewRow();
                            var bufInd = 2;
                            foreach (var fieldDef in fieldDefs) {
                                bool isNull = fieldDef.AllowNull && rowBuf[bufInd++] > 0;
                                int colInd = dataTable.Columns.IndexOf(fieldDef.Name);

                                if (colInd >= 0) {
                                    row[colInd] = allowNulls && isNull
                                        ? DBNull.Value
                                        : BytesToObj(rowBuf, bufInd, fieldDef.DataType);
                                }

                                bufInd += fieldDef.DataSize;
                            }

                            dataTable.Rows.Add(row);
                        }
                    }
                }
            } catch (EndOfStreamException) {
                // normal file end situation
            } finally {
                if (fileMode) {
                    reader?.Close();
                    stream?.Close();
                }

                dataTable.EndLoadData();
                dataTable.AcceptChanges();

                if (dataTable.Columns.Count > 0)
                    dataTable.DefaultView.Sort = dataTable.Columns[0].ColumnName;
            }
        }

        /// <summary>
        /// Fill the baseTable table from a file or stream
        /// </summary>
        public void Fill<T>(BaseTable<T> baseTable, bool allowNulls) {
            if (baseTable == null)
                throw new ArgumentNullException(nameof(baseTable));

            Stream stream = null;
            BinaryReader reader = null;

            baseTable.Items.Clear();
            var props = TypeDescriptor.GetProperties(typeof(T));

            try {
                stream = ioStream ?? new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                reader = new BinaryReader(stream);
                FieldDef[] fieldDefs = ReadFieldDefs(stream, reader, out int recSize);

                if (fieldDefs.Length > 0) {
                    // read lines
                    var rowBuf = new byte[recSize];

                    while (stream.Position < stream.Length) {
                        // load table row data to buffer to increase speed
                        int readSize = reader.Read(rowBuf, 0, recSize);

                        // filling the row of the table from the buffer
                        if (readSize == recSize) {
                            var item = Activator.CreateInstance<T>();
                            var bufInd = 2;

                            foreach (var fieldDef in fieldDefs) {
                                bool isNull = fieldDef.AllowNull && rowBuf[bufInd++] > 0;
                                var prop = props[fieldDef.Name];

                                if (prop != null) {
                                    var val = allowNulls && isNull
                                        ? null
                                        : BytesToObj(rowBuf, bufInd, fieldDef.DataType);
                                    prop.SetValue(item, val);
                                }

                                bufInd += fieldDef.DataSize;
                            }

                            baseTable.AddItem(item);
                        }
                    }
                }
            } catch (EndOfStreamException) {
                // normal file end situation
            } finally {
                if (fileMode) {
                    reader?.Close();
                    stream?.Close();
                }
            }
        }

        /// <summary>
        /// Write dataTable table to file or stream
        /// </summary>
        public void Update(DataTable dataTable) {
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable));

            Stream stream = null;
            BinaryWriter writer = null;

            try {
                stream = ioStream ?? new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                writer = new BinaryWriter(stream, Encoding.Default);

                // header record
                var fieldCnt = (byte) Math.Min(dataTable.Columns.Count, byte.MaxValue);
                writer.Write(fieldCnt);
                writer.Write((ushort) 0); // reserve

                if (fieldCnt > 0) {
                    // generating and writing field definitions
                    var fieldDefs = new FieldDef[fieldCnt];
                    var recSize = 2; // line size in file

                    for (var i = 0; i < fieldCnt; i++) {
                        var col = dataTable.Columns[i];
                        var fieldDef = CreateFieldDef(col.ColumnName, col.DataType,
                            col.MaxLength, col.AllowDBNull, ref recSize);
                        fieldDefs[i] = fieldDef;
                        WriteFieldDef(fieldDef, writer);
                    }

                    // line entry
                    var rowBuf = new byte[recSize];
                    rowBuf[0] = rowBuf[1] = 0; // reserve

                    foreach (DataRowView rowView in dataTable.DefaultView) {
                        var bufInd = 2;

                        for (var i = 0; i < fieldCnt; i++) {
                            WriteValueToRowBuffer(fieldDefs[i], rowView[i], rowBuf, ref bufInd);
                        }

                        writer.Write(rowBuf);
                    }
                }
            } finally {
                if (fileMode) {
                    writer?.Close();
                    stream?.Close();
                }
            }
        }

        /// <summary>
        /// Write baseTable table to file or stream
        /// </summary>
        public void Update(IBaseTable baseTable) {
            if (baseTable == null)
                throw new ArgumentNullException(nameof(baseTable));

            Stream stream = null;
            BinaryWriter writer = null;

            try {
                stream = ioStream ?? new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                writer = new BinaryWriter(stream, Encoding.Default);

                // header record
                var props = TypeDescriptor.GetProperties(baseTable.ItemType);
                var fieldCnt = (byte) Math.Min(props.Count, byte.MaxValue);
                writer.Write(fieldCnt);
                writer.Write((ushort) 0);

                if (fieldCnt > 0) {
                    // getting max. string field lengths
                    var maxStrLenArr = new int[fieldCnt];

                    for (var i = 0; i < fieldCnt; i++) {
                        maxStrLenArr[i] = 0;
                        var prop = props[i];

                        if (prop.PropertyType == typeof(string)) {
                            foreach (var item in baseTable.EnumerateItems()) {
                                var val = prop.GetValue(item);
                                maxStrLenArr[i] = Math.Max(maxStrLenArr[i], val == null ? 0 : val.ToString().Length);
                            }
                        }
                    }

                    // generating and writing field definitions
                    var fieldDefs = new FieldDef[fieldCnt];
                    var recSize = 2; // line size in file

                    for (var i = 0; i < fieldCnt; i++) {
                        var prop = props[i];
                        var fieldDef = CreateFieldDef(prop.Name, prop.PropertyType,
                            maxStrLenArr[i], prop.PropertyType.IsNullable(), ref recSize);
                        fieldDefs[i] = fieldDef;
                        WriteFieldDef(fieldDef, writer);
                    }

                    // line entry
                    var rowBuf = new byte[recSize];
                    rowBuf[0] = rowBuf[1] = 0;

                    foreach (var item in baseTable.EnumerateItems()) {
                        var bufInd = 2;

                        for (var i = 0; i < fieldCnt; i++) {
                            WriteValueToRowBuffer(fieldDefs[i], props[i].GetValue(item), rowBuf, ref bufInd);
                        }

                        writer.Write(rowBuf);
                    }
                }
            } finally {
                if (fileMode) {
                    writer?.Close();
                    stream?.Close();
                }
            }
        }

        /// <summary>
        /// Write baseTable table to file or stream
        /// </summary>
        public void Update<T>(BaseTable<T> baseTable) {
            Update((IBaseTable) baseTable);
        }
    }
}