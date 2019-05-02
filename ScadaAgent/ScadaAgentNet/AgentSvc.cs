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
 * Summary  : WCF service for interacting with the agent
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using Scada.Agent.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Utils;

namespace Scada.Agent.Net {
    /// <summary>
    /// WCF service for interacting with the agent
    /// <para>WCF service to interact with the agent</para>
    /// </summary>
    [ServiceContract]
    public class AgentSvc {
        /// <summary>
        /// Buffer size for receiving files
        /// </summary>
        private const int ReceiveBufSize = 10240;

        /// <summary>
        /// Application data
        /// </summary>
        private static readonly AppData AppData = AppData.GetInstance();

        /// <summary>
        /// Application log
        /// </summary>
        private static readonly ILog Log = AppData.Log;

        /// <summary>
        /// Session manager
        /// </summary>
        private static readonly SessionManager SessionManager = AppData.SessionManager;

        /// <summary>
        /// System Instance Manager
        /// </summary>
        private static readonly InstanceManager InstanceManager = AppData.InstanceManager;


        /// <summary>
        /// Get current connection IP address
        /// </summary>
        private string GetClientIP() {
            try {
                var context = OperationContext.Current;
                var props = context.IncomingMessageProperties;
                var remoteEndPoint =
                    (RemoteEndpointMessageProperty) props[RemoteEndpointMessageProperty.Name];
                return remoteEndPoint.Address;
            } catch {
                return "";
            }
        }

        /// <summary>
        /// Attempt to get session by id
        /// </summary>
        private bool TryGetSession(long sessionID, out Session session) {
            session = SessionManager.GetSession(sessionID);

            if (session == null) {
                Log.WriteError(string.Format(
                    Localization.UseRussian ? "Сессия с ид. {0} не найдена" : "Session with ID {0} not found",
                    sessionID));
                return false;
            } else {
                session.RegisterActivity();
                return true;
            }
        }

        /// <summary>
        /// Try to get a copy of the system by ID. sessions
        /// </summary>
        private bool TryGetScadaInstance(long sessionID, out ScadaInstance scadaInstance) {
            if (TryGetSession(sessionID, out var session)) {
                scadaInstance = session.LoggedOn ? session.ScadaInstance : null;

                if (scadaInstance == null) {
                    Log.WriteError(string.Format(
                        Localization.UseRussian
                            ? "Экземпляр системы не определён для сессии с ид. {0}"
                            : "System instance is not defined for a session with ID {0}", sessionID));
                    return false;
                } else {
                    return true;
                }
            } else {
                scadaInstance = null;
                return false;
            }
        }

        /// <summary>
        /// Check message to load configuration
        /// </summary>
        private bool ValidateMessage(ConfigUploadMessage message) {
            return message != null && message.ConfigOptions != null || message.Stream != null;
        }

        /// <summary>
        /// Receive file
        /// </summary>
        private bool ReceiveFile(Stream srcStream, string destFileName) {
            try {
                var t0 = DateTime.UtcNow;

                using (var destStream = File.Create(destFileName)) {
                    srcStream.CopyTo(destStream, ReceiveBufSize);
                }

                Log.WriteAction(string.Format(
                    Localization.UseRussian
                        ? "Файл {0} принят успешно за {1} мс"
                        : "File {0} received successfully in {1} ms",
                    Path.GetFileName(destFileName), (int) (DateTime.UtcNow - t0).TotalMilliseconds));
                return true;
            } catch (Exception ex) {
                Log.WriteException(ex, Localization.UseRussian ? "Ошибка при приёме файла" : "Error receiving file");
                return false;
            }
        }


        /// <summary>
        /// Create a new session
        /// </summary>
        [OperationContract]
        public bool CreateSession(out long sessionID) {
            var session = SessionManager.CreateSession();

            if (session == null) {
                sessionID = 0;
                return false;
            } else {
                session.IpAddress = GetClientIP();
                sessionID = session.ID;
                return true;
            }
        }

        /// <summary>
        /// Sign in
        /// </summary>
        [OperationContract]
        public bool Login(long sessionID, string username, string encryptedPassword, string scadaInstanceName,
            out string errMsg) {
            if (TryGetSession(sessionID, out var session)) {
                session.ClearUser();
                var scadaInstance = InstanceManager.GetScadaInstance(scadaInstanceName);

                if (scadaInstance == null) {
                    errMsg = Localization.UseRussian ? "Экземпляр системы не найден" : "System instance not found";

                    Log.WriteError(string.Format(
                        Localization.UseRussian
                            ? "Экземпляр системы с наименованием \"{0}\" не найден"
                            : "System instance named \"{0}\" not found", scadaInstanceName));
                } else {
                    string password = CryptoUtils.SafelyDecryptPassword(encryptedPassword, sessionID,
                        AppData.Settings.SecretKey);

                    if (scadaInstance.ValidateUser(username, password, out errMsg)) {
                        Log.WriteAction(string.Format(
                            Localization.UseRussian
                                ? "Пользователь {0} подключился к {1}"
                                : "User {0} connected to {1}", username, scadaInstanceName));
                        session.SetUser(username, scadaInstance);
                        return true;
                    } else {
                        Log.WriteError(string.Format(
                            Localization.UseRussian
                                ? "Пользователь {0} не прошёл проверку для подключения к {1} - {2}"
                                : "User {0} failed validation to connect to {1} - {2}",
                            username, scadaInstanceName, errMsg));
                    }
                }
            } else {
                errMsg = Localization.UseRussian ? "Сессия не найдена" : "Session not found";
            }

            return false;
        }

