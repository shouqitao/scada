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
 * Summary  : Event table for fast read data access
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2007
 * Modified : 2018
 */

using System;
using System.Collections.Generic;

namespace Scada.Data.Tables {
    /// <summary>
    /// Event table for fast read data access
    /// <para>Event table for quick access to read data</para>
    /// </summary>
    public class EventTableLight {
        /// <summary>
        /// Event data
        /// </summary>
        public class Event {
            /// <summary>
            /// Constructor
            /// </summary>
            public Event() {
                Number = 0;
                DateTime = DateTime.MinValue;
                ObjNum = 0;
                KPNum = 0;
                ParamID = 0;
                CnlNum = 0;
                OldCnlVal = 0.0;
                OldCnlStat = 0;
                NewCnlVal = 0.0;
                NewCnlStat = 0;
                Checked = false;
                UserID = 0;
                Descr = "";
                Data = "";
            }

            /// <summary>
            /// Get or set the sequence number of the event in the file
            /// </summary>
            public int Number { get; set; }

            /// <summary>
            /// Get or set event timestamp
            /// </summary>
            public DateTime DateTime { get; set; }

            /// <summary>
            /// Get or set object number
            /// </summary>
            public int ObjNum { get; set; }

            /// <summary>
            /// Get or set KP number
            /// </summary>
            public int KPNum { get; set; }

            /// <summary>
            /// Get or set the parameter ID
            /// </summary>
            public int ParamID { get; set; }

            /// <summary>
            /// Get or set the input channel number
            /// </summary>
            public int CnlNum { get; set; }

            /// <summary>
            /// Get or set previous channel value
            /// </summary>
            public double OldCnlVal { get; set; }

            /// <summary>
            /// Get or set the previous channel status
            /// </summary>
            public int OldCnlStat { get; set; }

            /// <summary>
            /// Get or set new channel value
            /// </summary>
            public double NewCnlVal { get; set; }

            /// <summary>
            /// Get or set new channel status
            /// </summary>
            public int NewCnlStat { get; set; }

            /// <summary>
            /// Get or set an indication that an event has been acknowledged
            /// </summary>
            public bool Checked { get; set; }

            /// <summary>
            /// Get or set the identifier of the user acknowledging the event.
            /// </summary>
            public int UserID { get; set; }

            /// <summary>
            /// Get or set event description
            /// </summary>
            public string Descr { get; set; }

            /// <summary>
            /// Get or set additional event data
            /// </summary>
            public string Data { get; set; }
        }

        /// <summary>
        /// Filters (filter types) of events
        /// </summary>
        [Flags]
        public enum EventFilters {
            /// <summary>
            /// Empty filter
            /// </summary>
            None = 0,

            /// <summary>
            /// Filter by object
            /// </summary>
            Obj = 1,

            /// <summary>
            /// KP filter
            /// </summary>
            KP = 2,

            /// <summary>
            /// Filter by one or more parameters
            /// </summary>
            Param = 4,

            /// <summary>
            /// Filter by channels
            /// </summary>
            Cnls = 8,

            /// <summary>
            /// Filter by status
            /// </summary>
            Stat = 16,

            /// <summary>
            /// Acknowledgment filter
            /// </summary>
            Ack = 32
        }

        /// <summary>
        /// Event filter
        /// </summary>
        public class EventFilter {
            /// <inheritdoc />
            /// <summary>
            /// Constructor
            /// </summary>
            public EventFilter()
                : this(EventFilters.None) { }

            /// <summary>
            /// Constructor
            /// </summary>
            public EventFilter(EventFilters filters) {
                Filters = filters;
                ObjNum = 0;
                KPNum = 0;
                ParamID = 0;
                ParamIDs = null;
                CnlNums = null;
                Statuses = null;
                Checked = false;
            }

            /// <summary>
            /// Get or set the types of filters applied
            /// </summary>
            public EventFilters Filters { get; set; }

            /// <summary>
            /// Get or set the object number to filter
            /// </summary>
            public int ObjNum { get; set; }

            /// <summary>
            /// Get or set filtering number
            /// </summary>
            public int KPNum { get; set; }

            /// <summary>
            /// Get or set id. parameter to filter
            /// </summary>
            public int ParamID { get; set; }

            /// <summary>
            /// Get or set id. filtering options
            /// </summary>
            public ISet<int> ParamIDs { get; set; }

            /// <summary>
            /// Get or set input channel numbers to filter
            /// </summary>
            public ISet<int> CnlNums { get; set; }

            /// <summary>
            /// Get or set filtering statuses
            /// </summary>
            public ISet<int> Statuses { get; set; }

