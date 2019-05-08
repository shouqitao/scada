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
 * Module   : SCADA-Server Service
 * Summary  : Main server logic implementation
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2013
 * Modified : 2018
 */

using Scada.Data.Configuration;
using Scada.Data.Models;
using Scada.Data.Tables;
using Scada.Server.Modules;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Utils;

namespace Scada.Server.Engine {
    /// <summary>
    /// Main server logic implementation
    /// <para>实现主服务器逻辑</para>
    /// </summary>
    sealed partial class MainLogic : IServerData {
        /// <summary>
        /// 工作状态名称
        /// </summary>
        private static class WorkStateNames {
            /// <summary>
            /// 静态构造函数
            /// </summary>
            static WorkStateNames() {
                Normal = "normal";
                Stopped = "stopped";
                Error = "error";
            }

            /// <summary>
            /// Normal
            /// </summary>
            public static readonly string Normal;

            /// <summary>
            /// Stopped
            /// </summary>
            public static readonly string Stopped;

            /// <summary>
            /// Error
            /// </summary>
            public static readonly string Error;
        }

        /// <summary>
        /// 等待停止流的时间，ms
        /// </summary>
        private const int WaitForStop = 10000;

        /// <summary>
        /// 分钟表大小缓存容量
        /// </summary>
        private const int MinCacheCapacity = 5;

        /// <summary>
        /// 每小时切片表的缓存容量
        /// </summary>
        private const int HourCacheCapacity = 10;

        /// <summary>
        /// 分钟表缓存存储期
        /// </summary>
        private static readonly TimeSpan MinCacheStorePer = TimeSpan.FromMinutes(10);

        /// <summary>
        /// 每小时切片表的缓存的存储周期
        /// </summary>
        private static readonly TimeSpan HourCacheStorePer = TimeSpan.FromMinutes(10);

        /// <summary>
        /// 切片表缓存清除间隔
        /// </summary>
        private static readonly TimeSpan CacheClearSpan = TimeSpan.FromMinutes(1);

        /// <summary>
        /// DAT格式的配置数据库文件的名称
        /// </summary>
        private static readonly string[] BaseFiles = {
            "cmdtype.dat", "cmdval.dat", "cnltype.dat", "commline.dat", "ctrlcnl.dat", "evtype.dat", "format.dat",
            "formula.dat", "incnl.dat", "interface.dat", "kp.dat", "kptype.dat", "obj.dat", "param.dat", "right.dat",
            "role.dat", "unit.dat", "user.dat"
        };

        /// <summary>
        /// 有关输出到文件的应用程序的文本信息的格式
        /// </summary>
        private static readonly string AppInfoFormat =
            "SCADA-Server" + Environment.NewLine +
            "------------" + Environment.NewLine +
            "Started        : {0}" + Environment.NewLine +
            "Execution time : {1}" + Environment.NewLine +
            "State          : {2}" + Environment.NewLine +
            "Version        : {3}";

        private string infoFileName; // 完整的文件名信息
        private Thread thread; // 服务器工作流
        private volatile bool terminated; // 有必要关闭线程
        private volatile bool serverIsReady; // 服务器已准备好运行
        private DateTime startDT; // 开始日期和时间
        private string workState; // 工作状态
        private readonly Comm comm; // 与客户端通讯
        private readonly Calculator calculator; // 计算输入通道数据的计算器
        private readonly SortedList<int, InCnl> inCnls; // 主动输入通道
        private List<InCnl> drCnls; // 预先计算的TS和TI类型的通道列表，切换次数
        private List<InCnl> drmCnls; // 微型车辆和TI的通道列表
        private List<InCnl> drhCnls; // 时间车辆类型和TI的频道列表
        private int[] drCnlNums; // 频道号码 drCnls
        private int[] drmCnlNums; // 频道号码 drmCnls
        private int[] drhCnlNums; // 频道号码 drhCnls
        private List<int> avgCnlInds; // TI平均通道指数
        private readonly SortedList<int, CtrlCnl> ctrlCnls; // 主动控制通道
        private readonly SortedList<string, User> users; // 用户
        private readonly List<string> formulas; // 公式
        private SrezTable.Srez curSrez; // 当前切片，用于形成服务器数据
        private bool curSrezMod; // 当前切片更改的符号（对于更改记录）
        private SrezTableLight.Srez procSrez; // 处理切片进行公式计算
        private SrezTable.SrezDescr srezDescr; // 创建切片的描述
        private AvgData[] minAvgData; // 用于平均的分钟数据
        private AvgData[] hrAvgData; // 用于平均的小时数据
        private DateTime[] activeDTs; // 频道活动日期和时间
        private SrezAdapter curSrezAdapter; // 电流截止表适配器
        private SrezAdapter curSrezCopyAdapter; // 当前截止复制表适配器
        private EventAdapter eventAdapter; // 事件表适配器
        private EventAdapter eventCopyAdapter; // 事件复制表适配器
        private SortedList<DateTime, SrezTableCache> minSrezTableCache; // 分钟表缓存
        private SortedList<DateTime, SrezTableCache> hrSrezTableCache; // 小时表缓存
        private readonly List<EventTableLight.Event> eventsToWrite; // 要写的事件缓冲区
        private readonly List<ModLogic> modules; // 模块列表


        /// <summary>
        /// 构造函数
        /// </summary>
        public MainLogic() {
            AppDirs = new AppDirs();
            AppLog = new Log(Log.Formats.Full) {Encoding = Encoding.UTF8};
            Settings = new Settings();

            infoFileName = "";
            thread = null;
            terminated = false;
            serverIsReady = false;
            startDT = DateTime.MinValue;
            workState = "";
            comm = new Comm(this);
            calculator = new Calculator(this);
            inCnls = new SortedList<int, InCnl>();
            drCnls = null;
            drmCnls = null;
            drhCnls = null;
            drCnlNums = null;
            drmCnlNums = null;
            drhCnlNums = null;
            avgCnlInds = null;
            ctrlCnls = new SortedList<int, CtrlCnl>();
            users = new SortedList<string, User>();
            formulas = new List<string>();
            curSrez = null;
            curSrezMod = false;
            srezDescr = null;
            minAvgData = null;
            hrAvgData = null;
            activeDTs = null;
            curSrezAdapter = null;
            curSrezCopyAdapter = null;
            eventAdapter = null;
            eventCopyAdapter = null;
            minSrezTableCache = null;
            hrSrezTableCache = null;
            eventsToWrite = new List<EventTableLight.Event>();
            modules = new List<ModLogic>();
        }


        /// <summary>
        /// 获取应用程序目录
        /// </summary>
        public AppDirs AppDirs { get; private set; }

        /// <summary>
        /// 获取应用程序日志
        /// </summary>
        public Log AppLog { get; private set; }

        /// <summary>
        /// 获取应用设置
        /// </summary>
        public Settings Settings { get; private set; }

        /// <summary>
        /// 获得服务器准备就绪的信号
        /// </summary>
        public bool ServerIsReady {
            get { return serverIsReady; }
        }


        /// <summary>
        /// 下载模块
        /// </summary>
        private void LoadModules() {
            lock (modules) {
                // 清除模块列表
                modules.Clear();

                foreach (string fileName in Settings.ModuleFileNames) {
                    string fullFileName = AppDirs.ModDir + fileName;

                    try {
                        if (!File.Exists(fullFileName))
                            throw new Exception("File not found.");

                        // 实例化模块类
                        var asm = Assembly.LoadFile(fullFileName);
                        var type = asm.GetType("Scada.Server.Modules." +
                                               Path.GetFileNameWithoutExtension(fileName) + "Logic", true);
                        var modLogic = Activator.CreateInstance(type) as ModLogic;
                        modLogic.AppDirs = AppDirs;
                        modLogic.Settings = Settings;
                        modLogic.WriteToLog = AppLog.WriteAction;
                        modLogic.ServerData = this;
                        modLogic.ServerCommands = comm;
                        modules.Add(modLogic);
                        AppLog.WriteAction($"Module is loaded from the file {fullFileName}", Log.ActTypes.Action);
                    } catch (Exception ex) {
                        AppLog.WriteAction($"Error loading module from the file {fullFileName}: {ex.Message}",
                            Log.ActTypes.Exception);
                    }
                }
            }
        }

        /// <summary>
        /// 检查数据目录是否存在
        /// </summary>
        private bool CheckDataDirs() {
            // 检查目录的存在
            const string dirNotExistStr = "The {0}{1} directory does not exist.";
            const string datFormatStr = " in DAT format";
            var errors = new List<string>();

            if (!Directory.Exists(Settings.BaseDATDir))
                errors.Add(string.Format(dirNotExistStr, "configuration database", datFormatStr));
            if (!Directory.Exists(Settings.ItfDir))
                errors.Add(string.Format(dirNotExistStr, "interface", ""));
            if (!Directory.Exists(Settings.ArcDir))
                errors.Add(string.Format(dirNotExistStr, "archive", datFormatStr));
            if (!Directory.Exists(Settings.ArcCopyDir))
                errors.Add(string.Format(dirNotExistStr, "archive copy", datFormatStr));
            if (Settings.ArcDir == Settings.ArcCopyDir)
                errors.Add("The archive in DAT format directory and its copy directory are equal.");

            if (errors.Count > 0) {
                AppLog.WriteAction(string.Join(Environment.NewLine, errors), Log.ActTypes.Error);
                return false;
            }

            // 创建存档子目录（如果它们不存在）
            try {
                Directory.CreateDirectory(Settings.ArcDir + "Cur");
                Directory.CreateDirectory(Settings.ArcDir + "Min");
                Directory.CreateDirectory(Settings.ArcDir + "Hour");
                Directory.CreateDirectory(Settings.ArcDir + "Events");
                Directory.CreateDirectory(Settings.ArcCopyDir + "Cur");
                Directory.CreateDirectory(Settings.ArcCopyDir + "Min");
                Directory.CreateDirectory(Settings.ArcCopyDir + "Hour");
                Directory.CreateDirectory(Settings.ArcCopyDir + "Events");

                AppLog.WriteAction("Check the existence of the data directories is completed successfully",
                    Log.ActTypes.Action);
                return true;
            } catch (Exception ex) {
                AppLog.WriteAction(
                    ("Error creating subdirectories of the archive: ") + ex.Message, Log.ActTypes.Exception);
                return false;
            }
        }

