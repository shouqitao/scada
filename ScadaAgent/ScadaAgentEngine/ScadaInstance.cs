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
 * Summary  : Object for manipulating a system instance
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using Scada.Data.Configuration;
using Scada.Data.Tables;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using Utils;

namespace Scada.Agent.Engine {
    /// <summary>
    /// Object for manipulating a system instance
    /// <para>Object for manipulating the system instance</para>
    /// </summary>
    public class ScadaInstance {
        /// <summary>
        /// List of paths
        /// </summary>
        private class PathList {
            /// <summary>
            /// Constructor
            /// </summary>
            public PathList() {
                Dirs = new List<string>();
                Files = new List<string>();
            }

            /// <summary>
            /// Get absolute directory paths
            /// </summary>
            public List<string> Dirs { get; private set; }

            /// <summary>
            /// Get absolute file paths
            /// </summary>
            public List<string> Files { get; private set; }
        }

        /// <inheritdoc />
        /// <summary>
        /// Directory of paths, grouped by parts of configuration and application folders
        /// </summary>
        private class PathDict : Dictionary<ConfigParts, Dictionary<AppFolder, PathList>> {
            /// <summary>
            /// Get or add new path list
            /// </summary>
            public PathList GetOrAdd(ConfigParts configPart, AppFolder appFolder) {
                Dictionary<AppFolder, PathList> subDict;
                PathList pathList;

                if (TryGetValue(configPart, out subDict)) {
                    if (subDict.TryGetValue(appFolder, out pathList))
                        return pathList;
                } else {
                    subDict = new Dictionary<AppFolder, PathList>();
                    this[configPart] = subDict;
                }

                pathList = new PathList();
                subDict[appFolder] = pathList;
                return pathList;
            }
        }

        /// <summary>
        /// Max. number of user verification attempts
        /// </summary>
        private const int MaxValidateUserAttempts = 3;

        /// <summary>
        /// All parts of the configuration as an array
        /// </summary>
        private static readonly ConfigParts[] AllConfigParts = {
            ConfigParts.Base, ConfigParts.Interface, ConfigParts.Server, ConfigParts.Comm, ConfigParts.Web
        };

        private ILog log; // application log
        private int validateUserAttemptNum; // user verification number


        /// <summary>
        /// Constructor restricting the creation of an object without parameters
        /// </summary>
        private ScadaInstance() { }

        /// <summary>
        /// Constructor
        /// </summary>
        public ScadaInstance(ScadaInstanceSettings settings, object syncRoot, ILog log) {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            SyncRoot = syncRoot ?? throw new ArgumentNullException(nameof(syncRoot));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            validateUserAttemptNum = 0;
            Name = settings.Name;
        }


        /// <summary>
        /// Get the name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Get system instance settings
        /// </summary>
        public ScadaInstanceSettings Settings { get; private set; }

        /// <summary>
        /// Get or set an object to synchronize access to a system instance.
        /// </summary>
        public object SyncRoot { get; private set; }


        /// <summary>
        /// Get the name of the service status file
        /// </summary>
        private string GetServiceStatusFile(ServiceApp serviceApp) {
            switch (serviceApp) {
                case ServiceApp.Server:
                    return "ScadaServerSvc.txt";
                case ServiceApp.Comm:
                    return "ScadaCommSvc.txt";
                default:
                    throw new ArgumentException("Unknown service.");
            }
        }

        /// <summary>
        /// Get the name of the service command file
        /// </summary>
        private string GetServiceBatchFile(ServiceCommand command) {
            string ext = ScadaUtils.IsRunningOnWin ? ".bat" : ".sh";

            switch (command) {
                case ServiceCommand.Start:
                    return "svc_start" + ext;
                case ServiceCommand.Stop:
                    return "svc_stop" + ext;
                default: // ServiceCommand.Restart
                    return "svc_restart" + ext;
            }
        }

        /// <summary>
        /// Get relative configuration paths that match specified parts.
        /// </summary>
        private List<RelPath> GetConfigPaths(ConfigParts configParts) {
            var configPaths = new List<RelPath>();

            if (configParts.HasFlag(ConfigParts.Base))
                configPaths.Add(new RelPath(ConfigParts.Base, AppFolder.Root));

            if (configParts.HasFlag(ConfigParts.Interface))
                configPaths.Add(new RelPath(ConfigParts.Interface, AppFolder.Root));

            if (configParts.HasFlag(ConfigParts.Server))
                configPaths.Add(new RelPath(ConfigParts.Server, AppFolder.Config));

            if (configParts.HasFlag(ConfigParts.Comm))
                configPaths.Add(new RelPath(ConfigParts.Comm, AppFolder.Config));

            if (configParts.HasFlag(ConfigParts.Web)) {
                configPaths.Add(new RelPath(ConfigParts.Web, AppFolder.Config));
                configPaths.Add(new RelPath(ConfigParts.Web, AppFolder.Storage));
            }

            return configPaths;
        }

