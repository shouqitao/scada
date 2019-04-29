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
 * Summary  : The phrases used in the application
 *
 * Author   : Mikhail Shiryaev
 * Created  : 2014
 * Modified : 2018
 */

namespace ScadaAdmin {
    /// <summary>
    /// The phrases used in the application
    /// <para>Phrases used by the application</para>
    /// </summary>
    internal static class AppPhrases {
        static AppPhrases() {
            SetToDefault();
        }

        // ScadaAdmin dictionary
        public static string BaseSDFFileNotFound { get; private set; }

        public static string RefreshRequired { get; private set; }

        // ScadaAdmin.DownloadUpload dictionary
        public static string DownloadTitle { get; private set; }

        public static string ConnectionName { get; private set; }
        public static string SessionCreated { get; private set; }
        public static string UnableCreateSession { get; private set; }
        public static string LoggedOn { get; private set; }
        public static string UnableLogin { get; private set; }
        public static string ConnectAgentError { get; private set; }
        public static string DownloadDataEmpty { get; private set; }
        public static string DownloadSuccessful { get; private set; }
        public static string DownloadError { get; private set; }
        public static string UploadTitle { get; private set; }
        public static string NoConfigInSrc { get; private set; }
        public static string ConfigUploaded { get; private set; }
        public static string ServerRestarted { get; private set; }
        public static string UnableRestartServer { get; private set; }
        public static string CommRestarted { get; private set; }
        public static string UnableRestartComm { get; private set; }
        public static string UploadSuccessful { get; private set; }
        public static string UploadError { get; private set; }

        // ScadaAdmin.ImportExport dictionary
        public static string ChooseBaseTableFile { get; private set; }

        public static string ChooseArchiveFile { get; private set; }
        public static string BaseTableFileFilter { get; private set; }
        public static string ArchiveFileFilter { get; private set; }
        public static string ImportFileNotExist { get; private set; }
        public static string ImportDirNotExist { get; private set; }
        public static string ImportTitle { get; private set; }
        public static string ImportTableTitle { get; private set; }
        public static string ImportSource { get; private set; }
        public static string LoadTableError { get; private set; }
        public static string SrcTableColumns { get; private set; }
        public static string DestTableColumns { get; private set; }
        public static string NoColumns { get; private set; }
        public static string WriteDBError { get; private set; }
        public static string ImportCompleted { get; private set; }
        public static string ImportCompletedWithErr { get; private set; }
        public static string ImportTableCompleted { get; private set; }
        public static string ImportTableCompletedWithErr { get; private set; }
        public static string ImportResult { get; private set; }
        public static string ImportTableResult { get; private set; }
        public static string ImportTableErrors { get; private set; }
        public static string ImportTableError { get; private set; }
        public static string ImportAllTablesError { get; private set; }
        public static string ImportArchiveError { get; private set; }
        public static string ExportFileUndefined { get; private set; }
        public static string ExportDirUndefined { get; private set; }
        public static string ExportDirNotExists { get; private set; }
        public static string ExportCompleted { get; private set; }
        public static string ExportError { get; private set; }
        public static string DbPassCompleted { get; private set; }
        public static string DbPassError { get; private set; }

        // ScadaAdmin.FrmCloneCnls dictionary
        public static string NotReplace { get; private set; }

        public static string Undefined { get; private set; }
        public static string FillObjListError { get; private set; }
        public static string FillKPListError { get; private set; }
        public static string CloneInCnlsCompleted { get; private set; }
        public static string CloneCtrlCnlsCompleted { get; private set; }
        public static string AddedCnlsCount { get; private set; }
        public static string CloneInCnlsError { get; private set; }
        public static string CloneCtrlCnlsError { get; private set; }
        public static string CloneCnlError { get; private set; }

        // ScadaAdmin.FrmCnlMap dictionary
        public static string NoChannels { get; private set; }

        public static string InCnlsByObjTitle { get; private set; }
        public static string InCnlsByKPTitle { get; private set; }
        public static string CtrlCnlsByObjTitle { get; private set; }
        public static string CtrlCnlsByKPTitle { get; private set; }
        public static string ObjectCaptionFormat { get; private set; }
        public static string KPCaptionFormat { get; private set; }
        public static string UndefinedObject { get; private set; }
        public static string UndefinedKP { get; private set; }
        public static string CreateCnlMapError { get; private set; }

        // ScadaAdmin.FrmCreateCnls dictionary
        public static string LoadKPDllError { get; private set; }

        public static string DevCalcError { get; private set; }
        public static string DevHasNoCnls { get; private set; }
        public static string CalcCnlNumsErrors { get; private set; }
        public static string CreatedCnlsMissing { get; private set; }
        public static string CalcCnlNumsError { get; private set; }
        public static string ErrorsCount { get; private set; }
        public static string CnlError { get; private set; }
        public static string CreateCnlsTitle { get; private set; }
        public static string CheckDicts { get; private set; }
        public static string ParamNotFound { get; private set; }
        public static string UnitNotFound { get; private set; }
        public static string CmdValsNotFound { get; private set; }
        public static string CreateCnlsImpossible { get; private set; }
        public static string CreateCnlsStart { get; private set; }
        public static string InCnlNameTrancated { get; private set; }
        public static string CtrlCnlNameTrancated { get; private set; }
        public static string NumFormatNotFound { get; private set; }
        public static string TextFormatNotFound { get; private set; }
        public static string AddedInCnlsCount { get; private set; }
        public static string AddedCtrlCnlsCount { get; private set; }
        public static string CreateCnlsComplSucc { get; private set; }
        public static string CreateCnlsComplWithErr { get; private set; }
        public static string CreateCnlsError { get; private set; }
        public static string UndefinedItem { get; private set; }
        public static string DllError { get; private set; }
        public static string DllLoaded { get; private set; }
        public static string DllNotFound { get; private set; }
        public static string FillKPFilterError { get; private set; }
        public static string FillKPGridError { get; private set; }

