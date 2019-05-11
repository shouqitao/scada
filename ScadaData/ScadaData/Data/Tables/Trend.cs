/*
 * Copyright 2014 Mikhail Shiryaev
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
 * Summary  : Trend for fast reading one input channel data
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2006
 * Modified : 2006
 */

using System;
using System.Collections.Generic;

namespace Scada.Data.Tables {
    /// <summary>
    /// Trend for fast reading one input channel data
    /// <para>Trend for fast read data of one input channel</para>
    /// </summary>
    public class Trend {
        /// <summary>
        /// Trend point
        /// </summary>
        public struct Point : IComparable<Point> {
            /// <summary>
            /// Constructor
            /// </summary>
            public Point(DateTime dateTime, double val, int stat) {
                DateTime = dateTime;
                Val = val;
                Stat = stat;
            }

            /// <summary>
            /// Timestamp
            /// </summary>
            public DateTime DateTime;

            /// <summary>
            /// Value
            /// </summary>
            public double Val;

            /// <summary>
            /// Status
            /// </summary>
            public int Stat;

            /// <inheritdoc />
            /// <summary>
            /// Compare the current object with another object of the same type.
            /// </summary>
            public int CompareTo(Point other) {
                return DateTime.CompareTo(other.DateTime);
            }
        }

        /// <summary>
        /// The name of the table to which the trend belongs
        /// </summary>
        protected string tableName;

        /// <summary>
        /// The time of the last change to the table file to which the trend belongs
        /// </summary>
        protected DateTime fileModTime;

        /// <summary>
        /// The time of the last successful trend filling.
        /// </summary>
        protected DateTime lastFillTime;

        /// <summary>
        /// Trend points
        /// </summary>
        protected List<Point> points;

        /// <summary>
        /// Trend Input Channel Number
        /// </summary>
        protected int cnlNum;


        /// <summary>
        /// Constructor
        /// </summary>
        protected Trend() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cnlNum">trend input channel number</param>
        public Trend(int cnlNum) {
            tableName = "";
            fileModTime = DateTime.MinValue;
            lastFillTime = DateTime.MinValue;

            points = new List<Point>();
            this.cnlNum = cnlNum;
        }


        /// <summary>
        /// Get or set the file name of the table to which the trend belongs
        /// </summary>
        public string TableName {
            get { return tableName; }
            set {
                if (tableName == value) return;
                tableName = value;
                fileModTime = DateTime.MinValue;
                lastFillTime = DateTime.MinValue;
            }
        }

        /// <summary>
        /// Get or set the time of the last change to the file of the table to which the trend belongs
        /// </summary>
        public DateTime FileModTime {
            get { return fileModTime; }
            set { fileModTime = value; }
        }

        /// <summary>
        /// Get or set the time of the last successful trend filling.
        /// </summary>
        public DateTime LastFillTime {
            get { return lastFillTime; }
            set { fileModTime = value; }
        }

        /// <summary>
        /// Trend points
        /// </summary>
        public List<Point> Points {
            get { return points; }
        }

        /// <summary>
        /// Trend Input Channel Number
        /// </summary>
        public int CnlNum {
            get { return cnlNum; }
        }


        /// <summary>
        /// Sort trend points by time.
        /// </summary>
        public void Sort() {
            points.Sort();
        }

        /// <summary>
        /// Clear trend
        /// </summary>
        public void Clear() {
            points.Clear();
        }
    }
}