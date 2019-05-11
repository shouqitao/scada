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
 * Summary  : Snapshot table for read and write data access
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2013
 * Modified : 2017
 */

using System;
using System.Collections.Generic;
using System.Data;

namespace Scada.Data.Tables {
    /// <inheritdoc />
    /// <summary>
    /// Snapshot table for read and write data access
    /// <para>The table of slices for data access for reading and writing</para>
    /// </summary>
    public class SrezTable : SrezTableLight {
        /// <summary>
        /// Description of the structure of the slice
        /// </summary>
        public class SrezDescr {
            /// <summary>
            /// Constructor
            /// </summary>
            protected SrezDescr() { }

            /// <summary>
            /// Constructor
            /// </summary>
            public SrezDescr(int cnlCnt) {
                if (cnlCnt <= 0)
                    throw new ArgumentOutOfRangeException(nameof(cnlCnt));

                CnlNums = new int[cnlCnt];
                CS = 0;
            }

            /// <summary>
            /// Get slice channel numbers in ascending order
            /// </summary>
            public int[] CnlNums { get; protected set; }

            /// <summary>
            /// Check sum
            /// </summary>
            public ushort CS { get; protected set; }

            /// <summary>
            /// Calculate checksum
            /// </summary>
            public ushort CalcCS() {
                var cs = 1;
                foreach (int cnlNum in CnlNums)
                    cs += cnlNum;
                CS = (ushort) cs;
                return CS;
            }

            /// <summary>
            /// Check if the given object is identical to the current one.
            /// </summary>
            public bool Equals(SrezDescr srezDescr) {
                if (srezDescr == this) {
                    return true;
                }

                if (srezDescr == null || CS != srezDescr.CS || CnlNums.Length != srezDescr.CnlNums.Length)
                    return false;

                int len = CnlNums.Length;
                for (var i = 0; i < len; i++) {
                    if (CnlNums[i] != srezDescr.CnlNums[i])
                        return false;
                }

                return true;
            }

