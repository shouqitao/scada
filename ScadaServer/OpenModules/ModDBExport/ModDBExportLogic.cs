/*
 * Copyright 2015 Mikhail Shiryaev
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
 * Module   : ModDBExport
 * Summary  : Server module logic
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2015
 * Modified : 2015
 * 
 * Description
 * Server module for real time data export from Rapid SCADA to DB.
 */

using Scada.Data.Configuration;
using Scada.Data.Models;
using Scada.Data.Tables;
using Scada.Server.Modules.DBExport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Utils;

namespace Scada.Server.Modules {
    /// <summary>
    /// Server module logic
    /// <para>The logic of the server module</para>
    /// </summary>
    public class ModDBExportLogic : ModLogic {
        /// <summary>
        /// Module Log File Name
        /// </summary>
        internal const string LogFileName = "ModDBExport.log";

        /// <summary>
        /// File name of the module operation information
        /// </summary>
        private const string InfoFileName = "ModDBExport.txt";

        /// <summary>
        /// The delay in updating the file of the information file, ms
        /// </summary>
        private const int InfoThreadDelay = 500;

        private bool normalWork; // sign of normal module operation
        private string workState; // string record of work status
        private Log log; // module operation log
        private string infoFileName; // full file name information
        private Thread infoThread; // stream to update file information
        private Config config; // module configuration
        private List<Exporter> exporters; // exporters


        /// <summary>
        /// Constructor
        /// </summary>
        public ModDBExportLogic() {
            normalWork = true;
            workState = Localization.UseRussian ? "норма" : "normal";
            log = null;
            infoFileName = "";
            infoThread = null;
            config = null;
            exporters = null;
        }


        /// <summary>
        /// Get the module name
        /// </summary>
        public override string Name {
            get { return "ModDBExport"; }
        }


        /// <summary>
        /// Get command parameters
        /// </summary>
        private void GetCmdParams(Command cmd, out string dataSourceName, out DateTime dateTime) {
            string cmdDataStr = cmd.GetCmdDataStr();
            string[] parts = cmdDataStr.Split('\n');

            dataSourceName = parts[0];
            try {
                dateTime = ScadaUtils.XmlParseDateTime(parts[1]);
            } catch {
                dateTime = DateTime.MinValue;
            }
        }

        /// <summary>
        /// Find exporter by data source name
        /// </summary>
        private Exporter FindExporter(string dataSourceName) {
            foreach (var exporter in exporters)
                if (exporter.DataSource.Name == dataSourceName)
                    return exporter;
            return null;
        }

        /// <summary>
        /// Export current data by loading it from file
        /// </summary>
        private void ExportCurDataFromFile(Exporter exporter) {
            // load current slice from file
            SrezTableLight srezTable = new SrezTableLight();
            SrezAdapter srezAdapter = new SrezAdapter();
            srezAdapter.FileName = ServerUtils.BuildCurFileName(Settings.ArcDir);

            try {
                srezAdapter.Fill(srezTable);
            } catch (Exception ex) {
                log.WriteAction(string.Format(
                    Localization.UseRussian
                        ? "Ошибка при загрузке текущего среза из файла {0}: {1}"
                        : "Error loading current data from file {0}: {1}", srezAdapter.FileName, ex.Message));
            }

            // add slice to export queue
            if (srezTable.SrezList.Count > 0) {
                SrezTableLight.Srez sourceSrez = srezTable.SrezList.Values[0];
                SrezTableLight.Srez srez = new SrezTableLight.Srez(DateTime.Now, sourceSrez.CnlNums, sourceSrez);
                exporter.EnqueueCurData(srez);
                log.WriteAction(Localization.UseRussian
                    ? "Текущие данные добавлены в очередь экспорта"
                    : "Current data added to export queue");
            } else {
                log.WriteAction(Localization.UseRussian
                    ? "Отсутствуют текущие данные для экспорта"
                    : "No current data to export");
            }
        }

        /// <summary>
        /// Export archived data by downloading it from a file
        /// </summary>
        private void ExportArcDataFromFile(Exporter exporter, DateTime dateTime) {
            // loading the table of minute slices from a file
            SrezTableLight srezTable = new SrezTableLight();
            SrezAdapter srezAdapter = new SrezAdapter();
            srezAdapter.FileName = ServerUtils.BuildMinFileName(Settings.ArcDir, dateTime);

            try {
                srezAdapter.Fill(srezTable);
            } catch (Exception ex) {
                log.WriteAction(string.Format(
                    Localization.UseRussian
                        ? "Ошибка при загрузке таблицы минутных срезов из файла {0}: {1}"
                        : "Error loading minute data table from file {0}: {1}", srezAdapter.FileName, ex.Message));
            }

            // search for a slice for a specified time
            SrezTableLight.Srez srez = srezTable.GetSrez(dateTime);

            // add slice to export queue
            if (srez == null) {
                log.WriteAction(Localization.UseRussian
                    ? "Отсутствуют архивные данные для экспорта"
                    : "No archive data to export");
            } else {
                exporter.EnqueueArcData(srez);
                log.WriteAction(Localization.UseRussian
                    ? "Архивные данные добавлены в очередь экспорта"
                    : "Archive data added to export queue");
            }
        }

