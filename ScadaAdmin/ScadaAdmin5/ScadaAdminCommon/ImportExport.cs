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
 * Module   : ScadaAdminCommon
 * Summary  : Import and export configuration
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using Scada.Admin.Deployment;
using Scada.Admin.Project;
using Scada.Agent;
using Scada.Data.Tables;
using System;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.IO.Compression;

namespace Scada.Admin {
    /// <summary>
    /// Import and export configuration.
    /// <para>Import and export configuration.</para>
    /// </summary>
    public class ImportExport {
        /// <summary>
        /// Extracts the specified archive.
        /// </summary>
        private void ExtractArchive(string srcFileName, string destDir) {
            using (var fileStream =
                new FileStream(srcFileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read)) {
                    foreach (var zipEntry in zipArchive.Entries) {
                        string destFileName = Path.Combine(destDir, zipEntry.FullName);
                        Directory.CreateDirectory(Path.GetDirectoryName(destFileName));
                        zipEntry.ExtractToFile(destFileName, true);
                    }
                }
            }
        }

        /// <summary>
        /// Recursively moves the files overwriting the existing files.
        /// </summary>
        private void MergeDirectory(DirectoryInfo srcDirInfo, DirectoryInfo destDirInfo) {
            // create necessary directories
            if (!destDirInfo.Exists)
                destDirInfo.Create();

            foreach (var srcSubdirInfo in srcDirInfo.GetDirectories()) {
                var destSubdirInfo = new DirectoryInfo(
                    Path.Combine(destDirInfo.FullName, srcSubdirInfo.Name));

                MergeDirectory(srcSubdirInfo, destSubdirInfo);
            }

            // move files
            foreach (var srcFileInfo in srcDirInfo.GetFiles()) {
                string destFileName = Path.Combine(destDirInfo.FullName, srcFileInfo.Name);

                if (File.Exists(destFileName))
                    File.Delete(destFileName);

                srcFileInfo.MoveTo(destFileName);
            }
        }

        /// <summary>
        /// Moves the files overwriting the existing files.
        /// </summary>
        private void MergeDirectory(string srcDirName, string destDirName) {
            MergeDirectory(new DirectoryInfo(srcDirName), new DirectoryInfo(destDirName));
        }

        /// <summary>
        /// Imports the configuration database table.
        /// </summary>
        private void ImportBaseTable(DataTable srcTable, IBaseTable destTable) {
            // add primary keys if needed
            if (!srcTable.Columns.Contains(destTable.PrimaryKey)) {
                srcTable.Columns.Add(destTable.PrimaryKey, typeof(int));
                srcTable.BeginLoadData();
                int colInd = srcTable.Columns.Count - 1;
                var id = 1;

                foreach (DataRow row in srcTable.Rows) {
                    row[colInd] = id++;
                }

                srcTable.EndLoadData();
                srcTable.AcceptChanges();
            }

            // merge data
            destTable.Modified = true;
            var destProps = TypeDescriptor.GetProperties(destTable.ItemType);

            foreach (DataRowView srcRowView in srcTable.DefaultView) {
                object destItem = TableConverter.CreateItem(destTable.ItemType, srcRowView.Row, destProps);
                destTable.AddObject(destItem);
            }
        }

        /// <summary>
        /// Adds the directory content to the archive.
        /// </summary>
        private void PackDirectory(ZipArchive zipArchive, string srcDir, string entryPrefix, bool ignoreRegKeys) {
            var srcDirInfo = new DirectoryInfo(srcDir);
            int srcDirLen = srcDir.Length;

            foreach (var fileInfo in srcDirInfo.GetFiles("*", SearchOption.AllDirectories)) {
                if (!(fileInfo.Extension.Equals(".bak", StringComparison.OrdinalIgnoreCase) ||
                      ignoreRegKeys && (fileInfo.Name.EndsWith("_Reg.xml", StringComparison.OrdinalIgnoreCase) ||
                                        fileInfo.Name.Equals("CompCode.txt", StringComparison.OrdinalIgnoreCase)))) {
                    string entryName = entryPrefix + fileInfo.FullName.Substring(srcDirLen).Replace('\\', '/');
                    zipArchive.CreateEntryFromFile(fileInfo.FullName, entryName, CompressionLevel.Fastest);
                }
            }
        }