            /// <summary>
            /// Get or set an acknowledgment flag to filter
            /// </summary>
            public bool Checked { get; set; }

            /// <summary>
            /// Check the correctness of the filter
            /// </summary>
            public bool Check(bool throwOnFail = true) {
                if (!Filters.HasFlag(EventFilters.Cnls) || CnlNums != null) return true;
                if (throwOnFail)
                    throw new ScadaException("Event filter is incorrect.");
                return false;
            }

            /// <summary>
            /// Check that the event matches the filter.
            /// </summary>
            public bool Satisfied(Event ev) {
                // if filtering only by channel numbers is used, CnlNums should not be null
                if (Filters == EventFilters.Cnls) {
                    // quick check by channel numbers only
                    return CnlNums.Contains(ev.CnlNum);
                }

                // complete filter condition check
                return
                    (!Filters.HasFlag(EventFilters.Obj) || ObjNum == ev.ObjNum) &&
                    (!Filters.HasFlag(EventFilters.KP) || KPNum == ev.KPNum) &&
                    (!Filters.HasFlag(EventFilters.Param) || ParamID > 0 && ParamID == ev.ParamID ||
                     ParamIDs != null && ParamIDs.Contains(ev.ParamID)) &&
                    (!Filters.HasFlag(EventFilters.Cnls) || CnlNums != null && CnlNums.Contains(ev.CnlNum)) &&
                    (!Filters.HasFlag(EventFilters.Stat) || Statuses != null && Statuses.Contains(ev.NewCnlStat)) &&
                    (!Filters.HasFlag(EventFilters.Ack) || Checked == ev.Checked);
            }
        }

        /// <summary>
        /// Table name
        /// </summary>
        protected string tableName;

        /// <summary>
        /// The last time the table file was modified
        /// </summary>
        protected DateTime fileModTime;

        /// <summary>
        /// The time of the last successful filling of the table
        /// </summary>
        protected DateTime lastFillTime;

        /// <summary>
        /// List of all events
        /// </summary>
        protected List<Event> allEvents;


        /// <summary>
        /// Constructor
        /// </summary>
        public EventTableLight() {
            tableName = "";
            fileModTime = DateTime.MinValue;
            lastFillTime = DateTime.MinValue;

            allEvents = new List<Event>();
        }


        /// <summary>
        /// Get or set table file name
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
        /// Get or set the last time the table file was modified
        /// </summary>
        public DateTime FileModTime {
            get { return fileModTime; }
            set { fileModTime = value; }
        }

        /// <summary>
        /// Get or set the time of the last successful filling of the table
        /// </summary>
        public DateTime LastFillTime {
            get { return lastFillTime; }
            set { lastFillTime = value; }
        }

        /// <summary>
        /// Get a list of all events
        /// </summary>
        public List<Event> AllEvents {
            get { return allEvents; }
        }


        /// <summary>
        /// Add event to table
        /// </summary>
        public void AddEvent(Event ev) {
            allEvents.Add(ev);
        }

        /// <summary>
        /// Clear event table
        /// </summary>
        public void Clear() {
            allEvents.Clear();
        }

        /// <summary>
        /// Get filtered events
        /// </summary>
        public List<Event> GetFilteredEvents(EventFilter filter) {
            return GetFilteredEvents(filter, 0, 0, out bool reversed);
        }

        /// <summary>
        /// Get filtered events in the specified range.
        /// </summary>
        public List<Event> GetFilteredEvents(EventFilter filter, int lastCount, int startEvNum, out bool reversed) {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));
            filter.Check();

            reversed = false;
            List<Event> filteredEvents = lastCount > 0 ? new List<Event>(lastCount) : new List<Event>();
            int startEvInd = Math.Max(0, startEvNum - 1);
            int allEventsCnt = allEvents.Count;

            void AddEventAction(int i) {
                var ev = allEvents[i];
                if (filter.Satisfied(ev)) filteredEvents.Add(ev);
            }

            if (lastCount > 0) {
                for (int i = allEventsCnt - 1; i >= startEvInd && filteredEvents.Count < lastCount; i--)
                    AddEventAction(i);
                reversed = true;
            } else {
                for (int i = startEvInd; i < allEventsCnt; i++)
                    AddEventAction(i);
            }

            return filteredEvents;
        }

        /// <summary>
        /// Get event by number
        /// </summary>
        public Event GetEventByNum(int evNum) {
            if (1 > evNum || evNum > allEvents.Count) return null;
            var ev = allEvents[evNum - 1];
            return ev.Number == evNum ? ev : null;
        }
    }
}