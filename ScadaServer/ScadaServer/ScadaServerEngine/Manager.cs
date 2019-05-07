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
        private MainLogic mainLogic; // an object that implements server logic
        private Log appLog; // application log


        /// <summary>
        /// Constructor
        /// </summary>
        public Manager() {
            mainLogic = new MainLogic();
            appLog = mainLogic.AppLog;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }


        /// <summary>
        /// 在日志中打印未处理的异常信息
        /// </summary>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args) {
            var ex = args.ExceptionObject as Exception;
            appLog.WriteException(ex,
                string.Format(Localization.UseRussian ? "Необработанное исключение" : "Unhandled exception"));
        }


        /// <summary>
        /// 启动服务
        /// </summary>
        public void StartService() {
            // 初始化逻辑对象
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            mainLogic.Init(exeDir);

            appLog.WriteBreak();
            appLog.WriteAction(
                string.Format(
                    Localization.UseRussian
                        ? "Служба ScadaServerService {0} запущена"
                        : "ScadaServerService {0} is started", ServerUtils.AppVersion), Log.ActTypes.Action);

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
                appLog.WriteError(string.Format(
                    Localization.UseRussian
                        ? "Необходимые директории не существуют:{0}{1}"
                        : "The required directories do not exist:{0}{1}",
                    Environment.NewLine, string.Join(Environment.NewLine, mainLogic.AppDirs.GetRequiredDirs())));
            }

            appLog.WriteError(Localization.UseRussian
                ? "Нормальная работа программы невозможна"
                : "Normal program execution is impossible");
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        public void StopService() {
            mainLogic.Stop();
            appLog.WriteAction(Localization.UseRussian
                ? "Служба ScadaServerService остановлена"
                : "ScadaServerService is stopped");
            appLog.WriteBreak();
        }

        /// <summary>
        /// 关闭计算机时立即关闭服务
        /// </summary>
        public void ShutdownService() {
            mainLogic.Stop();
            appLog.WriteAction(Localization.UseRussian
                ? "Служба ScadaServerService отключена"
                : "ScadaServerService is shutdown");
            appLog.WriteBreak();
        }
    }
}