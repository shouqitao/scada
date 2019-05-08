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
        /// <remarks>Метод используется, если тип предсталения неизвестен на момент компиляции</remarks>
        public BaseView GetView(Type viewType, int viewID, bool throwOnError = false) {
            try {
                if (viewType == null)
                    throw new ArgumentNullException("viewType");

                // получение представления из кэша
                var utcNowDT = DateTime.UtcNow;
                var cacheItem = Cache.GetOrCreateItem(viewID, utcNowDT);

                // блокировка доступа только к одному представлению
                lock (cacheItem) {
                    BaseView view = null; // представление, которое необходимо получить
                    var viewFromCache = cacheItem.Value; // представление из кэша
                    var viewAge = cacheItem.ValueAge; // время изменения файла представления
                    DateTime newViewAge; // новое время изменения файла представления

                    if (viewFromCache == null) {
                        // создание нового представления
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
                        // представление могло устареть
                        bool viewIsNotValid = utcNowDT - cacheItem.ValueRefrDT > ViewValidSpan;

                        if (viewIsNotValid && LoadView(viewType, viewID, viewAge, ref view, out newViewAge))
                            Cache.UpdateItem(cacheItem, view, newViewAge, utcNowDT);
                    }

                    // использование представления из кэша
                    if (view == null && viewFromCache != null) {
                        if (viewFromCache.GetType().Equals(viewType))
                            view = viewFromCache;
                        else
                            throw new ScadaException(Localization.UseRussian
                                ? "Несоответствие типа представления."
                                : "View type mismatch.");
                    }

                    // привязка свойств каналов или обновление существующей привязки
                    if (view != null)
                        dataAccess.BindCnlProps(view);

                    return view;
                }
            } catch (Exception ex) {
                string errMsg =
                    string.Format(
                        Localization.UseRussian
                            ? "Ошибка при получении представления с ид.={0} из кэша или от сервера: {1}"
                            : "Error getting view with ID={0} from the cache or from the server: {1}", viewID,
                        ex.Message);
                log.WriteException(ex, errMsg);

                if (throwOnError)
                    throw new ScadaException(errMsg);
                else
                    return null;
            }
        }

        /// <summary>
        /// Получить представление из кэша или от сервера
        /// </summary>
        public T GetView<T>(int viewID, bool throwOnError = false) where T : BaseView {
            return GetView(typeof(T), viewID, throwOnError) as T;
        }

        /// <summary>
        /// Получить уже загруженное представление только из кэша
        /// </summary>
        public BaseView GetViewFromCache(int viewID, bool throwOnFail = false) {
            try {
                var cacheItem = Cache.GetItem(viewID, DateTime.UtcNow);
                var view = cacheItem == null ? null : cacheItem.Value;

                if (view == null && throwOnFail)
                    throw new ScadaException(string.Format(
                        Localization.UseRussian
                            ? "Представление не найдено в кэше"
                            : "The view is not found in the cache", viewID));

                return view;
            } catch (Exception ex) {
                string errMsg =
                    string.Format(
                        Localization.UseRussian
                            ? "Ошибка при получении представления с ид.={0} из кэша: {1}"
                            : "Error getting view with ID={0} from the cache: {1}", viewID, ex.Message);
                log.WriteException(ex, errMsg);

                if (throwOnFail)
                    throw new ScadaException(errMsg);
                else
                    return null;
            }
        }
    }
}