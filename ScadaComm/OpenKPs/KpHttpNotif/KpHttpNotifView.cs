﻿/*
 * Copyright 2017 Mikhail Shiryaev
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
 * Module   : KpHttpNotif
 * Summary  : Device library user interface
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2017
 */

using Scada.Comm.Devices.AB;
using Scada.Comm.Devices.HttpNotif.UI;
using Scada.Data.Configuration;
using Scada.Data.Tables;
using Scada.UI;

namespace Scada.Comm.Devices {
    /// <summary>
    /// Device library user interface
    /// <para>KP library user interface</para>
    /// </summary>
    public class KpHttpNotifView : KPView {
        /// <summary>
        /// KP library version
        /// </summary>
        internal const string KpVersion = "5.0.0.2";


        /// <summary>
        /// Constructor for general setting of the KP library
        /// </summary>
        public KpHttpNotifView()
            : this(0) { }

        /// <summary>
        /// Constructor for setting specific KP
        /// </summary>
        public KpHttpNotifView(int number)
            : base(number) {
            CanShowProps = true;
        }


        /// <summary>
        /// Description of library KP
        /// </summary>
        public override string KPDescr {
            get {
                return Localization.UseRussian
                    ? "Отправка уведомлений с помощью HTTP-запросов.\n\n" +
                      "Параметр командной строки:\n" +
                      "URL запроса, содержащее опциональные параметры {phone}, {email} и {text}.\n\n" +
                      "Команды ТУ:\n" +
                      "1 (бинарная) - отправка уведомления.\n\n" +
                      "Примеры текста команды:\n" +
                      "имя_группы;сообщение\n" +
                      "имя_контакта;сообщение\n" +
                      "эл_почта;сообщение"
                    : "Sending notifications via HTTP requests.\n\n" +
                      "Command line parameter:\n" +
                      "Request URL with optional parameters {phone}, {email} and {text}.\n\n" +
                      "Commands:\n" +
                      "1 (binary) - send the notification.\n\n" +
                      "Command text examples:\n" +
                      "group_name;message\n" +
                      "contact_name;message\n" +
                      "email;message";
            }
        }

        /// <summary>
        /// Get KP Library Version
        /// </summary>
        public override string Version {
            get { return KpVersion; }
        }

        /// <summary>
        /// Get prototypes of default KP channels
        /// </summary>
        public override KPCnlPrototypes DefaultCnls {
            get {
                KPCnlPrototypes prototypes = new KPCnlPrototypes();

                prototypes.CtrlCnls.Add(new CtrlCnlPrototype(
                    Localization.UseRussian ? "Отправка уведомления" : "Send notification",
                    BaseValues.CmdTypes.Binary) {CmdNum = 1});

                prototypes.InCnls.Add(new InCnlPrototype(
                    Localization.UseRussian ? "Отправлено уведомлений" : "Sent notifications",
                    BaseValues.CnlTypes.TI) {
                    Signal = 1,
                    DecDigits = 0,
                    UnitName = BaseValues.UnitNames.Pcs,
                    CtrlCnlProps = prototypes.CtrlCnls[0]
                });

                return prototypes;
            }
        }

        /// <summary>
        /// Get default poll polling parameters
        /// </summary>
        public override KPReqParams DefaultReqParams {
            get { return new KPReqParams(10000, 200); }
        }

        /// <summary>
        /// Display KP properties
        /// </summary>
        public override void ShowProps() {
            // dictionary loading
            string errMsg;
            if (!Localization.LoadDictionaries(AppDirs.LangDir, "KpHttpNotif", out errMsg))
                ScadaUiUtils.ShowError(errMsg);

            if (Number > 0)
                // KP property form display
                FrmDevProps.ShowDialog(Number, KPProps, AppDirs);
            else
                // address book mapping
                FrmAddressBook.ShowDialog(AppDirs);
        }
    }
}