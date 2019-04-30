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
 * Module   : Administrator
 * Summary  : Creates columns for a DataGridView control
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using Scada.Admin.Project;
using Scada.Data.Entities;
using Scada.Data.Tables;
using System;
using System.Data;
using System.Windows.Forms;

namespace Scada.Admin.App.Code {
    /// <summary>
    /// Creates columns for a DataGridView control
    /// <para>Creates columns for a DataGridView control.</para>
    /// </summary>
    internal class ColumnBuilder {
        private readonly ConfigBase configBase; // the reference to the configuration database


        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <remarks>The configuration database is required for creating combo box columns.</remarks>
        public ColumnBuilder(ConfigBase configBase) {
            this.configBase = configBase ?? throw new ArgumentNullException(nameof(configBase));
        }


        /// <summary>
        /// Creates a new column that hosts text cells.
        /// </summary>
        private DataGridViewColumn NewTextBoxColumn(string dataPropertyName) {
            return new DataGridViewTextBoxColumn {
                Name = dataPropertyName,
                HeaderText = dataPropertyName,
                DataPropertyName = dataPropertyName
            };
        }

        /// <summary>
        /// Creates a new column that hosts cells with checkboxes.
        /// </summary>
        private DataGridViewColumn NewCheckBoxColumn(string dataPropertyName) {
            return new DataGridViewCheckBoxColumn {
                Name = dataPropertyName,
                HeaderText = dataPropertyName,
                DataPropertyName = dataPropertyName,
                SortMode = DataGridViewColumnSortMode.Automatic
            };
        }

        /// <summary>
        /// Creates a new column that hosts cells which values are selected from a combo box.
        /// </summary>
        private DataGridViewColumn NewComboBoxColumn(
            string dataPropertyName, string valueMember, string displayMember, object dataSource) {
            return ScadaUtils.IsRunningOnMono
                ? NewTextBoxColumn(dataPropertyName) /*because of the bugs in Mono*/
                : new DataGridViewComboBoxColumn {
                    Name = dataPropertyName,
                    HeaderText = dataPropertyName,
                    DataPropertyName = dataPropertyName,
                    ValueMember = valueMember,
                    DisplayMember = displayMember,
                    DataSource = dataSource,
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DisplayStyleForCurrentCellOnly = true
                };
        }

        /// <summary>
        /// Creates a new column that hosts cells which values are selected from a combo box.
        /// </summary>
        private DataGridViewColumn NewComboBoxColumn<T>(
            string dataPropertyName, string displayMember, BaseTable<T> dataSource, bool addEmptyRow = false) {
            return NewComboBoxColumn(dataPropertyName, dataPropertyName, displayMember,
                CreateComboBoxSource(dataSource, dataPropertyName, displayMember, addEmptyRow));
        }

        /// <summary>
        /// Creates a data table for using as a data source of a combo box.
        /// </summary>
        private DataTable CreateComboBoxSource<T>(
            BaseTable<T> baseTable, string valueMember, string displayMember, bool addEmptyRow) {
            DataTable dataTable = baseTable.ToDataTable(true);

            if (addEmptyRow) {
                DataRow emptyRow = dataTable.NewRow();
                emptyRow[valueMember] = DBNull.Value;
                emptyRow[displayMember] = " ";
                dataTable.Rows.Add(emptyRow);
            }

            dataTable.DefaultView.Sort = displayMember;

            return dataTable;
        }

        /// <summary>
        /// Translates the column headers.
        /// </summary>
        private DataGridViewColumn[] TranslateHeaders(string tableName, DataGridViewColumn[] columns) {
            if (Localization.Dictionaries.TryGetValue("Scada.Admin.App.Code.ColumnBuilder." + tableName,
                out Localization.Dict dict)) {
                foreach (DataGridViewColumn col in columns) {
                    if (dict.Phrases.TryGetValue(col.Name, out string header))
                        col.HeaderText = header;
                }
            }

            return columns;
        }


        /// <summary>
        /// Creates columns for the command type table.
        /// </summary>
        private DataGridViewColumn[] CreateCmdTypeTableColumns() {
            return TranslateHeaders("CmdTypeTable",
                new DataGridViewColumn[] {
                    NewTextBoxColumn("CmdTypeID"), NewTextBoxColumn("Name"), NewTextBoxColumn("Descr")
                });
        }

        /// <summary>
        /// Creates columns for the command value table.
        /// </summary>
        private DataGridViewColumn[] CreateCmdValTableColumns() {
            return TranslateHeaders("CmdValTable",
                new DataGridViewColumn[] {
                    NewTextBoxColumn("CmdValID"), NewTextBoxColumn("Name"), NewTextBoxColumn("Val"),
                    NewTextBoxColumn("Descr")
                });
        }

        /// <summary>
        /// Creates columns for the input channel type table.
        /// </summary>
        private DataGridViewColumn[] CreateCnlTypeTableColumns() {
            return TranslateHeaders("CnlTypeTable",
                new DataGridViewColumn[] {
                    NewTextBoxColumn("CnlTypeID"), NewTextBoxColumn("Name"), NewTextBoxColumn("ShtName"),
                    NewTextBoxColumn("Descr")
                });
        }

