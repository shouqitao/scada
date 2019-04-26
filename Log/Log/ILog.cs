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
 * Summary  : Log interface
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2017
 * Modified : 2017
 */

using System;

namespace Utils {
    /// <summary>
    /// Log interface
    /// <para>日志接口</para>
    /// </summary>
    public interface ILog {
        /// <summary>
        /// 将特定类型的操作记录到日志中。
        /// </summary>
        void WriteAction(string text, Log.ActTypes actType);

        /// <summary>
        /// 记录信息操作以记录
        /// </summary>
        void WriteInfo(string text);

        /// <summary>
        /// 记录正常操作以记录日志
        /// </summary>
        void WriteAction(string text);

        /// <summary>
        /// 记录错误
        /// </summary>
        void WriteError(string text);

        /// <summary>
        /// 记录异常
        /// </summary>
        void WriteException(Exception ex, string errMsg = "", params object[] args);

        /// <summary>
        /// 记录行
        /// </summary>
        void WriteLine(string text = "");

        /// <summary>
        /// 日志分隔符
        /// </summary>
        void WriteBreak();
    }
}