        /// <summary>
        /// Export events by loading them from a file.
        /// </summary>
        private void ExportEventsFromFile(Exporter exporter, DateTime date) {
            // loading event table from file
            EventTableLight eventTable = new EventTableLight();
            EventAdapter eventAdapter = new EventAdapter();
            eventAdapter.FileName = ServerUtils.BuildEvFileName(Settings.ArcDir, date);

            try {
                eventAdapter.Fill(eventTable);
            } catch (Exception ex) {
                log.WriteAction(string.Format(
                    Localization.UseRussian
                        ? "Ошибка при загрузке таблицы событий из файла {0}: {1}"
                        : "Error loading event table from file {0}: {1}", eventAdapter.FileName, ex.Message));
            }

            // adding events to the export queue
            if (eventTable.AllEvents.Count > 0) {
                foreach (EventTableLight.Event ev in eventTable.AllEvents)
                    exporter.EnqueueEvent(ev);
                log.WriteAction(Localization.UseRussian
                    ? "События добавлены в очередь экспорта"
                    : "Events added to export queue");
            } else {
                log.WriteAction(Localization.UseRussian ? "Отсутствуют события для экспорта" : "No events to export");
            }
        }

        /// <summary>
        /// Write to the file information about the module
        /// </summary>
        private void WriteInfo() {
            try {
                // text formation
                var sbInfo = new StringBuilder();

                if (Localization.UseRussian) {
                    sbInfo
                        .AppendLine("Модуль экспорта данных")
                        .AppendLine("----------------------")
                        .Append("Состояние: ").AppendLine(workState).AppendLine()
                        .AppendLine("Источники данных")
                        .AppendLine("----------------");
                } else {
                    sbInfo
                        .AppendLine("Export Data Module")
                        .AppendLine("------------------")
                        .Append("State: ").AppendLine(workState).AppendLine()
                        .AppendLine("Data Sources")
                        .AppendLine("------------");
                }

                int cnt = exporters.Count;
                if (cnt > 0) {
                    for (var i = 0; i < cnt; i++)
                        sbInfo.Append((i + 1).ToString()).Append(". ").AppendLine(exporters[i].GetInfo());
                } else {
                    sbInfo.AppendLine(Localization.UseRussian ? "Нет" : "No");
                }

                // output to file
                using (var writer = new StreamWriter(infoFileName, false, Encoding.UTF8))
                    writer.Write(sbInfo.ToString());
            } catch (ThreadAbortException) { } catch (Exception ex) {
                log.WriteAction(ModPhrases.WriteInfoError + ": " + ex.Message, Log.ActTypes.Exception);
            }
        }


        /// <summary>
        /// Perform actions at server startup
        /// </summary>
        public override void OnServerStart() {
            // logging
            log = new Log(Log.Formats.Simple);
            log.Encoding = Encoding.UTF8;
            log.FileName = AppDirs.LogDir + LogFileName;
            log.WriteBreak();
            log.WriteAction(string.Format(ModPhrases.StartModule, Name));

            // determining the full file name information
            infoFileName = AppDirs.LogDir + InfoFileName;

            // banged configuration
            config = new Config(AppDirs.ConfigDir);
            string errMsg;

            if (config.Load(out errMsg)) {
                // creating and launching exporters
                exporters = new List<Exporter>();
                foreach (var expDest in config.ExportDestinations) {
                    var exporter = new Exporter(expDest, log);
                    exporters.Add(exporter);
                    exporter.Start();
                }

                // creating and running a stream to update the file information
                infoThread = new Thread(() => {
                    while (true) {
                        WriteInfo();
                        Thread.Sleep(InfoThreadDelay);
                    }
                });
                infoThread.Start();
            } else {
                normalWork = false;
                workState = Localization.UseRussian ? "ошибка" : "error";
                WriteInfo();
                log.WriteAction(errMsg);
                log.WriteAction(ModPhrases.NormalModExecImpossible);
            }
        }

        /// <summary>
        /// Perform actions when shutting down the server
        /// </summary>
        public override void OnServerStop() {
            // stop exporters
            foreach (var exporter in exporters)
                exporter.Terminate();

            // waiting for exporters to complete
            var nowDT = DateTime.Now;
            var begDT = nowDT;
            var endDT = nowDT.AddMilliseconds(WaitForStop);
            bool running;

            do {
                running = false;
                foreach (var exporter in exporters) {
                    if (exporter.Running) {
                        running = true;
                        break;
                    }
                }

                if (running)
                    Thread.Sleep(ScadaUtils.ThreadDelay);
                nowDT = DateTime.Now;
            } while (begDT <= nowDT && nowDT <= endDT && running);

            // interruption of work of exporters
            if (running) {
                foreach (var exporter in exporters)
                    if (exporter.Running)
                        exporter.Abort();
            }

            // stream interruption to update file information
            if (infoThread != null) {
                infoThread.Abort();
                infoThread = null;
            }

            // information output
            workState = Localization.UseRussian ? "остановлен" : "stopped";
            WriteInfo();
            log.WriteAction(string.Format(ModPhrases.StopModule, Name));
            log.WriteBreak();
        }

