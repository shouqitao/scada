/*
 * Copyright 2016 Mikhail Shiryaev
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
 * Summary  : The base class for adapter
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2016
 */

using System;
using System.Globalization;
using System.IO;

namespace Scada.Data.Tables {
    /// <summary>
    /// The base class for adapter
    /// <para>Adapter Base Class</para>
    /// </summary>
    public abstract class Adapter {
        /// <summary>
        /// Slice Table Directory
        /// </summary>
        protected string directory;

        /// <summary>
        /// Input and output stream
        /// </summary>
        protected Stream ioStream;

        /// <summary>
        /// Slice table file name
        /// </summary>
        protected string tableName;

        /// <summary>
        /// Full name of the slice table file
        /// </summary>
        protected string fileName;

        /// <summary>
        /// Data access is performed through a file on disk.
        /// </summary>
        protected bool fileMode;


        /// <summary>
        /// Constructor
        /// </summary>
        protected Adapter() {
            directory = "";
            ioStream = null;
            tableName = "";
            fileName = "";
            fileMode = true;
        }


        /// <summary>
        /// Get or set the slice table directory
        /// </summary>
        public string Directory {
            get { return directory; }
            set {
                ioStream = null;
                fileMode = true;
                if (directory != value) {
                    directory = value;
                    fileName = directory + tableName;
                }
            }
        }

        /// <summary>
        /// Get or set the input and output stream (instead of the directory)
        /// </summary>
        public Stream Stream {
            get { return ioStream; }
            set {
                directory = "";
                ioStream = value;
                fileName = tableName;
                fileMode = false;
            }
        }

        /// <summary>
        /// Get or set slice table file name
        /// </summary>
        public string TableName {
            get { return tableName; }
            set {
                if (tableName != value) {
                    tableName = value;
                    fileName = directory + tableName;
                }
            }
        }

        /// <summary>
        /// Get or set the full name of the slice table file
        /// </summary>
        public string FileName {
            get { return fileName; }
            set {
                if (fileName != value) {
                    directory = Path.GetDirectoryName(value);
                    ioStream = null;
                    tableName = Path.GetFileName(value);
                    fileName = value;
                    fileMode = true;
                }
            }
        }


        /// <summary>
        /// Extract date from slice or event table file name (without directory)
        /// </summary>
        protected DateTime ExtractDate(string tableName) {
            try {
                return DateTime.ParseExact(tableName.Substring(1, 6), "yyMMdd", CultureInfo.InvariantCulture);
            } catch {
                return DateTime.MinValue;
            }
        }
    }
}