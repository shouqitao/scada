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
 * Summary  : Generic cache
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2018
 */

using System;
using System.Collections.Generic;

namespace Scada {
    /// <summary>
    /// Generic cache
    /// <para>Universal Cache</para>
    /// </summary>
    /// <remarks>The class is thread safe
    /// <para>Class is thread safe</para></remarks>
    public class Cache<TKey, TValue> {
        /// <summary>
        /// Cached item
        /// </summary>
        public class CacheItem {
            /// <summary>
            /// Constructor restricting the creation of an object without parameters
            /// </summary>
            protected CacheItem() { }

            /// <summary>
            /// Constructor
            /// </summary>
            protected internal CacheItem(TKey key, TValue value, DateTime valueAge, DateTime nowDT) {
                Key = key;
                Value = value;
                ValueAge = valueAge;
                ValueRefrDT = nowDT;
                AccessDT = nowDT;
            }

            /// <summary>
            /// Get or set the key
            /// </summary>
            public TKey Key { get; set; }

            /// <summary>
            /// Get or set cached value
            /// </summary>
            public TValue Value { get; set; }

            /// <summary>
            /// Get or set the time to change the value in the source
            /// </summary>
            public DateTime ValueAge { get; set; }

            /// <summary>
            /// Get or set the update time in the cache
            /// </summary>
            public DateTime ValueRefrDT { get; set; }

            /// <summary>
            /// Get or set the date and time of the last access to the item
            /// </summary>
            public DateTime AccessDT { get; set; }
        }


        /// <summary>
        /// Cached Items 
        /// </summary>
        /// <remarks>SortedDictionary versus SortedList: 
        /// insertion and deletion of items faster extraction speed is similar</remarks>
        protected SortedDictionary<TKey, CacheItem> items;


        /// <summary>
        /// Constructor restricting the creation of an object without parameters
        /// </summary>
        protected Cache() { }

        /// <summary>
        /// Constructor
        /// </summary>
        public Cache(TimeSpan storePeriod, int capacity) {
            if (capacity < 1)
                throw new ArgumentException("Capacity must be positive.", "capacity");

            items = new SortedDictionary<TKey, CacheItem>();
            StorePeriod = storePeriod;
            Capacity = capacity;
            LastRemoveDT = DateTime.MinValue;
        }


        /// <summary>
        /// Get item retention period since last access
        /// </summary>
        public TimeSpan StorePeriod { get; protected set; }

        /// <summary>
        /// Get a capacity
        /// </summary>
        public int Capacity { get; protected set; }

        /// <summary>
        /// Get the time of the last deletion of obsolete items
        /// </summary>
        public DateTime LastRemoveDT { get; protected set; }


        /// <summary>
        /// Add value to cache
        /// </summary>
        public CacheItem AddValue(TKey key, TValue value) {
            return AddValue(key, value, DateTime.MinValue, DateTime.Now);
        }

        /// <summary>
        /// Add value to cache
        /// </summary>
        public CacheItem AddValue(TKey key, TValue value, DateTime valueAge) {
            return AddValue(key, value, valueAge, DateTime.Now);
        }

        /// <summary>
        /// Add value to cache
        /// </summary>
        public CacheItem AddValue(TKey key, TValue value, DateTime valueAge, DateTime nowDT) {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            lock (this) {
                var cacheItem = new CacheItem(key, value, valueAge, nowDT);
                items.Add(key, cacheItem);
                return cacheItem;
            }
        }


        /// <summary>
        /// Retrieve item by key, updating access time
        /// </summary>
        public CacheItem GetItem(TKey key) {
            return GetItem(key, DateTime.Now);
        }

        /// <summary>
        /// Retrieve item by key, updating access time
        /// </summary>
        public CacheItem GetItem(TKey key, DateTime nowDT) {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            lock (this) {
                // getting the requested item
                if (items.TryGetValue(key, out var item))
                    item.AccessDT = nowDT;

                // automatic cleaning of obsolete items
                if (nowDT - LastRemoveDT > StorePeriod)
                    RemoveOutdatedItems(nowDT);

                return item;
            }
        }

        /// <summary>
        /// Get the item by key, updating the access time,
        /// or create a new empty item if the key is not in the cache
        /// </summary>
        public CacheItem GetOrCreateItem(TKey key, DateTime nowDT) {
            lock (this) {
                var cacheItem = GetItem(key, nowDT);
                if (cacheItem == null)
                    cacheItem = AddValue(key, default(TValue), DateTime.MinValue, nowDT);
                return cacheItem;
            }
        }

        /// <summary>
        /// Get all items to view without updating access time
        /// </summary>
        public CacheItem[] GetAllItemsForWatching() {
            lock (this) {
                var itemsCopy = new CacheItem[items.Count];
                var i = 0;
                foreach (var item in items.Values)
                    itemsCopy[i++] = item;
                return itemsCopy;
            }
        }


        /// <summary>
        /// Stream safe update element properties
        /// </summary>
        public void UpdateItem(CacheItem cacheItem, TValue value) {
            UpdateItem(cacheItem, value, DateTime.MinValue, DateTime.Now);
        }

        /// <summary>
        /// Stream safe update element properties
        /// </summary>
        public void UpdateItem(CacheItem cacheItem, TValue value, DateTime valueAge) {
            UpdateItem(cacheItem, value, valueAge, DateTime.Now);
        }

        /// <summary>
        /// Stream safe update element properties
        /// </summary>
        public void UpdateItem(CacheItem cacheItem, TValue value, DateTime valueAge, DateTime nowDT) {
            if (cacheItem == null)
                throw new ArgumentNullException(nameof(cacheItem));

            lock (this) {
                cacheItem.Value = value;
                cacheItem.ValueAge = valueAge;
                cacheItem.ValueRefrDT = nowDT;
            }
        }


        /// <summary>
        /// Remove obsolete items
        /// </summary>
        public void RemoveOutdatedItems() {
            RemoveOutdatedItems(DateTime.Now);
        }

        /// <summary>
        /// Remove obsolete items
        /// </summary>
        public void RemoveOutdatedItems(DateTime nowDT) {
            lock (this) {
                // delete items by last access time
                var keysToRemove = new List<TKey>();

                foreach (KeyValuePair<TKey, CacheItem> pair in items) {
                    if (nowDT - pair.Value.AccessDT > StorePeriod)
                        keysToRemove.Add(pair.Key);
                }

                foreach (var key in keysToRemove)
                    items.Remove(key);

                // removal of elements if capacity is exceeded, taking into account access time
                int itemsCnt = items.Count;

                if (itemsCnt > Capacity) {
                    var keys = new TKey[itemsCnt];
                    var accessDTs = new DateTime[itemsCnt];
                    var i = 0;

                    foreach (KeyValuePair<TKey, CacheItem> pair in items) {
                        keys[i] = pair.Key;
                        accessDTs[i] = pair.Value.AccessDT;
                        i++;
                    }

                    Array.Sort(accessDTs, keys);
                    int delCnt = itemsCnt - Capacity;

                    for (var j = 0; j < delCnt; j++)
                        items.Remove(keys[j]);
                }

                LastRemoveDT = nowDT;
            }
        }

        /// <summary>
        /// Delete item by key
        /// </summary>
        public void RemoveItem(TKey key) {
            lock (this) {
                items.Remove(key);
            }
        }

        /// <summary>
        /// Clear cache
        /// </summary>
        public void Clear() {
            lock (this) {
                items.Clear();
            }
        }
    }
}