        /// <summary>
        /// Perform actions after processing new current data
        /// </summary>
        public override void OnCurDataProcessed(int[] cnlNums, SrezTableLight.Srez curSrez) {
            // export of current data to the database
            if (normalWork) {
                // creating an exported slice
                SrezTableLight.Srez srez = new SrezTableLight.Srez(DateTime.Now, cnlNums, curSrez);

                // add slice to export queue
                foreach (var exporter in exporters)
                    exporter.EnqueueCurData(srez);
            }
        }

        /// <summary>
        /// Perform actions after processing new archived data
        /// </summary>
        public override void OnArcDataProcessed(int[] cnlNums, SrezTableLight.Srez arcSrez) {
            // export of archive data to the database
            if (normalWork) {
                // creating an exported slice
                SrezTableLight.Srez srez = new SrezTableLight.Srez(arcSrez.DateTime, cnlNums, arcSrez);

                // add slice to export queue
                foreach (var exporter in exporters)
                    exporter.EnqueueArcData(srez);
            }
        }

        /// <summary>
        /// Perform actions after creating an event and writing to disk
        /// </summary>
        public override void OnEventCreated(EventTableLight.Event ev) {
            // export event to DB
            if (normalWork) {
                // adding event to export queue
                foreach (var exporter in exporters)
                    exporter.EnqueueEvent(ev);
            }
        }

        /// <summary>
        /// Perform actions after receiving the command TU
        /// </summary>
        public override void OnCommandReceived(int ctrlCnlNum, Command cmd, int userID, ref bool passToClients) {
            // manual export
            if (normalWork) {
                bool exportCurData = ctrlCnlNum == config.CurDataCtrlCnlNum;
                bool exportArcData = ctrlCnlNum == config.ArcDataCtrlCnlNum;
                bool exportEvents = ctrlCnlNum == config.EventsCtrlCnlNum;
                var procCmd = true;

                if (exportCurData)
                    log.WriteAction(Localization.UseRussian
                        ? "Получена команда экспорта текущих данных"
                        : "Export current data command received");
                else if (exportArcData)
                    log.WriteAction(Localization.UseRussian
                        ? "Получена команда экспорта архивных данных"
                        : "Export archive data command received");
                else if (exportEvents)
                    log.WriteAction(Localization.UseRussian
                        ? "Получена команда экспорта событий"
                        : "Export events command received");
                else
                    procCmd = false;

                if (procCmd) {
                    passToClients = false;

                    if (cmd.CmdTypeID == BaseValues.CmdTypes.Binary) {
                        string dataSourceName;
                        DateTime dateTime;
                        GetCmdParams(cmd, out dataSourceName, out dateTime);

                        if (dataSourceName == "") {
                            log.WriteLine(string.Format(Localization.UseRussian
                                ? "Источник данных не задан"
                                : "Data source is not specified"));
                        } else {
                            var exporter = FindExporter(dataSourceName);

                            if (exporter == null) {
                                log.WriteLine(string.Format(
                                    Localization.UseRussian
                                        ? "Неизвестный источник данных {0}"
                                        : "Unknown data source {0}", dataSourceName));
                            } else {
                                log.WriteLine(string.Format(
                                    Localization.UseRussian ? "Источник данных: {0}" : "Data source: {0}",
                                    dataSourceName));

                                if (exportCurData) {
                                    ExportCurDataFromFile(exporter);
                                } else if (exportArcData) {
                                    if (dateTime == DateTime.MinValue) {
                                        log.WriteLine(string.Format(Localization.UseRussian
                                            ? "Некорректная дата и время"
                                            : "Incorrect date and time"));
                                    } else {
                                        log.WriteLine(string.Format(
                                            Localization.UseRussian ? "Дата и время: {0:G}" : "Date and time: {0:G}",
                                            dateTime));
                                        ExportArcDataFromFile(exporter, dateTime);
                                    }
                                } else // exportEvents
                                {
                                    if (dateTime == DateTime.MinValue) {
                                        log.WriteLine(string.Format(Localization.UseRussian
                                            ? "Некорректная дата"
                                            : "Incorrect date"));
                                    } else {
                                        log.WriteLine(string.Format(
                                            Localization.UseRussian ? "Дата: {0:d}" : "Date: {0:d}", dateTime));
                                        ExportEventsFromFile(exporter, dateTime);
                                    }
                                }
                            }
                        }
                    } else {
                        log.WriteAction(ModPhrases.IllegalCommand);
                    }
                }
            }
        }
    }
}