        /// <summary>
        /// Creates columns for the communication line table.
        /// </summary>
        private DataGridViewColumn[] CreateCommLineTableColumns() {
            return TranslateHeaders("CommLineTable",
                new DataGridViewColumn[] {
                    NewTextBoxColumn("CommLineNum"), NewTextBoxColumn("Name"), NewTextBoxColumn("Descr")
                });
        }

        /// <summary>
        /// Creates columns for the output channel table.
        /// </summary>
        private DataGridViewColumn[] CreateCtrlCnlTableColumns() {
            return TranslateHeaders("CtrlCnlTable",
                new DataGridViewColumn[] {
                    NewTextBoxColumn("CtrlCnlNum"), NewCheckBoxColumn("Active"), NewTextBoxColumn("Name"),
                    NewComboBoxColumn("CmdTypeID", "Name", configBase.CmdTypeTable),
                    NewComboBoxColumn("ObjNum", "Name", configBase.ObjTable, true),
                    NewComboBoxColumn("KPNum", "Name", configBase.KPTable, true), NewTextBoxColumn("CmdNum"),
                    NewComboBoxColumn("CmdValID", "Name", configBase.CmdValTable, true),
                    NewCheckBoxColumn("FormulaUsed"), NewTextBoxColumn("Formula"), NewCheckBoxColumn("EvEnabled")
                });
        }

        /// <summary>
        /// Creates columns for the event type table.
        /// </summary>
        private DataGridViewColumn[] CreateEvTypeTableColumns() {
            return TranslateHeaders("EvTypeTable",
                new DataGridViewColumn[] {
                    NewTextBoxColumn("CnlStatus"), NewTextBoxColumn("Name"), NewTextBoxColumn("Color"),
                    NewTextBoxColumn("Descr")
                });
        }

        /// <summary>
        /// Creates columns for the format table.
        /// </summary>
        private DataGridViewColumn[] CreateFormatTableColumns() {
            return TranslateHeaders("FormatTable",
                new DataGridViewColumn[] {
                    NewTextBoxColumn("FormatID"), NewTextBoxColumn("Name"), NewCheckBoxColumn("ShowNumber"),
                    NewTextBoxColumn("DecDigits")
                });
        }

        /// <summary>
        /// Creates columns for the formula table.
        /// </summary>
        private DataGridViewColumn[] CreateFormulaTableColumns() {
            return TranslateHeaders("FormulaTable",
                new DataGridViewColumn[] {
                    NewTextBoxColumn("FormulaID"), NewTextBoxColumn("Name"), NewTextBoxColumn("Source"),
                    NewTextBoxColumn("Descr")
                });
        }

        /// <summary>
        /// Creates columns for the input channel table.
        /// </summary>
        private DataGridViewColumn[] CreateInCnlTableColumns() {
            return TranslateHeaders("InCnlTable",
                new DataGridViewColumn[] {
                    NewTextBoxColumn("CnlNum"), NewCheckBoxColumn("Active"), NewTextBoxColumn("Name"),
                    NewComboBoxColumn("CnlTypeID", "Name", configBase.CnlTypeTable),
                    NewComboBoxColumn("ObjNum", "Name", configBase.ObjTable, true),
                    NewComboBoxColumn("KPNum", "Name", configBase.KPTable, true), NewTextBoxColumn("Signal"),
                    NewCheckBoxColumn("FormulaUsed"), NewTextBoxColumn("Formula"), NewCheckBoxColumn("Averaging"),
                    NewComboBoxColumn("ParamID", "Name", configBase.ParamTable, true),
                    NewComboBoxColumn("FormatID", "Name", configBase.FormatTable, true),
                    NewComboBoxColumn("UnitID", "Name", configBase.UnitTable, true), NewTextBoxColumn("CtrlCnlNum"),
                    NewCheckBoxColumn("EvEnabled"), NewCheckBoxColumn("EvSound"), NewCheckBoxColumn("EvOnChange"),
                    NewCheckBoxColumn("EvOnUndef"), NewTextBoxColumn("LimLowCrash"), NewTextBoxColumn("LimLow"),
                    NewTextBoxColumn("LimHigh"), NewTextBoxColumn("LimHighCrash")
                });
        }

        /// <summary>
        /// Creates columns for the interface table.
        /// </summary>
        private DataGridViewColumn[] CreateInterfaceTableColumns() {
            return TranslateHeaders("InterfaceTable",
                new DataGridViewColumn[] {
                    NewTextBoxColumn("ItfID"), NewTextBoxColumn("Name"), NewTextBoxColumn("Descr")
                });
        }

        /// <summary>
        /// Creates columns for the devices table.
        /// </summary>
        private DataGridViewColumn[] CreateKPTableColumns() {
            return TranslateHeaders("KPTable",
                new DataGridViewColumn[] {
                    NewTextBoxColumn("KPNum"), NewTextBoxColumn("Name"),
                    NewComboBoxColumn("KPTypeID", "Name", configBase.KPTypeTable), NewTextBoxColumn("Address"),
                    NewTextBoxColumn("CallNum"),
                    NewComboBoxColumn("CommLineNum", "Name", configBase.CommLineTable, true), NewTextBoxColumn("Descr")
                });
        }

