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
 * Module   : ModDBExport
 * Summary  : Server module user interface
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2015
 * Modified : 2018
 */

using Scada.Server.Modules.DBExport;

namespace Scada.Server.Modules {
    /// <summary>
    /// Server module user interface
    /// <para>Server Module User Interface</para>
    /// </summary>
    public class ModDBExportView : ModView {
        /// <summary>
        /// Module version
        /// </summary>
        internal const string ModVersion = "5.0.0.2";


        /// <summary>
        /// Constructor
        /// </summary>
        public ModDBExportView() {
            CanShowProps = true;
        }

        /// <summary>
        /// Get module description
        /// </summary>
        public override string Descr {
            get {
                return Localization.UseRussian
                    ? "Экспорт данных в БД в реальном времени."
                    : "Real time data export to DB.";
            }
        }

        /// <summary>
        /// Get the module version
        /// </summary>
        public override string Version {
            get { return ModVersion; }
        }


        /// <summary>
        /// Display module properties
        /// </summary>
        public override void ShowProps() {
            FrmDBExportConfig.ShowDialog(AppDirs, ServerComm);
        }
    }
}