        /// <summary>
        /// 检查配置数据库文件是否存在
        /// </summary>
        private bool CheckBaseFiles() {
            var requiredFiles = new List<string>();

            foreach (string fileName in BaseFiles) {
                string path = Settings.BaseDATDir + fileName;
                if (!File.Exists(path))
                    requiredFiles.Add($"The configuration database file {path} not found");
            }


            if (requiredFiles.Count > 0) {
                AppLog.WriteAction(string.Join(Environment.NewLine, requiredFiles), Log.ActTypes.Error);
                return false;
            }

            AppLog.WriteAction("Check the existence of the configuration database files is completed successfully",
                Log.ActTypes.Action);
            return true;
        }

        /// <summary>
        /// 从配置数据库中读取输入通道
        /// </summary>
        private bool ReadInCnls() {
            try {
                lock (inCnls) {
                    // 清洁频道信息
                    inCnls.Clear();
                    drCnls = new List<InCnl>();
                    drmCnls = new List<InCnl>();
                    drhCnls = new List<InCnl>();
                    drCnlNums = null;
                    drmCnlNums = null;
                    drhCnlNums = null;
                    avgCnlInds = new List<int>();

                    // 填写频道信息
                    var tblInCnl = new DataTable();
                    var adapter = new BaseAdapter {FileName = Settings.BaseDATDir + "incnl.dat"};
                    adapter.Fill(tblInCnl, false);

                    foreach (DataRow dataRow in tblInCnl.Rows) {
                        if ((bool) dataRow["Active"]) {
                            // 仅填充应用程序使用的属性
                            var inCnl = new InCnl {
                                CnlNum = (int) dataRow["CnlNum"],
                                CnlTypeID = (int) dataRow["CnlTypeID"],
                                ObjNum = (int) dataRow["ObjNum"],
                                KPNum = (int) dataRow["KPNum"],
                                FormulaUsed = (bool) dataRow["FormulaUsed"],
                                Formula = (string) dataRow["Formula"],
                                Averaging = (bool) dataRow["Averaging"],
                                ParamID = (int) dataRow["ParamID"],
                                EvEnabled = (bool) dataRow["EvEnabled"],
                                EvOnChange = (bool) dataRow["EvOnChange"],
                                EvOnUndef = (bool) dataRow["EvOnUndef"],
                                LimLowCrash = (double) dataRow["LimLowCrash"],
                                LimLow = (double) dataRow["LimLow"],
                                LimHigh = (double) dataRow["LimHigh"],
                                LimHighCrash = (double) dataRow["LimHighCrash"]
                            };

                            int cnlTypeID = inCnl.CnlTypeID;
                            if (BaseValues.CnlTypes.MinCnlTypeID <= cnlTypeID &&
                                cnlTypeID <= BaseValues.CnlTypes.MaxCnlTypeID)
                                inCnls.Add(inCnl.CnlNum, inCnl);

                            if (cnlTypeID == BaseValues.CnlTypes.TSDR || cnlTypeID == BaseValues.CnlTypes.TIDR ||
                                cnlTypeID == BaseValues.CnlTypes.SWCNT)
                                drCnls.Add(inCnl);
                            else if (cnlTypeID == BaseValues.CnlTypes.TSDRM || cnlTypeID == BaseValues.CnlTypes.TIDRM)
                                drmCnls.Add(inCnl);
                            else if (cnlTypeID == BaseValues.CnlTypes.TSDRH || cnlTypeID == BaseValues.CnlTypes.TIDRH)
                                drhCnls.Add(inCnl);

                            if (inCnl.Averaging && cnlTypeID == BaseValues.CnlTypes.TI)
                                avgCnlInds.Add(inCnls.Count - 1);
                        }
                    }

                    // 填写额外计算通道的数量
                    int cnt = drCnls.Count;
                    drCnlNums = new int[cnt];
                    for (var i = 0; i < cnt; i++)
                        drCnlNums[i] = drCnls[i].CnlNum;

                    cnt = drmCnls.Count;
                    drmCnlNums = new int[cnt];
                    for (var i = 0; i < cnt; i++)
                        drmCnlNums[i] = drmCnls[i].CnlNum;

                    cnt = drhCnls.Count;
                    drhCnlNums = new int[cnt];
                    for (var i = 0; i < cnt; i++)
                        drhCnlNums[i] = drhCnls[i].CnlNum;

                    // 结果确定
                    if (inCnls.Count > 0) {
                        AppLog.WriteAction(
                            $"Input channels are read from the configuration database. Active channel count: {inCnls.Count}",
                            Log.ActTypes.Action);
                        return true;
                    }

                    AppLog.WriteAction(
                        "No active input channels in the configuration database", Log.ActTypes.Error);
                    return false;
                }
            } catch (Exception ex) {
                AppLog.WriteAction(("Error reading input channels from the configuration database: ") +
                                   ex.Message, Log.ActTypes.Exception);
                return false;
            }
        }

        /// <summary>
        /// 从配置数据库中读取控制通道
        /// </summary>
        private bool ReadCtrlCnls() {
            try {
                lock (ctrlCnls) {
                    ctrlCnls.Clear();
                    var tblCtrlCnl = new DataTable();
                    var adapter = new BaseAdapter {FileName = Settings.BaseDATDir + "ctrlcnl.dat"};
                    adapter.Fill(tblCtrlCnl, false);

                    foreach (DataRow dataRow in tblCtrlCnl.Rows) {
                        if ((bool) dataRow["Active"]) {
                            // 仅填充应用程序使用的属性
                            var ctrlCnl = new CtrlCnl {
                                CtrlCnlNum = (int) dataRow["CtrlCnlNum"],
                                CmdTypeID = (int) dataRow["CmdTypeID"],
                                ObjNum = (int) dataRow["ObjNum"],
                                KPNum = (int) dataRow["KPNum"],
                                CmdNum = (int) dataRow["CmdNum"],
                                FormulaUsed = (bool) dataRow["FormulaUsed"],
                                Formula = (string) dataRow["Formula"],
                                EvEnabled = (bool) dataRow["EvEnabled"]
                            };
                            ctrlCnls.Add(ctrlCnl.CtrlCnlNum, ctrlCnl);
                        }
                    }
                }

                AppLog.WriteAction(
                    "Output channels are read from the configuration database", Log.ActTypes.Action);
                return true;
            } catch (Exception ex) {
                AppLog.WriteAction("Error reading output channels from the configuration database: " +
                                   ex.Message, Log.ActTypes.Exception);
                return false;
            }
        }

        /// <summary>
        /// 从配置数据库中读取用户
        /// </summary>
        private bool ReadUsers() {
            try {
                lock (users) {
                    users.Clear();
                    var tblUser = new DataTable();
                    var adapter = new BaseAdapter {FileName = Settings.BaseDATDir + "user.dat"};
                    adapter.Fill(tblUser, false);

                    foreach (DataRow dataRow in tblUser.Rows) {
                        var user = new User {
                            Name = (string) dataRow["Name"],
                            Password = (string) dataRow["Password"],
                            RoleID = (int) dataRow["RoleID"]
                        };
                        users[user.Name.Trim().ToLowerInvariant()] = user;
                    }
                }

                AppLog.WriteAction("Users are read from the configuration database", Log.ActTypes.Action);
                return true;
            } catch (Exception ex) {
                AppLog.WriteAction("Error reading users from the configuration database: " +
                                   ex.Message, Log.ActTypes.Exception);
                return false;
            }
        }

        /// <summary>
        /// 从配置数据库中读取公式
        /// </summary>
        private bool ReadFormulas() {
            try {
                formulas.Clear();
                var tblFormula = new DataTable();
                var adapter = new BaseAdapter {FileName = Settings.BaseDATDir + "formula.dat"};
                adapter.Fill(tblFormula, false);

                foreach (DataRow dataRow in tblFormula.Rows)
                    formulas.Add((string) dataRow["Source"]);

                AppLog.WriteAction(
                    "Formulas are read from the configuration database", Log.ActTypes.Action);
                return true;
            } catch (Exception ex) {
                AppLog.WriteAction("Error reading formulas from the configuration database: " + ex.Message,
                    Log.ActTypes.Exception);
                return false;
            }
        }

        /// <summary>
        /// 从配置数据库中读取必要的数据
        /// </summary>
        private bool ReadBase() {
            return ReadInCnls() && ReadCtrlCnls() && ReadUsers() && ReadFormulas();
        }