        /// <summary>
        /// Imports the configuration from the specified archive.
        /// </summary>
        public void ImportArchive(string srcFileName, ScadaProject project, Instance instance,
            out ConfigParts foundConfigParts) {
            if (srcFileName == null)
                throw new ArgumentNullException("srcFileName");
            if (project == null)
                throw new ArgumentNullException("project");
            if (instance == null)
                throw new ArgumentNullException("instance");

            foundConfigParts = ConfigParts.None;
            string extractDir = Path.Combine(Path.GetDirectoryName(srcFileName),
                Path.GetFileNameWithoutExtension(srcFileName));

            try {
                // extract the configuration
                ExtractArchive(srcFileName, extractDir);

                // import the configuration database
                string srcBaseDir = Path.Combine(extractDir, DirectoryBuilder.GetDirectory(ConfigParts.Base));

                if (Directory.Exists(srcBaseDir)) {
                    foundConfigParts |= ConfigParts.Base;

                    foreach (IBaseTable destTable in project.ConfigBase.AllTables) {
                        string datFileName = Path.Combine(srcBaseDir, destTable.Name.ToLowerInvariant() + ".dat");

                        if (File.Exists(datFileName)) {
                            try {
                                BaseAdapter baseAdapter = new BaseAdapter() {FileName = datFileName};
                                var srcTable = new DataTable();
                                baseAdapter.Fill(srcTable, true);
                                ImportBaseTable(srcTable, destTable);
                            } catch (Exception ex) {
                                throw new ScadaException(string.Format(
                                    AdminPhrases.ImportBaseTableError, destTable.Name), ex);
                            }
                        }
                    }
                }

                // import the interface files
                string srcInterfaceDir = Path.Combine(extractDir, DirectoryBuilder.GetDirectory(ConfigParts.Interface));

                if (Directory.Exists(srcInterfaceDir)) {
                    foundConfigParts |= ConfigParts.Interface;
                    MergeDirectory(srcInterfaceDir, project.Interface.InterfaceDir);
                }

                // import the Server settings
                if (instance.ServerApp.Enabled) {
                    string srcServerDir = Path.Combine(extractDir, DirectoryBuilder.GetDirectory(ConfigParts.Server));

                    if (Directory.Exists(srcServerDir)) {
                        foundConfigParts |= ConfigParts.Server;
                        MergeDirectory(srcServerDir, instance.ServerApp.AppDir);

                        if (!instance.ServerApp.LoadSettings(out string errMsg))
                            throw new ScadaException(errMsg);
                    }
                }

                // import the Communicator settings
                if (instance.CommApp.Enabled) {
                    string srcCommDir = Path.Combine(extractDir, DirectoryBuilder.GetDirectory(ConfigParts.Comm));

                    if (Directory.Exists(srcCommDir)) {
                        foundConfigParts |= ConfigParts.Comm;
                        MergeDirectory(srcCommDir, instance.CommApp.AppDir);

                        if (!instance.CommApp.LoadSettings(out string errMsg))
                            throw new ScadaException(errMsg);
                    }
                }

                // import the Webstation settings
                if (instance.WebApp.Enabled) {
                    string srcWebDir = Path.Combine(extractDir, DirectoryBuilder.GetDirectory(ConfigParts.Web));

                    if (Directory.Exists(srcWebDir)) {
                        foundConfigParts |= ConfigParts.Web;
                        MergeDirectory(srcWebDir, instance.WebApp.AppDir);
                    }
                }
            } catch (Exception ex) {
                throw new ScadaException(AdminPhrases.ImportArchiveError, ex);
            } finally {
                // delete the extracted files
                if (Directory.Exists(extractDir))
                    Directory.Delete(extractDir, true);
            }
        }

        /// <summary>
        /// Exports the configuration to the specified archive.
        /// </summary>
        public void ExportToArchive(string destFileName, ScadaProject project, Instance instance,
            TransferSettings transferSettings) {
            if (destFileName == null)
                throw new ArgumentNullException("destFileName");
            if (project == null)
                throw new ArgumentNullException("project");
            if (instance == null)
                throw new ArgumentNullException("instance");

            FileStream fileStream = null;
            ZipArchive zipArchive = null;

            try {
                fileStream = new FileStream(destFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
                zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create);
                bool ignoreRegKeys = transferSettings.IgnoreRegKeys;

                // add the configuration database to the archive
                if (transferSettings.IncludeBase) {
                    foreach (IBaseTable srcTable in project.ConfigBase.AllTables) {
                        string entryName = "BaseDAT/" + srcTable.Name.ToLowerInvariant() + ".dat";
                        var tableEntry = zipArchive.CreateEntry(entryName, CompressionLevel.Fastest);

                        using (var entryStream = tableEntry.Open()) {
                            // convert the table to DAT format
                            BaseAdapter baseAdapter = new BaseAdapter() {Stream = entryStream};
                            baseAdapter.Update(srcTable);
                        }
                    }
                }

                // add the interface files to the archive
                if (transferSettings.IncludeInterface) {
                    PackDirectory(zipArchive, project.Interface.InterfaceDir,
                        DirectoryBuilder.GetDirectory(ConfigParts.Interface, '/'), ignoreRegKeys);
                }

                // add the Server settings to the archive
                if (transferSettings.IncludeServer && instance.ServerApp.Enabled) {
                    PackDirectory(zipArchive, instance.ServerApp.AppDir,
                        DirectoryBuilder.GetDirectory(ConfigParts.Server, '/'), ignoreRegKeys);
                }

                // add the Communicator settings to the archive
                if (transferSettings.IncludeServer && instance.ServerApp.Enabled) {
                    PackDirectory(zipArchive, instance.CommApp.AppDir,
                        DirectoryBuilder.GetDirectory(ConfigParts.Comm, '/'), ignoreRegKeys);
                }

                // add the Webstation settings to the archive
                if (transferSettings.IncludeServer && instance.ServerApp.Enabled) {
                    PackDirectory(zipArchive, Path.Combine(instance.WebApp.AppDir, "config"),
                        DirectoryBuilder.GetDirectory(ConfigParts.Web, AppFolder.Config, '/'), ignoreRegKeys);

                    if (!transferSettings.IgnoreWebStorage) {
                        PackDirectory(zipArchive, Path.Combine(instance.WebApp.AppDir, "storage"),
                            DirectoryBuilder.GetDirectory(ConfigParts.Web, AppFolder.Storage, '/'), ignoreRegKeys);
                    }
                }
            } catch (Exception ex) {
                throw new ScadaException(AdminPhrases.ExportToArchiveError, ex);
            } finally {
                zipArchive?.Dispose();
                fileStream?.Dispose();
            }
        }
    }
}