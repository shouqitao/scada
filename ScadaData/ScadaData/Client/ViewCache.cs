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
 * Summary  : Cache of views
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2016
 */

using Scada.Data.Models;
using System;
using Utils;

namespace Scada.Client {
    /// <summary>
    /// Cache of views
    /// <para>View cache</para>
    /// </summary>
    public class ViewCache {
        /// <summary>
        /// Cache capacity unlimited by number of items
        /// </summary>
        protected const int Capacity = int.MaxValue;

        /// <summary>
        /// Cache retention period since last access
        /// </summary>
        protected static readonly TimeSpan StorePeriod = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Cache time
        /// </summary>
        protected static readonly TimeSpan ViewValidSpan = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Object for data exchange with SCADA-Server
        /// </summary>
        protected readonly ServerComm serverComm;

        /// <summary>
        /// Object for thread-safe access to client cache data
        /// </summary>
        protected readonly DataAccess dataAccess;

        /// <summary>
        /// Log
        /// </summary>
        protected readonly Log log;


        /// <summary>
        /// Constructor restricting the creation of an object without parameters
        /// </summary>
        protected ViewCache() { }

        /// <summary>
        /// Constructor
        /// </summary>
        public ViewCache(ServerComm serverComm, DataAccess dataAccess, Log log) {
            this.serverComm = serverComm ?? throw new ArgumentNullException(nameof(serverComm));
            this.dataAccess = dataAccess ?? throw new ArgumentNullException(nameof(dataAccess));
            this.log = log ?? throw new ArgumentNullException(nameof(log));

            Cache = new Cache<int, BaseView>(StorePeriod, Capacity);
        }


        /// <summary>
        /// Get view cache object
        /// </summary>
        /// <remarks>Use outside of this class only to get cache state</remarks>
        public Cache<int, BaseView> Cache { get; protected set; }


        /// <summary>
        /// Get view properties by calling an exception in case of failure
        /// </summary>
        protected UiObjProps GetViewProps(int viewID) {
            var viewProps = dataAccess.GetUiObjProps(viewID);

            if (viewProps == null) {
                throw new ScadaException("View properties are missing.");
            }

            return viewProps;
        }

        /// <summary>
        /// Download the view from the server
        /// </summary>
        protected bool LoadView(Type viewType, int viewID, DateTime viewAge,
            ref BaseView view, out DateTime newViewAge) {
            var viewProps = GetViewProps(viewID);
            newViewAge = serverComm.ReceiveFileAge(ServerComm.Dirs.Itf, viewProps.Path);

            if (newViewAge == DateTime.MinValue) {
                throw new ScadaException("Unable to receive view file modification time.");
            }

            if (newViewAge == viewAge) return false; // view file changed

            // creating and loading a new view
            if (view == null)
                view = (BaseView) Activator.CreateInstance(viewType);

            if (serverComm.ReceiveView(viewProps.Path, view)) {
                return true;
            }

            throw new ScadaException("Unable to receive view.");
        }


        /// <summary>
        /// Get submission from cache or from server
        /// </summary>
        /// <remarks>The method is used if the presentation type is unknown at the time of compilation.</remarks>
        public BaseView GetView(Type viewType, int viewID, bool throwOnError = false) {
            try {
                if (viewType == null)
                    throw new ArgumentNullException(nameof(viewType));

                // getting submission from cache
                var utcNowDT = DateTime.UtcNow;
                var cacheItem = Cache.GetOrCreateItem(viewID, utcNowDT);

                // block access to only one view
                lock (cacheItem) {
                    BaseView view = null; // presentation you need to get
                    var viewFromCache = cacheItem.Value; // cached view
                    var viewAge = cacheItem.ValueAge; // view file change time
                    DateTime newViewAge; // new view file change time

                    if (viewFromCache == null) {
                        // creating a new view
                        view = (BaseView) Activator.CreateInstance(viewType);

                        if (view.StoredOnServer) {
                            if (LoadView(viewType, viewID, viewAge, ref view, out newViewAge))
                                Cache.UpdateItem(cacheItem, view, newViewAge, utcNowDT);
                        } else {
                            var viewProps = GetViewProps(viewID);
                            view.Path = viewProps.Path;
                            Cache.UpdateItem(cacheItem, view, DateTime.Now, utcNowDT);
                        }
                    } else if (viewFromCache.StoredOnServer) {
                        // performance might be out of date
                        bool viewIsNotValid = utcNowDT - cacheItem.ValueRefrDT > ViewValidSpan;

                        if (viewIsNotValid && LoadView(viewType, viewID, viewAge, ref view, out newViewAge))
                            Cache.UpdateItem(cacheItem, view, newViewAge, utcNowDT);
                    }

                    // using cached views
                    if (view == null && viewFromCache != null) {
                        if (viewFromCache.GetType().Equals(viewType))
                            view = viewFromCache;
                        else
                            throw new ScadaException("View type mismatch.");
                    }

                    // linking channel properties or updating an existing link
                    if (view != null)
                        dataAccess.BindCnlProps(view);

                    return view;
                }
            } catch (Exception ex) {
                string errMsg =
                    $"Error getting view with ID={viewID} from the cache or from the server: {ex.Message}";
                log.WriteException(ex, errMsg);

                if (throwOnError)
                    throw new ScadaException(errMsg);

                return null;
            }
        }

        /// <summary>
        /// Get submission from cache or from server
        /// </summary>
        public T GetView<T>(int viewID, bool throwOnError = false) where T : BaseView {
            return GetView(typeof(T), viewID, throwOnError) as T;
        }

        /// <summary>
        /// Get already loaded view from cache only
        /// </summary>
        public BaseView GetViewFromCache(int viewID, bool throwOnFail = false) {
            try {
                var cacheItem = Cache.GetItem(viewID, DateTime.UtcNow);
                var view = cacheItem?.Value;

                if (view == null && throwOnFail)
                    throw new ScadaException($"The view  with ID={viewID} is not found in the cache");

                return view;
            } catch (Exception ex) {
                string errMsg =
                    $"Error getting view with ID={viewID} from the cache: {ex.Message}";
                log.WriteException(ex, errMsg);

                if (throwOnFail)
                    throw new ScadaException(errMsg);

                return null;
            }
        }
    }
}