        /// <summary>
        /// 初始化计算器以计算输入通道数据
        /// </summary>
        private bool InitCalculator() {
            // 清洁并向计算器添加公式
            calculator.ClearFormulas();

            foreach (string formula in formulas)
                calculator.AddAuxFormulaSource(formula);

            foreach (var inCnl in inCnls.Values) {
                if (inCnl.FormulaUsed)
                    calculator.AddCnlFormulaSource(inCnl.CnlNum, inCnl.Formula);
            }

            foreach (var ctrlCnl in ctrlCnls.Values) {
                if (ctrlCnl.FormulaUsed) {
                    if (ctrlCnl.CmdTypeID == BaseValues.CmdTypes.Standard)
                        calculator.AddCtrlCnlStandardFormulaSource(ctrlCnl.CtrlCnlNum, ctrlCnl.Formula);
                    else if (ctrlCnl.CmdTypeID == BaseValues.CmdTypes.Binary)
                        calculator.AddCtrlCnlBinaryFormulaSource(ctrlCnl.CtrlCnlNum, ctrlCnl.Formula);
                }
            }

            // 编制公式并获取计算渠道的方法
            if (calculator.CompileSource()) {
                foreach (var inCnl in inCnls.Values) {
                    if (inCnl.FormulaUsed) {
                        inCnl.CalcCnlData = calculator.GetCalcCnlData(inCnl.CnlNum);
                        if (inCnl.CalcCnlData == null)
                            return false;
                    } else {
                        inCnl.CalcCnlData = null;
                    }
                }

                foreach (var ctrlCnl in ctrlCnls.Values) {
                    ctrlCnl.CalcCmdVal = null;
                    ctrlCnl.CalcCmdData = null;

                    if (ctrlCnl.FormulaUsed) {
                        if (ctrlCnl.CmdTypeID == BaseValues.CmdTypes.Standard) {
                            ctrlCnl.CalcCmdVal = calculator.GetCalcCmdVal(ctrlCnl.CtrlCnlNum);
                            if (ctrlCnl.CalcCmdVal == null)
                                return false;
                        } else if (ctrlCnl.CmdTypeID == BaseValues.CmdTypes.Binary) {
                            ctrlCnl.CalcCmdData = calculator.GetCalcCmdData(ctrlCnl.CtrlCnlNum);
                            if (ctrlCnl.CalcCmdData == null)
                                return false;
                        }
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 服务器的循环（该方法在单独的线程中调用）
        /// </summary>
        private void Execute() {
            try {
                // 记录有关应用程序的信息
                workState = WorkStateNames.Normal;
                WriteInfo();

                // 执行模块操作
                RaiseOnServerStart();

                // 初始化当前截止表适配器和事件
                curSrezAdapter = new SrezAdapter();
                curSrezCopyAdapter = new SrezAdapter();
                eventAdapter = new EventAdapter();
                eventCopyAdapter = new EventAdapter();
                curSrezAdapter.FileName = ServerUtils.BuildCurFileName(Settings.ArcDir);
                curSrezCopyAdapter.FileName = ServerUtils.BuildCurFileName(Settings.ArcCopyDir);
                eventAdapter.Directory = Settings.ArcDir + "Events" + Path.DirectorySeparatorChar;
                eventCopyAdapter.Directory = Settings.ArcCopyDir + "Events" + Path.DirectorySeparatorChar;

                // 分钟和小时切片的缓存表的初始化
                minSrezTableCache = new SortedList<DateTime, SrezTableCache>();
                hrSrezTableCache = new SortedList<DateTime, SrezTableCache>();

                // 初始化创建的切片的描述
                int cnlCnt = inCnls.Count;
                srezDescr = new SrezTable.SrezDescr(cnlCnt);
                for (var i = 0; i < cnlCnt; i++)
                    srezDescr.CnlNums[i] = inCnls.Values[i].CnlNum;
                srezDescr.CalcCS();

                // 从文件加载原始当前切片
                SrezTableLight.Srez curSrezSrc = null;
                var tblCurSrezScr = new SrezTableLight();

                try {
                    if (File.Exists(curSrezAdapter.FileName)) {
                        curSrezAdapter.Fill(tblCurSrezScr);
                        if (tblCurSrezScr.SrezList.Count > 0)
                            curSrezSrc = tblCurSrezScr.SrezList.Values[0];
                    }

                    AppLog.WriteAction(curSrezSrc == null ? "Current data are not loaded" : "Current data are loaded",
                        Log.ActTypes.Action);
                } catch (Exception ex) {
                    AppLog.WriteAction(
                        ("Error loading current data: ") + ex.Message, Log.ActTypes.Exception);
                }

                // 初始化当前切片，用于形成服务器数据
                curSrez = new SrezTable.Srez(DateTime.MinValue, srezDescr, curSrezSrc);

                // 用于平均和通道活动时间的数据初始化
                minAvgData = new AvgData[cnlCnt];
                hrAvgData = new AvgData[cnlCnt];
                activeDTs = new DateTime[cnlCnt];
                var nowDT = DateTime.Now;

                for (var i = 0; i < cnlCnt; i++) {
                    minAvgData[i] = new AvgData() {
                        Sum = 0.0,
                        Cnt = 0
                    };
                    hrAvgData[i] = new AvgData() {
                        Sum = 0.0,
                        Cnt = 0
                    };
                    activeDTs[i] = nowDT;
                }

                // 初始化生成的事件列表
                var events = new List<EventTableLight.Event>();

                // 服务器周期
                nowDT = DateTime.MaxValue;
                DateTime today;
                DateTime prevDT;
                var writeCurSrezDT = DateTime.MinValue;
                var writeMinSrezDT = DateTime.MinValue;
                var writeHrSrezDT = DateTime.MinValue;
                var calcMinDT = DateTime.MinValue;
                var calcHrDT = DateTime.MinValue;
                var clearCacheDT = nowDT;

                bool calcDR = drCnls.Count > 0;
                bool writeCur = Settings.WriteCur || Settings.WriteCurCopy;
                bool writeCurOnMod = Settings.WriteCurPer <= 0;
                bool writeMin = (Settings.WriteMin || Settings.WriteMinCopy) && Settings.WriteMinPer > 0;
                bool writeHr = (Settings.WriteHr || Settings.WriteHrCopy) && Settings.WriteHrPer > 0;

                curSrezMod = false;
                serverIsReady = true;

                while (!terminated) {
                    prevDT = nowDT;
                    nowDT = DateTime.Now;
                    today = nowDT.Date;

                    // 计算记录切片的时间并计算计算通道的值
                    // 当换回时间或首次通过一个循环时
                    if (prevDT > nowDT) {
                        writeCurSrezDT = nowDT;
                        writeMinSrezDT = CalcNextTime(nowDT, Settings.WriteMinPer);
                        writeHrSrezDT = CalcNextTime(nowDT, Settings.WriteHrPer);
                        calcMinDT = drmCnls.Count > 0 ? CalcNextTime(nowDT, 60) : DateTime.MaxValue;
                        calcHrDT = drhCnls.Count > 0 ? CalcNextTime(nowDT, 3600) : DateTime.MaxValue;
                    }

                    // 更改日期或循环首次通过时删除过时的快照文件和事件
                    if (prevDT.Date != today) {
                        ClearArchive(Settings.ArcDir + "Min", "m*.dat", today.AddDays(-Settings.StoreMinPer));
                        ClearArchive(Settings.ArcDir + "Hour", "h*.dat", today.AddDays(-Settings.StoreHrPer));
                        ClearArchive(Settings.ArcDir + "Events", "e*.dat", today.AddDays(-Settings.StoreEvPer));
                        ClearArchive(Settings.ArcCopyDir + "Min", "m*.dat", today.AddDays(-Settings.StoreMinPer));
                        ClearArchive(Settings.ArcCopyDir + "Hour", "h*.dat", today.AddDays(-Settings.StoreHrPer));
                        ClearArchive(Settings.ArcCopyDir + "Events", "e*.dat", today.AddDays(-Settings.StoreEvPer));
                    }

                    bool calcMinDR = calcMinDT <= nowDT; // 有必要计算分钟通道
                    bool calcHrDR = calcHrDT <= nowDT; // 有必要计算每小时频道

                    lock (curSrez) {
                        // 无效的频道不准确设置
                        SetUnreliable(events);

                        // 计算额外的计算渠道
                        if (calcDR) {
                            CalcDRCnls(drCnls, curSrez, events);
                        }

                        // 分钟通道计算
                        if (calcMinDR) {
                            CalcDRCnls(drmCnls, curSrez, events);
                            calcMinDT = CalcNextTime(nowDT, 60);
                            curSrezMod = true;
                        }

                        // 计算每小时频道
                        if (calcHrDR) {
                            CalcDRCnls(drhCnls, curSrez, events);
                            calcHrDT = CalcNextTime(nowDT, 3600);
                            curSrezMod = true;
                        }
                    }

                    // 使用模块记录和处理事件而不阻塞当前切片
                    WriteEvents(events);

                    lock (eventsToWrite) {
                        WriteEvents(eventsToWrite);
                    }

                    // 执行模块操作而不阻塞当前切片
                    if (calcDR)
                        RaiseOnCurDataCalculated(drCnlNums, curSrez);

                    if (calcMinDR)
                        RaiseOnCurDataCalculated(drmCnlNums, curSrez);

                    if (calcHrDR)
                        RaiseOnCurDataCalculated(drhCnlNums, curSrez);

                    // 记录切片
                    lock (curSrez) {
                        // 目前削减记录
                        if ((writeCurSrezDT <= nowDT || writeCurOnMod && curSrezMod) && writeCur) {
                            if (writeCurOnMod) {
                                WriteSrez(SnapshotTypes.Cur, nowDT);
                                curSrezMod = false;
                                writeCurSrezDT = DateTime.MaxValue;
                            } else {
                                WriteSrez(SnapshotTypes.Cur, writeCurSrezDT);
                                writeCurSrezDT = CalcNextTime(nowDT, Settings.WriteCurPer);
                            }
                        }

                        // 分钟记录
                        if (writeMinSrezDT <= nowDT && writeMin) {
                            WriteSrez(SnapshotTypes.Min, writeMinSrezDT);
                            writeMinSrezDT = CalcNextTime(nowDT, Settings.WriteMinPer);
                        }

                        // 时间刻录
                        if (writeHrSrezDT <= nowDT && writeHr) {
                            WriteSrez(SnapshotTypes.Hour, writeHrSrezDT);
                            writeHrSrezDT = CalcNextTime(nowDT, Settings.WriteHrPer);
                        }
                    }

                    // 清除过时的缓存数据
                    if (nowDT - clearCacheDT > CacheClearSpan || nowDT < clearCacheDT /*时间搬回来了*/) {
                        clearCacheDT = nowDT;
                        ClearSrezTableCache(minSrezTableCache, MinCacheStorePer, MinCacheCapacity);
                        ClearSrezTableCache(hrSrezTableCache, HourCacheStorePer, HourCacheCapacity);
                    }

                    // 记录有关应用程序的信息
                    WriteInfo();

                    // 延迟节省CPU资源
                    Thread.Sleep(100);
                }
            } finally {
                // 执行模块操作
                RaiseOnServerStop();

                // 记录有关应用程序的信息
                workState = WorkStateNames.Stopped;
                WriteInfo();
            }
        }

        /// <summary>
        /// 计算下次写切片的时间
        /// </summary>
        /// <remarks>周期以秒为单位</remarks>
        private static DateTime CalcNextTime(DateTime nowDT, int period) {
            return period > 0
                ? nowDT.Date.AddSeconds(((int) nowDT.TimeOfDay.TotalSeconds / period + 1) * period)
                : nowDT;
        }

        /// <summary>
        /// 很快就会计算切片记录
        /// </summary>
        /// <remarks>周期以秒为单位</remarks>
        private static DateTime CalcNearestTime(DateTime dateTime, int period) {
            if (period <= 0) return dateTime;

            var dt1 = dateTime.Date.AddSeconds((int) dateTime.TimeOfDay.TotalSeconds / period * period);
            var dt2 = dt1.AddSeconds(period);
            double delta1 = Math.Abs((dateTime - dt1).TotalSeconds);
            double delta2 = Math.Abs((dateTime - dt2).TotalSeconds);

            return delta1 <= delta2 ? dt1 : dt2;
        }

        /// <summary>
        /// 获取切片表缓存，必要时创建它
        /// </summary>
        private SrezTableCache GetSrezTableCache(DateTime date, SnapshotTypes srezType) {
            SortedList<DateTime, SrezTableCache> srezTableCacheList;
            SrezTableCache srezTableCache;

            if (srezType == SnapshotTypes.Min)
                srezTableCacheList = minSrezTableCache;
            else if (srezType == SnapshotTypes.Hour)
                srezTableCacheList = hrSrezTableCache;
            else
                throw new ArgumentException("Illegal snapshot type.");

            lock (srezTableCacheList) {
                if (srezTableCacheList.TryGetValue(date, out srezTableCache)) {
                    srezTableCache.AccessDT = DateTime.Now;
                } else {
                    // 创建切片表缓存
                    srezTableCache = new SrezTableCache(date);
                    srezTableCacheList.Add(date, srezTableCache);

                    if (srezType == SnapshotTypes.Min) {
                        if (Localization.UseRussian) {
                            srezTableCache.SrezTable.Descr = "минутных срезов";
                            srezTableCache.SrezTableCopy.Descr = "копий минутных срезов";
                        } else {
                            srezTableCache.SrezTable.Descr = "minute data";
                            srezTableCache.SrezTableCopy.Descr = "minute data copy";
                        }

                        srezTableCache.SrezAdapter.FileName =
                            ServerUtils.BuildMinFileName(Settings.ArcDir, date);
                        srezTableCache.SrezCopyAdapter.FileName =
                            ServerUtils.BuildMinFileName(Settings.ArcCopyDir, date);
                    } else {
                        if (Localization.UseRussian) {
                            srezTableCache.SrezTable.Descr = "часовых срезов";
                            srezTableCache.SrezTableCopy.Descr = "копий часовых срезов";
                        } else {
                            {
                                srezTableCache.SrezTable.Descr = "hourly data";
                                srezTableCache.SrezTableCopy.Descr = "hourly data copy";
                            }
                        }

                        srezTableCache.SrezAdapter.FileName =
                            ServerUtils.BuildHourFileName(Settings.ArcDir, date);
                        srezTableCache.SrezCopyAdapter.FileName =
                            ServerUtils.BuildHourFileName(Settings.ArcCopyDir, date);
                    }
                }
            }

            return srezTableCache;
        }

        /// <summary>
        /// 清除过时的缓存数据
        /// </summary>
        private void ClearSrezTableCache(SortedList<DateTime, SrezTableCache> srezTableCacheList,
            TimeSpan storePer, int capacity) {
            lock (srezTableCacheList) {
                // 删除过时的数据
                var nowDT = DateTime.Now;
                var today = nowDT.Date;
                var i = 0;

                while (i < srezTableCacheList.Count) {
                    var srezTableCache = srezTableCacheList.Values[i];
                    if (nowDT - srezTableCache.AccessDT > storePer && srezTableCache.Date != today)
                        srezTableCacheList.RemoveAt(i);
                    else
                        i++;
                }

                // 如果超出容量，则删除访问时间最短的数据
                if (srezTableCacheList.Count > capacity) {
                    int cnt = srezTableCacheList.Count;
                    var accDTs = new DateTime[cnt];
                    var keyDates = new DateTime[cnt];

                    for (var j = 0; j < cnt; j++) {
                        var srezTableCache = srezTableCacheList.Values[j];
                        accDTs[j] = srezTableCache.AccessDT;
                        keyDates[j] = srezTableCache.Date;
                    }

                    Array.Sort(accDTs, keyDates);
                    int delCnt = cnt - capacity;

                    for (int j = 0,
                        k = 0;
                        j < cnt && k < delCnt;
                        j++) {
                        var keyDate = keyDates[j];
                        if (keyDate != today) {
                            srezTableCacheList.Remove(keyDate);
                            k++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 清除过时的归档数据
        /// </summary>
        private void ClearArchive(string dir, string pattern, DateTime arcBegDate) {
            try {
                var dirInfo = new DirectoryInfo(dir);

                if (dirInfo.Exists) {
                    FileInfo[] files = dirInfo.GetFiles(pattern, SearchOption.TopDirectoryOnly);

                    foreach (var fileInfo in files) {
                        string fileName = fileInfo.Name;
                        int year,
                            month,
                            day;

                        if (fileName.Length >= 7 &&
                            int.TryParse(fileName.Substring(1, 2), out year) &&
                            int.TryParse(fileName.Substring(3, 2), out month) &&
                            int.TryParse(fileName.Substring(5, 2), out day)) {
                            DateTime fileDate;
                            try {
                                fileDate = new DateTime(2000 + year, month, day);
                            } catch {
                                fileDate = DateTime.MaxValue;
                            }

                            if (fileDate < arcBegDate)
                                fileInfo.Delete();
                        }
                    }
                }
            } catch (Exception ex) {
                AppLog.WriteAction(string.Format(
                    Localization.UseRussian
                        ? "Ошибка при очистке устаревших архивных данных: {0},{1}Директория: {2}"
                        : "Error clearing outdated archive data: {0}{1}Directory: {2}",
                    ex.Message, Environment.NewLine, dir), Log.ActTypes.Exception);
            }
        }

        /// <summary>
        /// 通过选择所需的表将切片写入切片表。
        /// </summary>
        private void WriteSrez(SnapshotTypes srezType, DateTime srezDT) {
            if (srezType == SnapshotTypes.Cur) {
                // 写新的当前切片
                if (Settings.WriteCur)
                    WriteCurSrez(curSrezAdapter, srezDT);

                if (Settings.WriteCurCopy)
                    WriteCurSrez(curSrezCopyAdapter, srezDT);
            } else {
                // 切片记录参数定义
                bool writeMain;
                bool writeCopy;
                AvgData[] avgData;

                if (srezType == SnapshotTypes.Min) {
                    writeMain = Settings.WriteMin;
                    writeCopy = Settings.WriteMinCopy;
                    avgData = minAvgData;
                } else // srezType == SrezTypes.Hour
                {
                    writeMain = Settings.WriteHr;
                    writeCopy = Settings.WriteHrCopy;
                    avgData = hrAvgData;
                }

                // 获取切片表缓存
                var srezTableCache = GetSrezTableCache(srezDT.Date, srezType);

                // 录制新的分钟或小时片段
                lock (srezTableCache) {
                    if (writeMain)
                        WriteArcSrez(srezTableCache.SrezTable, srezTableCache.SrezAdapter, srezDT, avgData);

                    if (writeCopy)
                        WriteArcSrez(srezTableCache.SrezTableCopy, srezTableCache.SrezCopyAdapter, srezDT, avgData);
                }
            }
        }

        /// <summary>
        /// 将切片写入当前切片的表
        /// </summary>
        private void WriteCurSrez(SrezAdapter srezAdapter, DateTime srezDT) {
            var fileName = "";

            try {
                fileName = srezAdapter.FileName;
                srezAdapter.Create(curSrez, srezDT);

                if (Settings.DetailedLog) {
                    if (srezAdapter == curSrezAdapter)
                        AppLog.WriteAction(
                            Localization.UseRussian
                                ? "Запись среза в таблицу текущего среза завершена"
                                : "Writing snapshot in the current data table is completed",
                            Log.ActTypes.Action);
                    else
                        AppLog.WriteAction(
                            Localization.UseRussian
                                ? "Запись среза в таблицу копии текущего среза завершена"
                                : "Writing snapshot in the current data copy table is completed",
                            Log.ActTypes.Action);
                }
            } catch (Exception ex) {
                string fileNameStr = string.IsNullOrEmpty(fileName)
                    ? ""
                    : Environment.NewLine + (Localization.UseRussian ? "Имя файла: " : "Filename: ") + fileName;
                AppLog.WriteAction(string.Format(
                    Localization.UseRussian
                        ? "Ошибка при записи среза в таблицу текущего среза: {0}{1}"
                        : "Error writing snapshot in the current data table: {0}{1}",
                    ex.Message, fileNameStr), Log.ActTypes.Exception);
            }
        }

        /// <summary>
        /// 将切片写入存档（分钟或小时）切片表
        /// </summary>
        private void WriteArcSrez(SrezTable srezTable, SrezAdapter srezAdapter, DateTime srezDT, AvgData[] avgData) {
            var fileName = "";

            try {
                // 如果文件已更改，则填写切片表
                fileName = srezAdapter.FileName;
                SrezTableCache.FillSrezTable(srezTable, srezAdapter);

                // 将复制切片添加到表中
                var newSrez = srezTable.AddSrezCopy(curSrez, srezDT);

                // 平均数据记录
                var changed = false;

                foreach (int cnlInd in avgCnlInds) {
                    var ad = avgData[cnlInd];

                    if (ad.Cnt > 0) {
                        newSrez.CnlData[cnlInd] =
                            new SrezTableLight.CnlData(ad.Sum / ad.Cnt, BaseValues.CnlStatuses.Defined);
                        avgData[cnlInd] = new AvgData() {
                            Sum = 0.0,
                            Cnt = 0
                        }; // сброс
                        changed = true;
                    }
                }

                // 如果添加的切片已更改，则计算其他计算通道
                if (changed)
                    CalcDRCnls(drCnls, newSrez, null);

                // 记录对切片表的更改
                srezAdapter.Update(srezTable);
                srezTable.FileModTime = File.GetLastWriteTime(fileName);

                if (Settings.DetailedLog)
                    AppLog.WriteAction(string.Format(
                        Localization.UseRussian
                            ? "Запись среза в таблицу {0} завершена"
                            : "Writing snapshot in the {0} table is completed",
                        srezTable.Descr), Log.ActTypes.Action);
            } catch (Exception ex) {
                string fileNameStr = string.IsNullOrEmpty(fileName)
                    ? ""
                    : Environment.NewLine + (Localization.UseRussian ? "Имя файла: " : "Filename: ") + fileName;
                AppLog.WriteAction(string.Format(
                    Localization.UseRussian
                        ? "Ошибка при записи среза в таблицу архивных срезов: {0}{1}"
                        : "Error writing snapshot in the archive data table: {0}{1}",
                    ex.Message, fileNameStr), Log.ActTypes.Exception);
            }
        }

        /// <summary>
        /// 将接受的切片写入归档切片表
        /// </summary>
        private bool WriteReceivedSrez(SrezTable srezTable, SrezAdapter srezAdapter,
            SrezTableLight.Srez receivedSrez, DateTime srezDT, ref SrezTableLight.Srez arcSrez) {
            var fileName = "";

            try {
                // 获取现有或创建新的存档切片
                fileName = srezAdapter.FileName;
                SrezTableCache.FillSrezTable(srezTable, srezAdapter);
                var srez = srezTable.GetSrez(srezDT);
                bool addSrez;

                if (srez == null) {
                    srez = new SrezTable.Srez(srezDT, srezDescr, receivedSrez);
                    addSrez = true;
                } else {
                    addSrez = false;
                }

                if (arcSrez == null)
                    arcSrez = srez;

                // 改变档案切割
                lock (calculator) {
                    try {
                        procSrez = srez;
                        int cntCnt = receivedSrez.CnlNums.Length;

                        for (var i = 0; i < cntCnt; i++) {
                            int cnlNum = receivedSrez.CnlNums[i];
                            int cnlInd = srez.GetCnlIndex(cnlNum);
                            InCnl inCnl;

                            if (inCnls.TryGetValue(cnlNum, out inCnl) && cnlInd >= 0) // 输入通道存在
                            {
                                // 设置频道的存档状态
                                var newCnlData = receivedSrez.CnlData[i];
                                if (newCnlData.Stat == BaseValues.CnlStatuses.Defined)
                                    newCnlData.Stat = BaseValues.CnlStatuses.Archival;

                                // 计算TC或TI类型的输入通道的输入数据
                                if (inCnl.CnlTypeID == BaseValues.CnlTypes.TS ||
                                    inCnl.CnlTypeID == BaseValues.CnlTypes.TI) {
                                    var oldCnlData = srez.CnlData[cnlInd];
                                    CalcCnlData(inCnl, oldCnlData, ref newCnlData);
                                }

                                // 将新数据写入存档切片
                                srez.CnlData[cnlInd] = newCnlData;
                            }
                        }
                    } finally {
                        procSrez = null;
                    }
                }

                // 计算额外的计算渠道
                CalcDRCnls(drCnls, srez, null);

                if (addSrez)
                    srezTable.AddSrez(srez);
                else
                    srezTable.MarkSrezAsModified(srez);

                // 记录对切片表的更改
                srezAdapter.Update(srezTable);
                srezTable.FileModTime = File.GetLastWriteTime(fileName);
                return true;
            } catch (Exception ex) {
                string fileNameStr = string.IsNullOrEmpty(fileName)
                    ? ""
                    : Environment.NewLine + (Localization.UseRussian ? "Имя файла: " : "Filename: ") + fileName;
                AppLog.WriteAction(string.Format(
                    Localization.UseRussian
                        ? "Ошибка при записи принятого среза в таблицу архивных срезов: {0}{1}"
                        : "Error writing received snapshot in the archive data table: {0}{1}",
                    ex.Message, fileNameStr), Log.ActTypes.Exception);
                return false;
            }
        }

        /// <summary>
        /// 将事件写入事件表
        /// </summary>
        private bool WriteEvent(string tableName, EventAdapter eventAdapter, EventTableLight.Event ev) {
            var fileName = "";

            try {
                lock (eventAdapter) {
                    eventAdapter.TableName = tableName;
                    fileName = eventAdapter.FileName;
                    eventAdapter.AppendEvent(ev);

                    if (Settings.DetailedLog) {
                        string tableDescr = eventAdapter == this.eventAdapter
                            ? (Localization.UseRussian ? "событий" : "event")
                            : (Localization.UseRussian ? "копий событий" : "event copy");
                        AppLog.WriteAction(string.Format(
                            Localization.UseRussian
                                ? "Запись события в таблицу {0} завершена"
                                : "Writing event in the {0} table is completed",
                            tableDescr), Log.ActTypes.Action);
                    }

                    return true;
                }
            } catch (Exception ex) {
                string fileNameStr = string.IsNullOrEmpty(fileName)
                    ? ""
                    : Environment.NewLine + (Localization.UseRussian ? "Имя файла: " : "Filename: ") + fileName;
                AppLog.WriteAction(string.Format(
                    Localization.UseRussian
                        ? "Ошибка при записи события в таблицу событий: {0}{1}"
                        : "Error writing event in the event table: {0}{1}",
                    ex.Message, fileNameStr), Log.ActTypes.Exception);
                return false;
            }
        }

        /// <summary>
        /// 根据设置在事件表中记录事件，执行模块的操作
        /// </summary>
        private bool WriteEvent(EventTableLight.Event ev) {
            // 在记录之前执行模块操作
            RaiseOnEventCreating(ev);

            // 事件记录
            string tableName = "e" + ev.DateTime.ToString("yyMMdd") + ".dat";
            bool writeOk1 = Settings.WriteEv ? WriteEvent(tableName, eventAdapter, ev) : true;
            bool writeOk2 = Settings.WriteEvCopy ? WriteEvent(tableName, eventCopyAdapter, ev) : true;

            // 写完后执行模块动作
            RaiseOnEventCreated(ev);

            return writeOk1 && writeOk2;
        }

        /// <summary>
        /// 记录指定列表中的事件并清除列表
        /// </summary>
        private void WriteEvents(List<EventTableLight.Event> events) {
            if (events.Count > 0) {
                foreach (var ev in events) {
                    WriteEvent(ev);
                }

                events.Clear();
            }
        }

        /// <summary>
        /// 将事件确认写入事件表
        /// </summary>
        private bool WriteEventCheck(string tableName, EventAdapter eventAdapter, int evNum, int userID) {
            var fileName = "";

            try {
                lock (eventAdapter) {
                    eventAdapter.TableName = tableName;
                    fileName = eventAdapter.FileName;
                    eventAdapter.CheckEvent(evNum, userID);

                    if (Settings.DetailedLog) {
                        string tableDescr = eventAdapter == this.eventAdapter
                            ? (Localization.UseRussian ? "событий" : "event")
                            : (Localization.UseRussian ? "копий событий" : "event copy");
                        AppLog.WriteAction(
                            string.Format(
                                Localization.UseRussian
                                    ? "Запись квитирования события в таблицу {0} завершена"
                                    : "Writing event check in the {0} table is completed", tableDescr),
                            Log.ActTypes.Action);
                    }

                    return true;
                }
            } catch (Exception ex) {
                string fileNameStr = string.IsNullOrEmpty(fileName)
                    ? ""
                    : Environment.NewLine + (Localization.UseRussian ? "Имя файла: " : "Filename: ") + fileName;
                AppLog.WriteAction(string.Format(
                    Localization.UseRussian
                        ? "Ошибка при записи квитирования события в таблицу событий: {0}{1}"
                        : "Error writing event check in the event table: {0}{1}",
                    ex.Message, fileNameStr), Log.ActTypes.Exception);
                return false;
            }
        }

        /// <summary>
        /// 计算输入通道数据
        /// </summary>
        private void CalcCnlData(InCnl inCnl, SrezTableLight.CnlData oldCnlData,
            ref SrezTableLight.CnlData newCnlData) {
            if (inCnl != null) {
                try {
                    // 计算新数据
                    if (inCnl.CalcCnlData != null)
                        inCnl.CalcCnlData(ref newCnlData);

                    // 增加开关数量
                    if (inCnl.CnlTypeID == BaseValues.CnlTypes.SWCNT &&
                        newCnlData.Stat > BaseValues.CnlStatuses.Undefined) {
                        bool even = (int) oldCnlData.Val % 2 == 0; // старое значение чётное
                        newCnlData.Val = newCnlData.Val <= 0 && even || newCnlData.Val > 0 && !even
                            ? Math.Truncate(oldCnlData.Val) + 1
                            : Math.Truncate(oldCnlData.Val);
                    }

                    // 如果设置了边界值检查，则调整新状态
                    if (newCnlData.Stat == BaseValues.CnlStatuses.Defined &&
                        (inCnl.LimLow < inCnl.LimHigh || inCnl.LimLowCrash < inCnl.LimHighCrash)) {
                        newCnlData.Stat = BaseValues.CnlStatuses.Normal;

                        if (inCnl.LimLow < inCnl.LimHigh) {
                            if (newCnlData.Val < inCnl.LimLow)
                                newCnlData.Stat = BaseValues.CnlStatuses.Low;
                            else if (newCnlData.Val > inCnl.LimHigh)
                                newCnlData.Stat = BaseValues.CnlStatuses.High;
                        }

                        if (inCnl.LimLowCrash < inCnl.LimHighCrash) {
                            if (newCnlData.Val < inCnl.LimLowCrash)
                                newCnlData.Stat = BaseValues.CnlStatuses.LowCrash;
                            else if (newCnlData.Val > inCnl.LimHighCrash)
                                newCnlData.Stat = BaseValues.CnlStatuses.HighCrash;
                        }
                    }
                } catch {
                    newCnlData.Stat = BaseValues.CnlStatuses.FormulaError;
                }
            }
        }

        /// <summary>
        /// 根据输入通道属性和数据生成事件。
        /// </summary>
        private EventTableLight.Event GenEvent(InCnl inCnl,
            SrezTableLight.CnlData oldCnlData, SrezTableLight.CnlData newCnlData) {
            if (inCnl.EvEnabled) {
                double oldVal = oldCnlData.Val;
                double newVal = newCnlData.Val;
                int oldStat = oldCnlData.Stat;
                int newStat = newCnlData.Stat;

                bool dataChanged =
                    oldStat > BaseValues.CnlStatuses.Undefined && newStat > BaseValues.CnlStatuses.Undefined &&
                    (oldVal != newVal || oldStat != newStat);

                if ( // 改变事件
                    inCnl.EvOnChange && dataChanged ||
                    // events on indefinite state and exit from it
                    inCnl.EvOnUndef &&
                    (oldStat > BaseValues.CnlStatuses.Undefined && newStat == BaseValues.CnlStatuses.Undefined ||
                     oldStat == BaseValues.CnlStatuses.Undefined && newStat > BaseValues.CnlStatuses.Undefined) ||
                    // normalization events
                    newStat == BaseValues.CnlStatuses.Normal &&
                    oldStat != newStat && oldStat != BaseValues.CnlStatuses.Undefined ||
                    // 轻描淡写和夸大其词的事件
                    (newStat == BaseValues.CnlStatuses.LowCrash || newStat == BaseValues.CnlStatuses.Low ||
                     newStat == BaseValues.CnlStatuses.High || newStat == BaseValues.CnlStatuses.HighCrash) &&
                    oldStat != newStat) {
                    // 事件创建
                    return new EventTableLight.Event() {
                        DateTime = DateTime.Now,
                        ObjNum = inCnl.ObjNum,
                        KPNum = inCnl.KPNum,
                        ParamID = inCnl.ParamID,
                        CnlNum = inCnl.CnlNum,
                        OldCnlVal = oldCnlData.Val,
                        OldCnlStat = oldStat,
                        NewCnlVal = newCnlData.Val,
                        NewCnlStat = dataChanged && oldStat == BaseValues.CnlStatuses.Defined &&
                                     newStat == BaseValues.CnlStatuses.Defined
                            ? BaseValues.CnlStatuses.Changed
                            : newStat
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// 计算预计算通道
        /// </summary>
        private void CalcDRCnls(List<InCnl> inCnls, SrezTableLight.Srez srez, List<EventTableLight.Event> outEvents) {
            lock (calculator) {
                try {
                    procSrez = srez;

                    foreach (var inCnl in inCnls) {
                        int cnlInd = srez.GetCnlIndex(inCnl.CnlNum);

                        if (cnlInd >= 0) {
                            // 计算新的输入通道数据
                            var oldCnlData = srez.CnlData[cnlInd];
                            var newCnlData =
                                new SrezTableLight.CnlData(oldCnlData.Val, BaseValues.CnlStatuses.Defined);
                            CalcCnlData(inCnl, oldCnlData, ref newCnlData);

                            // 将新数据写入切片
                            srez.CnlData[cnlInd] = newCnlData;

                            // 事件生成
                            if (outEvents != null) {
                                var ev = GenEvent(inCnl, oldCnlData, newCnlData);
                                if (ev != null)
                                    outEvents.Add(ev);
                            }
                        }
                    }
                } catch (Exception ex) {
                    AppLog.WriteAction(
                        (Localization.UseRussian
                            ? "Ошибка при вычислении дорасчётных каналов: "
                            : "Error calculating channels: ") + ex.Message, Log.ActTypes.Exception);
                } finally {
                    procSrez = null;
                }
            }
        }

        /// <summary>
        /// 设置非活动通道的不准确性
        /// </summary>
        private void SetUnreliable(List<EventTableLight.Event> outEvents) {
            if (Settings.InactUnrelTime > 0) {
                var inactUnrelSpan = TimeSpan.FromMinutes(Settings.InactUnrelTime);
                var nowDT = DateTime.Now;
                int cnlCnt = srezDescr.CnlNums.Length;

                for (var i = 0; i < cnlCnt; i++) {
                    var inCnl = inCnls.Values[i];
                    int cnlTypeID = inCnl.CnlTypeID;

                    if ((cnlTypeID == BaseValues.CnlTypes.TS || cnlTypeID == BaseValues.CnlTypes.TI) &&
                        curSrez.CnlData[i].Stat > BaseValues.CnlStatuses.Undefined &&
                        nowDT - activeDTs[i] > inactUnrelSpan) {
                        // 设置错误状态
                        var oldCnlData = curSrez.CnlData[i];
                        var newCnlData =
                            new SrezTableLight.CnlData(oldCnlData.Val, BaseValues.CnlStatuses.Unreliable);
                        curSrez.CnlData[i] = newCnlData;
                        curSrezMod = true;

                        // 事件生成
                        if (outEvents != null) {
                            var ev = GenEvent(inCnl, oldCnlData, newCnlData);
                            if (ev != null)
                                outEvents.Add(ev);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 写入有关应用程序的文件信息
        /// </summary>
        private void WriteInfo() {
            try {
                using (var writer = new StreamWriter(infoFileName, false, Encoding.UTF8)) {
                    var workSpan = DateTime.Now - startDT;
                    writer.WriteLine(AppInfoFormat,
                        startDT.ToLocalizedString(),
                        workSpan.Days > 0 ? workSpan.ToString(@"d\.hh\:mm\:ss") : workSpan.ToString(@"hh\:mm\:ss"),
                        workState, ServerUtils.AppVersion);
                    writer.WriteLine();
                    writer.Write(comm.GetClientsInfo());
                }
            } catch (Exception ex) {
                AppLog.WriteAction(
                    (Localization.UseRussian
                        ? "Ошибка при записи в файл информации о работе приложения: "
                        : "Error writing application information to the file: ") + ex.Message, Log.ActTypes.Exception);
            }
        }


        /// <summary>
        /// 为模块调用OnServerStart事件
        /// </summary>
        private void RaiseOnServerStart() {
            lock (modules) {
                foreach (var modLogic in modules) {
                    try {
                        modLogic.OnServerStart();
                    } catch (Exception ex) {
                        AppLog.WriteAction(string.Format(
                            Localization.UseRussian
                                ? "Ошибка при выполнении действий при запуске работы сервера в модуле {0}: {1}"
                                : "Error executing actions on server start in module {0}: {1}",
                            modLogic.Name, ex.Message), Log.ActTypes.Exception);
                    }
                }
            }
        }

        /// <summary>
        /// 提升模块的OnServerStop事件
        /// </summary>
        private void RaiseOnServerStop() {
            lock (modules) {
                foreach (var modLogic in modules) {
                    try {
                        modLogic.OnServerStop();
                    } catch (Exception ex) {
                        AppLog.WriteAction(string.Format(
                            Localization.UseRussian
                                ? "Ошибка при выполнении действий при остановке работы сервера в модуле {0}: {1}"
                                : "Error executing actions on server stop in module {0}: {1}",
                            modLogic.Name, ex.Message), Log.ActTypes.Exception);
                    }
                }
            }
        }

        /// <summary>
        /// 触发模块的OnCurDataProcessed事件
        /// </summary>
        private void RaiseOnCurDataProcessed(int[] cnlNums, SrezTableLight.Srez curSrez) {
            lock (modules) {
                foreach (var modLogic in modules) {
                    try {
                        modLogic.OnCurDataProcessed(cnlNums, curSrez);
                    } catch (Exception ex) {
                        AppLog.WriteAction(string.Format(
                            Localization.UseRussian
                                ? "Ошибка при выполнении действий после обработки новых текущих данных в модуле {0}: {1}"
                                : "Error executing actions on current data processed in module {0}: {1}",
                            modLogic.Name, ex.Message), Log.ActTypes.Exception);
                    }
                }
            }
        }

        /// <summary>
        /// 触发模块的OnCurDataCalculated事件
        /// </summary>
        private void RaiseOnCurDataCalculated(int[] cnlNums, SrezTableLight.Srez curSrez) {
            lock (modules) {
                foreach (var modLogic in modules) {
                    try {
                        modLogic.OnCurDataCalculated(cnlNums, curSrez);
                    } catch (Exception ex) {
                        AppLog.WriteAction(string.Format(Localization.UseRussian
                                ? "Ошибка при выполнении действий " +
                                  "после вычисления дорасчётных каналов текущего среза в модуле {0}: {1}"
                                : "Error executing actions on current data calculated in module {0}: {1}",
                            modLogic.Name, ex.Message), Log.ActTypes.Exception);
                    }
                }
            }
        }

        /// <summary>
        /// 触发模块的OnArcDataProcessed事件
        /// </summary>
        private void RaiseOnArcDataProcessed(int[] cnlNums, SrezTableLight.Srez arcSrez) {
            lock (modules) {
                foreach (var modLogic in modules) {
                    try {
                        modLogic.OnArcDataProcessed(cnlNums, arcSrez);
                    } catch (Exception ex) {
                        AppLog.WriteAction(string.Format(
                            Localization.UseRussian
                                ? "Ошибка при выполнении действий после обработки новых архивных данных в модуле {0}: {1}"
                                : "Error executing actions on archive data processed in module {0}: {1}",
                            modLogic.Name, ex.Message), Log.ActTypes.Exception);
                    }
                }
            }
        }

        /// <summary>
        /// 触发模块的OnEventCreating事件
        /// </summary>
        private void RaiseOnEventCreating(EventTableLight.Event ev) {
            lock (modules) {
                foreach (var modLogic in modules) {
                    try {
                        modLogic.OnEventCreating(ev);
                    } catch (Exception ex) {
                        AppLog.WriteAction(string.Format(
                            Localization.UseRussian
                                ? "Ошибка при выполнении действий при создании события в модуле {0}: {1}"
                                : "Error executing actions on event creating in module {0}: {1}",
                            modLogic.Name, ex.Message), Log.ActTypes.Exception);
                    }
                }
            }
        }

        /// <summary>
        /// 提升模块的OnEventCreated事件
        /// </summary>
        private void RaiseOnEventCreated(EventTableLight.Event ev) {
            lock (modules) {
                foreach (var modLogic in modules) {
                    try {
                        modLogic.OnEventCreated(ev);
                    } catch (Exception ex) {
                        AppLog.WriteAction(string.Format(
                            Localization.UseRussian
                                ? "Ошибка при выполнении действий после создания события в модуле {0}: {1}"
                                : "Error executing actions on event created in module {0}: {1}",
                            modLogic.Name, ex.Message), Log.ActTypes.Exception);
                    }
                }
            }
        }

        /// <summary>
        /// 提升模块的OnEventChecked事件
        /// </summary>
        private void RaiseOnEventChecked(DateTime date, int evNum, int userID) {
            lock (modules) {
                foreach (var modLogic in modules) {
                    try {
                        modLogic.OnEventChecked(date, evNum, userID);
                    } catch (Exception ex) {
                        AppLog.WriteAction(string.Format(
                            Localization.UseRussian
                                ? "Ошибка при выполнении действий после квитирования события в модуле {0}: {1}"
                                : "Error executing actions on event checked in module {0}: {1}",
                            modLogic.Name, ex.Message), Log.ActTypes.Exception);
                    }
                }
            }
        }

        /// <summary>
        /// 为模块调用OnCommandReceived事件
        /// </summary>
        private void RaiseOnCommandReceived(int ctrlCnlNum, Command cmd, int userID, ref bool passToClients) {
            lock (modules) {
                foreach (var modLogic in modules) {
                    try {
                        modLogic.OnCommandReceived(ctrlCnlNum, cmd, userID, ref passToClients);
                    } catch (Exception ex) {
                        AppLog.WriteAction(string.Format(
                            Localization.UseRussian
                                ? "Ошибка при выполнении действий после приёма команды ТУ в модуле {0}: {1}"
                                : "Error executing actions on command received in module {0}: {1}",
                            modLogic.Name, ex.Message), Log.ActTypes.Exception);
                    }
                }
            }
        }


        /// <summary>
        /// 初始化对象
        /// </summary>
        public void Init(string exeDir) {
            AppDirs.Init(exeDir);
            AppLog.FileName = AppDirs.LogDir + ServerUtils.AppLogFileName;
            infoFileName = AppDirs.LogDir + ServerUtils.AppStateFileName;
        }

        /// <summary>
        /// 启动服务器
        /// </summary>
        public bool Start() {
            try {
                // 停止工作
                Stop();

                // 开始工作
                startDT = DateTime.Now;
                string errMsg;

                if (Settings.Load(AppDirs.ConfigDir + Settings.DefFileName, out errMsg)) {
                    LoadModules();

                    if (CheckDataDirs() && CheckBaseFiles() && ReadBase() && InitCalculator() && comm.Start()) {
                        AppLog.WriteAction(Localization.UseRussian ? "Запуск работы сервера" : "Start server",
                            Log.ActTypes.Action);
                        terminated = false;
                        serverIsReady = false;
                        thread = new Thread(new ThreadStart(Execute));
                        thread.Start();
                    }
                } else {
                    AppLog.WriteAction(errMsg, Log.ActTypes.Error);
                }

                return thread != null;
            } catch (Exception ex) {
                AppLog.WriteAction(
                    (Localization.UseRussian ? "Ошибка при запуске работы сервера: " : "Error starting server: ") +
                    ex.Message, Log.ActTypes.Exception);
                return false;
            } finally {
                if (thread == null) {
                    workState = WorkStateNames.Error;
                    WriteInfo();
                }
            }
        }

        /// <summary>
        /// 停止服务器操作
        /// </summary>
        public void Stop() {
            try {
                // 停止客户互动
                comm.Stop();

                // 停止服务器工作流程
                if (thread != null) {
                    serverIsReady = false;
                    terminated = true;

                    if (thread.Join(WaitForStop)) {
                        AppLog.WriteAction(Localization.UseRussian ? "Работа сервера остановлена" : "Server is stopped",
                            Log.ActTypes.Action);
                    } else {
                        thread.Abort();
                        AppLog.WriteAction(Localization.UseRussian ? "Работа сервера прервана" : "Server is aborted",
                            Log.ActTypes.Action);
                    }

                    thread = null;
                }
            } catch (Exception ex) {
                workState = WorkStateNames.Error;
                WriteInfo();
                AppLog.WriteAction(
                    (Localization.UseRussian ? "Ошибка при остановке работы сервера: " : "Error stop server: ") +
                    ex.Message, Log.ActTypes.Exception);
            }
        }


        /// <summary>
        /// 按ID获取控制通道
        /// </summary>
        public CtrlCnl GetCtrlCnl(int ctrlCnlNum) {
            lock (ctrlCnls) {
                CtrlCnl ctrlCnl;
                return ctrlCnls.TryGetValue(ctrlCnlNum, out ctrlCnl) ? ctrlCnl.Clone() : null;
            }
        }

        /// <summary>
        /// 检查用户名和密码，获取他的角色
        /// </summary>
        /// <remarks>如果密码为空，则不会检查。</remarks>
        public bool CheckUser(string username, string password, out int roleID) {
            // 用户检查模块
            lock (modules) {
                foreach (var modLogic in modules) {
                    try {
                        bool isValid = modLogic.ValidateUser(username, password, out int modRoleID, out bool handled);

                        if (handled) {
                            roleID = modRoleID;
                            return isValid;
                        }
                    } catch (Exception ex) {
                        AppLog.WriteAction(string.Format(
                            Localization.UseRussian
                                ? "Ошибка при при проверке имени и пароля пользователя в модуле {0}: {1}"
                                : "Error validating user name and password in module {0}: {1}",
                            modLogic.Name, ex.Message), Log.ActTypes.Exception);
                    }
                }
            }

            // 检查用户配置数据库
            lock (users) {
                if (users.TryGetValue(username.Trim().ToLowerInvariant(), out var user) &&
                    (string.IsNullOrEmpty(password) || password == user.Password)) {
                    roleID = user.RoleID;
                    return true;
                } else {
                    roleID = BaseValues.Roles.Err;
                    return false;
                }
            }
        }

        /// <summary>
        /// 获取包含指定通道数据的切片表。
        /// </summary>
        /// <remarks>频道号必须按升序排序。</remarks>
        public SrezTableLight GetSnapshotTable(DateTime date, SnapshotTypes snapshotType, int[] cnlNums) {
            try {
                SrezTableLight destSnapshotTable = null;
                int cnlCnt = cnlNums == null ? 0 : cnlNums.Length;

                if (serverIsReady && cnlCnt > 0) {
                    destSnapshotTable = new SrezTableLight();

                    if (snapshotType == SnapshotTypes.Cur) {
                        lock (curSrez) {
                            var destSnapshot =
                                new SrezTableLight.Srez(DateTime.MinValue, cnlNums, curSrez);
                            destSnapshotTable.SrezList.Add(destSnapshot.DateTime, destSnapshot);
                        }
                    } else {
                        // 获取切片表缓存
                        var srezTableCache = GetSrezTableCache(date.Date, snapshotType);

                        lock (srezTableCache) {
                            // 填充缓存中的切片表
                            srezTableCache.FillSrezTable();

                            // 创建一个新的切片表并将指定通道的数据复制到其中
                            var srcSnapshotTable = srezTableCache.SrezTable;
                            SrezTable.SrezDescr prevSnapshotDescr = null;
                            var cnlNumIndexes = new int[cnlCnt];

                            foreach (SrezTable.Srez srcSnapshot in srcSnapshotTable.SrezList.Values) {
                                // 渠道索引
                                if (!srcSnapshot.SrezDescr.Equals(prevSnapshotDescr)) {
                                    for (var i = 0; i < cnlCnt; i++)
                                        cnlNumIndexes[i] = Array.BinarySearch(srcSnapshot.CnlNums, cnlNums[i]);
                                }

                                // 创建并填充包含指定通道的切片
                                var destSnapshot =
                                    new SrezTableLight.Srez(srcSnapshot.DateTime, cnlCnt);

                                for (var i = 0; i < cnlCnt; i++) {
                                    destSnapshot.CnlNums[i] = cnlNums[i];
                                    int cnlNumInd = cnlNumIndexes[i];
                                    destSnapshot.CnlData[i] = cnlNumInd < 0
                                        ? SrezTableLight.CnlData.Empty
                                        : srcSnapshot.CnlData[cnlNumInd];
                                }

                                destSnapshotTable.SrezList.Add(destSnapshot.DateTime, destSnapshot);
                                prevSnapshotDescr = srcSnapshot.SrezDescr;
                            }
                        }
                    }
                }

                return destSnapshotTable;
            } catch (Exception ex) {
                AppLog.WriteException(ex,
                    Localization.UseRussian ? "Ошибка при получении таблицы срезов" : "Error getting snapshot table");
                return null;
            }
        }

        /// <summary>
        /// 获取包含给定通道数据的当前切片
        /// </summary>
        /// <remarks>频道号必须按升序排序</remarks>
        public SrezTableLight.Srez GetCurSnapshot(int[] cnlNums) {
            return GetSnapshot(DateTime.MinValue, SnapshotTypes.Cur, cnlNums);
        }

        /// <summary>
        /// 获取包含给定通道数据的切片
        /// </summary>
        /// <remarks>频道号必须按升序排序。</remarks>
        public SrezTableLight.Srez GetSnapshot(DateTime dateTime, SnapshotTypes snapshotType, int[] cnlNums) {
            try {
                int cnlCnt = cnlNums == null ? 0 : cnlNums.Length;

                if (serverIsReady && cnlCnt > 0) {
                    if (snapshotType == SnapshotTypes.Cur) {
                        lock (curSrez) {
                            return new SrezTableLight.Srez(DateTime.MinValue, cnlNums, curSrez);
                        }
                    } else {
                        // 获取切片表缓存
                        var srezTableCache = GetSrezTableCache(dateTime.Date, snapshotType);

                        lock (srezTableCache) {
                            // 填充缓存中的切片表
                            srezTableCache.FillSrezTable();
                            SrezTableLight.Srez srcSnapshot = srezTableCache.SrezTable.GetSrez(dateTime);

                            // 使用指定的通道创建切片
                            return srcSnapshot == null
                                ? new SrezTableLight.Srez(dateTime, cnlNums)
                                : new SrezTableLight.Srez(dateTime, cnlNums, srcSnapshot);
                        }
                    }
                } else {
                    return null;
                }
            } catch (Exception ex) {
                AppLog.WriteException(ex,
                    Localization.UseRussian ? "Ошибка при получении среза" : "Error getting snapshot");
                return null;
            }
        }

        /// <summary>
        /// 处理新的当前数据
        /// </summary>
        public bool ProcCurData(SrezTableLight.Srez receivedSrez) {
            try {
                if (serverIsReady) {
                    int cnlCnt = receivedSrez == null ? 0 : receivedSrez.CnlNums.Length;

                    if (cnlCnt > 0) {
                        var events = new List<EventTableLight.Event>();

                        lock (curSrez)
                        lock (calculator) {
                            try {
                                procSrez = curSrez;

                                for (var i = 0; i < cnlCnt; i++) {
                                    int cnlNum = receivedSrez.CnlNums[i];
                                    int cnlInd = curSrez.GetCnlIndex(cnlNum);

                                    if (cnlInd >= 0 && inCnls.TryGetValue(cnlNum, out var inCnl)) // канал существует
                                    {
                                        if (inCnl.CnlTypeID == BaseValues.CnlTypes.TS ||
                                            inCnl.CnlTypeID == BaseValues.CnlTypes.TI) {
                                            // 计算新的输入通道数据
                                            var oldCnlData = curSrez.CnlData[cnlInd];
                                            var newCnlData = receivedSrez.CnlData[i];
                                            CalcCnlData(inCnl, oldCnlData, ref newCnlData);

                                            // 用于平均的数据计算
                                            if (inCnl.Averaging &&
                                                newCnlData.Stat > BaseValues.CnlStatuses.Undefined &&
                                                newCnlData.Stat != BaseValues.CnlStatuses.FormulaError &&
                                                newCnlData.Stat != BaseValues.CnlStatuses.Unreliable) {
                                                minAvgData[cnlInd].Sum += newCnlData.Val;
                                                minAvgData[cnlInd].Cnt++;
                                                hrAvgData[cnlInd].Sum += newCnlData.Val;
                                                hrAvgData[cnlInd].Cnt++;
                                            }

                                            // 将新数据写入当前切片
                                            curSrez.CnlData[cnlInd] = newCnlData;

                                            // 事件生成
                                            var ev = GenEvent(inCnl, oldCnlData, newCnlData);
                                            if (ev != null)
                                                events.Add(ev);

                                            // 更新频道活动信息
                                            activeDTs[cnlInd] = DateTime.Now;
                                        } else {
                                            // 将新数据记录到当前切片中而不计算预先计算的通道
                                            curSrez.CnlData[cnlInd] = receivedSrez.CnlData[i];
                                        }
                                    }
                                }
                            } finally {
                                procSrez = null;
                                curSrezMod = true;
                            }
                        }

                        // 使用模块记录和处理事件
                        foreach (var ev in events) {
                            WriteEvent(ev);
                        }

                        // 执行模块操作
                        RaiseOnCurDataProcessed(receivedSrez.CnlNums, curSrez);
                    }

                    return true;
                } else {
                    return false;
                }
            } catch (Exception ex) {
                AppLog.WriteException(ex,
                    Localization.UseRussian
                        ? "Ошибка при обработке новых текущих данных"
                        : "Error processing new current data");
                return false;
            }
        }

        /// <summary>
        /// 处理新的存档数据
        /// </summary>
        public bool ProcArcData(SrezTableLight.Srez receivedSrez) {
            try {
                if (serverIsReady) {
                    var result = true;
                    int cnlCnt = receivedSrez == null ? 0 : receivedSrez.CnlNums.Length;

                    if (cnlCnt > 0) {
                        // 确定记录存档数据的时间
                        var paramSrezDT = receivedSrez.DateTime;
                        var paramSrezDate = paramSrezDT.Date;
                        var nearestMinDT = CalcNearestTime(paramSrezDT, Settings.WriteMinPer);
                        var nearestHrDT = CalcNearestTime(paramSrezDT, Settings.WriteHrPer);

                        // 获取切片表缓存
                        var minCache = Settings.WriteMin || Settings.WriteMinCopy
                            ? GetSrezTableCache(paramSrezDate, SnapshotTypes.Min)
                            : null;
                        var hrCache =
                            nearestHrDT == paramSrezDT && (Settings.WriteHr || Settings.WriteHrCopy)
                                ? GetSrezTableCache(paramSrezDate, SnapshotTypes.Hour)
                                : null;
                        SrezTableLight.Srez arcSrez = null;

                        // 分钟记录
                        if (minCache != null) {
                            lock (minCache) {
                                if (Settings.WriteMin && !WriteReceivedSrez(minCache.SrezTable,
                                        minCache.SrezAdapter, receivedSrez, nearestMinDT, ref arcSrez))
                                    result = false;

                                if (Settings.WriteMinCopy && !WriteReceivedSrez(minCache.SrezTableCopy,
                                        minCache.SrezCopyAdapter, receivedSrez, nearestMinDT, ref arcSrez))
                                    result = false;
                            }
                        }

                        // 小时记录
                        if (hrCache != null) {
                            lock (hrCache) {
                                if (Settings.WriteHr && !WriteReceivedSrez(hrCache.SrezTable,
                                        hrCache.SrezAdapter, receivedSrez, nearestHrDT, ref arcSrez))
                                    result = false;

                                if (Settings.WriteHrCopy && !WriteReceivedSrez(hrCache.SrezTableCopy,
                                        hrCache.SrezCopyAdapter, receivedSrez, nearestHrDT, ref arcSrez))
                                    result = false;
                            }
                        }

                        // 执行模块操作
                        RaiseOnArcDataProcessed(receivedSrez.CnlNums, arcSrez);
                    }

                    return result;
                } else {
                    return false;
                }
            } catch (Exception ex) {
                AppLog.WriteException(ex,
                    Localization.UseRussian
                        ? "Ошибка при обработке новых архивных данных"
                        : "Error processing new archive data");
                return false;
            }
        }

        /// <summary>
        /// 获取已处理切片的输入通道数据
        /// </summary>
        /// <remarks>该方法用于计算配置基础公式。</remarks>
        public SrezTableLight.CnlData GetProcSrezCnlData(int cnlNum) {
            var cnlData = SrezTableLight.CnlData.Empty;

            try {
                if (procSrez != null)
                    procSrez.GetCnlData(cnlNum, out cnlData);
            } catch (Exception ex) {
                AppLog.WriteException(ex,
                    Localization.UseRussian
                        ? "Ошибка при получении данных входного канала обрабатываемого среза"
                        : "Error getting input channel data of the processed snapshot");
            }

            return cnlData;
        }

        /// <summary>
        /// 设置已处理切片的输入通道数据
        /// </summary>
        /// <remarks>该方法用于计算配置基础公式。</remarks>
        public void SetProcSrezCnlData(int cnlNum, SrezTableLight.CnlData cnlData) {
            try {
                if (procSrez == curSrez) {
                    // 为当前切片生成事件的数据设置
                    int cnlInd = procSrez.GetCnlIndex(cnlNum);

                    if (cnlInd >= 0 && inCnls.TryGetValue(cnlNum, out var inCnl)) {
                        var oldCnlData = procSrez.CnlData[cnlInd];
                        procSrez.CnlData[cnlInd] = cnlData;
                        var ev = GenEvent(inCnl, oldCnlData, cnlData);

                        if (ev != null) {
                            lock (eventsToWrite) {
                                eventsToWrite.Add(ev);
                            }
                        }
                    }
                } else if (procSrez != null) {
                    // 设置数据而不生成事件
                    procSrez.SetCnlData(cnlNum, cnlData);
                }
            } catch (Exception ex) {
                AppLog.WriteException(ex,
                    Localization.UseRussian
                        ? "Ошибка при установке данных входного канала обрабатываемого среза"
                        : "Error setting input channel data of the processed snapshot");
            }
        }

        /// <summary>
        /// 处理新事件
        /// </summary>
        public bool ProcEvent(EventTableLight.Event ev) {
            if (serverIsReady)
                return ev == null ? true : WriteEvent(ev);
            else
                return false;
        }

        /// <summary>
        /// 确认事件（Acknowledge event）
        /// </summary>
        public bool CheckEvent(DateTime date, int evNum, int userID) {
            if (serverIsReady) {
                // 事件确认记录
                string tableName = EventAdapter.BuildEvTableName(date);
                bool writeOk1 = Settings.WriteEv ? WriteEventCheck(tableName, eventAdapter, evNum, userID) : true;
                bool writeOk2 = Settings.WriteEvCopy
                    ? WriteEventCheck(tableName, eventCopyAdapter, evNum, userID)
                    : true;

                // 执行模块操作
                RaiseOnEventChecked(date, evNum, userID);

                return writeOk1 && writeOk2;
            } else {
                return false;
            }
        }

        /// <summary>
        /// 处理命令TU
        /// </summary>
        public void ProcCommand(CtrlCnl ctrlCnl, Command cmd, int userID, out bool passToClients) {
            passToClients = false;

            if (serverIsReady && ctrlCnl != null && cmd != null) {
                int ctrlCnlNum = ctrlCnl.CtrlCnlNum;

                // 使用控制通道公式计算命令值或数据
                if (ctrlCnl.CalcCmdVal != null) {
                    // 标准命令值计算
                    lock (curSrez)
                    lock (calculator) {
                        try {
                            procSrez = curSrez; // 公式Val（n）和Stat（n）运算所必需的
                            double cmdVal = cmd.CmdVal;
                            ctrlCnl.CalcCmdVal(ref cmdVal);
                            cmd.CmdVal = cmdVal;
                            passToClients = !double.IsNaN(cmdVal);
                        } catch (Exception ex) {
                            AppLog.WriteError(string.Format(
                                Localization.UseRussian
                                    ? "Ошибка при вычислении значения стандартной команды для канала управления {0}: {1}"
                                    : "Error calculating standard command value for the output channel {0}: {1}",
                                ctrlCnlNum, ex.Message));
                            cmd.CmdVal = double.NaN;
                        } finally {
                            procSrez = null;
                        }
                    }
                } else if (ctrlCnl.CalcCmdData != null) {
                    // 计算二进制命令数据
                    lock (curSrez)
                    lock (calculator) {
                        try {
                            procSrez = curSrez;
                            byte[] cmdData = cmd.CmdData;
                            ctrlCnl.CalcCmdData(ref cmdData);
                            cmd.CmdData = cmdData;
                            passToClients = cmdData != null;
                        } catch (Exception ex) {
                            AppLog.WriteError(string.Format(
                                Localization.UseRussian
                                    ? "Ошибка при вычислении данных бинарной команды для канала управления {0}: {1}"
                                    : "Error calculating binary command data for the output channel {0}: {1}",
                                ctrlCnlNum, ex.Message));
                            cmd.CmdVal = double.NaN;
                        } finally {
                            procSrez = null;
                        }
                    }
                } else {
                    passToClients = true;
                }

                // 在接收命令后执行模块动作
                RaiseOnCommandReceived(ctrlCnlNum, cmd, userID, ref passToClients);

                // 事件创建
                if (passToClients && ctrlCnl.EvEnabled) {
                    var ev = new EventTableLight.Event();
                    ev.DateTime = DateTime.Now;
                    ev.ObjNum = ctrlCnl.ObjNum;
                    ev.KPNum = ctrlCnl.KPNum;
                    ev.Descr = cmd.GetCmdDescr(ctrlCnlNum, userID);

                    // 事件记录和模块动作执行
                    WriteEvent(ev);
                }

                // 如果未指定命令编号，则取消命令传输
                if (ctrlCnl.CmdNum <= 0)
                    passToClients = false;
            }
        }
    }
}