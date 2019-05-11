﻿/*
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
 * Summary  : Represents the table of the configuration database
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Scada.Data.Tables {
    /// <inheritdoc />
    /// <summary>
    /// Represents the table of the configuration database.
    /// <para>Represents the configuration database table.</para>
    /// </summary>
    public class BaseTable<T> : IBaseTable {
        /// <summary>
        /// The primary key of the table.
        /// </summary>
        protected string primaryKey;

        /// <summary>
        /// The property that is a primary key.
        /// </summary>
        protected PropertyDescriptor primaryKeyProp;


        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public BaseTable(string name, string primaryKey, string title) {
            Name = name;
            PrimaryKey = primaryKey;
            Title = title;
            Items = new SortedDictionary<int, T>();
            Modified = false;
        }


        /// <inheritdoc />
        /// <summary>
        /// Gets the table name.
        /// </summary>
        public string Name { get; protected set; }

        /// <inheritdoc />
        /// <summary>
        /// Gets the primary key of the table.
        /// </summary>
        public string PrimaryKey {
            get { return primaryKey; }
            protected set {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("The primary key can not be empty.");

                var prop = TypeDescriptor.GetProperties(ItemType)[value];

                if (prop == null)
                    throw new ArgumentException("The primary key property not found.");

                if (prop.PropertyType != typeof(int))
                    throw new ArgumentException("The primary key must be an integer.");

                primaryKey = value;
                primaryKeyProp = prop;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets or sets the table title.
        /// </summary>
        public string Title { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// Gets the short file name of the table.
        /// </summary>
        public string FileName {
            get { return Name + ".xml"; }
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets the type of the table items.
        /// </summary>
        public Type ItemType {
            get { return typeof(T); }
        }

        /// <summary>
        /// Gets the table items sorted by primary key.
        /// </summary>
        public SortedDictionary<int, T> Items { get; protected set; }

        /// <inheritdoc />
        /// <summary>
        /// Gets or sets a value indicating whether the table was modified.
        /// </summary>
        public bool Modified { get; set; }


        /// <summary>
        /// Gets the primary key value of the item.
        /// </summary>
        protected int GetPkValue(T item) {
            return (int) primaryKeyProp.GetValue(item);
        }


        /// <summary>
        /// Adds or updates an item in the table.
        /// </summary>
        public void AddItem(T item) {
            Items[GetPkValue(item)] = item;
        }

        /// <inheritdoc />
        /// <summary>
        /// Adds or updates an item in the table.
        /// </summary>
        public void AddObject(object obj) {
            if (obj is T item)
                AddItem(item);
        }

        /// <inheritdoc />
        /// <summary>
        /// Returns an enumerable collection of the table items.
        /// </summary>
        public IEnumerable<object> EnumerateItems() {
            foreach (var item in Items.Values) {
                yield return item;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Loads the table from the specified file.
        /// </summary>
        public void Load(string fileName) {
            Items.Clear();
            Modified = false;

            List<T> list;
            var serializer = new XmlSerializer(typeof(List<T>));

            using (var reader = XmlReader.Create(fileName)) {
                list = (List<T>) serializer.Deserialize(reader);
            }

            foreach (var item in list) {
                Items.Add(GetPkValue(item), item);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Saves the table to the specified file.
        /// </summary>
        public void Save(string fileName) {
            var list = new List<T>(Items.Values);
            var serializer = new XmlSerializer(list.GetType());
            var writerSettings = new XmlWriterSettings() {Indent = true};

            using (var writer = XmlWriter.Create(fileName, writerSettings)) {
                serializer.Serialize(writer, list);
            }

            Modified = false;
        }
    }
}