        /// <summary>
        /// Prepare ignored paths: split into groups, apply file search by mask
        /// </summary>
        private PathDict PrepareIgnoredPaths(ICollection<RelPath> relPaths) {
            var pathDict = new PathDict();

            if (relPaths != null) {
                foreach (var relPath in relPaths) {
                    var pathList = pathDict.GetOrAdd(relPath.ConfigPart, relPath.AppFolder);
                    string[] absPathArr;

                    if (relPath.IsMask) {
                        string dir = GetAbsPath(relPath.ConfigPart, relPath.AppFolder, "");
                        absPathArr = Directory.Exists(dir) ? Directory.GetFiles(dir, relPath.Path) : new string[0];
                    } else {
                        absPathArr = new string[] {GetAbsPath(relPath)};
                    }

                    foreach (string absPath in absPathArr) {
                        char lastSym = absPath[absPath.Length - 1];

                        if (lastSym == Path.DirectorySeparatorChar || lastSym == Path.AltDirectorySeparatorChar)
                            pathList.Dirs.Add(absPath);
                        else
                            pathList.Files.Add(absPath);
                    }
                }
            }

            return pathDict;
        }

        /// <summary>
        /// Pack directory
        /// </summary>
        private void PackDir(ZipArchive zipArchive, string srcDir, string entryPrefix, PathList ignoredPaths) {
            srcDir = ScadaUtils.NormalDir(srcDir);

            if (!ignoredPaths.Dirs.Contains(srcDir) && Directory.Exists(srcDir)) {
                var srcDirInfo = new DirectoryInfo(srcDir);

                // packing subdirectories
                DirectoryInfo[] dirInfoArr = srcDirInfo.GetDirectories("*", SearchOption.TopDirectoryOnly);

                foreach (var dirInfo in dirInfoArr) {
                    PackDir(zipArchive, dirInfo.FullName, entryPrefix + dirInfo.Name + "/", ignoredPaths);
                }

                // file packing
                FileInfo[] fileInfoArr = srcDirInfo.GetFiles("*", SearchOption.TopDirectoryOnly);
                int srcDirLen = srcDir.Length;

                foreach (var fileInfo in fileInfoArr) {
                    if (!ignoredPaths.Files.Contains(fileInfo.FullName) &&
                        !fileInfo.Extension.Equals(".bak", StringComparison.OrdinalIgnoreCase)) {
                        string entryName = entryPrefix + fileInfo.FullName.Substring(srcDirLen).Replace('\\', '/');
                        zipArchive.CreateEntryFromFile(fileInfo.FullName, entryName, CompressionLevel.Fastest);
                    }
                }
            }
        }

        /// <summary>
        /// Pack directory
        /// </summary>
        private void PackDir(ZipArchive zipArchive, RelPath relPath, PathDict ignoredPathDict) {
            PackDir(zipArchive,
                GetAbsPath(relPath),
                DirectoryBuilder.GetDirectory(relPath.ConfigPart, relPath.AppFolder, '/'),
                ignoredPathDict.GetOrAdd(relPath.ConfigPart, relPath.AppFolder));
        }

        /// <summary>
        /// Clear directory
        /// </summary>
        private void ClearDir(DirectoryInfo dirInfo, PathList ignoredPaths, out bool dirEmpty) {
            if (ignoredPaths.Dirs.Contains(dirInfo.FullName)) {
                dirEmpty = false;
            } else {
                // sub directory cleanup
                DirectoryInfo[] subdirInfoArr = dirInfo.GetDirectories("*", SearchOption.TopDirectoryOnly);

                foreach (var subdirInfo in subdirInfoArr) {
                    ClearDir(subdirInfo, ignoredPaths, out bool subdirEmpty);
                    if (subdirEmpty)
                        subdirInfo.Delete();
                }

                // file deletion
                FileInfo[] fileInfoArr = dirInfo.GetFiles("*", SearchOption.TopDirectoryOnly);
                dirEmpty = true;

                foreach (var fileInfo in fileInfoArr) {
                    if (ignoredPaths.Files.Contains(fileInfo.FullName))
                        dirEmpty = false;
                    else
                        fileInfo.Delete();
                }
            }
        }

        /// <summary>
        /// Clear directory
        /// </summary>
        private void ClearDir(RelPath relPath, PathDict ignoredPathDict) {
            var dirInfo = new DirectoryInfo(GetAbsPath(relPath));

            if (dirInfo.Exists) {
                ClearDir(dirInfo, ignoredPathDict.GetOrAdd(relPath.ConfigPart, relPath.AppFolder),
                    out bool dirEmpty);
            }
        }

