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
 * Summary  : Cache of the data received from SCADA-Server for clients usage
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2017
 */

using Scada.Data.Configuration;
using Scada.Data.Models;
using Scada.Data.Tables;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using Utils;

namespace Scada.Client {
    /// <summary>
    /// Cache of the data received from SCADA-Server for clients usage
    /// <para>Cache data received from SCADA Server for use by clients</para>
    /// </summary>
    /// <remarks>All the returned data are not thread safe
    /// <para>All returned data is not thread safe.</para></remarks>
    public class DataCache {
        /// <summary>
        /// The capacity of the cache of tables of hourly slices
        /// </summary>
        protected const int HourCacheCapacity = 100;

        /// <summary>
        /// Event Table Cache Capacity
        /// </summary>
        protected const int EventCacheCapacity = 100;

        /// <summary>
        /// The storage period for hourly slice tables in the cache since the last access
        /// </summary>
        protected static readonly TimeSpan HourCacheStorePeriod = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Period of storage of event tables in the cache since the last access
        /// </summary>
        protected static readonly TimeSpan EventCacheStorePeriod = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Time of the relevance of the configuration database tables
        /// </summary>
        protected static readonly TimeSpan BaseValidSpan = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Time of current and historical data
        /// </summary>
        protected static readonly TimeSpan DataValidSpan = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Timeout for unlocking configuration database
        /// </summary>
        protected static readonly TimeSpan WaitBaseLock = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Value separator within the table field
        /// </summary>
        protected static readonly char[] FieldSeparator = new char[] {';'};


        /// <summary>
        /// Object for data exchange with SCADA-Server
        /// </summary>
        protected readonly ServerComm serverComm;

        /// <summary>
        /// Log
        /// </summary>
        protected readonly Log log;

        /// <summary>
        /// Object to synchronize access to configuration database tables
        /// </summary>
        protected readonly object baseLock;

        /// <summary>
        /// Object to synchronize access to current data
        /// </summary>
        protected readonly object curDataLock;

        /// <summary>
        /// The time of the last successful update of the configuration database tables
        /// </summary>
        protected DateTime baseRefrDT;

        /// <summary>
        /// Current slice table
        /// </summary>
        protected SrezTableLight tblCur;

        /// <summary>
        /// Time of the last successful update of the current slice table
        /// </summary>
        protected DateTime curDataRefrDT;


        /// <summary>
        /// Constructor restricting the creation of an object without parameters
        /// </summary>
        protected DataCache() { }

        /// <summary>
        /// Constructor
        /// </summary>
        public DataCache(ServerComm serverComm, Log log) {
            this.serverComm = serverComm ?? throw new ArgumentNullException(nameof(serverComm));
            this.log = log ?? throw new ArgumentNullException(nameof(log));

            baseLock = new object();
            curDataLock = new object();

            baseRefrDT = DateTime.MinValue;
            tblCur = new SrezTableLight();
            curDataRefrDT = DateTime.MinValue;

            BaseTables = new BaseTables();
            CnlProps = new InCnlProps[0];
            CtrlCnlProps = new CtrlCnlProps[0];
            CnlStatProps = new SortedList<int, CnlStatProps>();
            HourTableCache = new Cache<DateTime, SrezTableLight>(HourCacheStorePeriod, HourCacheCapacity);
            EventTableCache = new Cache<DateTime, EventTableLight>(EventCacheStorePeriod, EventCacheCapacity);
        }


        /// <summary>
        /// Get configuration database tables
        /// </summary>
        /// <remarks>When updating, the table object is re-created, ensuring integrity.
        /// Tables after loading are not changed by an instance of this class and should not be changed from the outside.,
        /// thus, reading data from tables is thread safe.
        /// but, when using data table. Default View it is necessary to synchronize access to the tables 
        /// by calling lock (BaseTables.SyncRoot)</remarks>
        public BaseTables BaseTables { get; protected set; }