        // ScadaAdmin.FrmImport dictionary
        public static string AllTablesItem { get; private set; }

        public static string ArchiveItem { get; private set; }

        // ScadaAdmin.FrmInCnlProps dictionary
        public static string ShowInCnlPropsError { get; private set; }

        public static string IncorrectInCnlNum { get; private set; }
        public static string IncorrectInCnlName { get; private set; }
        public static string IncorrectCnlType { get; private set; }
        public static string IncorrectSignal { get; private set; }
        public static string IncorrectCtrlCnlNum { get; private set; }
        public static string CtrlCnlNotExists { get; private set; }
        public static string IncorrectLimLowCrash { get; private set; }
        public static string IncorrectLimLow { get; private set; }
        public static string IncorrectLimHigh { get; private set; }
        public static string IncorrectLimHighCrash { get; private set; }
        public static string WriteInCnlPropsError { get; private set; }

        // ScadaAdmin.FrmLanguage dictionary
        public static string IncorrectLanguage { get; private set; }

        // ScadaAdmin.FrmMain dictionary
        public static string SelectTable { get; private set; }

        public static string SaveReqCaption { get; private set; }
        public static string SaveReqQuestion { get; private set; }
        public static string SaveReqYes { get; private set; }
        public static string SaveReqNo { get; private set; }
        public static string SaveReqCancel { get; private set; }
        public static string DbNode { get; private set; }
        public static string SystemNode { get; private set; }
        public static string DictNode { get; private set; }
        public static string ConnectError { get; private set; }
        public static string DisconnectError { get; private set; }
        public static string UndefObj { get; private set; }
        public static string UndefKP { get; private set; }
        public static string CnlGroupError { get; private set; }
        public static string BackupCompleted { get; private set; }
        public static string BackupError { get; private set; }
        public static string CompactCompleted { get; private set; }
        public static string CompactError { get; private set; }
        public static string ConnectionUndefined { get; private set; }
        public static string ServiceRestartError { get; private set; }
        public static string LanguageChanged { get; private set; }

        // ScadaAdmin.FrmReplace dictionary
        public static string ValueNotFound { get; private set; }

        public static string FindCompleted { get; private set; }
        public static string ReplaceCount { get; private set; }

        // ScadaAdmin.FrmSettings dictionary
        public static string ChooseBaseSDFFile { get; private set; }

        public static string BaseSDFFileFilter { get; private set; }
        public static string ChooseBackupDir { get; private set; }
        public static string ChooseCommDir { get; private set; }
        public static string BaseSDFFileNotExists { get; private set; }
        public static string BackupDirNotExists { get; private set; }
        public static string CommDirNotExists { get; private set; }

        // ScadaAdmin.FrmTable dictionary
        public static string RefreshDataError { get; private set; }

        public static string DeleteRowConfirm { get; private set; }
        public static string DeleteRowsConfirm { get; private set; }
        public static string ClearTableConfirm { get; private set; }

        // ScadaAdmin.ServersSettings dictionary
        public static string LoadServersSettingsError { get; private set; }

        public static string SaveServersSettingsError { get; private set; }

        // ScadaAdmin.Tables dictionary
        public static string UpdateDataError { get; private set; }

        public static string FillSchemaError { get; private set; }
        public static string DataRequired { get; private set; }
        public static string UniqueRequired { get; private set; }
        public static string UnableDeleteRow { get; private set; }
        public static string UnableAddRow { get; private set; }
        public static string TranslateError { get; private set; }
        public static string GetTableError { get; private set; }
        public static string GetTableByObjError { get; private set; }
        public static string GetTableByKPError { get; private set; }
        public static string GetCtrlCnlNameError { get; private set; }
        public static string GetInCnlNumsError { get; private set; }
        public static string GetCtrlCnlNumsError { get; private set; }

        // ScadaAdmin.Remote dictionary
        public static string ChooseConfigDir { get; private set; }

        public static string ConfigDirRequired { get; private set; }
        public static string ConfigArcRequired { get; private set; }

        // Vocabulary ScadaAdmin.Remote.CtrlServerConn
        public static string DeleteConnConfirm { get; private set; }

        // Vocabulary ScadaAdmin.Remote.FrmConnSettings
        public static string EmptyFieldsNotAllowed { get; private set; }

        public static string ConnNameDuplicated { get; private set; }
        public static string IncorrectSecretKey { get; private set; }

        // Vocabulary ScadaAdmin.Remote.FrmServerStatus
        public static string UndefinedSvcStatus { get; private set; }

