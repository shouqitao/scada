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
 * Module   : SCADA-Administrator
 * Summary  : Downloading and uploading configuration
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using Ionic.Zip;
using Scada;
using ScadaAdmin.AgentSvcRef;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.Text;

namespace ScadaAdmin {
    /// <summary>
    /// Downloading and uploading configuration
    /// <para>Download and transfer configuration</para>
    /// </summary>
    internal static class DownloadUpload {
        /// <summary>
        /// Ignored system instance-specific file paths
        /// </summary>
        private static readonly RelPath[] IgnoredPaths;


        /// <summary>
        /// Static Constructor
        /// </summary>
        static DownloadUpload() {
            IgnoredPaths = new RelPath[] {
                new RelPath() {
                    ConfigPart = ConfigParts.Comm,
                    AppFolder = AppFolder.Config,
                    Path = "*_Reg.xml"
                },
                new RelPath() {
                    ConfigPart = ConfigParts.Comm,
                    AppFolder = AppFolder.Config,
                    Path = "CompCode.txt"
                },
                new RelPath() {
                    ConfigPart = ConfigParts.Server,
                    AppFolder = AppFolder.Config,
                    Path = "*_Reg.xml"
                },
                new RelPath() {
                    ConfigPart = ConfigParts.Server,
                    AppFolder = AppFolder.Config,
                    Path = "CompCode.txt"
                },
                new RelPath() {
                    ConfigPart = ConfigParts.Web,
                    AppFolder = AppFolder.Config,
                    Path = "*_Reg.xml"
                },
                new RelPath() {
                    ConfigPart = ConfigParts.Web,
                    AppFolder = AppFolder.Storage,
                    Path = ""
                },
            };
        }


        /// <summary>
        /// Create an initialization vector on the template. sessions
        /// </summary>
        private static byte[] CreateIV(long sessionID) {
            var iv = new byte[ScadaUtils.IVSize];
            byte[] sessBuf = BitConverter.GetBytes(sessionID);
            int sessBufLen = sessBuf.Length;

            for (var i = 0; i < ScadaUtils.IVSize; i++) {
                iv[i] = sessBuf[i % sessBufLen];
            }

            return iv;
        }

        /// <summary>
        /// Generate an address to connect to the Agent
        /// </summary>
        private static EndpointAddress GetEpAddress(string host, int port) {
            return new EndpointAddress($"http://{host}:{port}/ScadaAgent/ScadaAgentSvc/");
        }

        /// <summary>
        /// Connect with Agent
        /// </summary>
        private static void Connect(ServersSettings.ConnectionSettings connectionSettings,
            StreamWriter writer, out AgentSvcClient client, out long sessionID) {
            // connection setup
            client = new AgentSvcClient();
            client.Endpoint.Address = GetEpAddress(connectionSettings.Host, connectionSettings.Port);

            // session creation
            if (client.CreateSession(out sessionID))
                writer?.WriteLine(AppPhrases.SessionCreated, sessionID);
            else
                throw new ScadaException(AppPhrases.UnableCreateSession);

            // login
            string encryptedPassword = ScadaUtils.Encrypt(connectionSettings.Password,
                connectionSettings.SecretKey, CreateIV(sessionID));

            if (client.Login(out string errMsg, sessionID, connectionSettings.Username,
                encryptedPassword, connectionSettings.ScadaInstance))
                writer?.WriteLine(AppPhrases.LoggedOn);
            else
                throw new ScadaException(string.Format(AppPhrases.UnableLogin, errMsg));
        }

        /// <summary>
        /// Pack configuration in temporary file
        /// </summary>
        private static void PackConfig(string srcDir, List<string> selectedFiles,
            out string outFileName, out ConfigParts configParts) {
            srcDir = ScadaUtils.NormalDir(srcDir);
            int srcDirLen = srcDir.Length;
            outFileName = srcDir + "upload-config_" +
                          DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".zip";
            configParts = ConfigParts.None;

            using (ZipFile zipFile = new ZipFile(outFileName)) {
                foreach (string relPath in selectedFiles) {
                    string path = srcDir + relPath;
                    configParts = configParts | GetConfigPart(relPath);

                    if (Directory.Exists(path)) // path is a directory
                    {
                        string[] filesInDir = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                        foreach (string fileName in filesInDir) {
                            if (Path.GetExtension(fileName) != ".bak") {
                                string dirInArc = Path.GetDirectoryName(fileName.Substring(srcDirLen))
                                    .Replace('\\', '/');
                                zipFile.AddFile(fileName, dirInArc);
                            }
                        }
                    } else if (File.Exists(path)) {
                        string dirInArc = Path.GetDirectoryName(relPath).Replace('\\', '/');
                        zipFile.AddFile(path, dirInArc);
                    }
                }

                zipFile.Save();
            }
        }

