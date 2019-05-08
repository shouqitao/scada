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
 * Summary  : The base class for view
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2011
 * Modified : 2017
 */

using Scada.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace Scada.Client {
    /// <inheritdoc />
    /// <summary>
    /// The base class for view
    /// <para>Base View Class</para>
    /// </summary>
    /// <remarks>
    /// Derived views must provide thread safe read access in case that the object is not being changed. 
    /// Write operations must be synchronized
    /// <para>Child representations must provide thread-safe read access provided, 
    /// that the object does not change. Write operations must be synchronized.</para></remarks>
    public abstract class BaseView : ISupportLoading {
        /// <summary>
        /// Constructor
        /// </summary>
        protected BaseView() {
            Title = "";
            Path = "";
            CnlSet = new HashSet<int>();
            CnlList = new List<int>();
            CtrlCnlSet = new HashSet<int>();
            CtrlCnlList = new List<int>();
            StoredOnServer = true;
            BaseAge = DateTime.MinValue;
            Stamp = 0;
        }


        /// <summary>
        /// Get or set view title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Get or set view file path
        /// </summary>
        /// <remarks>If the view file is stored on the server, 
        /// This path is relative to the interface directory.</remarks>
        public string Path { get; set; }

        /// <summary>
        /// Get the name of the view file
        /// </summary>
        public string FileName {
            get { return System.IO.Path.GetFileName(Path); }
        }

        /// <summary>
        /// Get the many numbers of input channels that are used in the view.
        /// </summary>
        public HashSet<int> CnlSet { get; protected set; }

        /// <summary>
        /// Get a list of input channel numbers ordered without repetitions, 
        /// which are used in the presentation
        /// </summary>
        public List<int> CnlList { get; protected set; }

        /// <summary>
        /// Get the many control channel numbers used in the view.
        /// </summary>
        public HashSet<int> CtrlCnlSet { get; protected set; }

        /// <summary>
        /// Get a list of control channel numbers ordered without repetitions, 
        /// which are used in the presentation
        /// </summary>
        public List<int> CtrlCnlList { get; protected set; }

        /// <summary>
        /// Get the sign of storing the presentation file on the server (in the interface directory)
        /// </summary>
        public bool StoredOnServer { get; protected set; }

        /// <summary>
        /// Get or set the last time the configuration database was changed, 
        /// for which channels are assigned
        /// </summary>
        public DateTime BaseAge { get; set; }

        /// <summary>
        /// Get or set a unique object label within a certain data set.
        /// </summary>
        /// <remarks>Used to control data integrity when retrieving a cached view.</remarks>
        public long Stamp { get; set; }

        /// <summary>
        /// Get object to synchronize access to the view
        /// </summary>
        public object SyncRoot {
            get { return this; }
        }


        /// <summary>
        /// Add the number of the input channel to the set and to the list
        /// </summary>
        protected void AddCnlNum(int cnlNum) {
            if (cnlNum <= 0 || !CnlSet.Add(cnlNum)) return;

            int index = CnlList.BinarySearch(cnlNum);
            if (index < 0)
                CnlList.Insert(~index, cnlNum);
        }

        /// <summary>
        /// Add control channel number to set and to list.
        /// </summary>
        protected void AddCtrlCnlNum(int ctrlCnlNum) {
            if (ctrlCnlNum <= 0 || !CtrlCnlSet.Add(ctrlCnlNum)) return;

            int index = CtrlCnlList.BinarySearch(ctrlCnlNum);
            if (index < 0)
                CtrlCnlList.Insert(~index, ctrlCnlNum);
        }


        /// <inheritdoc />
        /// <summary>
        /// Download view from stream
        /// </summary>
        public virtual void LoadFromStream(Stream stream) { }

        /// <summary>
        /// Bind input channel properties to view elements
        /// </summary>
        public virtual void BindCnlProps(InCnlProps[] cnlPropsArr) { }

        /// <summary>
        /// Bind control channel properties to view controls
        /// </summary>
        public virtual void BindCtrlCnlProps(CtrlCnlProps[] ctrlCnlPropsArr) { }

        /// <summary>
        /// Determine that the input channel is used in the view
        /// </summary>
        public virtual bool ContainsCnl(int cnlNum) {
            return CnlSet.Contains(cnlNum);
        }

        /// <summary>
        /// Determine that all specified input channels are used in the view.
        /// </summary>
        public virtual bool ContainsAllCnls(IEnumerable<int> cnlNums) {
            return CnlSet.Count > 0 && CnlSet.IsSupersetOf(cnlNums);
        }

        /// <summary>
        /// Determine that the control channel is used in the view
        /// </summary>
        public virtual bool ContainsCtrlCnl(int ctrlCnlNum) {
            return CtrlCnlSet.Contains(ctrlCnlNum);
        }

        /// <inheritdoc />
        /// <summary>
        /// Clear view
        /// </summary>
        public virtual void Clear() {
            Title = "";
            Path = "";
            CnlList.Clear();
            CtrlCnlList.Clear();
            CnlSet.Clear();
            CtrlCnlSet.Clear();
        }
    }
}