        /// <summary>
        /// Get input channel properties in ascending channel numbers.
        /// </summary>
        /// <remarks>The array is re-created after updating the configuration database tables.
        /// The array after initialization is not changed by an instance of this class and should not be changed from the outside,
        /// thus, reading its data is thread safe.
        /// </remarks>
        public InCnlProps[] CnlProps { get; protected set; }

        /// <summary>
        /// Get control channel properties sorted in ascending channel numbers
        /// </summary>
        /// <remarks>The array is re-created after updating the configuration database tables.
        /// The array after initialization is not changed by an instance of this class and should not be changed from the outside,
        /// thus, reading its data is thread safe.
        /// </remarks>
        public CtrlCnlProps[] CtrlCnlProps { get; protected set; }

        /// <summary>
        /// Get input channel status properties
        /// </summary>
        /// <remarks>The list is re-created after updating the configuration database tables.
        /// The list after initialization is not changed by an instance of this class and should not be changed from the outside,
        /// thus, reading its data is thread safe.
        /// </remarks>
        public SortedList<int, CnlStatProps> CnlStatProps { get; protected set; }

        /// <summary>
        /// Get cache of hour slice tables
        /// </summary>
        /// <remarks>Use outside of this class only to get cache state</remarks>
        public Cache<DateTime, SrezTableLight> HourTableCache { get; protected set; }

        /// <summary>
        /// Get event table cache
        /// </summary>
        /// <remarks>Use outside of this class only to get cache state</remarks>
        public Cache<DateTime, EventTableLight> EventTableCache { get; protected set; }


        /// <summary>
        /// Fill properties of input channels
        /// </summary>
        protected void FillCnlProps() {
            try {
                log.WriteAction("Fill input channels properties");

                var confDAO = new ConfDAO(BaseTables);
                List<InCnlProps> cnlPropsList = confDAO.GetInCnlProps();
                CnlProps = cnlPropsList.ToArray();
            } catch (Exception ex) {
                log.WriteException(ex, "Error filling input channels properties");
            }
        }

        /// <summary>
        /// Fill control channel properties
        /// </summary>
        protected void FillCtrlCnlProps() {
            try {
                log.WriteAction("Fill output channels properties");

                var confDAO = new ConfDAO(BaseTables);
                List<CtrlCnlProps> ctrlCnlPropsList = confDAO.GetCtrlCnlProps();
                CtrlCnlProps = ctrlCnlPropsList.ToArray();
            } catch (Exception ex) {
                log.WriteException(ex, "Error filling output channels properties");
            }
        }

        /// <summary>
        /// Fill properties of the status of input channels
        /// </summary>
        protected void FillCnlStatProps() {
            try {
                log.WriteAction("Fill input channel statuses properties");

                var confDAO = new ConfDAO(BaseTables);
                CnlStatProps = confDAO.GetCnlStatProps();
            } catch (Exception ex) {
                log.WriteException(ex, "Error filling input channel statuses properties");
            }
        }

        /// <summary>
        /// Update current data
        /// </summary>
        protected void RefreshCurData() {
            try {
                var utcNowDT = DateTime.UtcNow;
                if (utcNowDT - curDataRefrDT > DataValidSpan) { // data is out of date
                    curDataRefrDT = utcNowDT;
                    var newCurTableAge = serverComm.ReceiveFileAge(ServerComm.Dirs.Cur, SrezAdapter.CurTableName);

                    if (newCurTableAge == DateTime.MinValue) {
                        // the slice file does not exist or there is no connection to the server
                        tblCur.Clear();
                        tblCur.FileModTime = DateTime.MinValue;
                        log.WriteError("Unable to receive the current data file modification time.");
                    } else if (tblCur.FileModTime != newCurTableAge) { // slice file changed
                        if (serverComm.ReceiveSrezTable(SrezAdapter.CurTableName, tblCur)) {
                            tblCur.FileModTime = newCurTableAge;
                            tblCur.LastFillTime = utcNowDT;
                        } else {
                            tblCur.FileModTime = DateTime.MinValue;
                        }
                    }
                }
            } catch (Exception ex) {
                tblCur.FileModTime = DateTime.MinValue;
                log.WriteException(ex, "Error refreshing the current data");
            }
        }


