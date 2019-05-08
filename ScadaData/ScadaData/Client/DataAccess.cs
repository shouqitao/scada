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
 * Summary  : Handy and thread safe access to the client cache data
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2016
 */

using Scada.Data.Configuration;
using Scada.Data.Models;
using Scada.Data.Tables;
using System;
using System.Collections.Generic;
using System.Data;
using Utils;

namespace Scada.Client {
    /// <summary>
    /// Handy and thread safe access to the client cache data
    /// <para>Convenient and thread-safe access to client cache data</para>
    /// </summary>
    public class DataAccess {
        /// <summary>
        /// Data cache
        /// </summary>
        protected readonly DataCache dataCache;

        /// <summary>
        /// Log
        /// </summary>
        protected readonly Log log;


        /// <summary>
        /// Constructor restricting the creation of an object without parameters
        /// </summary>
        protected DataAccess() { }

        /// <summary>
        /// Constructor
        /// </summary>
        public DataAccess(DataCache dataCache, Log log) {
            this.dataCache = dataCache ?? throw new ArgumentNullException(nameof(dataCache));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }


        /// <summary>
        /// Get data cache
        /// </summary>
        public DataCache DataCache {
            get { return dataCache; }
        }


        /// <summary>
        /// Get the name of the role by ID from the configuration database
        /// </summary>
        protected string GetRoleNameFromBase(int roleID, string defaultRoleName) {
            try {
                dataCache.RefreshBaseTables();
                var baseTables = dataCache.BaseTables;

                lock (baseTables.SyncRoot) {
                    BaseTables.CheckColumnsExist(baseTables.RoleTable, true);
                    var viewRole = baseTables.RoleTable.DefaultView;
                    viewRole.Sort = "RoleID";
                    int rowInd = viewRole.Find(roleID);
                    return rowInd >= 0 ? (string) viewRole[rowInd]["Name"] : defaultRoleName;
                }
            } catch (Exception ex) {
                log.WriteException(ex, "Error getting role name by ID {0}", roleID);
                return defaultRoleName;
            }
        }


        /// <summary>
        /// Get the properties of the input channel by its number
        /// </summary>
        public InCnlProps GetCnlProps(int cnlNum) {
            try {
                if (cnlNum <= 0) return null;

                dataCache.RefreshBaseTables();

                // you must save the link, because the object can be recreated by another thread
                InCnlProps[] cnlProps = dataCache.CnlProps;

                // search for properties of a given channel
                int ind = Array.BinarySearch(cnlProps, cnlNum, InCnlProps.IntComp);
                return ind >= 0 ? cnlProps[ind] : null;
            } catch (Exception ex) {
                log.WriteException(ex, "Error getting input channel {0} properties", cnlNum);
                return null;
            }
        }

        /// <summary>
        /// Get control channel properties by its number
        /// </summary>
        public CtrlCnlProps GetCtrlCnlProps(int ctrlCnlNum) {
            try {
                dataCache.RefreshBaseTables();

                // you must save the link, because the object can be recreated by another thread
                CtrlCnlProps[] ctrlCnlProps = dataCache.CtrlCnlProps;

                // search for properties of a given channel
                int ind = Array.BinarySearch(ctrlCnlProps, ctrlCnlNum, CtrlCnlProps.IntComp);
                return ind >= 0 ? ctrlCnlProps[ind] : null;
            } catch (Exception ex) {
                log.WriteException(ex, "Error getting output channel {0} properties", ctrlCnlNum);
                return null;
            }
        }

        /// <summary>
        /// Get input channel status properties by status value
        /// </summary>
        public CnlStatProps GetCnlStatProps(int stat) {
            try {
                dataCache.RefreshBaseTables();
                CnlStatProps cnlStatProps;
                return dataCache.CnlStatProps.TryGetValue(stat, out cnlStatProps) ? cnlStatProps : null;
            } catch (Exception ex) {
                log.WriteException(ex,
                    Localization.UseRussian
                        ? "Ошибка при получении цвета по статусу {0}"
                        : "Error getting color by status {0}", stat);
                return null;
            }
        }

        /// <summary>
        /// Bind properties of input channels and control channels to view elements
        /// </summary>
        public void BindCnlProps(BaseView view) {
            try {
                dataCache.RefreshBaseTables();
                var baseAge = dataCache.BaseTables.BaseAge;
                if (view != null && view.BaseAge != baseAge && baseAge > DateTime.MinValue) {
                    lock (view.SyncRoot) {
                        view.BaseAge = baseAge;
                        view.BindCnlProps(dataCache.CnlProps);
                        view.BindCtrlCnlProps(dataCache.CtrlCnlProps);
                    }
                }
            } catch (Exception ex) {
                log.WriteException(ex, "Error binding channel properties to the view elements");
            }
        }