        public static string NormalSvcStatus { get; private set; }
        public static string StoppedSvcStatus { get; private set; }
        public static string ErrorSvcStatus { get; private set; }

        private static void SetToDefault() {
            BaseSDFFileNotFound = "SDF configuration database file {0} not found.";
            RefreshRequired = "\r\nUpdate open tables to display changes.";

            DownloadTitle = "{0} Configuration download";
            ConnectionName = "Compound : {0}";
            SessionCreated = "Session created {0}";
            UnableCreateSession = "Could not create session";
            LoggedOn = "Login is done";
            UnableLogin = "Login failed - {0}";
            ConnectAgentError = "Error connecting to Agent";
            DownloadDataEmpty = "No data to download";
            DownloadSuccessful = "Download completed successfully in {0} s.";
            DownloadError = "Error loading configuration";
            UploadTitle = "{0} Configuration transfer";
            NoConfigInSrc = "The configuration is not in the specified source.";
            ConfigUploaded = "Configuration transferred";
            ServerRestarted = "Server Service Restarted";
            UnableRestartServer = "Failed to restart the Server service.";
            CommRestarted = "Communicator service restarted";
            UnableRestartComm = "Failed to restart the Communicator service.";
            UploadSuccessful = "Transfer completed successfully in {0} s.\r\n" +
                               "Check the operation of the remote server.";
            UploadError = "Error while transferring the configuration";

            ChooseBaseTableFile = "Select the configuration database table file";
            ChooseArchiveFile = "Select a configuration archive file";
            BaseTableFileFilter = "Configuration base tables (* .dat) | * .dat | All files (*. *) | *. * ";
            ArchiveFileFilter = "Configuration archive (* .zip) | * .zip | All files (*. *) | *. *";
            ImportFileNotExist = "The file being imported does not exist.";
            ImportDirNotExist = "The imported directory does not exist.";
            ImportTitle = "Import configuration database";
            ImportTableTitle = "Import configuration database table \"{0}\"";
            ImportSource = "Source file or directory : ";
            LoadTableError = "Error loading imported table";
            SrcTableColumns = "Source table fields";
            DestTableColumns = "Fields in the import table";
            NoColumns = "Absent";
            WriteDBError = "Error when writing information to the database";
            ImportCompleted = "Import completed successfully.\r\n" +
                              "Added/updated entries: {0}.";
            ImportCompletedWithErr = "Import completed with errors.\r\n" +
                                     "Added/updated entries: {0}. Bugs: {1}.";
            ImportTableCompleted =
                "Import of configuration database table completed successfully.\r\n" +
                "Added/updated entries: {0}.";
            ImportTableCompletedWithErr = "Import of configuration database table completed with errors.\r\n" +
                                          "Added/updated entries: {0}. Bugs: {1}.";
            ImportResult = "Import result";
            ImportTableResult = "The result of importing a table";
            ImportTableErrors = "Table import errors";
            ImportTableError = "Error while importing configuration database table";
            ImportAllTablesError = "Error when importing all configuration database tables";
            ImportArchiveError = "Error when importing configuration database from archive";
            ExportFileUndefined = "Export file not defined.";
            ExportDirUndefined = "Export directory not defined.";
            ExportDirNotExists = "Export directory does not exist.";
            ExportCompleted = "Export configuration database table completed successfully.";
            ExportError = "Error while exporting the configuration database table";
            DbPassCompleted = "Transfer of the configuration base to SCADA-Server completed successfully.\r\n" +
                              "Changes will take effect after restarting the SCADA-Server service..";
            DbPassError = "Error while transferring the configuration base to SCADA-Server";

            NotReplace = "<Do not replace>";
            Undefined = "<Not determined>";
            FillObjListError = "Error filling the list of objects";
            FillKPListError = "Error when filling the list of KP";
            CloneInCnlsCompleted = "Input channel cloning completed successfully.";
            CloneCtrlCnlsCompleted = "Clone control channels successfully completed.";
            AddedCnlsCount = "Number of channels added: {0}.";
            CloneInCnlsError = "Error when cloning input channels";
            CloneCtrlCnlsError = "Error when cloning control channels";
            CloneCnlError = "Error when cloning a channel {0}";

            NoChannels = "Channels are missing";
            InCnlsByObjTitle = "Input channels by object";
            InCnlsByKPTitle = "KP Input Channels";
            CtrlCnlsByObjTitle = "Object control channels";
            CtrlCnlsByKPTitle = "KP control channels";
            ObjectCaptionFormat = "An object {0} \"{1}\"";
            KPCaptionFormat = "КП {0} \"{1}\"";
            UndefinedObject = "<Object not defined>";
            UndefinedKP = "<KP not defined>";
            CreateCnlMapError = "Error creating channel map";

            LoadKPDllError = "Error loading KP libraries";
            DevCalcError = "Mistake";
            DevHasNoCnls = "Not";
            CalcCnlNumsErrors = "Error calculating channel numbers.\r\n" +
                                "Errors are shown in the KP table.";
            CreatedCnlsMissing = "No channels created.";
            CalcCnlNumsError = "Error calculating channel numbers";
            ErrorsCount = "Number of mistakes: {0}.";
            CnlError = "Channel {0}: {1}";
            CreateCnlsTitle = "Creating channels";
            CheckDicts = "Check references.";
            ParamNotFound = "Value not found \"{0}\".";
            UnitNotFound = "No dimension found \"{0}\".";
            CmdValsNotFound = "Command values not found \"{0}\".";
            CreateCnlsImpossible = "Channel creation is not possible.";
            CreateCnlsStart = "Creating channels.";
            InCnlNameTrancated = "Input channel name {0} has been cut off.";
            CtrlCnlNameTrancated = "Control channel name {0} has been truncated.";
            NumFormatNotFound = "The input channel format {0} was not found. " +
                                "Format description: numeric, the number of decimal places is {1}.";
            TextFormatNotFound = "The input channel format {0} was not found. Format description: text.";
            AddedInCnlsCount = "Added input channels: {0}.";
            AddedCtrlCnlsCount = "Added control channels: {0}.";
            CreateCnlsComplSucc = "Channel creation completed successfully.";
            CreateCnlsComplWithErr = "Channel creation completed with errors.";
            CreateCnlsError = "Error creating channels";
            UndefinedItem = "<Not determined>";
            DllError = "Mistake";
            DllLoaded = "Loaded";
            DllNotFound = "Not found";
            FillKPFilterError = "Error filling filter KP";
            FillKPGridError = "Error when filling the selection table KP";

            AllTablesItem = "All tables";
            ArchiveItem = "Tables from the archive";

            ShowInCnlPropsError = "Error displaying input channel properties";
            IncorrectInCnlNum = "Invalid input channel number value:";
            IncorrectInCnlName = "Invalid input channel name value:";
            IncorrectCnlType = "Incorrect channel type value:";
            IncorrectSignal = "Incorrect signal value:";
            IncorrectCtrlCnlNum = "Incorrect value of control channel number:";
            CtrlCnlNotExists = "Control channel {0} does not exist.";
            IncorrectLimLowCrash = "Incorrect value of the lower alarm limit:";
            IncorrectLimLow = "Incorrect lower bound value:";
            IncorrectLimHigh = "Incorrect upper bound value:";
            IncorrectLimHighCrash = "Incorrect upper alarm limit value:";
            WriteInCnlPropsError = "Error writing input channel properties";

            IncorrectLanguage = "Invalid language.";

            SelectTable = "Select in explorer\r\n" +
                          "table for editing";
            SaveReqCaption = "Saving changes";
            SaveReqQuestion = "Save changes?";
            SaveReqYes = "&Yes";
            SaveReqNo = "&Not";
            SaveReqCancel = "Cancel";
            DbNode = "Configuration database";
            SystemNode = "System";
            DictNode = "Directories";
            ConnectError = "Error connecting to the database";
            DisconnectError = "Error when disconnecting from the database";
            UndefObj = "<Object not defined.>";
            UndefKP = "<CP not set.>";
            CnlGroupError = "Error when grouping channels";
            BackupCompleted = "Configuration base backup completed successfully.\r\n" +
                              "The data is stored in a file.\r\n{0}";
            BackupError = "Error while backing up the configuration database";
            CompactCompleted = "Packing configuration database completed successfully.";
            CompactError = "Error while packing configuration base";
            ConnectionUndefined = "Configuration base not defined.";
            ServiceRestartError = "Error restarting service";
            LanguageChanged = "The language change will take effect after the program is restarted.";

            ValueNotFound = "Value not found.";
            FindCompleted = "Search complete.";
            ReplaceCount = "Replaced by: {0}.";

            ChooseBaseSDFFile = "Select the database file of the configuration in the SDF format";
            BaseSDFFileFilter = "Configuration Database | * .sdf | All Files | *. *";
            ChooseBackupDir = "Select the configuration base backup directory";
            ChooseCommDir = "Select the SCADA-Communicator directory";
            BaseSDFFileNotExists = "SDF configuration database file does not exist.";
            BackupDirNotExists = "The backup directory of the configuration database does not exist.";
            CommDirNotExists = "Directory SCADA-Communicator does not exist.";

            RefreshDataError = "Error updating table data";
            DeleteRowConfirm = "Are you sure you want to delete the line?";
            DeleteRowsConfirm = "Are you sure you want to delete the lines?";
            ClearTableConfirm = "Are you sure you want to clear the table?";

            LoadServersSettingsError = "Error loading settings for interaction with remote servers";
            SaveServersSettingsError = "Error while saving settings of interaction with remote servers";

            UpdateDataError = "Error saving changes to the table in the database";
            FillSchemaError = "Error getting table data schema";
            DataRequired = "Column \"{0}\" cannot contain empty values.";
            UniqueRequired = "For column \"{0}\" no duplicate values allowed.";
            UnableDeleteRow = "Deletion or modification of the string is impossible, " +
                              "since data from the table refers to it \"{0}\".";
            UnableAddRow = "Adding or changing a string is not possible, because no data exists for column \"{0}\".";
            TranslateError = "Error translating message";
            GetTableError = "Error getting table \"{0}\"";
            GetTableByObjError = "Error getting table \"{0}\" by object number";
            GetTableByKPError = "Error getting table \"{0}\" by the number of KP";
            GetCtrlCnlNameError = "Error getting control channel name";
            GetInCnlNumsError = "Error getting channel numbers";
            GetCtrlCnlNumsError = "Error getting control channel numbers";

            ChooseConfigDir = "Select a configuration directory";
            ConfigDirRequired = "Specify the configuration directory.";
            ConfigArcRequired = "Specify the name of the configuration archive file..";

            DeleteConnConfirm = "Are you sure you want to delete the connection?";

            EmptyFieldsNotAllowed = "Empty field values are not allowed.";
            ConnNameDuplicated = "A connection with this name already exists..";
            IncorrectSecretKey = "Invalid secret key.";

            UndefinedSvcStatus = "Not determined";
            NormalSvcStatus = "Norm";
            StoppedSvcStatus = "Stopped";
            ErrorSvcStatus = "Mistake";
        }