        /// <summary>
        /// Check that the string starts with at least one of the specified values.
        /// </summary>
        private bool StartsWith(string s, ICollection<string> values, StringComparison comparisonType) {
            foreach (string val in values) {
                if (s.StartsWith(val, comparisonType))
                    return true;
            }

            return false;
        }


        /// <summary>
        /// Check user
        /// </summary>
        /// <remarks>Checks username, password and role</remarks>
        public bool ValidateUser(string username, string password, out string errMsg) {
            try {
                // check the number of attempts
                if (validateUserAttemptNum > MaxValidateUserAttempts) {
                    errMsg = Localization.UseRussian
                        ? "Превышено количество попыток входа"
                        : "Number of login attempts exceeded";
                    return false;
                } else {
                    validateUserAttemptNum++;
                }

                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password)) {
                    // opening user table
                    BaseAdapter baseAdapter = new BaseAdapter();
                    var userTable = new DataTable();
                    baseAdapter.FileName = Path.Combine(Settings.Directory,
                        DirectoryBuilder.GetDirectory(ConfigParts.Base), "user.dat");
                    baseAdapter.Fill(userTable, false);

                    // search and verification of user information
                    userTable.CaseSensitive = false;
                    DataRow[] rows = userTable.Select(string.Format("Name = '{0}'", username));

                    if (rows.Length > 0) {
                        var row = rows[0];
                        if ((string) row["Password"] == password) {
                            if ((int) row["RoleID"] == BaseValues.Roles.App) {
                                validateUserAttemptNum = 0;
                                errMsg = "";
                                return true;
                            } else {
                                errMsg = Localization.UseRussian ? "Недостаточно прав" : "Insufficient rights";
                                return false;
                            }
                        }
                    }
                }

