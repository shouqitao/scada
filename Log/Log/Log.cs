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
 * Module   : Log
 * Summary  : Log file implementation
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2005
 * Modified : 2017
 */

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace Utils {
    /// <summary>
    /// Log file implementation
    /// <para>日志文件实现</para>
    /// </summary>
    public class Log : ILog {
        /// <summary>
        /// 记录在日志中的操作类型
        /// </summary>
        public enum ActTypes {
            /// <summary>
            /// 信息
            /// </summary>
            Information,

            /// <summary>
            /// 动作
            /// </summary>
            Action,

            /// <summary>
            /// 错误
            /// </summary>
            Error,

            /// <summary>
            /// 异常
            /// </summary>
            Exception,
        }

        /// <summary>
        /// 日志格式    
        /// </summary>
        public enum Formats {
            /// <summary>
            /// 简单（日期，时间，描述）
            /// </summary>
            Simple,

            /// <summary>
            /// 完整（日期，时间，计算机，用户，操作，描述）
            /// </summary>
            Full
        }

        /// <summary>
        /// 行日志条目委托
        /// </summary>
        public delegate void WriteLineDelegate(string text);

        /// <summary>
        /// 记录操作委托
        /// </summary>
        public delegate void WriteActionDelegate(string text, ActTypes actType);

        /// <summary>
        /// 默认文件的容量（最大大小）为1 MB
        /// </summary>
        public const int DefCapacity = 1048576;

        private readonly Formats format; // format
        private readonly object writeLock; // object用于同步来自不同线程的日志访问
        private StreamWriter writer; // the object to be written to the file
        private FileInfo fileInfo; // fileInfo


        /// <summary>
        /// 创建Log类的新实例
        /// </summary>
        protected Log() {
            format = Formats.Simple;
            writeLock = new object();
            writer = null;
            fileInfo = null;

            FileName = "";
            Encoding = Encoding.Default;
            Capacity = DefCapacity;
            CompName = Environment.MachineName;
            UserName = Environment.UserName;
            Break = new string('-', 80);
            DateTimeFormat = "yyyy'-'MM'-'dd' 'HH':'mm':'ss";
        }

        /// <summary>
        /// 使用指定的记录格式创建Log类的新实例
        /// </summary>
        public Log(Formats logFormat)
            : this() {
            format = logFormat;
        }


        /// <summary>
        /// 获取或设置日志名称
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 获取或设置日志编码
        /// </summary>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// 获取或设置日志容量（最大大小）
        /// </summary>
        public int Capacity { get; set; }

        /// <summary>
        /// 获取计算机名称
        /// Get computer name
        /// </summary>
        public string CompName { get; private set; }

        /// <summary>
        /// 获取用户名
        /// </summary>
        public string UserName { get; private set; }

        /// <summary>
        /// 获取或设置分隔符
        /// </summary>
        public string Break { get; set; }

        /// <summary>
        /// 获取或设置日期和时间格式
        /// </summary>
        public string DateTimeFormat { get; set; }


        /// <summary>
        /// 打开日志以添加信息
        /// </summary>
        protected void Open() {
            try {
                writer = new StreamWriter(FileName, true, Encoding);
                fileInfo = new FileInfo(FileName);
            } catch {
                // ignored
            }
        }

        /// <summary>
        /// 关闭日志
        /// </summary>
        protected void Close() {
            try {
                if (writer != null) {
                    writer.Close();
                    writer = null;
                }
            } catch {
                // ignored
            }
        }

        /// <summary>
        /// 获取操作类型的字符串表示形式
        /// </summary>
        protected string ActTypeToStr(ActTypes actType) {
            switch (actType) {
                case ActTypes.Exception:
                    return "EXC";
                case ActTypes.Error:
                    return "ERR";
                case ActTypes.Action:
                    return "ACT";
                default: // ActTypes.Information:
                    return "INF";
            }
        }


        /// <summary>
        /// 将特定类型的操作记录到日志中。
        /// </summary>
        public void WriteAction(string text, ActTypes actType) {
            var sb = new StringBuilder(DateTime.Now.ToString(DateTimeFormat));

            if (format == Formats.Simple) {
                WriteLine(sb.Append(" ").Append(text).ToString());
            } else {
                WriteLine(sb.Append(" <")
                    .Append(CompName).Append("><")
                    .Append(UserName).Append("><")
                    .Append(ActTypeToStr(actType)).Append("> ")
                    .Append(text).ToString());
            }
        }

        /// <summary>
        /// 记录信息操作以记录
        /// </summary>
        public void WriteInfo(string text) {
            WriteAction(text, ActTypes.Information);
        }

        /// <summary>
        /// 记录正常操作以记录日志
        /// </summary>
        public void WriteAction(string text) {
            WriteAction(text, ActTypes.Action);
        }

        /// <summary>
        /// 记录错误
        /// </summary>
        public void WriteError(string text) {
            WriteAction(text, ActTypes.Error);
        }

        /// <summary>
        /// 记录异常以记录日志
        /// </summary>
        public void WriteException(Exception ex, string errMsg = "", params object[] args) {
            if (string.IsNullOrEmpty(errMsg)) {
                WriteAction(ex.ToString(), ActTypes.Exception);
            } else {
                WriteAction(new StringBuilder()
                        .Append(args == null || args.Length == 0 ? errMsg : string.Format(errMsg, args))
                        .Append(":").Append(Environment.NewLine)
                        .Append(ex.ToString()).ToString(),
                    ActTypes.Exception);
            }
        }

        /// <summary>
        /// 记录行
        /// </summary>
        public void WriteLine(string text = "") {
            try {
                Monitor.Enter(writeLock);
                Open();
                if (fileInfo.Length > Capacity) {
                    string bakName = FileName + ".bak";
                    writer.Close();
                    File.Delete(bakName);
                    File.Move(FileName, bakName);

                    writer = new StreamWriter(FileName, true, Encoding);
                }

                writer.WriteLine(text);
                writer.Flush();
            } catch {
                // ignored
            } finally {
                Close();
                Monitor.Exit(writeLock);
            }
        }

        /// <summary>
        /// 日志分隔符
        /// </summary>
        public void WriteBreak() {
            WriteLine(Break);
        }
    }
}