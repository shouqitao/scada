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
 * Module   : SCADA-Server Service
 * Summary  : Main server logic implementation. Derived types
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2013
 * Modified : 2017
 */

using System;
using System.IO;
using Scada.Data.Models;
using Scada.Data.Tables;

namespace Scada.Server.Engine {
    partial class MainLogic {
        /// <summary>
        /// 输入通道
        /// </summary>
        private class InCnl : InCnlProps {
            /// <summary>
            /// 输入通道计算方法
            /// </summary>
            public Calculator.CalcCnlDataDelegate CalcCnlData;
        }

        /// <summary>
        /// 控制通道
        /// </summary>
        internal class CtrlCnl : CtrlCnlProps {
            /// <summary>
            /// 标准指令值计算方法
            /// </summary>
            public Calculator.CalcCmdValDelegate CalcCmdVal;

            /// <summary>
            /// 计算二进制命令数据的方法
            /// </summary>
            public Calculator.CalcCmdDataDelegate CalcCmdData;

            /// <summary>
            /// 克隆控制通道
            /// </summary>
            /// <remarks>仅克隆应用程序使用的属性。</remarks>
            public CtrlCnl Clone() {
                return new CtrlCnl() {
                    CtrlCnlNum = this.CtrlCnlNum,
                    CmdTypeID = this.CmdTypeID,
                    ObjNum = this.ObjNum,
                    KPNum = this.KPNum,
                    CmdNum = this.CmdNum,
                    FormulaUsed = this.FormulaUsed,
                    Formula = this.Formula,
                    EvEnabled = this.EvEnabled,
                    CalcCmdVal = this.CalcCmdVal,
                    CalcCmdData = this.CalcCmdData
                };
            }
        }

        /// <summary>
        /// 用户
        /// </summary>
        /// <remarks>该类仅包含应用程序使用的那些属性。</remarks>
        internal class User {
            /// <summary>
            /// 获取或设置名称
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// 获取或设置密码
            /// </summary>
            public string Password { get; set; }

            /// <summary>
            /// 获取或设置角色ID
            /// </summary>
            public int RoleID { get; set; }

            /// <summary>
            /// 克隆用户
            /// </summary>
            public User Clone() {
                return new User() {
                    Name = this.Name,
                    Password = this.Password,
                    RoleID = this.RoleID
                };
            }
        }

        /// <summary>
        /// 切片表缓存
        /// </summary>
        private class SrezTableCache {
            /// <summary>
            /// 构造函数
            /// </summary>
            private SrezTableCache() { }

            /// <summary>
            /// 构造函数
            /// </summary>
            public SrezTableCache(DateTime date) {
                AccessDT = DateTime.Now;
                Date = date;
                SrezTable = new SrezTable();
                SrezTableCopy = new SrezTable();
                SrezAdapter = new SrezAdapter();
                SrezCopyAdapter = new SrezAdapter();
            }

            /// <summary>
            /// 获取或设置上次访问对象的日期和时间
            /// </summary>
            public DateTime AccessDT { get; set; }

            /// <summary>
            /// 获取切片表的日期
            /// </summary>
            public DateTime Date { get; private set; }

            /// <summary>
            /// 获取切片表
            /// </summary>
            public SrezTable SrezTable { get; private set; }

            /// <summary>
            /// 获取切片副本表
            /// </summary>
            public SrezTable SrezTableCopy { get; private set; }

            /// <summary>
            /// 获取切片表适配器
            /// </summary>
            public SrezAdapter SrezAdapter { get; private set; }

            /// <summary>
            /// 获取切片表适配器
            /// </summary>
            public SrezAdapter SrezCopyAdapter { get; private set; }

            /// <summary>
            /// 填充切片表或切片复制表
            /// </summary>
            public void FillSrezTable(bool copy = false) {
                if (copy)
                    FillSrezTable(SrezTableCopy, SrezCopyAdapter);
                else
                    FillSrezTable(SrezTable, SrezAdapter);
            }

            /// <summary>
            /// 填写切片表
            /// </summary>
            public static void FillSrezTable(SrezTable srezTable, SrezAdapter srezAdapter) {
                string fileName = srezAdapter.FileName;

                if (File.Exists(fileName)) {
                    // 确定切片表文件的最后修改时间
                    DateTime fileModTime = File.GetLastWriteTime(fileName);

                    // 如果文件已更改，则加载数据
                    if (srezTable.FileModTime != fileModTime) {
                        srezAdapter.Fill(srezTable);
                        srezTable.FileModTime = fileModTime;
                    }
                } else {
                    srezTable.Clear();
                }
            }
        }

        /// <summary>
        /// 平均数据
        /// </summary>
        private struct AvgData {
            /// <summary>
            /// 通道总数
            /// </summary>
            public double Sum { get; set; }

            /// <summary>
            /// 通道值的数量
            /// </summary>
            public int Cnt { get; set; }
        }
    }
}