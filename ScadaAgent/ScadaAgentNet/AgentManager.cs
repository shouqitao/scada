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
 * Module   : ScadaAgentNet
 * Summary  : Agent manager
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using Scada.Agent.Engine;
using System;
using System.IO;
using System.Reflection;
using System.ServiceModel;
using Utils;

namespace Scada.Agent.Net {
    /// <summary>
    /// Agent manager
    /// <para>Agent Manager</para>
    /// </summary>
    public class AgentManager {
        private ILog log; // application log
        private AgentLogic agentLogic; // the object that implements the main logic of the agent
        private ServiceHost agentSvcHost; // WCF service host for agent interaction


        /// <summary>
        /// Constructor
        /// </summary>
        public AgentManager() {
            log = new LogStub();
            agentLogic = null;
            agentSvcHost = null;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }


        /// <summary>
        /// Print unhandled exception information in a log
        /// </summary>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args) {
            var ex = args.ExceptionObject as Exception;
            log.WriteException(ex,
                string.Format(Localization.UseRussian ? "Необработанное исключение" : "Unhandled exception"));
        }

        /// <summary>
        /// Run WCF service to interact with the agent
        /// </summary>
        private bool StartWcfService() {
            try {
                agentSvcHost = new ServiceHost(typeof(AgentSvc));
                var behavior =
                    agentSvcHost.Description.Behaviors.Find<ServiceBehaviorAttribute>();
                behavior.ConcurrencyMode = ConcurrencyMode.Multiple;
                behavior.InstanceContextMode = InstanceContextMode.Single;
                behavior.UseSynchronizationContext = false;
                agentSvcHost.Open();
                string serviceUrl =
                    agentSvcHost.BaseAddresses.Count > 0 ? agentSvcHost.BaseAddresses[0].AbsoluteUri : "";

                log.WriteAction(string.Format(
                    Localization.UseRussian ? "WCF-служба запущена по адресу {0}" : "WCF service is started at {0}",
                    serviceUrl));

                return true;
            } catch (Exception ex) {
                log.WriteException(ex,
                    Localization.UseRussian ? "Ошибка при запуске WCF-службы" : "Error starting WCF service");
                return false;
            }
        }

        /// <summary>
        /// Stop WCF service interacting with the web interface
        /// </summary>
        private void StopWcfService() {
            if (agentSvcHost != null) {
                try {
                    agentSvcHost.Close();
                    log.WriteAction(Localization.UseRussian ? "WCF-служба остановлена" : "WCF service is stopped");
                } catch {
                    agentSvcHost.Abort();
                    log.WriteAction(Localization.UseRussian ? "WCF-служба прервана" : "WCF service is aborted");
                }

                agentSvcHost = null;
            }
        }


        /// <summary>
        /// Launch agent
        /// </summary>
        public bool StartAgent() {
            // common data initialization
            var appData = AppData.GetInstance();
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            appData.Init(exeDir);

            log = appData.Log;
            log.WriteBreak();
            log.WriteAction(string.Format(Localization.UseRussian ? "Агент {0} запущен" : "Agent {0} started",
                AgentUtils.AppVersion));

            if (appData.AppDirs.Exist) {
                // localization
                if (Localization.LoadDictionaries(appData.AppDirs.LangDir, "ScadaData", out string errMsg))
                    CommonPhrases.Init();
                else
                    log.WriteError(errMsg);

                // launch
                string settingsFileName = appData.AppDirs.ConfigDir + Settings.DefFileName;
                agentLogic = new AgentLogic(appData.SessionManager, appData.AppDirs, appData.Log);

                if (appData.Settings.Load(settingsFileName, out errMsg) &&
                    StartWcfService() && agentLogic.StartProcessing()) {
                    return true;
                } else if (!string.IsNullOrEmpty(errMsg)) {
                    log.WriteError(errMsg);
                }
            } else {
                log.WriteError(string.Format(
                    Localization.UseRussian
                        ? "Необходимые директории не существуют:{0}{1}"
                        : "The required directories do not exist:{0}{1}",
                    Environment.NewLine, string.Join(Environment.NewLine, appData.AppDirs.GetRequiredDirs())));
            }

            log.WriteError(Localization.UseRussian
                ? "Нормальная работа программы невозможна"
                : "Normal program execution is impossible");
            return false;
        }

        /// <summary>
        /// Stop agent
        /// </summary>
        public void StopAgent() {
            StopWcfService();
            agentLogic?.StopProcessing();

            log.WriteAction(Localization.UseRussian ? "Агент остановлен" : "Agent is stopped");
            log.WriteBreak();
        }
    }
}