        /// <summary>
        /// Get the part of the configuration that matches the path
        /// </summary>
        private static ConfigParts GetConfigPart(string relPath) {
            if (relPath.StartsWith("BaseDAT", StringComparison.Ordinal))
                return ConfigParts.Base;
            if (relPath.StartsWith("Interface", StringComparison.Ordinal))
                return ConfigParts.Interface;
            if (relPath.StartsWith("ScadaComm", StringComparison.Ordinal))
                return ConfigParts.Comm;
            if (relPath.StartsWith("ScadaServer", StringComparison.Ordinal))
                return ConfigParts.Server;
            if (relPath.StartsWith("ScadaWeb", StringComparison.Ordinal))
                return ConfigParts.Web;
            return ConfigParts.None;
        }

        /// <summary>
        /// Get the parts of the configuration that are contained in the archive
        /// </summary>
        private static ConfigParts GetConfigParts(string arcFileName) {
            var configParts = ConfigParts.None;

            using (ZipFile zipFile = new ZipFile(arcFileName)) {
                foreach (ZipEntry zipEntry in zipFile.Entries) {
                    configParts = configParts | GetConfigPart(zipEntry.FileName);
                }
            }

            return configParts;
        }


        /// <summary>
        /// Download configuration
        /// </summary>
        public static bool DownloadConfig(ServersSettings.ServerSettings serverSettings,
            string logFileName, out bool logCreated, out string msg) {
            if (serverSettings == null)
                throw new ArgumentNullException(nameof(serverSettings));
            if (logFileName == null)
                throw new ArgumentNullException(nameof(logFileName));

            logCreated = false;
            StreamWriter writer = null;
            AgentSvcClient client = null;

            try {
                var t0 = DateTime.UtcNow;
                writer = new StreamWriter(logFileName, false, Encoding.UTF8);
                logCreated = true;

                AppUtils.WriteTitle(writer,
                    string.Format(AppPhrases.DownloadTitle, DateTime.Now.ToString("G", Localization.Culture)));
                writer.WriteLine(AppPhrases.ConnectionName, serverSettings.Connection.Name);
                writer.WriteLine();

                // Agent connection
                Connect(serverSettings.Connection, writer, out client, out long sessionID);

                // configuration download
                var downloadSettings = serverSettings.Download;
                var configOptions = new ConfigOptions() {ConfigParts = ConfigParts.All};

                if (!downloadSettings.IncludeSpecificFiles)
                    configOptions.IgnoredPaths = IgnoredPaths;

                var downloadStream = client.DownloadConfig(sessionID, configOptions);

                if (downloadStream == null)
                    throw new ScadaException(AppPhrases.DownloadDataEmpty);

                if (downloadSettings.SaveToDir) {
                    // save to directory
                    string destDir = downloadSettings.DestDir;
                    Directory.CreateDirectory(destDir);
                    string tempFileName = destDir + "download-config_" +
                                          DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".zip";

                    try {
                        // saving to a temporary file, because unpacking from MemoryStream does not work
                        using (var destStream = File.Create(tempFileName)) {
                            downloadStream.CopyTo(destStream);
                        }

                        // unpacking
                        using (ZipFile zipFile = ZipFile.Read(tempFileName)) {
                            foreach (ZipEntry zipEntry in zipFile) {
                                zipEntry.Extract(destDir, ExtractExistingFileAction.OverwriteSilently);
                            }
                        }
                    } finally {
                        try {
                            File.Delete(tempFileName);
                        } catch {
                            // ignored
                        }
                    }
                } else {
                    // save to file
                    string destFile = downloadSettings.DestFile;
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile));

                    using (var destStream = File.Create(destFile)) {
                        downloadStream.CopyTo(destStream);
                    }
                }