                errMsg = Localization.UseRussian
                    ? "Неверное имя пользователя или пароль"
                    : "Invalid username or password";
                return false;
            } catch (Exception ex) {
                errMsg = Localization.UseRussian ? "Ошибка при проверке пользователя" : "Error validating user";
                log.WriteException(ex, errMsg);
                return false;
            }
        }

        /// <summary>
        /// Manage the service
        /// </summary>
        public bool ControlService(ServiceApp serviceApp, ServiceCommand command) {
            try {
                string batchFileName = Path.Combine(Settings.Directory,
                    DirectoryBuilder.GetDirectory(serviceApp), GetServiceBatchFile(command));

                if (File.Exists(batchFileName)) {
                    Process.Start(new ProcessStartInfo() {
                        FileName = batchFileName,
                        UseShellExecute = false
                    });
                    return true;
                } else {
                    log.WriteError(string.Format(
                        Localization.UseRussian
                            ? "Не найден файл для управления службой {0}"
                            : "File {0} for service control not found", batchFileName));
                    return false;
                }
            } catch (Exception ex) {
                log.WriteException(ex,
                    Localization.UseRussian ? "Ошибка при управлении службой" : "Error controlling service");
                return false;
            }
        }

        /// <summary>
        /// Get service status
        /// </summary>
        public bool GetServiceStatus(ServiceApp serviceApp, out ServiceStatus status) {
            try {
                status = ServiceStatus.Undefined;
                string statusFileName = Path.Combine(Settings.Directory,
                    DirectoryBuilder.GetDirectory(serviceApp),
                    DirectoryBuilder.GetDirectory(AppFolder.Log),
                    GetServiceStatusFile(serviceApp));

                if (File.Exists(statusFileName)) {
                    string[] lines = File.ReadAllLines(statusFileName, Encoding.UTF8);

                    foreach (string line in lines) {
                        if (line.StartsWith("State", StringComparison.Ordinal) ||
                            line.StartsWith("Состояние", StringComparison.Ordinal)) {
                            int colonInd = line.IndexOf(':');

                            if (colonInd > 0) {
                                string statusStr = line.Substring(colonInd + 1).Trim();

                                if (statusStr.Equals("normal", StringComparison.OrdinalIgnoreCase) ||
                                    statusStr.Equals("норма", StringComparison.OrdinalIgnoreCase)) {
                                    status = ServiceStatus.Normal;
                                } else if (statusStr.Equals("stopped", StringComparison.OrdinalIgnoreCase) ||
                                           statusStr.Equals("остановлен", StringComparison.OrdinalIgnoreCase)) {
                                    status = ServiceStatus.Stopped;
                                } else if (statusStr.Equals("error", StringComparison.OrdinalIgnoreCase) ||
                                           statusStr.Equals("ошибка", StringComparison.OrdinalIgnoreCase)) {
                                    status = ServiceStatus.Error;
                                }
                            }
                        }
                    }
                }

                return true;
            } catch (Exception ex) {
                log.WriteException(ex,
                    Localization.UseRussian ? "Ошибка при получении статуса службы" : "Error getting service status");
                status = ServiceStatus.Undefined;
                return false;
            }
        }

        /// <summary>
        /// Get the absolute path from relative
        /// </summary>
        public string GetAbsPath(RelPath relPath) {
            return GetAbsPath(relPath.ConfigPart, relPath.AppFolder, relPath.Path);
        }

        /// <summary>
        /// Get the absolute path from relative
        /// </summary>
        public string GetAbsPath(ConfigParts configPart, AppFolder appFolder, string path) {
            return Path.Combine(Settings.Directory, DirectoryBuilder.GetDirectory(configPart, appFolder), path);
        }

        /// <summary>
        /// Get available configuration parts
        /// </summary>
        public bool GetAvailableConfig(out ConfigParts configParts) {
            try {
                configParts = ConfigParts.None;

                foreach (var configPart in AllConfigParts) {
                    if (Directory.Exists(Path.Combine(Settings.Directory, DirectoryBuilder.GetDirectory(configPart))))
                        configParts |= configPart;
                }

                return true;
            } catch (Exception ex) {
                log.WriteException(ex,
                    Localization.UseRussian
                        ? "Ошибка при получении доступных частей конфигурации"
                        : "Error getting available parts of the configuration");
                configParts = ConfigParts.None;
                return false;
            }
        }

        /// <summary>
        /// Pack configuration into archive
        /// </summary>
        public bool PackConfig(string destFileName, ConfigOptions configOptions) {
            try {
                List<RelPath> configPaths = GetConfigPaths(configOptions.ConfigParts);
                var ignoredPathDict = PrepareIgnoredPaths(configOptions.IgnoredPaths);

                using (var fileStream =
                    new FileStream(destFileName, FileMode.Create, FileAccess.Write, FileShare.Read)) {
                    using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create)) {
                        foreach (var relPath in configPaths) {
                            PackDir(zipArchive, relPath, ignoredPathDict);
                        }

                        return true;
                    }
                }
            } catch (Exception ex) {
                log.WriteException(ex,
                    Localization.UseRussian
                        ? "Ошибка при упаковке конфигурации в архив"
                        : "Error packing configuration into archive");
                return false;
            }
        }

        /// <summary>
        /// Unpack configuration archive
        /// </summary>
        public bool UnpackConfig(string srcFileName, ConfigOptions configOptions) {
            try {
                // delete existing configuration
                List<RelPath> configPaths = GetConfigPaths(configOptions.ConfigParts);
                var pathDict = PrepareIgnoredPaths(configOptions.IgnoredPaths);

                foreach (var relPath in configPaths) {
                    ClearDir(relPath, pathDict);
                }

                // definition of valid unpacking directories
                var configParts = configOptions.ConfigParts;
                var allowedEntries = new List<string>(AllConfigParts.Length);

                foreach (var configPart in AllConfigParts) {
                    if (configParts.HasFlag(configPart))
                        allowedEntries.Add(DirectoryBuilder.GetDirectory(configPart, '/'));
                }

                // unpacking new configuration
                using (var fileStream =
                    new FileStream(srcFileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read)) {
                        string instanceDir = Settings.Directory;

                        foreach (var entry in zipArchive.Entries) {
                            if (StartsWith(entry.FullName, allowedEntries, StringComparison.Ordinal)) {
                                string relPath = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                                string destFileName = instanceDir + relPath;
                                Directory.CreateDirectory(Path.GetDirectoryName(destFileName));
                                entry.ExtractToFile(destFileName, true);
                            }
                        }

                        return true;
                    }
                }
            } catch (Exception ex) {
                log.WriteException(ex,
                    Localization.UseRussian
                        ? "Ошибка при распаковке конфигурации из архива"
                        : "Error unpacking configuration from archive");
                return false;
            }
        }

        /// <summary>
        /// Directory Overview
        /// </summary>
        public bool Browse(RelPath relPath, out ICollection<string> directories, out ICollection<string> files) {
            try {
                string absPath = GetAbsPath(relPath);
                var dirInfo = new DirectoryInfo(absPath);

                // getting subdirectories
                directories = new List<string>();
                DirectoryInfo[] subdirInfoArr = dirInfo.GetDirectories("*", SearchOption.TopDirectoryOnly);

                foreach (var subdirInfo in subdirInfoArr) {
                    directories.Add(subdirInfo.Name);
                }

                // receiving files
                files = new List<string>();
                FileInfo[] fileInfoArr = dirInfo.GetFiles("*", SearchOption.TopDirectoryOnly);

                foreach (var fileInfo in fileInfoArr) {
                    files.Add(fileInfo.Name);
                }

                return true;
            } catch (Exception ex) {
                log.WriteException(ex,
                    Localization.UseRussian ? "Ошибка при обзоре директории" : "Error browsing directory");
                directories = null;
                files = null;
                return false;
            }
        }
    }
}