        public static void Init() {
            Localization.Dict dict;
            if (Localization.Dictionaries.TryGetValue("ScadaAdmin", out dict)) {
                BaseSDFFileNotFound = dict.GetPhrase("BaseSDFFileNotFound", BaseSDFFileNotFound);
                RefreshRequired = dict.GetPhrase("RefreshRequired", RefreshRequired);
            }

            if (Localization.Dictionaries.TryGetValue("ScadaAdmin.DownloadUpload", out dict)) {
                DownloadTitle = dict.GetPhrase("DownloadTitle", DownloadTitle);
                ConnectionName = dict.GetPhrase("ConnectionName", ConnectionName);
                SessionCreated = dict.GetPhrase("SessionCreated", SessionCreated);
                UnableCreateSession = dict.GetPhrase("UnableCreateSession", UnableCreateSession);
                LoggedOn = dict.GetPhrase("LoggedOn", LoggedOn);
                UnableLogin = dict.GetPhrase("UnableLogin", UnableLogin);
                ConnectAgentError = dict.GetPhrase("ConnectAgentError", ConnectAgentError);
                DownloadDataEmpty = dict.GetPhrase("DownloadDataEmpty", DownloadDataEmpty);
                DownloadSuccessful = dict.GetPhrase("DownloadSuccessful", DownloadSuccessful);
                DownloadError = dict.GetPhrase("DownloadError", DownloadError);
                UploadTitle = dict.GetPhrase("UploadTitle", UploadTitle);
                NoConfigInSrc = dict.GetPhrase("NoConfigInSrc", NoConfigInSrc);
                ConfigUploaded = dict.GetPhrase("ConfigUploaded", ConfigUploaded);
                ServerRestarted = dict.GetPhrase("ServerRestarted", ServerRestarted);
                UnableRestartServer = dict.GetPhrase("UnableRestartServer", UnableRestartServer);
                CommRestarted = dict.GetPhrase("CommRestarted", CommRestarted);
                UnableRestartComm = dict.GetPhrase("UnableRestartComm", UnableRestartComm);
                UploadSuccessful = dict.GetPhrase("UploadSuccessful", UploadSuccessful);
                UploadError = dict.GetPhrase("UploadError", UploadError);
            }

            if (Localization.Dictionaries.TryGetValue("ScadaAdmin.ImportExport", out dict)) {
                ChooseBaseTableFile = dict.GetPhrase("ChooseBaseTableFile", ChooseBaseTableFile);
                ChooseArchiveFile = dict.GetPhrase("ChooseArchiveFile", ChooseArchiveFile);
                BaseTableFileFilter = dict.GetPhrase("BaseTableFileFilter", BaseTableFileFilter);
                ArchiveFileFilter = dict.GetPhrase("ArchiveFileFilter", ArchiveFileFilter);
                ImportFileNotExist = dict.GetPhrase("ImportFileNotExist", ImportFileNotExist);
                ImportDirNotExist = dict.GetPhrase("ImportDirNotExist", ImportDirNotExist);
                ImportTitle = dict.GetPhrase("ImportTitle", ImportTitle);
                ImportTableTitle = dict.GetPhrase("ImportTableTitle", ImportTableTitle);
                ImportSource = dict.GetPhrase("ImportSource", ImportSource);
                LoadTableError = dict.GetPhrase("LoadTableError", LoadTableError);
                SrcTableColumns = dict.GetPhrase("SrcTableColumns", SrcTableColumns);
                DestTableColumns = dict.GetPhrase("DestTableColumns", DestTableColumns);
                NoColumns = dict.GetPhrase("NoColumns", NoColumns);
                WriteDBError = dict.GetPhrase("WriteDBError", WriteDBError);
                ImportCompleted = dict.GetPhrase("ImportCompleted", ImportCompleted);
                ImportCompletedWithErr = dict.GetPhrase("ImportCompletedWithErr", ImportCompletedWithErr);
                ImportTableCompleted = dict.GetPhrase("ImportTableCompleted", ImportTableCompleted);
                ImportTableCompletedWithErr =
                    dict.GetPhrase("ImportTableCompletedWithErr", ImportTableCompletedWithErr);
                ImportResult = dict.GetPhrase("ImportResult", ImportResult);
                ImportTableResult = dict.GetPhrase("ImportTableResult", ImportTableResult);
                ImportTableErrors = dict.GetPhrase("ImportTableErrors", ImportTableErrors);
                ImportTableError = dict.GetPhrase("ImportError", ImportTableError);
                ImportAllTablesError = dict.GetPhrase("ImportAllTablesError", ImportAllTablesError);
                ImportArchiveError = dict.GetPhrase("ImportArchiveError", ImportArchiveError);
                ExportFileUndefined = dict.GetPhrase("ExportFileUndefined", ExportFileUndefined);
                ExportDirUndefined = dict.GetPhrase("ExportDirUndefined", ExportDirUndefined);
                ExportDirNotExists = dict.GetPhrase("ExportDirNotExists", ExportDirNotExists);
                ExportCompleted = dict.GetPhrase("ExportCompleted", ExportCompleted);
                ExportError = dict.GetPhrase("ExportError", ExportError);
                DbPassCompleted = dict.GetPhrase("DbPassCompleted", DbPassCompleted);
                DbPassError = dict.GetPhrase("DbPassError", DbPassError);
            }

            if (Localization.Dictionaries.TryGetValue("ScadaAdmin.FrmCloneCnls", out dict)) {
                NotReplace = dict.GetPhrase("NotReplace", NotReplace);
                Undefined = dict.GetPhrase("Undefined", Undefined);
                FillObjListError = dict.GetPhrase("FillObjListError", FillObjListError);
                FillKPListError = dict.GetPhrase("FillKPListError", FillKPListError);
                CloneInCnlsCompleted = dict.GetPhrase("CloneInCnlsCompleted", CloneInCnlsCompleted);
                CloneCtrlCnlsCompleted = dict.GetPhrase("CloneCtrlCnlsCompleted", CloneCtrlCnlsCompleted);
                AddedCnlsCount = dict.GetPhrase("AddedCnlsCount", AddedCnlsCount);
                CloneInCnlsError = dict.GetPhrase("CloneInCnlsError", CloneInCnlsError);
                CloneCtrlCnlsError = dict.GetPhrase("CloneCtrlCnlsError", CloneCtrlCnlsError);
                CloneCnlError = dict.GetPhrase("CloneCnlError", CloneCnlError);
            }

            if (Localization.Dictionaries.TryGetValue("ScadaAdmin.FrmCnlMap", out dict)) {
                NoChannels = dict.GetPhrase("NoChannels", NoChannels);
                InCnlsByObjTitle = dict.GetPhrase("InCnlsByObjTitle", InCnlsByObjTitle);
                InCnlsByKPTitle = dict.GetPhrase("InCnlsByKPTitle", InCnlsByKPTitle);
                CtrlCnlsByObjTitle = dict.GetPhrase("CtrlCnlsByObjTitle", CtrlCnlsByObjTitle);
                CtrlCnlsByKPTitle = dict.GetPhrase("CtrlCnlsByKPTitle", CtrlCnlsByKPTitle);
                ObjectCaptionFormat = dict.GetPhrase("ObjectCaptionFormat", ObjectCaptionFormat);
                KPCaptionFormat = dict.GetPhrase("KPCaptionFormat", KPCaptionFormat);
                UndefinedObject = dict.GetPhrase("UndefinedObject", UndefinedObject);
                UndefinedKP = dict.GetPhrase("UndefinedKP", UndefinedKP);
                CreateCnlMapError = dict.GetPhrase("CreateCnlMapError", CreateCnlMapError);
            }

            if (Localization.Dictionaries.TryGetValue("ScadaAdmin.FrmCreateCnls", out dict)) {
                LoadKPDllError = dict.GetPhrase("LoadKPDllError", LoadKPDllError);
                DevCalcError = dict.GetPhrase("DevCalcError", DevCalcError);
                DevHasNoCnls = dict.GetPhrase("DevHasNoCnls", DevHasNoCnls);
                CalcCnlNumsErrors = dict.GetPhrase("CalcCnlNumsErrors", CalcCnlNumsErrors);
                CreatedCnlsMissing = dict.GetPhrase("CreatedCnlsMissing", CreatedCnlsMissing);
                CalcCnlNumsError = dict.GetPhrase("CalcCnlNumsError", CalcCnlNumsError);
                ErrorsCount = dict.GetPhrase("ErrorsCount", ErrorsCount);
                CnlError = dict.GetPhrase("CnlError", CnlError);
                CreateCnlsTitle = dict.GetPhrase("CreateCnlsTitle", CreateCnlsTitle);
                CheckDicts = dict.GetPhrase("CheckDicts", CheckDicts);
                ParamNotFound = dict.GetPhrase("ParamNotFound", ParamNotFound);
                UnitNotFound = dict.GetPhrase("UnitNotFound", UnitNotFound);
                CmdValsNotFound = dict.GetPhrase("CmdValsNotFound", CmdValsNotFound);
                CreateCnlsImpossible = dict.GetPhrase("CreateCnlsImpossible", CreateCnlsImpossible);
                CreateCnlsStart = dict.GetPhrase("CreateCnlsStart", CreateCnlsStart);
                InCnlNameTrancated = dict.GetPhrase("InCnlNameTrancated", InCnlNameTrancated);
                CtrlCnlNameTrancated = dict.GetPhrase("CtrlCnlNameTrancated", CtrlCnlNameTrancated);
                NumFormatNotFound = dict.GetPhrase("NumFormatNotFound", NumFormatNotFound);
                TextFormatNotFound = dict.GetPhrase("TextFormatNotFound", TextFormatNotFound);
                AddedInCnlsCount = dict.GetPhrase("AddedInCnlsCount", AddedInCnlsCount);
                AddedCtrlCnlsCount = dict.GetPhrase("AddedCtrlCnlsCount", AddedCtrlCnlsCount);
                CreateCnlsComplSucc = dict.GetPhrase("CreateCnlsComplSucc", CreateCnlsComplSucc);
                CreateCnlsComplWithErr = dict.GetPhrase("CreateCnlsComplWithErr", CreateCnlsComplWithErr);
                CreateCnlsError = dict.GetPhrase("CreateCnlsError", CreateCnlsError);
                UndefinedItem = dict.GetPhrase("UndefinedItem", UndefinedItem);
                DllError = dict.GetPhrase("DllError", DllError);
                DllLoaded = dict.GetPhrase("DllLoaded", DllLoaded);
                DllNotFound = dict.GetPhrase("DllNotFound", DllNotFound);
                FillKPFilterError = dict.GetPhrase("FillKPFilterError", FillKPFilterError);
                FillKPGridError = dict.GetPhrase("FillKPGridError", FillKPGridError);
            }

            if (Localization.Dictionaries.TryGetValue("ScadaAdmin.FrmImport", out dict)) {
                AllTablesItem = dict.GetPhrase("AllTablesItem", AllTablesItem);
                ArchiveItem = dict.GetPhrase("ArchiveItem", ArchiveItem);
            }

            if (Localization.Dictionaries.TryGetValue("ScadaAdmin.FrmInCnlProps", out dict)) {
                ShowInCnlPropsError = dict.GetPhrase("ShowInCnlPropsError", ShowInCnlPropsError);
                IncorrectInCnlNum = dict.GetPhrase("IncorrectInCnlNum", IncorrectInCnlNum);
                IncorrectInCnlName = dict.GetPhrase("IncorrectInCnlName", IncorrectInCnlName);
                IncorrectCnlType = dict.GetPhrase("IncorrectCnlType", IncorrectCnlType);
                IncorrectSignal = dict.GetPhrase("IncorrectSignal", IncorrectSignal);
                IncorrectCtrlCnlNum = dict.GetPhrase("IncorrectCtrlCnlNum", IncorrectCtrlCnlNum);
                CtrlCnlNotExists = dict.GetPhrase("CtrlCnlNotExists", CtrlCnlNotExists);
                IncorrectLimLowCrash = dict.GetPhrase("IncorrectLimLowCrash", IncorrectLimLowCrash);
                IncorrectLimLow = dict.GetPhrase("IncorrectLimLow", IncorrectLimLow);
                IncorrectLimHigh = dict.GetPhrase("IncorrectLimHigh", IncorrectLimHigh);
                IncorrectLimHighCrash = dict.GetPhrase("IncorrectLimHighCrash", IncorrectLimHighCrash);
                WriteInCnlPropsError = dict.GetPhrase("WriteInCnlPropsError", WriteInCnlPropsError);
            }

            if (Localization.Dictionaries.TryGetValue("ScadaAdmin.FrmLanguage", out dict)) {
                IncorrectLanguage = dict.GetPhrase("IncorrectLanguage", IncorrectLanguage);
            }

            if (Localization.Dictionaries.TryGetValue("ScadaAdmin.FrmMain", out dict)) {
                SelectTable = dict.GetPhrase("SelectTable", SelectTable);
                SaveReqCaption = dict.GetPhrase("SaveReqCaption", SaveReqCaption);
                SaveReqQuestion = dict.GetPhrase("SaveReqQuestion", SaveReqQuestion);
                SaveReqYes = dict.GetPhrase("SaveReqYes", SaveReqYes);
                SaveReqNo = dict.GetPhrase("SaveReqNo", SaveReqNo);
                SaveReqCancel = dict.GetPhrase("SaveReqCancel", SaveReqCancel);
                DbNode = dict.GetPhrase("DbNode", DbNode);
                SystemNode = dict.GetPhrase("SystemNode", SystemNode);
                DictNode = dict.GetPhrase("DictNode", DictNode);
                ConnectError = dict.GetPhrase("ConnectError", ConnectError);
                DisconnectError = dict.GetPhrase("DisconnectError", DisconnectError);
                UndefObj = dict.GetPhrase("UndefObj", UndefObj);
                UndefKP = dict.GetPhrase("UndefKP", UndefKP);
                CnlGroupError = dict.GetPhrase("CnlGroupError", CnlGroupError);
                BackupCompleted = dict.GetPhrase("BackupCompleted", BackupCompleted);
                BackupError = dict.GetPhrase("BackupError", BackupError);
                CompactCompleted = dict.GetPhrase("CompactCompleted", CompactCompleted);
                CompactError = dict.GetPhrase("CompactError", CompactError);
                ConnectionUndefined = dict.GetPhrase("ConnectionUndefined", ConnectionUndefined);
                ServiceRestartError = dict.GetPhrase("ServiceRestartError", ServiceRestartError);
                LanguageChanged = dict.GetPhrase("LanguageChanged", LanguageChanged);
            }

            if (Localization.Dictionaries.TryGetValue("ScadaAdmin.FrmReplace", out dict)) {
                ValueNotFound = dict.GetPhrase("ValueNotFound", ValueNotFound);
                FindCompleted = dict.GetPhrase("FindCompleted", FindCompleted);
                ReplaceCount = dict.GetPhrase("ReplaceCount", ReplaceCount);
            }

            if (Localization.Dictionaries.TryGetValue("ScadaAdmin.FrmSettings", out dict)) {
                ChooseBaseSDFFile = dict.GetPhrase("ChooseBaseSDFFile", ChooseBaseSDFFile);
                BaseSDFFileFilter = dict.GetPhrase("BaseSDFFileFilter", BaseSDFFileFilter);
                ChooseBackupDir = dict.GetPhrase("ChooseBackupDir", ChooseBackupDir);
                ChooseCommDir = dict.GetPhrase("ChooseCommDir", ChooseCommDir);
                BaseSDFFileNotExists = dict.GetPhrase("BaseSDFFileNotExists", BaseSDFFileNotExists);
                BackupDirNotExists = dict.GetPhrase("BackupDirNotExists", BackupDirNotExists);
                CommDirNotExists = dict.GetPhrase("CommDirNotExists", CommDirNotExists);
            }

            if (Localization.Dictionaries.TryGetValue("ScadaAdmin.FrmTable", out dict)) {
                RefreshDataError = dict.GetPhrase("RefreshDataError", RefreshDataError);
                DeleteRowConfirm = dict.GetPhrase("DeleteRowConfirm", DeleteRowConfirm);
                DeleteRowsConfirm = dict.GetPhrase("DeleteRowsConfirm", DeleteRowsConfirm);
                ClearTableConfirm = dict.GetPhrase("ClearTableConfirm", ClearTableConfirm);
            }

            if (Localization.Dictionaries.TryGetValue("ScadaAdmin.ServersSettings", out dict)) {
                LoadServersSettingsError = dict.GetPhrase("LoadServersSettingsError", LoadServersSettingsError);
                SaveServersSettingsError = dict.GetPhrase("SaveServersSettingsError", SaveServersSettingsError);
            }

            if (Localization.Dictionaries.TryGetValue("ScadaAdmin.Tables", out dict)) {
                UpdateDataError = dict.GetPhrase("UpdateDataError", UpdateDataError);
                FillSchemaError = dict.GetPhrase("FillSchemaError", FillSchemaError);
                DataRequired = dict.GetPhrase("DataRequired", DataRequired);
                UniqueRequired = dict.GetPhrase("UniqueRequired", UniqueRequired);
                UnableDeleteRow = dict.GetPhrase("UnableDeleteRow", UnableDeleteRow);
                UnableAddRow = dict.GetPhrase("UnableAddRow", UnableAddRow);
                TranslateError = dict.GetPhrase("TranslateError", TranslateError);
                GetTableError = dict.GetPhrase("GetTableError", GetTableError);
                GetTableByObjError = dict.GetPhrase("GetTableByObjError", GetTableByObjError);
                GetTableByKPError = dict.GetPhrase("GetTableByKPError", GetTableByKPError);
                GetCtrlCnlNameError = dict.GetPhrase("GetCtrlCnlNameError", GetCtrlCnlNameError);
                GetInCnlNumsError = dict.GetPhrase("GetInCnlNumsError", GetInCnlNumsError);
                GetCtrlCnlNumsError = dict.GetPhrase("GetCtrlCnlNumsError", GetCtrlCnlNumsError);
            }

            if (Localization.Dictionaries.TryGetValue("ScadaAdmin.Remote", out dict)) {
                ChooseConfigDir = dict.GetPhrase("ChooseConfigDir", ChooseConfigDir);
                ConfigDirRequired = dict.GetPhrase("ConfigDirRequired", ConfigDirRequired);
                ConfigArcRequired = dict.GetPhrase("ConfigArcRequired", ConfigArcRequired);
            }

            if (Localization.Dictionaries.TryGetValue("ScadaAdmin.Remote.CtrlServerConn", out dict)) {
                DeleteConnConfirm = dict.GetPhrase("DeleteConnConfirm", DeleteConnConfirm);
            }

            if (Localization.Dictionaries.TryGetValue("ScadaAdmin.Remote.FrmConnSettings", out dict)) {
                EmptyFieldsNotAllowed = dict.GetPhrase("EmptyFieldsNotAllowed", EmptyFieldsNotAllowed);
                ConnNameDuplicated = dict.GetPhrase("ConnNameDuplicated", ConnNameDuplicated);
                IncorrectSecretKey = dict.GetPhrase("IncorrectSecretKey", IncorrectSecretKey);
            }

            if (Localization.Dictionaries.TryGetValue("ScadaAdmin.Remote.FrmServerStatus", out dict)) {
                UndefinedSvcStatus = dict.GetPhrase("UndefinedSvcStatus", UndefinedSvcStatus);
                NormalSvcStatus = dict.GetPhrase("NormalSvcStatus", NormalSvcStatus);
                StoppedSvcStatus = dict.GetPhrase("StoppedSvcStatus", StoppedSvcStatus);
                ErrorSvcStatus = dict.GetPhrase("ErrorSvcStatus", ErrorSvcStatus);
            }
        }
    }
}