                downloadStream.Close();
                msg = string.Format(AppPhrases.DownloadSuccessful, (int) (DateTime.UtcNow - t0).TotalSeconds);
                writer.WriteLine(msg);
                return true;
            } catch (Exception ex) {
                msg = AppPhrases.DownloadError + ":\r\n" + ex.Message;

                try {
                    writer?.WriteLine(msg);
                } catch {
                    // ignored
                }

                return false;
            } finally {
                try {
                    writer?.Close();
                } catch {
                    // ignored
                }

                try {
                    client?.Close();
                } catch {
                    // ignored
                }
            }
        }

        /// <summary>
        /// Transfer configuration
        /// </summary>
        public static bool UploadConfig(ServersSettings.ServerSettings serverSettings,
            string logFileName, out bool logCreated, out string msg) {
            if (serverSettings == null)
                throw new ArgumentNullException(nameof(serverSettings));
            if (logFileName == null)
                throw new ArgumentNullException(nameof(logFileName));

            logCreated = false;
            StreamWriter writer = null;
            AgentSvcClient client = null;

            try {
                var t0 = DateTime.UtcNow;

                writer = new StreamWriter(logFileName, false, Encoding.UTF8);
                logCreated = true;

                AppUtils.WriteTitle(writer,
                    string.Format(AppPhrases.UploadTitle, DateTime.Now.ToString("G", Localization.Culture)));
                writer.WriteLine(AppPhrases.ConnectionName, serverSettings.Connection.Name);
                writer.WriteLine();

                // Agent connection
                Connect(serverSettings.Connection, writer, out client, out long sessionID);

                // preparing the configuration for the transfer
                var uploadSettings = serverSettings.Upload;
                var configOptions = new ConfigOptions();
                ConfigParts configParts;
                string outFileName;
                bool deleteOutFile;

                if (uploadSettings.GetFromDir) {
                    PackConfig(uploadSettings.SrcDir, uploadSettings.SelectedFiles,
                        out outFileName, out configParts);
                    configOptions.ConfigParts = configParts;
                    deleteOutFile = true;
                } else {
                    outFileName = uploadSettings.SrcFile;
                    configOptions.ConfigParts = configParts = GetConfigParts(outFileName);
                    deleteOutFile = false;
                }

                if (configOptions.ConfigParts == ConfigParts.None)
                    throw new ScadaException(AppPhrases.NoConfigInSrc);

                if (!uploadSettings.ClearSpecificFiles)
                    configOptions.IgnoredPaths = IgnoredPaths;

                // передача конфигурации
                using (Stream outStream = File.Open(outFileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    client.UploadConfig(configOptions, sessionID, outStream);
                    writer.WriteLine(AppPhrases.ConfigUploaded);
                }

                // удаление временного файла
                if (deleteOutFile)
                    File.Delete(outFileName);

                // перезапуск служб на удалённом сервере
                if (configParts.HasFlag(ConfigParts.Base) || configParts.HasFlag(ConfigParts.Server)) {
                    if (client.ControlService(sessionID, ServiceApp.Server, ServiceCommand.Restart))
                        writer.WriteLine(AppPhrases.ServerRestarted);
                    else
                        writer.WriteLine(AppPhrases.UnableRestartServer);
                }

                if (configParts.HasFlag(ConfigParts.Base) || configParts.HasFlag(ConfigParts.Comm)) {
                    if (client.ControlService(sessionID, ServiceApp.Comm, ServiceCommand.Restart))
                        writer.WriteLine(AppPhrases.CommRestarted);
                    else
                        writer.WriteLine(AppPhrases.UnableRestartComm);
                }

                msg = string.Format(AppPhrases.UploadSuccessful, (int) (DateTime.UtcNow - t0).TotalSeconds);
                writer.WriteLine(msg);
                return true;
            } catch (Exception ex) {
                msg = AppPhrases.UploadError + ":\r\n" + ex.Message;

                try {
                    writer?.WriteLine(msg);
                } catch { }

                return false;
            } finally {
                try {
                    writer?.Close();
                } catch { }

                try {
                    client?.Close();
                } catch { }
            }
        }

        /// <summary>
        /// Соединиться с Агентом
        /// </summary>
        public static bool Connect(ServersSettings.ConnectionSettings connectionSettings,
            out AgentSvcClient client, out long sessionID, out string errMsg) {
            try {
                Connect(connectionSettings, null, out client, out sessionID);
                errMsg = "";
                return true;
            } catch (Exception ex) {
                client = null;
                sessionID = 0;
                errMsg = AppPhrases.ConnectAgentError + ":\r\n" + ex.Message;
                return false;
            }
        }
    }
}