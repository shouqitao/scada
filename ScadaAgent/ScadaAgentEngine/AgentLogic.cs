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
 * Module   : ScadaAgentEngine
 * Summary  : Implementation of the agent main logic 
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using System;
using System.IO;
using System.Text;
using System.Threading;
using Utils;

namespace Scada.Agent.Engine {
    /// <summary>
    /// Implementation of the agent main logic 
    /// <para>Implementing Agent Basic Logic</para>
    /// </summary>
    public sealed class AgentLogic {
        /// <summary>
        /// Agent Status
        /// </summary>
        private enum WorkState {
            Undefined = 0,
            Normal = 1,
            Error = 2,
            Terminated = 3
        }

        /// <summary>
        /// Job Status Names in English
        /// </summary>
        private static readonly string[] WorkStateNamesEn = {"undefined", "normal", "error", "terminated"};

        /// <summary>
        /// State names of work in Russian
        /// </summary>
        private static readonly string[] WorkStateNamesRu = {"не определено", "норма", "ошибка", "завершён"};

        /// <summary>
        /// Waiting time for stopping the stream, ms
        /// </summary>
        private const int WaitForStop = 10000;

        /// <summary>
        /// Session processing period
        /// </summary>
        private static readonly TimeSpan SessProcPeriod = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Period of deletion of temporary files
        /// </summary>
        private static readonly TimeSpan DelTempFilePeriod = TimeSpan.FromMinutes(1);

        /// <summary>
        /// The time of the temporary files
        /// </summary>
        private static readonly TimeSpan TempFileLifetime = TimeSpan.FromMinutes(10);

        /// <summary>
        /// The period of writing to the application information file
        /// </summary>
        private static readonly TimeSpan WriteInfoPeriod = TimeSpan.FromSeconds(1);

        private SessionManager sessionManager; // link to session manager
        private AppDirs appDirs; // application directories
        private ILog log; // application log
        private Thread thread; // server workflow
        private volatile bool terminated; // it is necessary to close the thread
        private string infoFileName; // full file name information
        private DateTime utcStartDT; // launch date and time (UTC)
        private DateTime startDT; // launch date and time
        private WorkState workState; // work status


        /// <summary>
        /// Constructor restricting the creation of an object without parameters
        /// </summary>
        private AgentLogic() { }

        /// <summary>
        /// Constructor
        /// </summary>
        public AgentLogic(SessionManager sessionManager, AppDirs appDirs, ILog log) {
            this.sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            this.appDirs = appDirs ?? throw new ArgumentNullException(nameof(appDirs));
            this.log = log ?? throw new ArgumentNullException(nameof(log));

            thread = null;
            terminated = false;
            infoFileName = appDirs.LogDir + AppData.InfoFileName;
            utcStartDT = startDT = DateTime.MinValue;
            workState = WorkState.Undefined;
        }


        /// <summary>
        /// Prepare logic processing
        /// </summary>
        private void PrepareProcessing() {
            terminated = false;
            utcStartDT = DateTime.UtcNow;
            startDT = utcStartDT.ToLocalTime();
            workState = WorkState.Normal;
            WriteInfo();
        }

        /// <summary>
        /// Delete obsolete temporary files
        /// </summary>
        private void DeleteOutdatedTempFiles() {
            try {
                var utcNow = DateTime.UtcNow;
                var dirInfo = new DirectoryInfo(appDirs.TempDir);

                foreach (var fileInfo in dirInfo.EnumerateFiles()) {
                    if (utcNow - fileInfo.CreationTimeUtc >= TempFileLifetime) {
                        fileInfo.Delete();
                        log.WriteAction(string.Format(
                            Localization.UseRussian ? "Удалён временный файл {0}" : "Temporary file {0} deleted",
                            fileInfo.Name));
                    }
                }
            } catch (Exception ex) {
                log.WriteException(ex,
                    Localization.UseRussian
                        ? "Ошибка при удалении устаревших временных файлов"
                        : "Error deleting outdated temporary files");
            }
        }

        /// <summary>
        /// Delete all temporary files
        /// </summary>
        private void DeleteAllTempFiles() {
            try {
                var dirInfo = new DirectoryInfo(appDirs.TempDir);

                foreach (var fileInfo in dirInfo.EnumerateFiles()) {
                    fileInfo.Delete();
                }

                log.WriteAction(Localization.UseRussian
                    ? "Удалены все временные файлы"
                    : "All temporary files deleted");
            } catch (Exception ex) {
                log.WriteException(ex,
                    Localization.UseRussian
                        ? "Ошибка при удалении всех временных файлов"
                        : "Error deleting all temporary files");
            }
        }