        /// <summary>
        /// Update configuration database tables, channel properties and statuses
        /// </summary>
        public void RefreshBaseTables() {
            lock (baseLock) {
                try {
                    var utcNowDT = DateTime.UtcNow;

                    if (utcNowDT - baseRefrDT > BaseValidSpan) { // data is out of date
                        baseRefrDT = utcNowDT;
                        var newBaseAge = serverComm.ReceiveFileAge(ServerComm.Dirs.BaseDAT,
                            BaseTables.GetFileName(BaseTables.InCnlTable));

                        if (newBaseAge == DateTime.MinValue) {
                            // configuration database does not exist or there is no connection to the server
                            throw new ScadaException("Unable to receive the configuration database modification time.");
                        }

                        if (BaseTables.BaseAge != newBaseAge) { // configuration base changed
                            log.WriteAction("Refresh the tables of the configuration database");

                            // waiting for unlocking possible configuration base
                            var t0 = utcNowDT;
                            while (serverComm.ReceiveFileAge(ServerComm.Dirs.BaseDAT, "baselock") > DateTime.MinValue &&
                                   DateTime.UtcNow - t0 <= WaitBaseLock) {
                                Thread.Sleep(ScadaUtils.ThreadDelay);
                            }

                            // loading data into tables
                            var newBaseTables = new BaseTables() {BaseAge = newBaseAge};
                            foreach (var dataTable in newBaseTables.AllTables) {
                                string tableName = BaseTables.GetFileName(dataTable);

                                if (!serverComm.ReceiveBaseTable(tableName, dataTable)) {
                                    throw new ScadaException($"Unable to receive the table {tableName}");
                                }
                            }

                            BaseTables = newBaseTables;

                            // filling channel properties and statuses
                            lock (BaseTables.SyncRoot) {
                                FillCnlProps();
                                FillCtrlCnlProps();
                                FillCnlStatProps();
                            }
                        }
                    }
                } catch (Exception ex) {
                    BaseTables.BaseAge = DateTime.MinValue;
                    log.WriteException(ex, "Error refreshing the tables of the configuration database");
                }
            }
        }

        /// <summary>
        /// Get current slice from cache or from server
        /// </summary>
        /// <remarks>The returned slice after loading is not changed by an instance of this class,
        /// thus, reading its data is thread safe.</remarks>
        public SrezTableLight.Srez GetCurSnapshot(out DateTime dataAge) {
            lock (curDataLock) {
                try {
                    RefreshCurData();
                    dataAge = tblCur.FileModTime;
                    return tblCur.SrezList.Count > 0 ? tblCur.SrezList.Values[0] : null;
                } catch (Exception ex) {
                    log.WriteException(ex, "Error getting the current snapshot the cache or from the server");
                    dataAge = DateTime.MinValue;
                    return null;
                }
            }
        }