        /// <summary>
        /// Get user interface object properties by id
        /// </summary>
        public UiObjProps GetUiObjProps(int uiObjID) {
            try {
                dataCache.RefreshBaseTables();

                // you must save the link, because the object can be recreated by another thread
                var baseTables = dataCache.BaseTables;

                lock (baseTables.SyncRoot) {
                    BaseTables.CheckColumnsExist(baseTables.InterfaceTable, true);
                    var viewInterface = baseTables.InterfaceTable.DefaultView;
                    viewInterface.Sort = "ItfID";
                    int rowInd = viewInterface.Find(uiObjID);

                    if (rowInd >= 0) {
                        var rowView = viewInterface[rowInd];
                        var uiObjProps = UiObjProps.Parse((string) rowView["Name"]);
                        uiObjProps.UiObjID = uiObjID;
                        uiObjProps.Title = (string) rowView["Descr"];
                        return uiObjProps;
                    }

                    return null;
                }
            } catch (Exception ex) {
                log.WriteException(ex, "Error getting user interface object properties by ID={0}", uiObjID);
                return null;
            }
        }

        /// <summary>
        /// Get a list of user interface object properties
        /// </summary>
        public List<UiObjProps> GetUiObjPropsList(UiObjProps.BaseUiTypes baseUiTypes) {
            var list = new List<UiObjProps>();

            try {
                dataCache.RefreshBaseTables();
                var baseTables = dataCache.BaseTables;

                lock (baseTables.SyncRoot) {
                    BaseTables.CheckColumnsExist(baseTables.InterfaceTable, true);
                    var viewInterface = baseTables.InterfaceTable.DefaultView;
                    viewInterface.Sort = "ItfID";

                    foreach (DataRowView rowView in viewInterface) {
                        var uiObjProps = UiObjProps.Parse((string) rowView["Name"]);
                        if (baseUiTypes.HasFlag(uiObjProps.BaseUiType)) {
                            uiObjProps.UiObjID = (int) rowView["ItfID"];
                            uiObjProps.Title = (string) rowView["Descr"];
                            list.Add(uiObjProps);
                        }
                    }
                }
            } catch (Exception ex) {
                log.WriteException(ex,
                    "Error getting list of user interface object properties");
            }

            return list;
        }

        /// <summary>
        /// Get user interface rights by role id
        /// </summary>
        public Dictionary<int, EntityRights> GetUiObjRights(int roleID) {
            var rightsDict = new Dictionary<int, EntityRights>();

            try {
                dataCache.RefreshBaseTables();
                var baseTables = dataCache.BaseTables;

                lock (baseTables.SyncRoot) {
                    BaseTables.CheckColumnsExist(baseTables.RightTable, true);
                    var viewRight = baseTables.RightTable.DefaultView;
                    viewRight.Sort = "RoleID";

                    foreach (var rowView in viewRight.FindRows(roleID)) {
                        var uiObjID = (int) rowView["ItfID"];
                        var rights =
                            new EntityRights((bool) rowView["ViewRight"], (bool) rowView["CtrlRight"]);
                        rightsDict[uiObjID] = rights;
                    }
                }
            } catch (Exception ex) {
                log.WriteException(ex,
                    Localization.UseRussian
                        ? "Ошибка при получении прав на объекты пользовательского интерфейса для роли с ид.={0}"
                        : "Error getting access rights on user interface objects for the role with ID={0}", roleID);
            }

            return rightsDict;
        }

        /// <summary>
        /// Get username by id
        /// </summary>
        public string GetUserName(int userID) {
            try {
                dataCache.RefreshBaseTables();
                var baseTables = dataCache.BaseTables;

                lock (baseTables.SyncRoot) {
                    BaseTables.CheckColumnsExist(baseTables.UserTable, true);
                    var viewUser = baseTables.UserTable.DefaultView;
                    viewUser.Sort = "UserID";
                    int rowInd = viewUser.Find(userID);
                    return rowInd >= 0 ? (string) viewUser[rowInd]["Name"] : "";
                }
            } catch (Exception ex) {
                log.WriteException(ex, "Error getting user name by ID={0}", userID);
                return null;
            }
        }

        /// <summary>
        /// Get user properties by ID
        /// </summary>
        public UserProps GetUserProps(int userID) {
            try {
                dataCache.RefreshBaseTables();
                var baseTables = dataCache.BaseTables;

                lock (baseTables.SyncRoot) {
                    BaseTables.CheckColumnsExist(baseTables.UserTable, true);
                    var viewUser = baseTables.UserTable.DefaultView;
                    viewUser.Sort = "UserID";
                    int rowInd = viewUser.Find(userID);

                    if (rowInd >= 0) {
                        var userProps = new UserProps(userID) {
                            UserName = (string) viewUser[rowInd]["Name"],
                            RoleID = (int) viewUser[rowInd]["RoleID"]
                        };
                        userProps.RoleName = GetRoleName(userProps.RoleID);
                        return userProps;
                    }

                    return null;
                }
            } catch (Exception ex) {
                log.WriteException(ex, "Error getting user properties by ID={0}", userID);
                return null;
            }
        }