        /// <summary>
        /// Creates columns for the device type table.
        /// </summary>
        private DataGridViewColumn[] CreateKPTypeTableColumns() {
            return TranslateHeaders("KPTypeTable",
                new DataGridViewColumn[] {
                    NewTextBoxColumn("KPTypeID"), NewTextBoxColumn("Name"), NewTextBoxColumn("DllFileName"),
                    NewTextBoxColumn("Descr")
                });
        }

        /// <summary>
        /// Creates columns for the object table.
        /// </summary>
        private DataGridViewColumn[] CreateObjTableColumns() {
            return TranslateHeaders("ObjTable",
                new DataGridViewColumn[] {
                    NewTextBoxColumn("ObjNum"), NewTextBoxColumn("Name"), NewTextBoxColumn("Descr")
                });
        }

        /// <summary>
        /// Creates columns for the quantity table.
        /// </summary>
        private DataGridViewColumn[] CreateParamTableColumns() {
            return TranslateHeaders("ParamTable",
                new DataGridViewColumn[] {
                    NewTextBoxColumn("ParamID"), NewTextBoxColumn("Name"), NewTextBoxColumn("Sign"),
                    NewTextBoxColumn("IconFileName")
                });
        }

        /// <summary>
        /// Creates columns for the right table.
        /// </summary>
        private DataGridViewColumn[] CreateRightTableColumns() {
            return TranslateHeaders("RightTable",
                new DataGridViewColumn[] {
                    NewTextBoxColumn("RightID"), NewComboBoxColumn("ItfID", "Name", configBase.InterfaceTable),
                    NewComboBoxColumn("RoleID", "Name", configBase.RoleTable), NewCheckBoxColumn("ViewRight"),
                    NewCheckBoxColumn("CtrlRight")
                });
        }

        /// <summary>
        /// Creates columns for the role table.
        /// </summary>
        private DataGridViewColumn[] CreateRoleTableColumns() {
            return TranslateHeaders("RoleTable",
                new DataGridViewColumn[] {
                    NewTextBoxColumn("RoleID"), NewTextBoxColumn("Name"), NewTextBoxColumn("Descr")
                });
        }

        /// <summary>
        /// Creates columns for the unit table.
        /// </summary>
        private DataGridViewColumn[] CreateUnitTableColumns() {
            return TranslateHeaders("UnitTable",
                new DataGridViewColumn[] {
                    NewTextBoxColumn("UnitID"), NewTextBoxColumn("Name"), NewTextBoxColumn("Sign"),
                    NewTextBoxColumn("Descr")
                });
        }

        /// <summary>
        /// Creates columns for the user table.
        /// </summary>
        private DataGridViewColumn[] CreateUserTableColumns() {
            return TranslateHeaders("UserTable",
                new DataGridViewColumn[] {
                    NewTextBoxColumn("UserID"), NewTextBoxColumn("Name"), NewTextBoxColumn("Password"),
                    NewComboBoxColumn("RoleID", "Name", configBase.RoleTable), NewTextBoxColumn("Descr")
                });
        }


        /// <summary>
        /// Creates columns for the specified table
        /// </summary>
        public DataGridViewColumn[] CreateColumns(Type itemType) {
            if (itemType == typeof(CmdType))
                return CreateCmdTypeTableColumns();
            else if (itemType == typeof(CmdVal))
                return CreateCmdValTableColumns();
            else if (itemType == typeof(CnlType))
                return CreateCnlTypeTableColumns();
            else if (itemType == typeof(CommLine))
                return CreateCommLineTableColumns();
            else if (itemType == typeof(CtrlCnl))
                return CreateCtrlCnlTableColumns();
            else if (itemType == typeof(EvType))
                return CreateEvTypeTableColumns();
            else if (itemType == typeof(Format))
                return CreateFormatTableColumns();
            else if (itemType == typeof(Formula))
                return CreateFormulaTableColumns();
            else if (itemType == typeof(InCnl))
                return CreateInCnlTableColumns();
            else if (itemType == typeof(Data.Entities.Interface))
                return CreateInterfaceTableColumns();
            else if (itemType == typeof(KP))
                return CreateKPTableColumns();
            else if (itemType == typeof(KPType))
                return CreateKPTypeTableColumns();
            else if (itemType == typeof(Obj))
                return CreateObjTableColumns();
            else if (itemType == typeof(Param))
                return CreateParamTableColumns();
            else if (itemType == typeof(Right))
                return CreateRightTableColumns();
            else if (itemType == typeof(Role))
                return CreateRoleTableColumns();
            else if (itemType == typeof(Unit))
                return CreateUnitTableColumns();
            else if (itemType == typeof(User))
                return CreateUserTableColumns();
            else
                return new DataGridViewColumn[0];
        }
    }
}