        /// <summary>
        /// The cycle of the agent (the method is called in a separate thread)
        /// </summary>
        private void Execute() {
            try {
                var sessProcDT = DateTime.MinValue; // session processing time
                var delTempFileDT = DateTime.MinValue; // time to delete temporary files
                var writeInfoDT = DateTime.MinValue; // time of recording information about the application

                while (!terminated) {
                    try {
                        var utcNow = DateTime.UtcNow;

                        // delete inactive sessions
                        if (utcNow - sessProcDT >= SessProcPeriod) {
                            sessProcDT = utcNow;
                            sessionManager.RemoveInactiveSessions();
                        }

                        // deletion of obsolete temporary files
                        if (utcNow - delTempFileDT >= DelTempFilePeriod) {
                            delTempFileDT = utcNow;
                            DeleteOutdatedTempFiles();
                        }

                        // recording information about the application
                        if (utcNow - writeInfoDT >= WriteInfoPeriod) {
                            writeInfoDT = utcNow;
                            WriteInfo();
                        }

                        Thread.Sleep(ScadaUtils.ThreadDelay);
                    } catch (ThreadAbortException) { } catch (Exception ex) {
                        log.WriteException(ex,
                            Localization.UseRussian ? "Ошибка в цикле работы агента" : "Error in the agent work cycle");
                        Thread.Sleep(ScadaUtils.ThreadDelay);
                    }
                }
            } finally {
                sessionManager.RemoveAllSessions();
                DeleteAllTempFiles();

                workState = WorkState.Terminated;
                WriteInfo();
            }
        }

        /// <summary>
        /// Write to the file information about the application
        /// </summary>
        private void WriteInfo() {
            try {
                // formation of information
                var sbInfo = new StringBuilder();
                var workSpan = DateTime.UtcNow - utcStartDT;
                string workSpanStr = workSpan.Days > 0
                    ? workSpan.ToString(@"d\.hh\:mm\:ss")
                    : workSpan.ToString(@"hh\:mm\:ss");

                if (Localization.UseRussian) {
                    sbInfo
                        .AppendLine("Агент")
                        .AppendLine("-----")
                        .Append("Запуск       : ").AppendLine(startDT.ToLocalizedString())
                        .Append("Время работы : ").AppendLine(workSpanStr)
                        .Append("Состояние    : ").AppendLine(WorkStateNamesRu[(int) workState])
                        .Append("Версия       : ").AppendLine(AgentUtils.AppVersion)
                        .AppendLine()
                        .AppendLine("Активные сессии")
                        .AppendLine("---------------");
                } else {
                    sbInfo
                        .AppendLine("Agent")
                        .AppendLine("-----")
                        .Append("Started        : ").AppendLine(startDT.ToLocalizedString())
                        .Append("Execution time : ").AppendLine(workSpanStr)
                        .Append("State          : ").AppendLine(WorkStateNamesEn[(int) workState])
                        .Append("Version        : ").AppendLine(AgentUtils.AppVersion)
                        .AppendLine()
                        .AppendLine("Active Sessions")
                        .AppendLine("---------------");
                }

                sbInfo.Append(sessionManager.GetInfo());

                // write to file
                using (var writer = new StreamWriter(infoFileName, false, Encoding.UTF8)) {
                    writer.Write(sbInfo.ToString());
                }
            } catch (ThreadAbortException) { } catch (Exception ex) {
                log.WriteException(ex,
                    Localization.UseRussian
                        ? "Ошибка при записи в файл информации о работе приложения"
                        : "Error writing application information to the file");
            }
        }


        /// <summary>
        /// Start processing logic
        /// </summary>
        public bool StartProcessing() {
            try {
                if (thread == null) {
                    log.WriteAction(Localization.UseRussian ? "Запуск обработки логики" : "Start logic processing");
                    PrepareProcessing();
                    thread = new Thread(new ThreadStart(Execute));
                    thread.Start();
                } else {
                    log.WriteAction(Localization.UseRussian
                        ? "Обработка логики уже запущена"
                        : "Logic processing is already started");
                }
            } catch (Exception ex) {
                log.WriteException(ex,
                    Localization.UseRussian
                        ? "Ошибка при запуске обработки логики"
                        : "Error starting logic processing");
            } finally {
                if (thread == null) {
                    workState = WorkState.Error;
                    WriteInfo();
                }
            }

            return true;
        }

        /// <summary>
        /// Stop processing logic
        /// </summary>
        public void StopProcessing() {
            try {
                if (thread != null) {
                    terminated = true;

                    if (thread.Join(WaitForStop)) {
                        log.WriteAction(Localization.UseRussian
                            ? "Обработка логики остановлена"
                            : "Logic processing is stopped");
                    } else {
                        thread.Abort();
                        log.WriteAction(Localization.UseRussian
                            ? "Обработка логики прервана"
                            : "Logic processing is aborted");
                    }

                    thread = null;
                }
            } catch (Exception ex) {
                workState = WorkState.Error;
                WriteInfo();
                log.WriteException(ex,
                    Localization.UseRussian
                        ? "Ошибка при остановке обработки логики"
                        : "Error stopping logic processing");
            }
        }
    }
}