        /// <summary>
        /// Check that the user is authorized
        /// </summary>
        [OperationContract]
        public bool IsLoggedOn(long sessionID) {
            return TryGetSession(sessionID, out var session) && session.LoggedOn;
        }

        /// <summary>
        /// Manage the service
        /// </summary>
        [OperationContract]
        public bool ControlService(long sessionID, ServiceApp serviceApp, ServiceCommand command) {
            if (TryGetScadaInstance(sessionID, out var scadaInstance)) {
                return scadaInstance.ControlService(serviceApp, command);
            } else {
                return false;
            }
        }

        /// <summary>
        /// Get service status
        /// </summary>
        [OperationContract]
        public bool GetServiceStatus(long sessionID, ServiceApp serviceApp, out ServiceStatus status) {
            if (TryGetScadaInstance(sessionID, out var scadaInstance)) {
                return scadaInstance.GetServiceStatus(serviceApp, out status);
            } else {
                status = ServiceStatus.Undefined;
                return false;
            }
        }

        /// <summary>
        /// Get available parts of a system instance configuration
        /// </summary>
        [OperationContract]
        public bool GetAvailableConfig(long sessionID, out ConfigParts configParts) {
            if (TryGetScadaInstance(sessionID, out var scadaInstance)) {
                return scadaInstance.GetAvailableConfig(out configParts);
            } else {
                configParts = ConfigParts.None;
                return false;
            }
        }

        /// <summary>
        /// Download configuration
        /// </summary>
        [OperationContract]
        public Stream DownloadConfig(long sessionID, ConfigOptions configOptions) {
            if (TryGetScadaInstance(sessionID, out var scadaInstance)) {
                lock (scadaInstance.SyncRoot) {
                    string tempFileName = AppData.GetTempFileName("download-config", "zip");
                    if (scadaInstance.PackConfig(tempFileName, configOptions)) {
                        return File.Open(tempFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Download configuration
        /// </summary>
        [OperationContract]
        public void UploadConfig(ConfigUploadMessage configUploadMessage) {
            if (ValidateMessage(configUploadMessage)) {
                if (TryGetScadaInstance(configUploadMessage.SessionID, out var scadaInstance)) {
                    lock (scadaInstance.SyncRoot) {
                        string tempFileName = AppData.GetTempFileName("upload-config", "zip");
                        if (ReceiveFile(configUploadMessage.Stream, tempFileName)) {
                            scadaInstance.UnpackConfig(tempFileName, configUploadMessage.ConfigOptions);
                        }
                    }
                }
            } else {
                Log.WriteError(Localization.UseRussian
                    ? "Загружаемая конфигурация не определена или некорректна"
                    : "Uploaded configuration is undefined or incorrect");
            }
        }

        /// <summary>
        /// Pack configuration into archive
        /// </summary>
        /// <remarks>Method to get configuration locally</remarks>
        [OperationContract]
        public bool PackConfig(long sessionID, string destFileName, ConfigOptions configOptions) {
            if (TryGetScadaInstance(sessionID, out var scadaInstance)) {
                lock (scadaInstance.SyncRoot) {
                    return scadaInstance.PackConfig(destFileName, configOptions);
                }
            } else {
                return false;
            }
        }

        /// <summary>
        /// Unpack configuration archive
        /// </summary>
        /// <remarks>Method to transfer configuration locally</remarks>
        [OperationContract]
        public bool UnpackConfig(long sessionID, string srcFileName, ConfigOptions configOptions) {
            if (TryGetScadaInstance(sessionID, out var scadaInstance)) {
                lock (scadaInstance.SyncRoot) {
                    return scadaInstance.UnpackConfig(srcFileName, configOptions);
                }
            } else {
                return false;
            }
        }

        /// <summary>
        /// Directory Overview
        /// </summary>
        [OperationContract]
        public bool Browse(long sessionID, RelPath relPath,
            out ICollection<string> directories, out ICollection<string> files) {
            if (TryGetScadaInstance(sessionID, out var scadaInstance)) {
                return scadaInstance.Browse(relPath, out directories, out files);
            } else {
                directories = null;
                files = null;
                return false;
            }
        }

        /// <summary>
        /// Get the date and time the file was changed (UTC)
        /// </summary>
        [OperationContract]
        public DateTime GetFileAgeUtc(long sessionID, RelPath relPath) {
            if (TryGetScadaInstance(sessionID, out var scadaInstance)) {
                string path = scadaInstance.GetAbsPath(relPath);
                try {
                    return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
                } catch {
                    return DateTime.MinValue;
                }
            } else {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Download file
        /// </summary>
        [OperationContract]
        public Stream DownloadFile(long sessionID, RelPath relPath) {
            return DownloadFileRest(sessionID, relPath, -1);
        }

        /// <summary>
        /// Download part of the file from a given position
        /// </summary>
        [OperationContract]
        public Stream DownloadFileRest(long sessionID, RelPath relPath, long offsetFromEnd) {
            if (TryGetScadaInstance(sessionID, out var scadaInstance)) {
                try {
                    string path = scadaInstance.GetAbsPath(relPath);
                    Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);

                    if (offsetFromEnd >= 0) {
                        long offset = -Math.Min(offsetFromEnd, stream.Length);
                        stream.Seek(offset, SeekOrigin.End);
                    }

                    return stream;
                } catch (FileNotFoundException) {
                    return null;
                } catch (Exception ex) {
                    Log.WriteException(ex,
                        Localization.UseRussian ? "Ошибка при открытии файла" : "Error opening file");
                    return null;
                }
            } else {
                return null;
            }
        }
    }
}