            /// <summary>
            /// Check if the specified cutoff channel numbers are identical to the current ones.
            /// </summary>
            public bool Equals(int[] cnlNums) {
                if (cnlNums == CnlNums) {
                    return true;
                }

                if (cnlNums == null || CnlNums.Length != cnlNums.Length)
                    return false;

                int len = CnlNums.Length;
                for (var i = 0; i < len; i++) {
                    if (CnlNums[i] != cnlNums[i])
                        return false;
                }

                return true;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Slice data input channels at a certain point in time
        /// </summary>
        public new class Srez : SrezTableLight.Srez {
            /// <inheritdoc />
            /// <summary>
            /// Constructor
            /// </summary>
            public Srez(DateTime dateTime, int cnlCnt)
                : base(dateTime, cnlCnt) {
                SrezDescr = new SrezDescr(cnlCnt);
                State = DataRowState.Detached;
                Position = -1;
            }

            /// <inheritdoc />
            /// <summary>
            /// Constructor
            /// </summary>
            public Srez(DateTime dateTime, SrezDescr srezDescr) {
                if (srezDescr == null)
                    throw new ArgumentNullException(nameof(srezDescr));

                DateTime = dateTime;
                int cnlCnt = srezDescr.CnlNums.Length;
                CnlNums = new int[cnlCnt];
                srezDescr.CnlNums.CopyTo(CnlNums, 0);
                CnlData = new CnlData[cnlCnt];

                SrezDescr = srezDescr;
                State = DataRowState.Detached;
                Position = -1;
            }

            /// <inheritdoc />
            /// <summary>
            /// Constructor
            /// </summary>
            public Srez(DateTime dateTime, SrezDescr srezDescr, SrezTableLight.Srez srcSrez)
                : this(dateTime, srezDescr) {
                CopyDataFrom(srcSrez);
            }

            /// <inheritdoc />
            /// <summary>
            /// Constructor
            /// </summary>
            public Srez(DateTime dateTime, Srez srcSrez)
                : this(dateTime, srcSrez.SrezDescr, srcSrez) { }

            /// <summary>
            /// Get a description of the slice structure, but the basis of which it was created
            /// </summary>
            public SrezDescr SrezDescr { get; protected set; }

            /// <summary>
            /// Get or set the current state of the slice
            /// </summary>
            public DataRowState State { get; protected internal set; }

            /// <summary>
            /// Get or set the position of the slice timestamp in a file or stream
            /// </summary>
            public long Position { get; protected internal set; }

            /// <summary>
            /// Copy data from source slice to current
            /// </summary>
            public void CopyDataFrom(SrezTableLight.Srez srcSrez) {
                if (srcSrez != null) {
                    if (SrezDescr.Equals(srcSrez.CnlNums)) {
                        srcSrez.CnlData.CopyTo(CnlData, 0);
                    } else {
                        int srcCnlCnt = srcSrez.CnlData.Length;
                        for (var i = 0; i < srcCnlCnt; i++)
                            SetCnlData(srcSrez.CnlNums[i], srcSrez.CnlData[i]);
                    }
                }
            }
        }


        /// <summary>
        /// The sign of data loading into the slice table
        /// </summary>
        protected bool dataLoading;


        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public SrezTable() {
            dataLoading = false;
            Descr = "";
            LastStoredSrez = null;
            AddedSrezList = new List<Srez>();
            ModifiedSrezList = new List<Srez>();
        }


        /// <summary>
        /// Get or set the table description
        /// </summary>
        public string Descr { get; set; }

        /// <summary>
        /// Get the last recorded slice
        /// </summary>
        public Srez LastStoredSrez { get; protected internal set; }

        /// <summary>
        /// Get list of added slices
        /// </summary>
        public List<Srez> AddedSrezList { get; protected set; }

        /// <summary>
        /// Get a list of changed slices
        /// </summary>
        public List<Srez> ModifiedSrezList { get; protected set; }

        /// <summary>
        /// Get the sign of changing the slice table
        /// </summary>
        public bool Modified {
            get { return AddedSrezList.Count > 0 || ModifiedSrezList.Count > 0; }
        }


        /// <inheritdoc />
        /// <summary>
        /// Add a slice to the table
        /// </summary>
        /// <remarks>If there is already a slice in the table with the specified time stamp, 
        /// then adding a new slice does not occur</remarks>
        public override bool AddSrez(SrezTableLight.Srez srez) {
            if (!(srez is Srez))
                throw new ArgumentException("Srez type is incorrect.", nameof(srez));

            if (base.AddSrez(srez)) {
                MarkSrezAsAdded((Srez) srez);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Add a copy of the slice to the table
        /// </summary>
        /// <remarks>If there is already a slice in the table with the specified time stamp, 
        /// then data is copied from the original slice</remarks>
        public Srez AddSrezCopy(Srez srcSrez, DateTime srezDT) {
            if (srcSrez == null)
                throw new ArgumentNullException(nameof(srcSrez));

            Srez srez;

            if (SrezList.TryGetValue(srezDT, out SrezTableLight.Srez lightSrez)) {
                // change existing slice in the table
                srez = (Srez)lightSrez; // InvalidCastException possible
                srez.CopyDataFrom(srcSrez);

                if (srez.State == DataRowState.Unchanged) {
                    srez.State = DataRowState.Modified;
                    ModifiedSrezList.Add(srez);
                }
            } else {
                // creating and adding a new slice to the table
                srez = new Srez(srezDT, srcSrez) { State = DataRowState.Added };
                AddedSrezList.Add(srez);
                SrezList.Add(srezDT, srez);
            }

            return srez;
        }

        /// <summary>
        /// Get a cut for a certain time
        /// </summary>
        public new Srez GetSrez(DateTime dateTime) {
            SrezTableLight.Srez srez;
            return SrezList.TryGetValue(dateTime, out srez) ? srez as Srez : null;
        }

        /// <summary>
        /// Start loading data into a table
        /// </summary>
        /// <remarks>Pauses the tracking of adding records to a table.</remarks>
        public void BeginLoadData() {
            dataLoading = true;
        }

        /// <summary>
        /// Complete data loading into the table
        /// </summary>
        /// <remarks>Tracking of adding records to the table resumes.</remarks>
        public void EndLoadData() {
            dataLoading = false;
        }

        /// <summary>
        /// Accept slice table changes
        /// </summary>
        public void AcceptChanges() {
            foreach (var lightSrez in SrezList.Values) {
                if (lightSrez is Srez srez)
                    srez.State = DataRowState.Unchanged;
            }

            AddedSrezList.Clear();
            ModifiedSrezList.Clear();
        }

        /// <summary>
        /// Mark slice as added
        /// </summary>
        public void MarkSrezAsAdded(Srez srez) {
            if (srez == null) return;

            if (dataLoading) {
                srez.State = DataRowState.Unchanged;
            } else {
                srez.State = DataRowState.Added;
                srez.Position = -1;
                AddedSrezList.Add(srez);
            }
        }

        /// <summary>
        /// Mark slice as modified
        /// </summary>
        public void MarkSrezAsModified(Srez srez) {
            if (srez == null || (srez.State != DataRowState.Unchanged && srez.State != DataRowState.Deleted)) return;
            srez.State = DataRowState.Modified;
            ModifiedSrezList.Add(srez);
        }

        /// <inheritdoc />
        /// <summary>
        /// Clear the slice table
        /// </summary>
        public override void Clear() {
            base.Clear();
            LastStoredSrez = null;
            AddedSrezList.Clear();
            ModifiedSrezList.Clear();
        }
    }
}