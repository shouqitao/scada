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
 * Summary  : Program execution management
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2015
 * Modified : 2018
 */

using Scada.Server.Modules;
using System;
using System.IO;
using System.Reflection;
using Utils;

namespace Scada.Server.Engine {
    /// <summary>
    /// Program execution management
    /// <para>Program Management</para>
    /// </summary>
    public sealed class Manager {
        private readonly Log appLog; // application log

        private readonly MainLogic mainLogic; // an object that implements server logic


        /// <summary>
        /// Constructor
        /// </summary>
        public Manager() {
            mainLogic = new MainLogic();
            appLog = mainLogic.AppLog;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }


        /// <summary>
        /// 关闭计算机时立即关闭服务
        /// </summary>
        public void ShutdownService() {
            mainLogic.Stop();
            appLog.WriteAction("ScadaServerService is shutdown");
            appLog.WriteBreak();
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        public void StartService() {
            // 初始化逻辑对象
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            mainLogic.Init(exeDir);

            appLog.WriteBreak();
            appLog.WriteAction($"ScadaServerService {ServerUtils.AppVersion} is started", Log.ActTypes.Action);

            if (mainLogic.AppDirs.Exist) {
                // 本地化
                if (Localization.LoadDictionaries(mainLogic.AppDirs.LangDir, "ScadaData", out string errMsg))
                    CommonPhrases.Init();
                else
                    appLog.WriteError(errMsg);

                if (Localization.LoadDictionaries(mainLogic.AppDirs.LangDir, "ScadaServer", out errMsg))
                    ModPhrases.InitFromDictionaries();
                else
                    appLog.WriteError(errMsg);

                // 启动
                if (mainLogic.Start())
                    return;
            } else {
                appLog.WriteError(
                    $"The required directories do not exist:{Environment.NewLine}{string.Join(Environment.NewLine, mainLogic.AppDirs.GetRequiredDirs())}");
            }

            appLog.WriteError("Normal program execution is impossible");
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        public void StopService() {
            mainLogic.Stop();
            appLog.WriteAction("ScadaServerService is stopped");
            appLog.WriteBreak();
        }

        /// <summary>
        /// 在日志中打印未处理的异常信息
        /// </summary>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args) {
            var ex = args.ExceptionObject as Exception;
            appLog.WriteException(ex, "Unhandled exception");
        }
    }
}