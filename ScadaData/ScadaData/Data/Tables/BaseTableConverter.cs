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
 * Summary  : Provides data exchange between instances of BaseTable and DataTable
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using System;
using System.ComponentModel;
using System.Data;

namespace Scada.Data.Tables {
    /// <summary>
    /// Provides data exchange between instances of BaseTable and DataTable.
    /// <para>Provides data exchange between BaseTable and DataTable instances.</para>
    /// </summary>
    public static class TableConverter {
        /// <summary>
        /// Creates a new item getting values from the row.
        /// </summary>
        private static T CreateItem<T>(DataRow row, PropertyDescriptorCollection props) {
            return (T) CreateItem(typeof(T), row, props);
        }


        /// <summary>
        /// Creates a new item getting values from the row.
        /// </summary>
        public static object CreateItem(Type itemType, DataRow row, PropertyDescriptorCollection props) {
            var item = Activator.CreateInstance(itemType);

            foreach (PropertyDescriptor prop in props) {
                try {
                    var value = row[prop.Name];
                    if (value != DBNull.Value)
                        prop.SetValue(item, value);
                } catch (ArgumentException) {
                    // column not found
                }
            }

            return item;
        }

        /// <summary>
        /// Converts the BaseTable to a DataTable.
        /// </summary>
        public static DataTable ToDataTable<T>(this BaseTable<T> baseTable, bool allowNull = false) {
            // create columns
            var props = TypeDescriptor.GetProperties(typeof(T));
            var dataTable = new DataTable();

            foreach (PropertyDescriptor prop in props) {
                bool isNullable = prop.PropertyType.IsNullable();
                bool isClass = prop.PropertyType.IsClass;

                dataTable.Columns.Add(new DataColumn() {
                    ColumnName = prop.Name,
                    DataType = isNullable ? Nullable.GetUnderlyingType(prop.PropertyType) : prop.PropertyType,
                    AllowDBNull = isNullable || isClass || allowNull
                });
            }

            // copy data
            int propCnt = props.Count;
            var values = new object[propCnt];
            dataTable.BeginLoadData();

            try {
                foreach (var item in baseTable.Items.Values) {
                    for (var i = 0; i < propCnt; i++) {
                        values[i] = props[i].GetValue(item) ?? DBNull.Value;
                    }

                    dataTable.Rows.Add(values);
                }
            } finally {
                dataTable.EndLoadData();
                dataTable.AcceptChanges();
            }

            return dataTable;
        }

        /// <summary>
        /// Copies the changes from the DataTable to the BaseTable.
        /// </summary>
        public static void RetrieveChanges<T>(this BaseTable<T> baseTable, DataTable dataTable) {
            // delete rows from the target table
            DataRow[] deletedRows = dataTable.Select("", "", DataViewRowState.Deleted);

            foreach (var row in deletedRows) {
                var key = (int) row[baseTable.PrimaryKey];
                baseTable.Items.Remove(key);
                row.AcceptChanges();
            }

            // change rows in the target table
            var props = TypeDescriptor.GetProperties(typeof(T));
            DataRow[] modifiedRows = dataTable.Select("", "", DataViewRowState.ModifiedCurrent);

            foreach (var row in modifiedRows) {
                var item = CreateItem<T>(row, props);
                var origKey = (int) row[baseTable.PrimaryKey, DataRowVersion.Original];
                var curKey = (int) row[baseTable.PrimaryKey, DataRowVersion.Current];

                if (origKey != curKey)
                    baseTable.Items.Remove(origKey);

                baseTable.AddItem(item);
                row.AcceptChanges();
            }

            // add rows to the target table
            DataRow[] addedRows = dataTable.Select("", "", DataViewRowState.Added);

            foreach (var row in addedRows) {
                var item = CreateItem<T>(row, props);
                baseTable.AddItem(item);
                row.AcceptChanges();
            }
        }
    }
}