        /// <summary>
        /// Get user ID by name
        /// </summary>
        public int GetUserID(string username) {
            try {
                username = username ?? "";
                dataCache.RefreshBaseTables();
                var baseTables = dataCache.BaseTables;

                lock (baseTables.SyncRoot) {
                    BaseTables.CheckColumnsExist(baseTables.UserTable, true);
                    var viewUser = baseTables.UserTable.DefaultView;
                    viewUser.Sort = "Name";
                    int rowInd = viewUser.Find(username);
                    return rowInd >= 0 ? (int) viewUser[rowInd]["UserID"] : BaseValues.EmptyDataID;
                }
            } catch (Exception ex) {
                log.WriteException(ex, "Error getting user ID by name \"{0}\"", username);
                return BaseValues.EmptyDataID;
            }
        }

        /// <summary>
        /// Get the name of the object by number
        /// </summary>
        public string GetObjName(int objNum) {
            try {
                dataCache.RefreshBaseTables();
                var baseTables = dataCache.BaseTables;

                lock (baseTables.SyncRoot) {
                    BaseTables.CheckColumnsExist(baseTables.ObjTable, true);
                    var viewObj = baseTables.ObjTable.DefaultView;
                    viewObj.Sort = "ObjNum";
                    int rowInd = viewObj.Find(objNum);
                    return rowInd >= 0 ? (string) viewObj[rowInd]["Name"] : "";
                }
            } catch (Exception ex) {
                log.WriteException(ex, "Error getting object name by number {0}", objNum);
                return "";
            }
        }

        /// <summary>
        /// Get the name of KP by number
        /// </summary>
        public string GetKPName(int kpNum) {
            try {
                dataCache.RefreshBaseTables();
                var baseTables = dataCache.BaseTables;

                lock (baseTables.SyncRoot) {
                    BaseTables.CheckColumnsExist(baseTables.ObjTable, true);
                    var viewObj = baseTables.KPTable.DefaultView;
                    viewObj.Sort = "KPNum";
                    int rowInd = viewObj.Find(kpNum);
                    return rowInd >= 0 ? (string) viewObj[rowInd]["Name"] : "";
                }
            } catch (Exception ex) {
                log.WriteException(ex, "Error getting device name by number {0}", kpNum);
                return "";
            }
        }

        /// <summary>
        /// Get the name of the role by ID
        /// </summary>
        public string GetRoleName(int roleID) {
            string roleName = BaseValues.Roles.GetRoleName(roleID); // standard role name
            return BaseValues.Roles.Custom <= roleID && roleID < BaseValues.Roles.Err
                ? GetRoleNameFromBase(roleID, roleName)
                : roleName;
        }


        /// <summary>
        /// Get current input channel data
        /// </summary>
        public SrezTableLight.CnlData GetCurCnlData(int cnlNum) {
            DateTime dataAge;
            return GetCurCnlData(cnlNum, out dataAge);
        }

        /// <summary>
        /// Get current input channel data
        /// </summary>
        public SrezTableLight.CnlData GetCurCnlData(int cnlNum, out DateTime dataAge) {
            try {
                var snapshot = dataCache.GetCurSnapshot(out dataAge);
                SrezTableLight.CnlData cnlData;
                return snapshot != null && snapshot.GetCnlData(cnlNum, out cnlData)
                    ? cnlData
                    : SrezTableLight.CnlData.Empty;
            } catch (Exception ex) {
                log.WriteException(ex, "Error getting current data of the input channel {0}", cnlNum);

                dataAge = DateTime.MinValue;
                return SrezTableLight.CnlData.Empty;
            }
        }

        /// <summary>
        /// Get a displayed event based on event data
        /// </summary>
        /// <remarks>The method always returns a non-null object.</remarks>
        public DispEvent GetDispEvent(EventTableLight.Event ev, DataFormatter dataFormatter) {
            var dispEvent = new DispEvent();

            try {
                dispEvent.Num = ev.Number;
                dispEvent.Time = ev.DateTime.ToLocalizedString();
                dispEvent.Ack = ev.Checked ? CommonPhrases.EventAck : CommonPhrases.EventNotAck;

                var cnlProps = GetCnlProps(ev.CnlNum);
                var cnlStatProps = GetCnlStatProps(ev.NewCnlStat);

                if (cnlProps == null) {
                    dispEvent.Obj = GetObjName(ev.ObjNum);
                    dispEvent.KP = GetKPName(ev.KPNum);
                } else {
                    dispEvent.Obj = cnlProps.ObjName;
                    dispEvent.KP = cnlProps.KPName;
                    dispEvent.Cnl = cnlProps.CnlName;
                    dispEvent.Color = dataFormatter.GetCnlValColor(
                        ev.NewCnlVal, ev.NewCnlStat, cnlProps, cnlStatProps);
                    dispEvent.Sound = cnlProps.EvSound;
                }

                dispEvent.Text = dataFormatter.GetEventText(ev, cnlProps, cnlStatProps);
            } catch (Exception ex) {
                log.WriteException(ex, "Error getting displayed event based on the event data");
            }

            return dispEvent;
        }
    }
}