        /// <summary>
        /// Get a table of hourly data per day from the cache or from the server
        /// </summary>
        /// <remarks>The returned table after loading is not changed by an instance of this class,
        /// thus, reading its data is thread safe. 
        /// The method always returns a non-null object.</remarks>
        public SrezTableLight GetHourTable(DateTime date) {
            try {
                // getting the table of hourly slices from the cache
                date = date.Date;
                var utcNowDT = DateTime.UtcNow;
                var cacheItem = HourTableCache.GetOrCreateItem(date, utcNowDT);

                // block access to only one table of hourly slices
                lock (cacheItem) {
                    var table = cacheItem.Value; // table to get
                    var tableAge = cacheItem.ValueAge; // table file change time
                    bool tableIsNotValid =
                        utcNowDT - cacheItem.ValueRefrDT > DataValidSpan; // the table might be out of date

                    // getting time slice table from server
                    if (table == null || tableIsNotValid) {
                        string tableName = SrezAdapter.BuildHourTableName(date);
                        var newTableAge = serverComm.ReceiveFileAge(ServerComm.Dirs.Hour, tableName);

                        if (newTableAge == DateTime.MinValue) {
                            // the table file does not exist or there is no connection to the server
                            table = null;
                            // do not clog the log
                            //log.WriteError($"Unable to receive modification time of the hourly data table {tableName}");
                        } else if (newTableAge != tableAge) { // table file changed
                            table = new SrezTableLight();
                            if (serverComm.ReceiveSrezTable(tableName, table)) {
                                table.FileModTime = newTableAge;
                                table.LastFillTime = utcNowDT;
                            } else {
                                throw new ScadaException("Unable to receive hourly data table.");
                            }
                        }

                        if (table == null)
                            table = new SrezTableLight();

                        // update table in cache
                        HourTableCache.UpdateItem(cacheItem, table, newTableAge, utcNowDT);
                    }

                    return table;
                }
            } catch (Exception ex) {
                log.WriteException(ex, "Error getting hourly data table for {0} from the cache or from the server",
                    date.ToLocalizedDateString());
                return new SrezTableLight();
            }
        }

        /// <summary>
        /// Get a table of events for the day from the cache or from the server
        /// </summary>
        /// <remarks>The returned table after loading is not changed by an instance of this class,
        /// thus, reading its data is thread safe.
        /// The method always returns a non-null object.</remarks>
        public EventTableLight GetEventTable(DateTime date) {
            try {
                // getting event table from cache
                date = date.Date;
                var utcNowDT = DateTime.UtcNow;
                var cacheItem = EventTableCache.GetOrCreateItem(date, utcNowDT);

                // blocking access to only one event table
                lock (cacheItem) {
                    var table = cacheItem.Value; // table to get
                    var tableAge = cacheItem.ValueAge; // table file change time
                    bool tableIsNotValid =
                        utcNowDT - cacheItem.ValueRefrDT > DataValidSpan; // the table might be out of date

                    // getting event table from server
                    if (table == null || tableIsNotValid) {
                        string tableName = EventAdapter.BuildEvTableName(date);
                        var newTableAge = serverComm.ReceiveFileAge(ServerComm.Dirs.Events, tableName);

                        if (newTableAge == DateTime.MinValue) {
                            // the table file does not exist or there is no connection to the server
                            table = null;
                            // do not clog the log
                            //log.WriteError($"Unable to receive modification time of the event table {tableName}");
                        } else if (newTableAge != tableAge) { // table file changed
                            table = new EventTableLight();
                            if (serverComm.ReceiveEventTable(tableName, table)) {
                                table.FileModTime = newTableAge;
                                table.LastFillTime = utcNowDT;
                            } else {
                                throw new ScadaException("Unable to receive event table.");
                            }
                        }

                        if (table == null)
                            table = new EventTableLight();

                        // update table in cache
                        EventTableCache.UpdateItem(cacheItem, table, newTableAge, utcNowDT);
                    }

                    return table;
                }
            } catch (Exception ex) {
                log.WriteException(ex, "Error getting event table for {0} from the cache or from the server",
                    date.ToLocalizedDateString());
                return new EventTableLight();
            }
        }

        /// <summary>
        /// Get the trend of the minute data of the specified channel for the day
        /// </summary>
        /// <remarks>Returned trend after loading is not changed by an instance of this class,
        /// thus, reading its data is thread safe.
        /// The method always returns a non-null object.</remarks>
        public Trend GetMinTrend(DateTime date, int cnlNum) {
            var trend = new Trend(cnlNum);

            try {
                if (serverComm.ReceiveTrend(SrezAdapter.BuildMinTableName(date), date, trend)) {
                    trend.LastFillTime = DateTime.UtcNow; // consistent with hourly data and events
                } else {
                    throw new ScadaException("Unable to receive trend.");
                }
            } catch (Exception ex) {
                log.WriteException(ex,"Error getting minute data trend for {0}", date.ToLocalizedDateString());
            }

            return trend;
        }
    }
}